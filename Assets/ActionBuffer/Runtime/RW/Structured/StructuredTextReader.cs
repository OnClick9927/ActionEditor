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
        private sealed class ReferenceEntry
        {
            internal object Value;
            internal Type Type;
            internal bool Defined;
        }
        private readonly Dictionary<int, ReferenceEntry> _references =
            new Dictionary<int, ReferenceEntry>();
        private int _objectReadDepth;
        private object _currentObject;
        object IObjectContextReader.CurrentObject => _currentObject;
        object IObjectContextReader.GetOrCreateReference(int referenceId, Type type) =>
            GetOrCreateReference(referenceId, type, false);

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
            _references.Clear();
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

                if (node.IsReference)
                    return (T)GetExistingReference(node.ReferenceId, typeof(T));

                var actualType = ResolveType(typeof(T), node);
                var instance = node.ReferenceId >= 0
                    ? GetOrCreateReference(node.ReferenceId, actualType, true)
                    : TypeHelper.CreateInstance(actualType);
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
                        var converter = field.GetConverter(BufferSerializerSettings.DefaultSetting);
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

        private object GetOrCreateReference(int referenceId, Type type, bool define)
        {
            if (referenceId < 0) throw new FormatException($"Invalid object reference id '{referenceId}'.");
            if (type == null || type.IsValueType)
                throw new FormatException($"Reference id '{referenceId}' must identify a reference type.");
            if (_references.TryGetValue(referenceId, out var entry))
            {
                if (entry.Type != type)
                    throw new FormatException(
                        $"Reference id '{referenceId}' changed type from '{entry.Type}' to '{type}'.");
                if (define && entry.Defined)
                    throw new FormatException($"Duplicate object definition for reference id '{referenceId}'.");
                if (define) entry.Defined = true;
                return entry.Value;
            }

            var value = TypeHelper.CreateInstance(type);
            _references.Add(referenceId, new ReferenceEntry
            {
                Value = value,
                Type = type,
                Defined = define
            });
            return value;
        }

        private object GetExistingReference(int referenceId, Type declaredType)
        {
            if (!_references.TryGetValue(referenceId, out var entry))
                throw new FormatException($"Unknown or forward object reference id '{referenceId}'.");
            if (!declaredType.IsAssignableFrom(entry.Type))
                throw new FormatException(
                    $"Reference type '{entry.Type}' is not assignable to '{declaredType}'.");
            return entry.Value;
        }

        internal void EnsureReferencesResolved()
        {
            foreach (var item in _references)
            {
                if (!item.Value.Defined)
                    throw new FormatException(
                        $"Object reference id '{item.Key}' has no object definition.");
            }
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

        public Array ReadMultiDimensionalArray<T>(int rank, Func<IBufferReader, T> read)
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (read == null) throw new ArgumentNullException(nameof(read));
            if (rank < 2 || rank > 5) throw new ArgumentOutOfRangeException(nameof(rank));
            RequireKind(node, StructuredNodeKind.Object);

            StructuredNode dimensions = default;
            StructuredNode values = default;
            bool hasDimensions = false;
            bool hasValues = false;
            for (int fieldIndex = 0; fieldIndex < node.FieldCount; fieldIndex++)
            {
                var field = node.GetField(fieldIndex);
                if (field.Name == "dimensions" && !hasDimensions)
                {
                    dimensions = field.Value;
                    hasDimensions = true;
                }
                else if (field.Name == "values" && !hasValues)
                {
                    values = field.Value;
                    hasValues = true;
                }
                else
                {
                    throw new FormatException(
                        $"Unknown or duplicate multi-dimensional array property '{field.Name}'.");
                }
            }
            if (!hasDimensions || !hasValues)
                throw new FormatException(
                    "Multi-dimensional arrays require dimensions and values properties.");
            RequireKind(dimensions, StructuredNodeKind.Sequence);
            RequireKind(values, StructuredNodeKind.Sequence);
            if (dimensions.ItemCount != rank)
                throw new FormatException(
                    $"Expected {rank} array dimensions but found {dimensions.ItemCount}.");

            int length0 = ParseArrayDimension(dimensions.GetItem(0));
            int length1 = ParseArrayDimension(dimensions.GetItem(1));
            int length2 = rank > 2 ? ParseArrayDimension(dimensions.GetItem(2)) : 0;
            int length3 = rank > 3 ? ParseArrayDimension(dimensions.GetItem(3)) : 0;
            int length4 = rank > 4 ? ParseArrayDimension(dimensions.GetItem(4)) : 0;
            var shape = new BufferScan.ArrayShape(rank, length0, length1, length2, length3,
                length4);
            bool hasZeroLength = false;
            for (int dimension = 0; dimension < rank; dimension++)
                hasZeroLength |= shape.GetLength(dimension) == 0;
            int maxCollectionCount = BufferSerializerSettings.DefaultSetting.MaxCollectionCount;
            long count = hasZeroLength ? 0 : 1;
            if (!hasZeroLength)
            {
                for (int dimension = 0; dimension < rank; dimension++)
                {
                    int length = shape.GetLength(dimension);
                    if (count > maxCollectionCount / length)
                        throw new FormatException(
                            $"Collection count cannot exceed {maxCollectionCount}.");
                    count *= length;
                }
            }
            if (values.ItemCount != count)
                throw new FormatException(
                    $"Array dimensions require {count} values but found {values.ItemCount}.");

            var result = MultiDimensionalArrayHelper.Create<T>(shape);
            for (int index = 0; index < values.ItemCount; index++)
                MultiDimensionalArrayHelper.SetValue(result, shape, index,
                    ReadItem(values, index, read));
            return result;
        }

        private static int ParseArrayDimension(StructuredNode node)
        {
            RequireKind(node, StructuredNodeKind.Scalar);
            if (!int.TryParse(node.Scalar, NumberStyles.None, CultureInfo.InvariantCulture,
                    out int result) || result < 0 || result >= ushort.MaxValue)
            {
                throw new FormatException(
                    $"Invalid multi-dimensional array dimension '{node.Scalar}'.");
            }
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
