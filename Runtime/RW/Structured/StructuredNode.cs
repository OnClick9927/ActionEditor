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

    internal struct StructuredNode
    {
        private const string TextFieldEscapePrefix = "$field:";
        public StructuredNodeKind Kind { get; private set; }
        public string Scalar { get; private set; }
        public bool Quoted { get; private set; }
        public string TypeName { get; set; }
        public string AssemblyName { get; set; }
        private List<StructuredField> _fields;
        private List<StructuredNode> _items;

        public int FieldCount => _fields?.Count ?? 0;
        public int ItemCount => _items?.Count ?? 0;

        public static string EncodeTextFieldName(string name)
        {
            if (name == "$type" || name == "$assembly" ||
                name.StartsWith(TextFieldEscapePrefix, StringComparison.Ordinal))
                return TextFieldEscapePrefix + name;
            return name;
        }

        public static string DecodeTextFieldName(string name)
        {
            return name.StartsWith(TextFieldEscapePrefix, StringComparison.Ordinal)
                ? name.Substring(TextFieldEscapePrefix.Length)
                : name;
        }

        public static StructuredNode Rent(StructuredNodeKind kind)
        {
            return new StructuredNode { Kind = kind };
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
            if (_fields == null)
            {
                _fields = ListPool<StructuredField>.Get();
            }
            _fields.Add(new StructuredField(name, value));
        }

        public void AddItem(StructuredNode value)
        {
            if (_items == null)
            {
                _items = ListPool<StructuredNode>.Get();
            }
            _items.Add(value);
        }

        public StructuredField GetField(int index)
        {
            return _fields[index];
        }

        public StructuredNode GetItem(int index)
        {
            return _items[index];
        }

        public static void Release(ref StructuredNode node)
        {
            var fields = node._fields;
            if (fields != null)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    var child = fields[i].Value;
                    Release(ref child);
                }
                fields.Clear();
                ListPool<StructuredField>.Back(fields);
            }

            var items = node._items;
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    var child = items[i];
                    Release(ref child);
                }
                items.Clear();
                ListPool<StructuredNode>.Back(items);
            }

            node = default;
        }
    }
}
