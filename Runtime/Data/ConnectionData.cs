using System;
namespace ActionEditor.Nodes
{
    [Serializable]
    public class ConnectionData
    {
        public string outNodeGuid;
        public string InNodeGuid;

        public string outPortType;
        public string inPortType;
        public string outputPortName;
        public string InPortName;

        public PortData output { get; internal set; }
        public PortData input { get; internal set; }
    }
}