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

        public abstract Blackboard blackBoard { get; }

        [System.NonSerialized] public BTRoot root;
        [System.NonSerialized] private List<BTComposite> aborted = new List<BTComposite>();
        [System.NonSerialized] private List<BTCondition> abort_coditions;





        public BTNode.State Update()
        {
            aborted.Clear();
            for (int i = 0; i < abort_coditions.Count; i++)
            {
                var condition = abort_coditions[i];
                if (condition.composite.abortType == BTComposite.AbortType.Both || condition.composite.abortType == BTComposite.AbortType.LowerPriority)
                {
                    if (condition.composite.state != BTNode.State.Running && condition.Update() == BTNode.State.Success)
                        condition.lowerAbortComposite.Abort();
                }
                if (condition.composite.abortType == BTComposite.AbortType.Both || condition.composite.abortType == BTComposite.AbortType.Self)
                {
                    if (condition.composite.state == BTNode.State.Running && condition.Update() == BTNode.State.Success)
                        condition.composite.Abort();
                }
            }

            return root.Update();
        }
        public void Abort() => root.Abort();
        public override void PrepareForRuntime()
        {
            base.PrepareForRuntime();

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];

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


            abort_coditions = root.Init(blackBoard, null, new List<BTCondition>());
        }

    }
}
