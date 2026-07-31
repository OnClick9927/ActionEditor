using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;

namespace ActionBuffer
{
    public abstract class StructuredTextReader : IBufferReader, IObjectContextReader,
        IBuffSerializerContext, ITypedEnumReader, IReferenceResolver, IPolymorphicReader,
        ICollectionReader
    {
        private StructuredNode _root;
        private StructuredNode _current;
        private StructuredNodeStorage _nodeStorage;
        private bool _hasRoot;
        private bool _hasCurrent;
        private readonly List<IBufferObject> _afterReadCallbacks = new List<IBufferObject>();
        private bool _deferCallbacks;
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
        private BuffSettings _settings;
        object IObjectContextReader.CurrentObject => _currentObject;
        BuffSettings IBuffSerializerContext.Settings => _settings;
        object IObjectContextReader.GetOrCreateReference(int referenceId, Type type) =>
            GetOrCreateReference(referenceId, type, false);

        internal void SetRoot(StructuredNode root)
        {
            _root = root;
            _current = root;
            _hasRoot = true;
            _hasCurrent = true;
        }

        protected void Prepare(BuffSettings settings)
        {
            Clear();
            _nodeStorage = ClassPool.Get<StructuredNodeStorage>();
            _nodeStorage.Clear();
            _settings = settings ?? BuffSettings.DefaultSetting;
        }

        internal StructuredNode RentNode(StructuredNodeKind kind) =>
            StructuredNode.Rent(_nodeStorage, kind);

        internal StructuredNode RentScalar(string value, bool quoted) =>
            StructuredNode.RentScalar(_nodeStorage, value, quoted);

        internal StructuredNode RentScalarSlice(string source, int start, int length,
            bool quoted) => StructuredNode.RentScalarSlice(_nodeStorage, source, start,
            length, quoted);

        public virtual void Clear()
        {
            if (_hasRoot)
                StructuredNode.Release(ref _root);
            if (_nodeStorage != null)
            {
                _nodeStorage.Clear();
                ClassPool.Back(_nodeStorage);
                _nodeStorage = null;
            }
            _current = default;
            _hasRoot = false;
            _hasCurrent = false;
            _objectReadDepth = 0;
            _currentObject = null;
            _settings = null;
            _deferCallbacks = false;
            _references.Clear();
            _afterReadCallbacks.Clear();
            if (_afterReadCallbacks.Capacity > BuffSettings.RetainedListCapacity)
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
            return BuffSerializer.ResolveSerializedType(
                declaredType, node.TypeName, node.AssemblyName, _settings);
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
                if (_objectReadDepth == 1 && !_deferCallbacks)
                {
                    EnsureReferencesResolved();
                    InvokeAfterReadCallbacks();
                }
                return (T)instance;
            }
            finally
            {
                _objectReadDepth--;
                if (_objectReadDepth == 0 && !_deferCallbacks)
                    _afterReadCallbacks.Clear();
            }
        }

        bool IPolymorphicReader.TryReadPolymorphic(Type declaredType, out object value)
        {
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
            var node = RequireCurrent();
            if (node.Kind != StructuredNodeKind.Object || node.IsReference ||
                string.IsNullOrEmpty(node.TypeName))
            {
                value = null;
                return false;
            }

            var actualType = ResolveType(declaredType, node);
            var converter = ConverterResolver.Get(actualType, _settings);
            if (converter.UsesObjectLayout)
            {
                value = null;
                return false;
            }
            if (node.FieldCount != 1)
                throw new FormatException(
                    $"Polymorphic value '{actualType}' must contain exactly one value field.");
            var fields = node.GetFieldEnumerator();
            if (!fields.MoveNext() ||
                fields.Current.Name != ObjectConverter<object>.PolymorphicValueField)
                throw new FormatException(
                    $"Polymorphic value '{actualType}' has an invalid value field.");

            var previous = _current;
            _current = fields.Current.Value;
            try
            {
                value = converter.Read(this, actualType);
                return true;
            }
            finally
            {
                _current = previous;
            }
        }

        private void ReadFields(StructuredNode node, object instance, TypeHelper.TypeFields fields)
        {
            var previousObject = _currentObject;
            var presentFields = ClassPool.GetHashSet<TypeHelper.TypeFields.Field>();
            _currentObject = instance;
            try
            {
                var serializedFields = node.GetFieldEnumerator();
                while (serializedFields.MoveNext())
                {
                    var serializedField = serializedFields.Current;
                    var field = fields.FindField(serializedField.Name);
                    if (field == null) continue;
                    presentFields.Add(field);

                    var previous = _current;
                    _current = serializedField.Value;
                    try
                    {
                        var converter = ConverterResolver.Get(field.FieldType, _settings);
                        field.ReadAndSet(this, instance, converter);
                    }
                    finally
                    {
                        _current = previous;
                    }
                }
                fields.SetMissingDefaultValues(instance, presentFields);
            }
            finally
            {
                _currentObject = previousObject;
                ClassPool.BackHashSet(presentFields);
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

        private void DefineCollectionReference(int referenceId, object value, Type type)
        {
            if (referenceId < 0) return;
            if (_references.ContainsKey(referenceId))
                throw new FormatException(
                    $"Duplicate object definition for reference id '{referenceId}'.");
            _references.Add(referenceId, new ReferenceEntry
            {
                Value = value,
                Type = type,
                Defined = true
            });
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

        public List<T> ReadIEnumerable<T>(List<T> result, BuffConverter<T> converter)
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (List<T>)GetExistingReference(node.ReferenceId, typeof(List<T>));
            RequireKind(node, StructuredNodeKind.Sequence);
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            DefineCollectionReference(node.ReferenceId, result, typeof(List<T>));

            int requiredCapacity = checked(result.Count + node.ItemCount);
            if (result.Capacity < requiredCapacity)
                result.Capacity = requiredCapacity;

            var items = node.GetItemEnumerator();
            while (items.MoveNext())
            {
                var previous = _current;
                _current = items.Current;
                try
                {
                    result.Add(converter.ReadValue(this, typeof(T)));
                }
                finally
                {
                    _current = previous;
                }
            }
            return result;
        }

        public List<T> ReadList<T>(BuffConverter<T> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (List<T>)GetExistingReference(node.ReferenceId, typeof(List<T>));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new List<T>(node.ItemCount);
            DefineCollectionReference(node.ReferenceId, result, typeof(List<T>));
            var items = node.GetItemEnumerator();
            while (items.MoveNext())
                result.Add(ReadItem(items.Current, converter));
            return result;
        }

        TCollection ICollectionReader.ReadCollection<TCollection, T>(
            BuffConverter<T> converter, CollectionReadMode mode)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (TCollection)GetExistingReference(
                    node.ReferenceId, typeof(TCollection));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = CollectionFactory<TCollection>.Create(node.ItemCount);
            DefineCollectionReference(node.ReferenceId, result, typeof(TCollection));
            if (mode != CollectionReadMode.Stack)
            {
                var items = node.GetItemEnumerator();
                while (items.MoveNext())
                    CollectionPopulator<TCollection, T>.Add(
                        result, ReadItem(items.Current, converter), mode);
                return result;
            }

            var values = ClassPool.GetList<T>(node.ItemCount);
            try
            {
                var items = node.GetItemEnumerator();
                while (items.MoveNext())
                    values.Add(ReadItem(items.Current, converter));
                for (int i = values.Count - 1; i >= 0; i--)
                    CollectionPopulator<TCollection, T>.Add(result, values[i], mode);
            }
            finally
            {
                ClassPool.BackList(values);
            }
            return result;
        }

        TCollection ICollectionReader.ReadArrayList<TCollection>(
            BuffConverter<object> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (TCollection)GetExistingReference(
                    node.ReferenceId, typeof(TCollection));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = CollectionFactory<TCollection>.Create(node.ItemCount);
            DefineCollectionReference(node.ReferenceId, result, typeof(TCollection));
            var items = node.GetItemEnumerator();
            while (items.MoveNext()) result.Add(ReadItem(items.Current, converter));
            return result;
        }

        TCollection ICollectionReader.ReadHashtable<TCollection>(
            BuffConverter<KeyValuePair<object, object>> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (TCollection)GetExistingReference(
                    node.ReferenceId, typeof(TCollection));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = CollectionFactory<TCollection>.Create(node.ItemCount);
            DefineCollectionReference(node.ReferenceId, result, typeof(TCollection));
            var items = node.GetItemEnumerator();
            while (items.MoveNext())
            {
                var item = ReadItem(items.Current, converter);
                try
                {
                    result.Add(item.Key, item.Value);
                }
                catch (ArgumentException exception)
                {
                    throw new FormatException("Invalid or duplicate hashtable key.",
                        exception);
                }
            }
            return result;
        }

        public T[] ReadArray<T>(BuffConverter<T> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (T[])GetExistingReference(node.ReferenceId, typeof(T[]));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new T[node.ItemCount];
            DefineCollectionReference(node.ReferenceId, result, typeof(T[]));
            int resultIndex = 0;
            var items = node.GetItemEnumerator();
            while (items.MoveNext())
                result[resultIndex++] = ReadItem(items.Current, converter);
            return result;
        }

        public Array ReadMultiDimensionalArray<T>(int rank, BuffConverter<T> converter)
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (rank < 2 || rank > 5) throw new ArgumentOutOfRangeException(nameof(rank));
            var arrayType = typeof(T).MakeArrayType(rank);
            if (node.IsReference)
                return (Array)GetExistingReference(node.ReferenceId, arrayType);
            RequireKind(node, StructuredNodeKind.Object);

            StructuredNode dimensions = default;
            StructuredNode values = default;
            bool hasDimensions = false;
            bool hasValues = false;
            var fields = node.GetFieldEnumerator();
            while (fields.MoveNext())
            {
                var field = fields.Current;
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
            int maxCollectionCount = BuffSettings.MaxCollectionCount;
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
            DefineCollectionReference(node.ReferenceId, result, arrayType);
            int valueIndex = 0;
            var valueItems = values.GetItemEnumerator();
            while (valueItems.MoveNext())
                MultiDimensionalArrayHelper.SetValue(result, shape, valueIndex++,
                    ReadItem(valueItems.Current, converter));
            return result;
        }

        private static int ParseArrayDimension(StructuredNode node)
        {
            RequireKind(node, StructuredNodeKind.Scalar);
            ulong result = ParseUnsignedScalar(node);
            if (result >= ushort.MaxValue)
                throw new FormatException("Invalid multi-dimensional array dimension.");
            return (int)result;
        }

        public HashSet<T> ReadHashSet<T>(BuffConverter<T> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (HashSet<T>)GetExistingReference(node.ReferenceId, typeof(HashSet<T>));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new HashSet<T>();
            DefineCollectionReference(node.ReferenceId, result, typeof(HashSet<T>));
            var items = node.GetItemEnumerator();
            while (items.MoveNext())
                if (!result.Add(ReadItem(items.Current, converter)))
                    throw new FormatException("Duplicate set value.");
            return result;
        }

        public Queue<T> ReadQueue<T>(BuffConverter<T> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (Queue<T>)GetExistingReference(node.ReferenceId, typeof(Queue<T>));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new Queue<T>(node.ItemCount);
            DefineCollectionReference(node.ReferenceId, result, typeof(Queue<T>));
            var items = node.GetItemEnumerator();
            while (items.MoveNext())
                result.Enqueue(ReadItem(items.Current, converter));
            return result;
        }

        void IReferenceResolver.EnsureReferencesResolved() => EnsureReferencesResolved();

        public Stack<T> ReadStack<T>(BuffConverter<T> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (Stack<T>)GetExistingReference(node.ReferenceId, typeof(Stack<T>));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new Stack<T>(node.ItemCount);
            DefineCollectionReference(node.ReferenceId, result, typeof(Stack<T>));
            var values = ClassPool.GetList<T>(node.ItemCount);
            try
            {
                var items = node.GetItemEnumerator();
                while (items.MoveNext())
                    values.Add(ReadItem(items.Current, converter));
                for (int i = values.Count - 1; i >= 0; i--)
                    result.Push(values[i]);
            }
            finally
            {
                ClassPool.BackList(values);
            }
            return result;
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BuffConverter<KeyValuePair<TKey, TValue>> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (Dictionary<TKey, TValue>)GetExistingReference(node.ReferenceId,
                    typeof(Dictionary<TKey, TValue>));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new Dictionary<TKey, TValue>(node.ItemCount);
            DefineCollectionReference(node.ReferenceId, result,
                typeof(Dictionary<TKey, TValue>));
            var items = node.GetItemEnumerator();
            while (items.MoveNext())
            {
                var item = ReadItem(items.Current, converter);
                result.Add(item.Key, item.Value);
            }
            return result;
        }

        public ConcurrentDictionary<TKey, TValue> ReadConcurrentDictionary<TKey, TValue>(
            BuffConverter<KeyValuePair<TKey, TValue>> converter)
        {
            var node = RequireSequence();
            if (node.Kind == StructuredNodeKind.Null) return null;
            if (node.IsReference)
                return (ConcurrentDictionary<TKey, TValue>)GetExistingReference(
                    node.ReferenceId, typeof(ConcurrentDictionary<TKey, TValue>));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var result = new ConcurrentDictionary<TKey, TValue>();
            DefineCollectionReference(node.ReferenceId, result,
                typeof(ConcurrentDictionary<TKey, TValue>));
            var items = node.GetItemEnumerator();
            while (items.MoveNext())
            {
                var item = ReadItem(items.Current, converter);
                if (!result.TryAdd(item.Key, item.Value))
                    throw new FormatException($"Duplicate dictionary key '{item.Key}'.");
            }
            return result;
        }

        private StructuredNode RequireSequence()
        {
            var node = RequireCurrent();
            if (node.Kind != StructuredNodeKind.Null && !node.IsReference)
                RequireKind(node, StructuredNodeKind.Sequence);
            return node;
        }

        private T ReadItem<T>(StructuredNode item, BuffConverter<T> converter)
        {
            var previous = _current;
            _current = item;
            try
            {
                return converter.ReadValue(this, typeof(T));
            }
            finally
            {
                _current = previous;
            }
        }

        public T? ReadNullable<T>(BuffConverter<T> converter) where T : struct
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var node = RequireCurrent();
            return node.Kind == StructuredNodeKind.Null
                ? (T?)null
                : converter.ReadValue(this, typeof(T));
        }

        internal void DeferCallbacks()
        {
            _deferCallbacks = true;
        }

        internal void CompleteCallbacks()
        {
            EnsureReferencesResolved();
            try
            {
                InvokeAfterReadCallbacks();
            }
            finally
            {
                _afterReadCallbacks.Clear();
                _deferCallbacks = false;
            }
        }

        public KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>(
            BuffConverter<TKey> keyConverter, BuffConverter<TValue> valueConverter)
        {
            if (keyConverter == null) throw new ArgumentNullException(nameof(keyConverter));
            if (valueConverter == null) throw new ArgumentNullException(nameof(valueConverter));
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return default;
            RequireKind(node, StructuredNodeKind.Object);

            TKey key = default;
            TValue value = default;
            var fields = node.GetFieldEnumerator();
            while (fields.MoveNext())
            {
                var field = fields.Current;
                var previous = _current;
                _current = field.Value;
                try
                {
                    if (field.Name == "key") key = keyConverter.ReadValue(this, typeof(TKey));
                    else if (field.Name == "value")
                        value = valueConverter.ReadValue(this, typeof(TValue));
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

        public bool ReadBool()
        {
            var node = RequireCurrent();
            RequireKind(node, StructuredNodeKind.Scalar);
            GetScalarRange(node, out string source, out int start, out int length);
            if (EqualsAsciiIgnoreCase(source, start, length, "true")) return true;
            if (EqualsAsciiIgnoreCase(source, start, length, "false")) return false;
            throw new FormatException("Expected a Boolean scalar.");
        }

        public byte ReadByte()
        {
            ulong value = ParseUnsignedScalar(RequireCurrent());
            if (value > byte.MaxValue) throw new OverflowException();
            return (byte)value;
        }

        public char ReadChar()
        {
            var node = RequireCurrent();
            RequireKind(node, StructuredNodeKind.Scalar);
            GetScalarRange(node, out string source, out int start, out int length);
            if (length != 1) throw new FormatException("Expected a single character.");
            return source[start];
        }

        public double ReadDouble() => double.Parse(ReadScalar(), NumberStyles.Float, CultureInfo.InvariantCulture);
        public float ReadFloat() => float.Parse(ReadScalar(), NumberStyles.Float, CultureInfo.InvariantCulture);
        public short ReadInt16()
        {
            long value = ParseSignedScalar(RequireCurrent());
            if (value < short.MinValue || value > short.MaxValue) throw new OverflowException();
            return (short)value;
        }

        public int ReadInt32()
        {
            long value = ParseSignedScalar(RequireCurrent());
            if (value < int.MinValue || value > int.MaxValue) throw new OverflowException();
            return (int)value;
        }

        public long ReadInt64() => ParseSignedScalar(RequireCurrent());

        public ushort ReadUInt16()
        {
            ulong value = ParseUnsignedScalar(RequireCurrent());
            if (value > ushort.MaxValue) throw new OverflowException();
            return (ushort)value;
        }

        public uint ReadUInt32()
        {
            ulong value = ParseUnsignedScalar(RequireCurrent());
            if (value > uint.MaxValue) throw new OverflowException();
            return (uint)value;
        }

        public ulong ReadUInt64() => ParseUnsignedScalar(RequireCurrent());
        public string ReadUTF8() => ReadScalar(true);
        public Enum ReadEnum(Type type) => (Enum)Enum.Parse(type, ReadScalar());
        T ITypedEnumReader.ReadEnumValue<T>()
        {
            if (Enum.TryParse(ReadScalar(), out T value)) return value;
            throw new FormatException("Invalid enum scalar.");
        }
        public Guid ReadGuid() => Guid.ParseExact(ReadScalar(), "D");

        private static long ParseSignedScalar(StructuredNode node)
        {
            RequireKind(node, StructuredNodeKind.Scalar);
            GetScalarRange(node, out string source, out int start, out int length);
            int end = start + length;
            bool negative = false;
            if (start < end && (source[start] == '-' || source[start] == '+'))
                negative = source[start++] == '-';
            if (start == end) throw new FormatException("Expected an integer scalar.");

            ulong limit = negative ? 0x8000000000000000UL : long.MaxValue;
            ulong result = 0;
            for (int i = start; i < end; i++)
            {
                uint digit = (uint)(source[i] - '0');
                if (digit > 9)
                    throw new FormatException("Expected an integer scalar.");
                if (result > (limit - digit) / 10)
                    throw new OverflowException();
                result = result * 10 + digit;
            }
            if (!negative) return (long)result;
            return result == 0x8000000000000000UL
                ? long.MinValue
                : -(long)result;
        }

        internal static ulong ParseUnsignedScalar(StructuredNode node)
        {
            RequireKind(node, StructuredNodeKind.Scalar);
            GetScalarRange(node, out string source, out int start, out int length);
            int end = start + length;
            if (start < end && source[start] == '+') start++;
            if (start == end) throw new FormatException("Expected an unsigned integer scalar.");

            ulong result = 0;
            for (int i = start; i < end; i++)
            {
                uint digit = (uint)(source[i] - '0');
                if (digit > 9)
                    throw new FormatException("Expected an unsigned integer scalar.");
                if (result > (ulong.MaxValue - digit) / 10)
                    throw new OverflowException();
                result = result * 10 + digit;
            }
            return result;
        }

        private static void GetScalarRange(StructuredNode node, out string source,
            out int start, out int length)
        {
            if (node.TryGetScalarSlice(out source, out start, out length)) return;
            source = node.Scalar ?? string.Empty;
            start = 0;
            length = source.Length;
        }

        private static bool EqualsAsciiIgnoreCase(string source, int start, int length,
            string expected)
        {
            if (length != expected.Length) return false;
            for (int i = 0; i < length; i++)
            {
                char value = source[start + i];
                if (value >= 'A' && value <= 'Z') value = (char)(value + 32);
                if (value != expected[i]) return false;
            }
            return true;
        }
    }
}
