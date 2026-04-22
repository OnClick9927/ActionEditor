using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes
{
    [System.Serializable]
    public class NodeData
    {
        [ReadOnly][Buffer] internal V4 position = new V4();
        [ReadOnly][Buffer] public string guid;
        public NodeData()
        {
            position = new V4();
            guid = Guid.NewGuid().ToString();
        }

        private List<PortData> _inPorts = new List<PortData>();
        private List<PortData> _outPorts = new List<PortData>();

        public IReadOnlyList<PortData> inPorts => _inPorts;
        public IReadOnlyList<PortData> outPorts => _outPorts;


    }

}