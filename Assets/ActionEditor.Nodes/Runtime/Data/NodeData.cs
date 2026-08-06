using ActionAttribute;
using ActionBuffer;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes
{
    [System.Serializable]
    public class NodeData
    {
        [ReadOnly][Buffer] internal V4 position = new V4();
        [ReadOnly, Buffer, Name("节点标识", "用于唯一识别当前图节点的只读标识。")]
        public string guid;
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
