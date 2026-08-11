using ActionAttribute;
using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    internal sealed class BTRuntimeLayout
    {
        internal BTRuntimeLayout(BTTree tree, BTRoot root, object token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            int nodeValueCount = root.RuntimeValueCount;
            int semaphoreCount = tree.semaphores.Count;
            int totalValueCount = checked(nodeValueCount + semaphoreCount);
            ValueCount = totalValueCount;
            DefaultRuntimeValues = new int[totalValueCount];
            MinimumValues = new int[totalValueCount];
            MaximumValues = new int[totalValueCount];
            SemaphoreOffset = nodeValueCount;

            var nodes = new List<BTNode>();
            var abortComposites = new List<BTComposite>();
            var interrupts = new Dictionary<string, BTInterrupt>(
                StringComparer.Ordinal);
            var eventLists =
                new Dictionary<string, List<IBTEventReceiver>>(
                    StringComparer.Ordinal);
            var pending = new Stack<BTNode>();
            pending.Push(root);
            while (pending.Count > 0)
            {
                BTNode node = pending.Pop();
                nodes.Add(node);
                node.WriteRuntimeRules(DefaultRuntimeValues, MinimumValues,
                    MaximumValues);

                if (node is BTComposite composite && composite.HasAutoAbort)
                    abortComposites.Add(composite);
                if (node is BTInterrupt interrupt && interrupt.CanInterrupt)
                    interrupts.Add(interrupt.flag, interrupt);
                if (node is IBTEventReceiver receiver)
                {
                    string eventName = receiver.EventName;
                    if (string.IsNullOrEmpty(eventName))
                        throw new InvalidOperationException(
                            $"{node.GetType()} requires an event name");
                    if (!eventLists.TryGetValue(eventName,
                            out List<IBTEventReceiver> receivers))
                    {
                        receivers = new List<IBTEventReceiver>();
                        eventLists.Add(eventName, receivers);
                    }
                    receivers.Add(receiver);
                }

                for (int i = node.RuntimeChildrenCount - 1; i >= 0; i--)
                    pending.Push(node.GetRuntimeChildAt(i));
            }

            for (int i = 0; i < semaphoreCount; i++)
            {
                int offset = SemaphoreOffset + i;
                MinimumValues[offset] = 0;
                MaximumValues[offset] = tree.semaphores[i].max;
            }

            Nodes = nodes.ToArray();
            AbortComposites = abortComposites.ToArray();
            Interrupts = interrupts;
            EventReceivers =
                new Dictionary<string, IBTEventReceiver[]>(eventLists.Count,
                    StringComparer.Ordinal);
            foreach (KeyValuePair<string, List<IBTEventReceiver>> pair in
                     eventLists)
                EventReceivers.Add(pair.Key, pair.Value.ToArray());
        }

        internal object Token { get; }
        internal BTNode[] Nodes { get; }
        internal int ValueCount { get; }
        internal int[] DefaultRuntimeValues { get; }
        internal int[] MinimumValues { get; }
        internal int[] MaximumValues { get; }
        internal int SemaphoreOffset { get; }
        internal BTComposite[] AbortComposites { get; }
        internal Dictionary<string, BTInterrupt> Interrupts { get; }
        internal Dictionary<string, IBTEventReceiver[]> EventReceivers { get; }
    }

    [AssetFileExtension("bt.bytes")]
    [System.Serializable, Name("行为树", "保存节点图、编辑器黑板默认值和信号量配置；准备运行时只构建节点关系与共享布局，实例状态和运行索引由外部黑板持有。")]
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
        public static Func<string, BTTree> loader;
        public static event Action<BTTree, Blackboard> onInstanceChanged;
        private static BTTree _instance;
        private static Blackboard _instanceBlackboard;
        public static BTTree instance => _instance;
        public static Blackboard instanceBlackboard => _instanceBlackboard;

        public static void SetAsInstance(BTTree tree,
            Blackboard runtimeBlackboard)
        {
            if (tree == null)
            {
                if (runtimeBlackboard != null)
                    throw new ArgumentException(
                        "A runtime Blackboard requires a behavior tree",
                        nameof(runtimeBlackboard));
            }
            else
            {
                if (runtimeBlackboard == null)
                    throw new ArgumentNullException(nameof(runtimeBlackboard));
                if (!runtimeBlackboard.IsInitializedFor(tree))
                    throw new ArgumentException(
                        "The Blackboard is not initialized for this behavior tree",
                        nameof(runtimeBlackboard));
            }
            if (ReferenceEquals(_instance, tree) &&
                ReferenceEquals(_instanceBlackboard, runtimeBlackboard)) return;
            _instance = tree;
            _instanceBlackboard = runtimeBlackboard;
            onInstanceChanged?.Invoke(tree, runtimeBlackboard);
        }

        public static void ClearInstance() => SetAsInstance(null, null);
        public abstract Blackboard blackboard { get; }
        [Name("子树?", "开启后该资源只能由同类型父树通过子树节点加载，并共享父树黑板；关闭时它作为可独立运行的主树初始化运行容器。")]
        public bool IsSubTree;
        [System.NonSerialized] private BTTree _parent;
        [System.NonSerialized] private BTRoot _root;
        [System.NonSerialized] private List<BTTree> _subs = new List<BTTree>();
        [System.NonSerialized] private BTRuntimeLayout _runtimeLayout;

        public BTTree parent => _parent;
        public BTRoot root => _root;
        public IReadOnlyList<BTTree> subs => _subs;
        internal BTRuntimeLayout RuntimeLayout => _runtimeLayout;
        internal bool HasRuntimeLayout(BTRuntimeLayout layout) =>
            layout != null && ReferenceEquals(_runtimeLayout, layout);

        [Name("中断标识", "可由中断装饰节点和执行中断节点选择的协议键；子树沿用主树配置。")]
        [Condition(ConditionMode.Disable, nameof(IsSubTree))]
        public List<string> interruptFlags = new();
        [Name("事件", "可由事件发送、等待和条件节点选择的同步事件键；子树沿用主树配置。")]
        [Condition(ConditionMode.Disable, nameof(IsSubTree))]
        public List<string> events = new();
        [Name("信号量", "限制并行分支占用数量的信号量配置；当前占用数保存在各自运行黑板中。")]
        [Condition(ConditionMode.Disable, nameof(IsSubTree))]
        public List<Semaphore> semaphores = new List<Semaphore>();

        internal bool IsValidSemaphore(int index) =>
            semaphores != null && index >= 0 && index < semaphores.Count;
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


        public BTNode.State Update(Blackboard runtimeBlackboard)
            => runtimeBlackboard.Update();

        public bool Abort(Blackboard runtimeBlackboard, string flag)
            => runtimeBlackboard.Abort(flag);

        public void Abort(Blackboard runtimeBlackboard)
            => runtimeBlackboard.Abort();

        public bool PushEvent(Blackboard runtimeBlackboard, string eve)
            => runtimeBlackboard.PushEvent(eve);
        public override void PrepareForRuntime()
        {
            _parent = null;
            PrepareForRuntime(new HashSet<string>(StringComparer.Ordinal));
            ValidateSemaphores();
            var context = new BTPrepareContext(this);
            _root.Init(null, context);
            _root.CompleteRuntimeLayout(context.RuntimeValueCount);
            _runtimeLayout = new BTRuntimeLayout(this, _root,
                context.LayoutToken);
        }

        private void PrepareForRuntime(HashSet<string> loadingSubTreePaths)
        {
            _runtimeLayout = null;
            if (_subs == null)
                _subs = new List<BTTree>();
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
                        throw new InvalidOperationException(
                            $"{nameof(BTTree)}.{nameof(loader)} cannot be null " +
                            "when a behavior tree contains a subtree");
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
                        tree.PrepareForRuntime(loadingSubTreePaths);
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

        }

        private void ResetRuntimeLinks()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                NodeData node = nodes[i];
                if (node is BTNode btNode)
                    btNode.ResetPreparedData();
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

        internal void EnsureRelationsPrepared()
        {
            if (_root == null || _runtimeLayout == null ||
                !_root.IsPreparedFor(this) ||
                !ReferenceEquals(_runtimeLayout.Token,
                    _root.RuntimeLayoutToken))
                throw new InvalidOperationException(
                    "PrepareForRuntime must be called before using this behavior tree");
        }

        private void ValidateSemaphores()
        {
            if (semaphores == null) semaphores = new List<Semaphore>();
            for (int i = 0; i < semaphores.Count; i++)
                if (semaphores[i] == null || semaphores[i].max <= 0)
                    throw new InvalidOperationException(
                        $"Semaphore {i} must have a positive maximum");
        }
    }
}
