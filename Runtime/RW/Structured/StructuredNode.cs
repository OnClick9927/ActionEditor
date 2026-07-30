using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    internal enum StructuredNodeKind
    {
        Null,
        Scalar,
        Object,
        Sequence
    }

    internal struct StructuredField
    {
        public string Name;
        public StructuredNode Value;

        public StructuredField(string name, StructuredNode value)
        {
            Name = name;
            Value = value;
        }
    }

    internal sealed class StructuredNode
    {
        public StructuredNodeKind Kind { get; private set; }
        public string Scalar { get; private set; }
        public bool Quoted { get; private set; }
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }
        public readonly List<StructuredField> Fields = new List<StructuredField>();
        public readonly List<StructuredNode> Items = new List<StructuredNode>();

        public StructuredNode() { }

        public static StructuredNode Rent(StructuredNodeKind kind)
        {
            var node = ClassPool<StructuredNode>.Get();
            node.Kind = kind;
            return node;
        }

        public static StructuredNode RentScalar(string value, bool quoted)
        {
            var node = Rent(StructuredNodeKind.Scalar);
            node.Scalar = value;
            node.Quoted = quoted;
            return node;
        }

        public void AddField(string name, StructuredNode value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Fields.Add(new StructuredField(name, value));
        }

        public void AddItem(StructuredNode value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Items.Add(value);
        }

        public static void Release(StructuredNode node)
        {
            if (node == null) return;

            for (int i = 0; i < node.Fields.Count; i++)
                Release(node.Fields[i].Value);
            for (int i = 0; i < node.Items.Count; i++)
                Release(node.Items[i]);

            node.Fields.Clear();
            node.Items.Clear();
            node.Scalar = null;
            node.Quoted = false;
            node.TypeName = null;
            node.AssemblyName = null;
            node.Kind = StructuredNodeKind.Null;
            ClassPool<StructuredNode>.Back(node);
        }
    }
}
