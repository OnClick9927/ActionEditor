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
        internal string Name;
        internal StructuredNode Value;

        internal StructuredField(string name, StructuredNode value)
        {
            Name = name;
            Value = value;
        }
    }

    internal sealed class StructuredNodeStorage
    {
        public StructuredNodeStorage() { }

        private struct FieldEntry
        {
            internal StructuredField Field;
            internal int Next;
        }

        private struct ItemEntry
        {
            internal StructuredNode Item;
            internal int Next;
        }

        private readonly List<FieldEntry> _fields = new List<FieldEntry>();
        private readonly List<ItemEntry> _items = new List<ItemEntry>();

        internal int AddField(string name, StructuredNode value)
        {
            int index = _fields.Count;
            _fields.Add(new FieldEntry
            {
                Field = new StructuredField(name, value),
                Next = -1
            });
            return index;
        }

        internal void LinkField(int index, int next)
        {
            var entry = _fields[index];
            entry.Next = next;
            _fields[index] = entry;
        }

        internal StructuredField GetField(int index) => _fields[index].Field;
        internal int GetNextField(int index) => _fields[index].Next;

        internal int AddItem(StructuredNode value)
        {
            int index = _items.Count;
            _items.Add(new ItemEntry { Item = value, Next = -1 });
            return index;
        }

        internal void LinkItem(int index, int next)
        {
            var entry = _items[index];
            entry.Next = next;
            _items[index] = entry;
        }

        internal StructuredNode GetItem(int index) => _items[index].Item;
        internal int GetNextItem(int index) => _items[index].Next;

        internal void Clear()
        {
            _fields.Clear();
            _items.Clear();
            if (_fields.Capacity > BuffSettings.RetainedListCapacity) _fields.Capacity = 0;
            if (_items.Capacity > BuffSettings.RetainedListCapacity) _items.Capacity = 0;
        }
    }

    internal struct StructuredNode
    {
        internal struct FieldEnumerator
        {
            private readonly StructuredNodeStorage _storage;
            private int _next;
            internal StructuredField Current { get; private set; }

            internal FieldEnumerator(StructuredNodeStorage storage, int first)
            {
                _storage = storage;
                _next = first;
                Current = default;
            }

            internal bool MoveNext()
            {
                if (_next < 0) return false;
                int current = _next;
                Current = _storage.GetField(current);
                _next = _storage.GetNextField(current);
                return true;
            }
        }

        internal struct ItemEnumerator
        {
            private readonly StructuredNodeStorage _storage;
            private int _next;
            internal StructuredNode Current { get; private set; }

            internal ItemEnumerator(StructuredNodeStorage storage, int first)
            {
                _storage = storage;
                _next = first;
                Current = default;
            }

            internal bool MoveNext()
            {
                if (_next < 0) return false;
                int current = _next;
                Current = _storage.GetItem(current);
                _next = _storage.GetNextItem(current);
                return true;
            }
        }

        private const string TextFieldEscapePrefix = "$field:";
        private StructuredNodeStorage _storage;
        private string _scalar;
        private string _scalarSource;
        private int _scalarStart;
        private int _scalarLength;
        private int _firstField;
        private int _lastField;
        private int _fieldCount;
        private int _firstItem;
        private int _lastItem;
        private int _itemCount;

        internal StructuredNodeKind Kind { get; private set; }
        internal string Scalar => _scalarSource == null
            ? _scalar
            : _scalarLength == 0
                ? string.Empty
                : _scalarSource.Substring(_scalarStart, _scalarLength);
        internal bool Quoted { get; private set; }
        internal string TypeName { get; set; }
        internal string AssemblyName { get; set; }
        internal int ReferenceId { get; set; }
        internal bool IsReference { get; set; }
        internal int FieldCount => _fieldCount;
        internal int ItemCount => _itemCount;
        internal FieldEnumerator GetFieldEnumerator() =>
            new FieldEnumerator(_storage, _firstField);
        internal ItemEnumerator GetItemEnumerator() =>
            new ItemEnumerator(_storage, _firstItem);

        internal static string EncodeTextFieldName(string name)
        {
            if (name == "$type" || name == "$assembly" || name == "$id" ||
                name == "$ref" || name == "$values" ||
                name.StartsWith(TextFieldEscapePrefix, StringComparison.Ordinal))
                return TextFieldEscapePrefix + name;
            return name;
        }

        internal static string DecodeTextFieldName(string name)
        {
            return name.StartsWith(TextFieldEscapePrefix, StringComparison.Ordinal)
                ? name.Substring(TextFieldEscapePrefix.Length)
                : name;
        }

        internal static StructuredNode Rent(StructuredNodeStorage storage,
            StructuredNodeKind kind)
        {
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            return new StructuredNode
            {
                _storage = storage,
                Kind = kind,
                ReferenceId = -1,
                _firstField = -1,
                _lastField = -1,
                _firstItem = -1,
                _lastItem = -1
            };
        }

        internal static StructuredNode RentScalar(StructuredNodeStorage storage,
            string value, bool quoted)
        {
            var node = Rent(storage, StructuredNodeKind.Scalar);
            node._scalar = value;
            node.Quoted = quoted;
            return node;
        }

        internal static StructuredNode RentScalarSlice(StructuredNodeStorage storage,
            string source, int start, int length, bool quoted)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (start < 0 || length < 0 || start > source.Length - length)
                throw new ArgumentOutOfRangeException(nameof(start));
            var node = Rent(storage, StructuredNodeKind.Scalar);
            node._scalarSource = source;
            node._scalarStart = start;
            node._scalarLength = length;
            node.Quoted = quoted;
            return node;
        }

        internal bool TryGetScalarSlice(out string source, out int start, out int length)
        {
            source = _scalarSource;
            start = _scalarStart;
            length = _scalarLength;
            return source != null;
        }

        internal void AddField(string name, StructuredNode value)
        {
            int index = _storage.AddField(name, value);
            if (_lastField >= 0) _storage.LinkField(_lastField, index);
            else _firstField = index;
            _lastField = index;
            _fieldCount++;
        }

        internal void AddItem(StructuredNode value)
        {
            int index = _storage.AddItem(value);
            if (_lastItem >= 0) _storage.LinkItem(_lastItem, index);
            else _firstItem = index;
            _lastItem = index;
            _itemCount++;
        }

        internal StructuredNode GetItem(int ordinal)
        {
            int index = _firstItem;
            while (ordinal-- > 0 && index >= 0) index = _storage.GetNextItem(index);
            if (index < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
            return _storage.GetItem(index);
        }

        internal static void Release(ref StructuredNode node)
        {
            node = default;
        }
    }
}
