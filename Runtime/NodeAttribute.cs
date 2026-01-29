using System;

namespace ActionEditor.Nodes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NodeAttribute : System.Attribute
    {
        public string group;

        public NodeAttribute(string path)
        {
            this.group = path;
        }
    }
}
