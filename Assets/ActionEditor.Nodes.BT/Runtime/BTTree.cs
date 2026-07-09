using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("ÐÐÎªÊ÷")]
    public abstract class BTTree : GraphAsset
    {

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
        [Name("×ÓÊ÷?")]public bool IsSubTree;
        public BTTree parent { get; private set; }
        public BTRoot root { get; private set; }
        //[System.NonSerialized] private List<BTComposite> aborted = new List<BTComposite>();
        [System.NonSerialized] private List<BTComposite> abort_coditions;
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
            //aborted.Clear();
            for (int i = 0; i < abort_coditions.Count; i++)
            {
                var condition = abort_coditions[i];
                condition.TryAutoAbort();
               
            }

            return root.Update();
        }
        public void Abort() => root.Abort();
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
                    else if (node is BTDecorate decorate)
                    {
                        if (connections.Count == 1)
                            decorate.child = connections[0].input.node as BTNode;
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
            if (parent == null)
                abort_coditions = root.Init(blackboard, null, new List<BTComposite>());
        }
    }
}
