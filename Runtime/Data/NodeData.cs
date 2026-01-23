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

        [System.NonSerialized] public List<PortData> inPorts = new List<PortData>();
        [System.NonSerialized] public List<PortData> outPorts = new List<PortData>();


    }

}