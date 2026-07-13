using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("ÐÐÎªÊ÷")]
    public abstract class BTTree : GraphAsset
    {
        [System.Serializable]
        public class Semaphore
        {
            public string name;
            public int max = 1;
        }
        public static event Action<BTTree> onInstanceChanged;
        private static BTTree _instance;
        public static BTTree instance
        {
            get { return _instance; }
            private set
            {
                if (_instance != value)
                    _instance = value;
                onInstanceChanged?.Invoke(value);
            }

        }
        public void SetAsInstance() => instance = this;
        public static void ClearInstance() => instance = null;
        protected abstract Blackboard blackboard { get; }
        public Blackboard Blackboard => parent == null ? blackboard : parent.blackboard;
        [Name("×ÓÊ÷?")] public bool IsSubTree;
        public BTTree parent { get; private set; }
        public BTRoot root { get; private set; }

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
        [System.NonSerialized] private Dictionary<int, int> semaphore_value;
        [System.NonSerialized] private Dictionary<string, List<BTRecEventCondition>> eve_map;

        internal void ReleaseSemaphore(int index)
        {
            var value = semaphore_value.TryGetValue(index, out var result) ? result : 0;
            value--;
            if (value < 0)
                value = 0;
            semaphore_value[index] = value;
        }

        internal bool WaitSemaphore(int index)
        {
            var value = semaphore_value.TryGetValue(index, out var result) ? result : 0;
            var cfg = semaphores[index];
            if (value >= cfg.max) return false;
            semaphore_value[index] = result + 1;
            return true;
        }
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
            else if (node is BTRecEventCondition rec)
            {
                var flag = rec.eventName;
                if (!eve_map.TryGetValue(flag, out var list))
                {
                    list = new List<BTRecEventCondition>();
                    eve_map[flag] = list;
                }
                list.Add(rec);
            }
        }
        [System.NonSerialized] public List<BTTree> subs = new List<BTTree>();

        public T FindRuntimeTreeNode<T>(string guid) where T : NodeData
        {
            var result = this.FindNode<T>(guid);
            if (result != null) return result;
            for (int i = 0; i < subs.Count; i++)
            {
                var sub = subs[i];
                result = sub.FindNode<T>(guid);
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


            return root.Update();
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
        public void Abort() => root.Abort();
        public bool PushEvent(string eve)
        {
            if (!eve_map.TryGetValue(eve, out var list)) return false;
            for (int i = 0; i < list.Count; i++)
            {
                var rec = list[i];
                rec.recEve = true;
            }
            return true;
        }
        public new void PrepareForRuntime()
        {
            throw new Exception($"use loader method");
        }
        public void PrepareForRuntime(Func<string, BTTree> loader)
        {
            subs.Clear();
            base.PrepareForRuntime();
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

                if (node is BTSubTree sub)
                {
                    if (loader == null)
                        throw new Exception($"{nameof(loader)}  can not be null");
                    var tree = loader.Invoke(sub.path);
                    tree.parent = this;
                    tree.PrepareForRuntime(loader);
                    sub.tree = tree;
                    subs.Add(tree);
                }
                if (node.outPorts.Count == 1)
                {
                    var connections = node.outPorts[0].connections;

                    if (node is BTRoot root)
                    {
                        this.root = root;
                        if (connections.Count == 1)
                            root.child = connections[0].input.node as BTNode;
                    }
                    else if (node is BTDecorateSingle decorate)
                    {
                        if (connections.Count == 1)
                            decorate.child = connections[0].input.node as BTNode;
                    }
                    else if (node is BTDecorateMuti decorate_muti)
                    {
                        decorate_muti.children = new List<BTNode>();
                        for (int j = 0; j < connections.Count; j++)
                        {
                            decorate_muti.children.Add(connections[j].input.node as BTNode);
                        }
                    }
                    else if (node is BTComposite composite)
                    {
                        composite.children = new List<BTNode>();
                        for (int j = 0; j < connections.Count; j++)
                        {
                            composite.children.Add(connections[j].input.node as BTNode);
                        }
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
                        root.child = tree.tree.root.child;
                        tree.runtimeNode = root.child;
                    }
                }
                else if (node is BTDecorateSingle decorate)
                {
                    if (decorate.child is BTSubTree tree)
                    {
                        decorate.child = tree.tree.root.child;
                        tree.runtimeNode = decorate.child;
                    }
                }
                else if (node is BTDecorateMuti decorate_muti)
                {
                    for (int j = 0; j < decorate_muti.children.Count; j++)
                    {
                        var child = decorate_muti.children[i];
                        if (child is BTSubTree tree)
                        {
                            decorate_muti.children[j] = tree.tree.root.child;
                            tree.runtimeNode = decorate_muti.children[j];
                        }
                    }
                }
                else if (node is BTComposite composite)
                {
                    for (int j = 0; j < composite.children.Count; j++)
                    {
                        var child = composite.children[j];
                        if (child is BTSubTree tree)
                        {
                            composite.children[j] = tree.tree.root.child;
                            tree.runtimeNode = composite.children[j];

                        }
                    }
                }

            }

            if (!IsSubTree)
            {
                eve_map = new();
                interrupts = new();
                semaphore_value = new();

                abort_composites = new();
                root.Init(blackboard, null, this);
            }
        }
    }
}
