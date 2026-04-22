using System.Collections.Generic;

namespace ActionEditor.Nodes
{
    public class PortData
    {
        public List<ConnectionData> connections = new List<ConnectionData>();
        public PortDirection direction;
        public string type;
        public string name;
        public NodeData node;
    }

}