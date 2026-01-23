using System.Collections.Generic;
using System;
namespace ActionEditor.Nodes
{
    [Serializable]
    public class GroupData : NodeData
    {

        [ReadOnly][Buffer] internal V4 color = new V4() { x = 1, y = 1, z = 1, w = 1 };
        [ReadOnly][Buffer] public List<string> nodes = new List<string>();
        [ReadOnly][Buffer] public string description;
        public GroupData() : base()
        {
            description = "Group";
        }

    }
}