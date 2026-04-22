using System.Collections.Generic;
using System;
namespace ActionEditor.Nodes
{
    [Serializable]
    public class GroupData : NodeData
    {

        [ReadOnly][Buffer] internal V4 color = new V4() { x = 1, y = 1, z = 1, w = 1 };
        [ReadOnly][Buffer(nameof(nodes))] public List<string> _nodes = new List<string>();
        public IReadOnlyList<string> nodes { get { return _nodes; } internal set { _nodes = value as List<string>; } }
        [ReadOnly][Buffer] public string description;
        public GroupData() : base()
        {
            description = "Group";
        }

    }
}