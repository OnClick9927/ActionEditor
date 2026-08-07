using ActionAttribute;
using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [AssetFileExtension("bt.bytes")]
    [System.Serializable, Name("行为树", "保存节点图、黑板和信号量配置，并在准备运行后负责更新、中断、事件分发及完整整数状态快照的收集与恢复。")]
    public abstract class BTTree : GraphAsset
    {
        [System.Serializable]
        public class Semaphore
        {
            [Name("名称", "信号量的人类可读名称，供装饰节点选择和编辑器展示；运行时实际按稳定列表索引访问。")]
            public string name;
            [Name("最大数量", "同一时刻允许成功占用该信号量的最大分支数，必须大于零；当前占用数会包含在状态快照中。")]
            public int max = 1;
        }
        public static event Action<BTTree> onInstanceChanged;
        private static BTTree _instance;
        public static BTTree instance
        {
            get { return _instance; }
            private set
            {
                if (ReferenceEquals(_instance, value)) return;
                _instance = value;
                onInstanceChanged?.Invoke(value);
            }

        }
        public void SetAsInstance() => instance = this;
        public static void ClearInstance() => instance = null;
        protected abstract Blackboard blackboard { get; }
        public Blackboard Blackboard => _parent == null ? blackboard : _parent.blackboard;
        [Name("子树?", "开启后该资源只能由同类型父树通过子树节点加载，并共享父树黑板；关闭时它作为可独立运行的主树初始化运行容器。")]
        public bool IsSubTree;
        [System.NonSerialized] private BTTree _parent;
        [System.NonSerialized] private BTRoot _root;
        [System.NonSerialized] private List<BTTree> _subs = new List<BTTree>();
        [System.NonSerialized] private IReadOnlyList<BTTree> _subView;

        public BTTree parent => _parent;
        public BTRoot root => _root;
        public IReadOnlyList<BTTree> subs =>
            _subView ?? (_subView = _subs.AsReadOnly());

#if UNITY_5_3_OR_NEWER
        [UnityEngine.HideInInspector]
#endif
        public List<string> interruptFlags = new();
#if UNITY_5_3_OR_NEWER
        [UnityEngine.HideInInspector]
#endif
        public List<string> events = new();
#if UNITY_5_3_OR_NEWER
        [UnityEngine.HideInInspector]
#endif
        public List<Semaphore> semaphores = new List<Semaphore>();

        [System.NonSerialized] private List<BTComposite> abort_composites;
        [System.NonSerialized] private Dictionary<string, BTInterrupt> interrupts;
        [System.NonSerialized] private int[] semaphore_value;
        [System.NonSerialized] private int[] semaphore_limits;
        [System.NonSerialized] private Dictionary<string, List<IBTEventReceiver>> eve_map;

        internal void ReleaseSemaphore(int index)
        {
            if (semaphore_value[index] > 0)
                semaphore_value[index]--;
        }

        internal bool WaitSemaphore(int index)
        {
            if (semaphore_value[index] >= semaphore_limits[index]) return false;
            semaphore_value[index]++;
            return true;
        }

        internal bool IsValidSemaphore(int index) =>
            semaphore_value != null && index >= 0 && index < semaphore_value.Length;
        internal void AddSpecialNode(BTNode node)
        {
            if (node is BTComposite composite)
            {
                abort_composites.Add(composite);
            }
            else if (node is BTInterrupt interrupt)
            {
                var flag = interrupt.flag;
                if (!interrupts.TryAdd(flag, interrupt))
                    throw new Exception($"Same Flag {flag}");
            }
            else if (node is IBTEventReceiver receiver)
            {
                string flag = receiver.EventName;
                if (!eve_map.TryGetValue(flag, out var list))
                {
                    list = new List<IBTEventReceiver>();
                    eve_map[flag] = list;
                }
                list.Add(receiver);
            }
        }
        public T FindRuntimeTreeNode<T>(string guid) where T : NodeData
        {
            var result = this.FindNode<T>(guid);
            if (result != null) return result;
            if (_subs == null) return null;
            for (int i = 0; i < _subs.Count; i++)
            {
                var sub = _subs[i];
                result = sub.FindRuntimeTreeNode<T>(guid);
                if (result != null)
                    return result;
            }
            return null;
        }


        public BTNode.State Update()
        {
            if (abort_composites != null)
            {
                for (int i = 0; i < abort_composites.Count; i++)
                {
                    var condition = abort_composites[i];
                    condition.TryAutoAbort();

                }
            }


            return _root.Update();
        }

        public bool Abort(string flag)
        {
            if (interrupts.TryGetValue(flag, out var interrupt))
            {
                interrupt.Interrupt();
                return true;
            }
            return false;
        }
        public void Abort() => _root.Abort();
        public bool PushEvent(string eve)
        {
            if (!eve_map.TryGetValue(eve, out var list)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                list[i].ReceiveEvent();
            }
            return true;
        }
        public new void PrepareForRuntime()
        {
            throw new Exception($"use loader method");
        }
        public void PrepareForRuntime(Func<string, BTTree> loader)
        {
            _parent = null;
            PrepareForRuntime(loader, new HashSet<string>(StringComparer.Ordinal));
        }

        private void PrepareForRuntime(Func<string, BTTree> loader,
            HashSet<string> loadingSubTreePaths)
        {
            if (_subs == null)
            {
                _subs = new List<BTTree>();
                _subView = null;
            }
            _subs.Clear();
            _root = null;
            ResetRuntimeLinks();
            base.PrepareForRuntime();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                if (node is BTRoot nodeRoot)
                {
                    if (_root != null && !ReferenceEquals(_root, nodeRoot))
                        throw new InvalidOperationException(
                            $"{GetType()} contains more than one root node");
                    _root = nodeRoot;
                }

                if (node is BTSubTree sub)
                {
                    if (loader == null)
                        throw new Exception($"{nameof(loader)}  can not be null");
                    if (string.IsNullOrEmpty(sub.path))
                        throw new InvalidOperationException(
                            $"{sub.GetType()} has no subtree path");
                    if (!loadingSubTreePaths.Add(sub.path))
                        throw new InvalidOperationException(
                            $"Circular subtree reference detected at '{sub.path}'");
                    try
                    {
                        var tree = loader.Invoke(sub.path);
                        if (tree == null)
                            throw new InvalidOperationException(
                                $"Could not load subtree '{sub.path}'");
                        if (ReferenceEquals(tree, this))
                            throw new InvalidOperationException(
                                $"A behavior tree cannot contain itself: '{sub.path}'");
                        if (!tree.IsSubTree || tree.GetType() != GetType())
                            throw new InvalidOperationException(
                                $"Invalid subtree '{sub.path}' for {GetType()}");
                        tree._parent = this;
                        tree.PrepareForRuntime(loader, loadingSubTreePaths);
                        sub.SetRuntimeTree(tree);
                        _subs.Add(tree);
                    }
                    finally
                    {
                        loadingSubTreePaths.Remove(sub.path);
                    }
                }
                if (node.outPorts.Count == 1)
                {
                    var connections = node.outPorts[0].connections;

                    if (node is BTRoot root)
                    {
                        if (connections.Count == 1)
                            root.SetRuntimeChild(connections[0].input.node as BTNode);
                    }
                    else if (node is BTDecorateSingle decorate)
                    {
                        if (connections.Count == 1)
                            decorate.SetRuntimeChild(connections[0].input.node as BTNode);
                    }
                    else if (node is BTDecorateMuti decorate_muti)
                    {
                        var children = new List<BTNode>(connections.Count);
                        for (int j = 0; j < connections.Count; j++)
                        {
                            children.Add(connections[j].input.node as BTNode);
                        }
                        decorate_muti.SetRuntimeChildren(children);
                    }
                    else if (node is BTComposite composite)
                    {
                        var children = new List<BTNode>(connections.Count);
                        for (int j = 0; j < connections.Count; j++)
                        {
                            children.Add(connections[j].input.node as BTNode);
                        }
                        composite.SetRuntimeChildren(children);
                    }

                }
            }
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is BTRoot root)
                {
                    if (root.child is BTSubTree tree)
                    {
                        BTNode runtimeNode = tree.tree.root.child;
                        root.SetRuntimeChild(runtimeNode);
                        tree.SetRuntimeNode(runtimeNode);
                    }
                }
                else if (node is BTDecorateSingle decorate)
                {
                    if (decorate.child is BTSubTree tree)
                    {
                        BTNode runtimeNode = tree.tree.root.child;
                        decorate.SetRuntimeChild(runtimeNode);
                        tree.SetRuntimeNode(runtimeNode);
                    }
                }
                else if (node is BTDecorateMuti decorate_muti)
                {
                    for (int j = 0; j < decorate_muti.RuntimeChildrenCount; j++)
                    {
                        var child = decorate_muti.GetRuntimeChildAt(j);
                        if (child is BTSubTree tree)
                        {
                            BTNode runtimeNode = tree.tree.root.child;
                            decorate_muti.ReplaceRuntimeChild(j, runtimeNode);
                            tree.SetRuntimeNode(runtimeNode);
                        }
                    }
                }
                else if (node is BTComposite composite)
                {
                    for (int j = 0; j < composite.RuntimeChildrenCount; j++)
                    {
                        var child = composite.GetRuntimeChildAt(j);
                        if (child is BTSubTree tree)
                        {
                            BTNode runtimeNode = tree.tree.root.child;
                            composite.ReplaceRuntimeChild(j, runtimeNode);
                            tree.SetRuntimeNode(runtimeNode);

                        }
                    }
                }

            }

            if (_root == null || _root.child == null)
                throw new InvalidOperationException(
                    $"{GetType()} requires one connected root node");
            ValidateRuntimeTree(_root);

            if (!IsSubTree)
            {
                eve_map = new();
                interrupts = new();
                abort_composites = new();
                InitializeSemaphores();
                Blackboard runtimeBlackboard = blackboard;
                if (runtimeBlackboard == null)
                    throw new Exception($"{GetType()} {nameof(blackboard)} is Null");
                _root.Init(runtimeBlackboard, null, this);
            }
        }

        public List<int> CollectStatus(List<int> destination = null)
        {
            EnsureRuntimePrepared();
            destination = destination ?? new List<int>();
            destination.Clear();
            _root.CollectRuntimeStatus(destination);
            for (int i = 0; i < semaphore_value.Length; i++)
                destination.Add(semaphore_value[i]);
            return destination;
        }

        public void ReadStatus(List<int> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            EnsureRuntimePrepared();
            int index = 0;
            _root.ReadRuntimeStatus(source, ref index);
            int semaphoreOffset = index;
            for (int i = 0; i < semaphore_value.Length; i++)
            {
                int value = ReadStatusValue(source, ref index);
                if (value < 0 || value > semaphore_limits[i])
                    throw new ArgumentException(
                        $"Invalid runtime value for semaphore {i}",
                        nameof(source));
            }
            if (index != source.Count)
                throw new ArgumentException(
                    "Runtime status contains extra values", nameof(source));

            for (int i = 0; i < semaphore_value.Length; i++)
                semaphore_value[i] = source[semaphoreOffset + i];
        }

        private void InitializeSemaphores()
        {
            if (semaphores == null) semaphores = new List<Semaphore>();
            int count = semaphores.Count;
            semaphore_value = new int[count];
            semaphore_limits = new int[count];
            for (int i = 0; i < count; i++)
            {
                Semaphore semaphore = semaphores[i];
                if (semaphore == null || semaphore.max <= 0)
                    throw new InvalidOperationException(
                        $"Semaphore {i} must have a positive maximum");
                semaphore_limits[i] = semaphore.max;
            }
        }

        private void ResetRuntimeLinks()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData node = nodes[i];
                (node.inPorts as List<PortData>)?.Clear();
                (node.outPorts as List<PortData>)?.Clear();

                if (node is BTRoot rootNode)
                    rootNode.SetRuntimeChild(null);
                else if (node is BTDecorateSingle single)
                    single.SetRuntimeChild(null);
                else if (node is BTDecorateMuti multi)
                    multi.SetRuntimeChildren(null);
                else if (node is BTComposite composite)
                    composite.SetRuntimeChildren(null);

                if (node is BTSubTree subTree)
                    subTree.ResetRuntimeData();
            }
        }

        private static void ValidateRuntimeTree(BTNode runtimeRoot)
        {
            var visited = new HashSet<BTNode>();
            var pending = new Stack<BTNode>();
            pending.Push(runtimeRoot);
            while (pending.Count > 0)
            {
                BTNode node = pending.Pop();
                if (!visited.Add(node))
                    throw new InvalidOperationException(
                        $"Behavior tree contains a cycle or shared node: {node.GetType()}");

                int childCount = node.RuntimeChildrenCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    BTNode child = node.GetRuntimeChildAt(i);
                    if (child == null)
                        throw new InvalidOperationException(
                            $"{node.GetType()} runtime child {i} is null");
                    pending.Push(child);
                }
            }
        }

        private void EnsureRuntimePrepared()
        {
            if (_root == null || semaphore_value == null || semaphore_limits == null)
                throw new InvalidOperationException(
                    "PrepareForRuntime must be called before accessing status");
        }

        private static int ReadStatusValue(List<int> values, ref int index)
        {
            if (index >= values.Count)
                throw new ArgumentException(
                    "Runtime status does not contain enough values",
                    nameof(values));
            return values[index++];
        }
    }
}
