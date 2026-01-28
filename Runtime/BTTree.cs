using System.Collections.Generic;
using System.Linq;
namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("ÐÐÎªÊ÷")]
    public abstract class BTTree : GraphAsset
    {
        private Dictionary<string, BTNode> dic = new Dictionary<string, BTNode>();
        public BTNode FindNode(string guid)
        {
            if (dic.TryGetValue(guid, out var result))
            {
                result = this.nodes.FirstOrDefault(x => x.guid == guid) as BTNode;
                dic.Add(guid, result);
            }
            return result;
        }
        public static BTTree instance { get;private set; }
        public void SetAsInstance() => instance = this;
        public static void ClearInstance() => instance = null;

        public abstract Blackboard blackBoard { get; }

        [System.NonSerialized] public BTRoot root;
        [System.NonSerialized] private List<BTComposite> aborted = new List<BTComposite>();
        [System.NonSerialized] private List<BTCondition> coditions;
        private void AbortLowerPriority(BTComposite composite)
        {
            if (composite.state == BTNode.State.Running) return;
            if (aborted.Contains(composite)) return;
            var temp = composite;
            while (temp != null)
            {
                if (temp.abortType == BTComposite.AbortType.Both || temp.abortType == BTComposite.AbortType.LowerPriority)
                {
                    composite = temp;
                    if (!aborted.Contains(composite))
                        aborted.Add(composite);
                }
                else
                    break;
                temp = temp.composite;
            }
            composite.Abort();
        }
        public BTNode.State Update()
        {
            aborted.Clear();
            for (int i = 0; i < coditions.Count; i++)
            {
                var condition = coditions[i];
                if (condition.state == BTNode.State.Success) continue;
                if (condition.Update() != BTNode.State.Success) continue;
                var composite = condition.composite;
                if (composite == null) continue;

                if (composite.abortType == BTComposite.AbortType.Both || composite.abortType == BTComposite.AbortType.LowerPriority)
                    AbortLowerPriority(composite);
                if (composite.abortType == BTComposite.AbortType.Both || composite.abortType == BTComposite.AbortType.Self)
                    composite.Abort();
            }

            return root.Update();
        }
        public void Abort() => root.Abort();
        public new void PrepareForRuntime()
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
                            composite.children.Add(connections[i].input.node as BTNode);
                        }
                    }

                }
            }


            coditions = root.Init(blackBoard, null, new List<BTCondition>());
        }

    }
}
