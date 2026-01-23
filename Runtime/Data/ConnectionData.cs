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

        [System.NonSerialized] public PortData output;
        [System.NonSerialized] public PortData input;
    }
}