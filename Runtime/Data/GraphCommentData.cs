using ActionAttribute;
using System;

namespace ActionEditor.Nodes
{
    [Serializable, Name("注释", "用于在 Graph 画布中记录说明文字。")]
    public sealed class GraphCommentData : NodeData
    {
        public string title = "注释";
        public string content = "在这里输入注释";
        public int theme = 1;
        public int fontSize = 2;
    }
}
