using System;

namespace ActionEditor.Nodes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NodePortAttribute : System.Attribute
    {
        public Direction direction;
        public enum Direction
        {

            Input,
            Output
        }
        public bool single;
        public NodePortAttribute(Direction direction, bool single = true)
        {
            this.direction = direction;
            this.single = single;
        }
    }
}
