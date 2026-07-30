using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace ActionBuffer
{
    public abstract class StructuredTextReader : IBufferReader, IObjectContextReader
    {
        private StructuredNode _root;
        private StructuredNode _current;
        private bool _hasRoot;
        private bool _hasCurrent;
        private readonly List<IBufferObject> _afterReadCallbacks = new List<IBufferObject>();
        private int _objectReadDepth;
        private object _currentObject;
        object IObjectContextReader.CurrentObject => _currentObject;

        internal void SetRoot(StructuredNode root)
        {
            Clear();
            _root = root;
            _current = root;
            _hasRoot = true;
            _hasCurrent = true;
        }

        public virtual void Clear()
        {
            if (_hasRoot)
                StructuredNode.Release(ref _root);
            _current = default;
            _hasRoot = false;
            _hasCurrent = false;
            _objectReadDepth = 0;
            _currentObject = null;
            _afterReadCallbacks.Clear();
            if (_afterReadCallbacks.Capacity > BufferSerializer.RetainedListCapacity)
                _afterReadCallbacks.Capacity = 0;
        }

        private StructuredNode RequireCurrent()
        {
            if (!_hasCurrent)
                throw new InvalidOperationException("The reader has not been initialized.");
            return _current;
        }

        private static void RequireKind(StructuredNode node, StructuredNodeKind kind)
        {
            if (node.Kind != kind)
                throw new FormatException($"Expected {kind}, but found {node.Kind}.");
        }

        private Type ResolveType(Type declaredType, StructuredNode node)
        {
            if (string.IsNullOrEmpty(node.TypeName) && (declaredType.IsAbstract || declaredType.IsInterface))
                throw new FormatException($"Type metadata is required to instantiate '{declaredType}'.");
            return TypeHelper.ResolveSerializedType(declaredType, node.TypeName, node.AssemblyName);
        }

        public T ReadObject<T>()
        {
            _objectReadDepth++;
            try
            {
                var node = RequireCurrent();
                if (node.Kind == StructuredNodeKind.Null) return default;
                RequireKind(node, StructuredNodeKind.Object);

                var actualType = ResolveType(typeof(T), node);
                var instance = TypeHelper.CreateInstance(actualType);
                ReadFields(node, instance, TypeHelper.GetTypeFields(actualType));
                if (instance is IBufferObject callback)
                    _afterReadCallbacks.Add(callback);
                if (_objectReadDepth == 1)
                    InvokeAfterReadCallbacks();
                return (T)instance;
            }
            finally
            {
                _objectReadDepth--;
                if (_objectReadDepth == 0)
                    _afterReadCallbacks.Clear();
            }
        }

        private void ReadFields(StructuredNode node, object instance, TypeHelper.TypeFields fields)
        {
            var previousObject = _currentObject;
            _currentObject = instance;
            try
            {
                fields.SetDefaultValues(instance);
                for (int i = 0; i < node.FieldCount; i++)
                {
                    var serializedField = node.GetField(i);
                    var field = fields.FindField(serializedField.Name);
                    if (field == null) continue;

                    var previous = _current;
                    _current = serializedField.Value;
                    try
                    {
                        var converter = field.GetConverter();
                        field.SetValue(instance, converter.Read(this, field.FieldType));
                    }
                    finally
                    {
                        _current = previous;
                    }
                }
            }
            finally
            {
                _currentObject = previousObject;
            }
        }

        private void InvokeAfterReadCallbacks()
        {
            for (int i = 0; i < _afterReadCallbacks.Count; i++)
                _afterReadCallbacks[i].AfterReadBuffer();
        }

        public List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read)
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return null;
            RequireKind(node, StructuredNodeKind.Sequence);
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (read == null) throw new ArgumentNullException(nameof(read));

            int requiredCapacity = checked(result.Count + node.ItemCount);
            if (result.Capacity < requiredCapacity)
                result.Capacity = requiredCapacity;

            for (int i = 0; i < node.ItemCount; i++)
            {
                var previous = _current;
                _current = node.GetItem(i);
                try
                {
                    result.Add(read(this));
                }
                finally
                {
                    _current = previous;
                }
            }
            return result;
        }

        public List<T> ReadList<T>(Func<IBufferReader, T> read)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            var result = new List<T>(node.ItemCount);
            for (int i = 0; i < node.ItemCount; i++)
                result.Add(ReadItem(node, i, read));
            return result;
        }

        public T[] ReadArray<T>(Func<IBufferReader, T> read)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            var result = new T[node.ItemCount];
            for (int i = 0; i < node.ItemCount; i++)
                result[i] = ReadItem(node, i, read);
            return result;
        }

        public T[,] ReadArray2D<T>(Func<IBufferReader, T> read)
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            if (node.Kind == StructuredNodeKind.Object)
                return ReadEmptyArray2D<T>(node);
            RequireKind(node, StructuredNodeKind.Sequence);

            int rows = node.ItemCount;
            int columns = 0;
            if (rows > 0)
            {
                var firstRow = node.GetItem(0);
                RequireKind(firstRow, StructuredNodeKind.Sequence);
                columns = firstRow.ItemCount;
            }
            if (rows >= ushort.MaxValue || columns >= ushort.MaxValue)
                throw new FormatException(
                    $"Array dimensions cannot exceed {ushort.MaxValue - 1}.");
            long count = (long)rows * columns;
            if (count > BufferSerializer.MaxCollectionCount)
                throw new FormatException(
                    $"Collection count cannot exceed {BufferSerializer.MaxCollectionCount}.");

            var result = new T[rows, columns];
            for (int row = 0; row < rows; row++)
            {
                var rowNode = node.GetItem(row);
                RequireKind(rowNode, StructuredNodeKind.Sequence);
                if (rowNode.ItemCount != columns)
                    throw new FormatException("Two-dimensional array rows must have the same length.");
                for (int column = 0; column < columns; column++)
                    result[row, column] = ReadItem(rowNode, column, read);
            }
            return result;
        }

        private static T[,] ReadEmptyArray2D<T>(StructuredNode node)
        {
            int rows = -1;
            int columns = -1;
            for (int i = 0; i < node.FieldCount; i++)
            {
                var field = node.GetField(i);
                RequireKind(field.Value, StructuredNodeKind.Scalar);
                if (field.Name == "$rows")
                    rows = ParseArrayDimension(field.Value.Scalar);
                else if (field.Name == "$columns")
                    columns = ParseArrayDimension(field.Value.Scalar);
                else
                    throw new FormatException($"Unknown two-dimensional array property '{field.Name}'.");
            }
            if (rows != 0 || columns <= 0 || columns >= ushort.MaxValue)
                throw new FormatException("Invalid empty two-dimensional array dimensions.");
            return new T[rows, columns];
        }

        private static int ParseArrayDimension(string value)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result))
                throw new FormatException($"Invalid two-dimensional array dimension '{value}'.");
            return result;
        }

        public HashSet<T> ReadHashSet<T>(Func<IBufferReader, T> read)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            var result = new HashSet<T>();
            for (int i = 0; i < node.ItemCount; i++)
                result.Add(ReadItem(node, i, read));
            return result;
        }

        public Queue<T> ReadQueue<T>(Func<IBufferReader, T> read)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            var result = new Queue<T>(node.ItemCount);
            for (int i = 0; i < node.ItemCount; i++)
                result.Enqueue(ReadItem(node, i, read));
            return result;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            Func<IBufferReader, KeyValuePair<TKey, TValue>> read)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            var result = new Dictionary<TKey, TValue>(node.ItemCount);
            for (int i = 0; i < node.ItemCount; i++)
            {
                var item = ReadItem(node, i, read);
                result.Add(item.Key, item.Value);
            }
            return result;
        }

        public ConcurrentDictionary<TKey, TValue> ReadConcurrentDictionary<TKey, TValue>(
            Func<IBufferReader, KeyValuePair<TKey, TValue>> read)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            var result = new ConcurrentDictionary<TKey, TValue>();
            for (int i = 0; i < node.ItemCount; i++)
            {
                var item = ReadItem(node, i, read);
                if (!result.TryAdd(item.Key, item.Value))
                    throw new FormatException($"Duplicate dictionary key '{item.Key}'.");
            }
            return result;
        }

        private StructuredNode RequireSequence()
        {
            var node = RequireCurrent();
            if (node.Kind != StructuredNodeKind.Null)
                RequireKind(node, StructuredNodeKind.Sequence);
            return node;
        }

        private T ReadItem<T>(StructuredNode node, int index, Func<IBufferReader, T> read)
        {
            var previous = _current;
            _current = node.GetItem(index);
            try
            {
                return read(this);
            }
            finally
            {
                _current = previous;
            }
        }

        public T? ReadNullable<T>(Func<IBufferReader, T> read) where T : struct
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            var node = RequireCurrent();
            return node.Kind == StructuredNodeKind.Null ? (T?)null : read(this);
        }

        public KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>(
            Func<IBufferReader, TKey> readKey, Func<IBufferReader, TValue> readValue)
        {
            if (readKey == null) throw new ArgumentNullException(nameof(readKey));
            if (readValue == null) throw new ArgumentNullException(nameof(readValue));
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return default;
            RequireKind(node, StructuredNodeKind.Object);

            TKey key = default;
            TValue value = default;
            for (int i = 0; i < node.FieldCount; i++)
            {
                var field = node.GetField(i);
                var previous = _current;
                _current = field.Value;
                try
                {
                    if (field.Name == "key") key = readKey(this);
                    else if (field.Name == "value") value = readValue(this);
                }
                finally
                {
                    _current = previous;
                }
            }
            return new KeyValuePair<TKey, TValue>(key, value);
        }

        private string ReadScalar(bool allowNull = false)
        {
            var node = RequireCurrent();
            if (allowNull && node.Kind == StructuredNodeKind.Null) return null;
            RequireKind(node, StructuredNodeKind.Scalar);
            return node.Scalar;
        }

        public bool ReadBool() => bool.Parse(ReadScalar());
        public byte ReadByte() => byte.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);

        public char ReadChar()
        {
            var value = ReadScalar();
            if (value.Length != 1) throw new FormatException("Expected a single character.");
            return value[0];
        }

        public double ReadDouble() => double.Parse(ReadScalar(), NumberStyles.Float, CultureInfo.InvariantCulture);
        public float ReadFloat() => float.Parse(ReadScalar(), NumberStyles.Float, CultureInfo.InvariantCulture);
        public short ReadInt16() => short.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public int ReadInt32() => int.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public long ReadInt64() => long.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public ushort ReadUInt16() => ushort.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public uint ReadUInt32() => uint.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public ulong ReadUInt64() => ulong.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public string ReadUTF8() => ReadScalar(true);
        public Enum ReadEnum(Type type) => (Enum)Enum.Parse(type, ReadScalar());
    }
}
