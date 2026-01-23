using UnityEditor.Experimental.GraphView;

namespace ActionEditor.Nodes
{
    public class GraphConnection : Edge
    {
        public new GraphPort output { get { return base.output as GraphPort; } set { base.output = value; } }
        public new GraphPort input { get { return base.input as GraphPort; } set { base.input = value; } }
    }
}
