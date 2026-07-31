using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
namespace ActionBuffer
{
    public class BufferReader : IBufferReader, IObjectContextReader, IBuffSerializerContext,
        ITypedEnumReader, IReferenceResolver, IPolymorphicReader, ICollectionReader
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly BuffConverter<string> MetadataConverter = new StringConverter();
        private byte[] _buffer;
        private int _index = 0;
        private int _depth;
        private int _nodeCount;
        private int _limit;
        private int _precountedReadDepth;
        private int _maxDepth;
        private int _maxNodeCount;
        private int _maxCollectionCount;
        private int _maxObjectFieldCount;
        private int _maxScalarLength;
        private bool _suppressNodeCounting;
        private int _objectReadDepth;
        private object _currentObject;
        private BuffSettings _settings;
        private sealed class ReferenceEntry
        {
            internal object Value;
            internal Type Type;
            internal bool Defined;
        }
        private readonly Dictionary<int, ReferenceEntry> _references =
            new Dictionary<int, ReferenceEntry>();
        private bool _supportReferences;
        private bool _collectionReferences;
        private readonly List<IBufferObject> _afterReadCallbacks = new List<IBufferObject>();
        private bool _deferCallbacks;
        object IObjectContextReader.CurrentObject => _currentObject;
        BuffSettings IBuffSerializerContext.Settings => _settings;
        object IObjectContextReader.GetOrCreateReference(int referenceId, Type type) =>
            GetOrCreateReference(referenceId, type, false);
        public int index
        {
            get { return _index; }
            set
            {
                if (_buffer == null) throw new InvalidOperationException("The reader has not been initialized.");
                if (value < 0 || value > _buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _index = value;
            }
        }
        public void Init(byte[] data, BuffSettings settings = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            int maxBinaryLength = BuffSettings.MaxBinaryLength;
            if (data.Length > maxBinaryLength)
                throw new FormatException(
                    $"Binary data length cannot exceed {maxBinaryLength} bytes.");
            Clear();
            _settings = settings ?? BuffSettings.DefaultSetting;
            _maxDepth = BuffSettings.MaxDepth;
            _maxNodeCount = BuffSettings.MaxNodeCount;
            _maxCollectionCount = BuffSettings.MaxCollectionCount;
            _maxObjectFieldCount = BuffSettings.MaxObjectFieldCount;
            _maxScalarLength = BuffSettings.MaxScalarLength;
            _buffer = data;
            _limit = data.Length;
        }

        public void Clear()
        {
            _buffer = null;
            _index = 0;
            _depth = 0;
            _nodeCount = 0;
            _limit = 0;
            _precountedReadDepth = 0;
            _suppressNodeCounting = false;
            _objectReadDepth = 0;
            _currentObject = null;
            _settings = null;
            _supportReferences = false;
            _collectionReferences = false;
            _deferCallbacks = false;
            _references.Clear();
            _afterReadCallbacks.Clear();
            if (_afterReadCallbacks.Capacity > BuffSettings.RetainedListCapacity)
                _afterReadCallbacks.Capacity = 0;
            if (metas == null) return;
            metas.Clear();
            ClassPool.BackList(metas);
            metas = null;
        }
        private void CheckReaderIndex(int length)
        {
            if (length < 0 || _buffer == null || _index < 0 || _index > _limit - length)
                throw new FormatException("Unexpected end of binary data.");
        }
        public bool IsValid
        {
            get
            {
                if (_buffer == null || _buffer.Length == 0)
                    return false;
                else
                    return true;
            }
        }
        public int Capacity
        {
            get { return _buffer?.Length ?? 0; }
        }
        public Enum ReadEnum(Type type)
        {
            if (type == null || !type.IsEnum) throw new ArgumentException("Expected an enum type.", nameof(type));
            return Enum.ToObject(type, ReadUInt64()) as Enum;
        }

        T ITypedEnumReader.ReadEnumValue<T>() => EnumValue<T>.FromUInt64(ReadUInt64());
        public byte ReadByte()
        {
            CheckReaderIndex(1);
            return _buffer[_index++];
        }
        public char ReadChar()
        {
            CheckReaderIndex(2);
            char c = (char)(((_buffer[_index] & 0xFF) << 8) | (_buffer[_index + 1] & 0xFF));
            _index += 2;
            return c;
        }
        public bool ReadBool()
        {
            CheckReaderIndex(1);
            byte value = _buffer[_index++];
            if (value > 1) throw new FormatException($"Invalid Boolean value '{value}'.");
            return value == 1;
        }
        public short ReadInt16()
        {
            CheckReaderIndex(2);
            short value = (short)((_buffer[_index]) | (_buffer[_index + 1] << 8));
            _index += 2;
            return value;
        }
        public ushort ReadUInt16() => (ushort)ReadInt16();
        public float ReadFloat()
        {
            var _int = ReadInt32();
            var _value = new FloatUnion() { _int = _int }.value;
            return _value;
        }
        public double ReadDouble()
        {
            long _int = ReadInt64();
            var _value = new DoubleUnion() { _long = _int }.value;
            return _value;
        }
        public int ReadInt32()
        {
            CheckReaderIndex(4);
            int value = (_buffer[_index]) | (_buffer[_index + 1] << 8) | (_buffer[_index + 2] << 16) | (_buffer[_index + 3] << 24);
            _index += 4;
            return value;
        }
        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }
        public long ReadInt64()
        {
            CheckReaderIndex(8);
            int i1 = (_buffer[_index]) | (_buffer[_index + 1] << 8) | (_buffer[_index + 2] << 16) | (_buffer[_index + 3] << 24);
            int i2 = (_buffer[_index + 4]) | (_buffer[_index + 5] << 8) | (_buffer[_index + 6] << 16) | (_buffer[_index + 7] << 24);
            _index += 8;
            return (uint)i1 | ((long)i2 << 32);
        }
        public ulong ReadUInt64()
        {
            return (ulong)ReadInt64();
        }
        public Guid ReadGuid()
        {
            CheckReaderIndex(16);
            var value = new Guid(new ReadOnlySpan<byte>(_buffer, _index, 16));
            _index += 16;
            return value;
        }
        public string ReadUTF8()
        {
            int count = ReadInt32();
            if (count == -1)
                return null;
            if (count < -1)
                throw new FormatException($"Invalid binary scalar length '{count}'.");
            if (count == 0)
                return string.Empty;
            if (count > _maxScalarLength)
                throw new FormatException(
                    $"Binary scalar length cannot exceed {_maxScalarLength} bytes.");
            CheckReaderIndex(count);
            string value;
            try
            {
                value = Utf8.GetString(_buffer, _index, count);
            }
            catch (DecoderFallbackException exception)
            {
                throw new FormatException("Binary scalar contains invalid UTF-8 data.", exception);
            }
            _index += count;
            return value;
        }
        public List<T> ReadIEnumerable<T>(List<T> result, BuffConverter<T> converter)
        {
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (List<T>)GetExistingReference(header.ReferenceId, typeof(List<T>));

                if (result == null) throw new ArgumentNullException(nameof(result));
                if (converter == null) throw new ArgumentNullException(nameof(converter));
                DefineCollectionReference(header.ReferenceId, result, typeof(List<T>));
                List<T> values = result;
                int requiredCapacity = checked(values.Count + header.Count);
                if (values.Capacity < requiredCapacity)
                    values.Capacity = requiredCapacity;
                for (int i = 0; i < header.Count; i++)
                    values.Add(ReadPrecounted(converter));
                return values;
            }
            finally
            {
                ExitNode();
            }
        }

        public List<T> ReadList<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (List<T>)GetExistingReference(header.ReferenceId, typeof(List<T>));
                var result = new List<T>(header.Count);
                DefineCollectionReference(header.ReferenceId, result, typeof(List<T>));
                for (int i = 0; i < header.Count; i++) result.Add(ReadPrecounted(converter));
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        TCollection ICollectionReader.ReadCollection<TCollection, T>(
            BuffConverter<T> converter, CollectionReadMode mode)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (TCollection)GetExistingReference(
                        header.ReferenceId, typeof(TCollection));
                var result = CollectionFactory<TCollection>.Create(header.Count);
                DefineCollectionReference(header.ReferenceId, result, typeof(TCollection));
                if (mode != CollectionReadMode.Stack)
                {
                    for (int i = 0; i < header.Count; i++)
                        CollectionPopulator<TCollection, T>.Add(
                            result, ReadPrecounted(converter), mode);
                    return result;
                }

                var values = ClassPool.GetList<T>(header.Count);
                try
                {
                    for (int i = 0; i < header.Count; i++)
                        values.Add(ReadPrecounted(converter));
                    for (int i = values.Count - 1; i >= 0; i--)
                        CollectionPopulator<TCollection, T>.Add(
                            result, values[i], mode);
                }
                finally
                {
                    ClassPool.BackList(values);
                }
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        TCollection ICollectionReader.ReadArrayList<TCollection>(
            BuffConverter<object> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (TCollection)GetExistingReference(
                        header.ReferenceId, typeof(TCollection));
                var result = CollectionFactory<TCollection>.Create(header.Count);
                DefineCollectionReference(header.ReferenceId, result, typeof(TCollection));
                for (int i = 0; i < header.Count; i++)
                    result.Add(ReadPrecounted(converter));
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        TCollection ICollectionReader.ReadHashtable<TCollection>(
            BuffConverter<KeyValuePair<object, object>> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (TCollection)GetExistingReference(
                        header.ReferenceId, typeof(TCollection));
                var result = CollectionFactory<TCollection>.Create(header.Count);
                DefineCollectionReference(header.ReferenceId, result, typeof(TCollection));
                for (int i = 0; i < header.Count; i++)
                {
                    var item = ReadPrecounted(converter);
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
            finally
            {
                ExitNode();
            }
        }

        public T[] ReadArray<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (T[])GetExistingReference(header.ReferenceId, typeof(T[]));
                var result = new T[header.Count];
                DefineCollectionReference(header.ReferenceId, result, typeof(T[]));
                for (int i = 0; i < header.Count; i++) result[i] = ReadPrecounted(converter);
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public Array ReadMultiDimensionalArray<T>(int rank, BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (rank < 2 || rank > 5) throw new ArgumentOutOfRangeException(nameof(rank));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader(false);
                var arrayType = typeof(T).MakeArrayType(rank);
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (Array)GetExistingReference(header.ReferenceId, arrayType);
                ushort firstLength = ReadUInt16();
                if (firstLength == ushort.MaxValue)
                {
                    if (_collectionReferences)
                        throw new FormatException(
                            $"Array dimensions cannot exceed {ushort.MaxValue - 1}.");
                    return null;
                }
                int length1 = ReadArrayDimension();
                int length2 = rank > 2 ? ReadArrayDimension() : 0;
                int length3 = rank > 3 ? ReadArrayDimension() : 0;
                int length4 = rank > 4 ? ReadArrayDimension() : 0;
                var shape = new BufferScan.ArrayShape(rank, firstLength, length1, length2,
                    length3, length4);
                bool hasZeroLength = false;
                for (int dimension = 0; dimension < rank; dimension++)
                    hasZeroLength |= shape.GetLength(dimension) == 0;
                int maxCollectionCount = _maxCollectionCount;
                long longCount = hasZeroLength ? 0 : 1;
                if (!hasZeroLength)
                {
                    for (int dimension = 0; dimension < rank; dimension++)
                    {
                        int length = shape.GetLength(dimension);
                        if (longCount > maxCollectionCount / length)
                            throw new FormatException(
                                $"Collection count cannot exceed {maxCollectionCount}.");
                        longCount *= length;
                    }
                }
                int count = (int)longCount;
                if (header.Count >= 0 && header.Count != count)
                    throw new FormatException(
                        $"Array dimensions require {count} values but found {header.Count}.");
                CountNodes(checked(rank + 2 + count));

                var result = MultiDimensionalArrayHelper.Create<T>(shape);
                DefineCollectionReference(header.ReferenceId, result, arrayType);
                for (int index = 0; index < count; index++)
                    MultiDimensionalArrayHelper.SetValue(result, shape, index,
                        ReadPrecounted(converter));
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        private int ReadArrayDimension()
        {
            ushort value = ReadUInt16();
            if (value == ushort.MaxValue)
                throw new FormatException(
                    $"Array dimensions cannot exceed {ushort.MaxValue - 1}.");
            return value;
        }

        public HashSet<T> ReadHashSet<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (HashSet<T>)GetExistingReference(header.ReferenceId,
                        typeof(HashSet<T>));
                var result = new HashSet<T>();
                DefineCollectionReference(header.ReferenceId, result, typeof(HashSet<T>));
                for (int i = 0; i < header.Count; i++)
                    if (!result.Add(ReadPrecounted(converter)))
                        throw new FormatException("Duplicate set value.");
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public Queue<T> ReadQueue<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (Queue<T>)GetExistingReference(header.ReferenceId, typeof(Queue<T>));
                var result = new Queue<T>(header.Count);
                DefineCollectionReference(header.ReferenceId, result, typeof(Queue<T>));
                for (int i = 0; i < header.Count; i++) result.Enqueue(ReadPrecounted(converter));
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public Stack<T> ReadStack<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (Stack<T>)GetExistingReference(header.ReferenceId, typeof(Stack<T>));
                var result = new Stack<T>(header.Count);
                DefineCollectionReference(header.ReferenceId, result, typeof(Stack<T>));
                var values = ClassPool.GetList<T>(header.Count);
                try
                {
                    for (int i = 0; i < header.Count; i++)
                        values.Add(ReadPrecounted(converter));
                    for (int i = values.Count - 1; i >= 0; i--)
                        result.Push(values[i]);
                }
                finally
                {
                    ClassPool.BackList(values);
                }
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            BuffConverter<KeyValuePair<TKey, TValue>> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (Dictionary<TKey, TValue>)GetExistingReference(header.ReferenceId,
                        typeof(Dictionary<TKey, TValue>));
                var result = new Dictionary<TKey, TValue>(header.Count);
                DefineCollectionReference(header.ReferenceId, result,
                    typeof(Dictionary<TKey, TValue>));
                for (int i = 0; i < header.Count; i++)
                {
                    var item = ReadPrecounted(converter);
                    result.Add(item.Key, item.Value);
                }
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public ConcurrentDictionary<TKey, TValue> ReadConcurrentDictionary<TKey, TValue>(
            BuffConverter<KeyValuePair<TKey, TValue>> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var header = ReadCollectionHeader();
                if (header.IsNull) return null;
                if (header.IsReference)
                    return (ConcurrentDictionary<TKey, TValue>)GetExistingReference(
                        header.ReferenceId, typeof(ConcurrentDictionary<TKey, TValue>));
                var result = new ConcurrentDictionary<TKey, TValue>();
                DefineCollectionReference(header.ReferenceId, result,
                    typeof(ConcurrentDictionary<TKey, TValue>));
                for (int i = 0; i < header.Count; i++)
                {
                    var item = ReadPrecounted(converter);
                    if (!result.TryAdd(item.Key, item.Value))
                        throw new FormatException($"Duplicate dictionary key '{item.Key}'.");
                }
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        private readonly struct CollectionHeader
        {
            internal readonly int Count;
            internal readonly int ReferenceId;
            internal readonly bool IsNull;
            internal readonly bool IsReference;

            internal CollectionHeader(int count, int referenceId, bool isNull,
                bool isReference)
            {
                Count = count;
                ReferenceId = referenceId;
                IsNull = isNull;
                IsReference = isReference;
            }
        }

        private CollectionHeader ReadCollectionHeader(bool readCount = true)
        {
            if (!_suppressNodeCounting && metas == null &&
                HasCollectionReferenceMetadata())
                EnsureMetas();
            int referenceId = -1;
            if (_collectionReferences)
            {
                byte kind = ReadByte();
                if (kind == 0) return new CollectionHeader(0, -1, true, false);
                if (kind != 1 && kind != 2)
                    throw new FormatException($"Invalid collection reference marker '{kind}'.");
                referenceId = ReadInt32();
                if (referenceId < -1)
                    throw new FormatException($"Invalid collection reference id '{referenceId}'.");
                if (kind == 1)
                {
                    if (referenceId < 0)
                        throw new FormatException("A collection reference requires an id.");
                    return new CollectionHeader(0, referenceId, false, true);
                }
            }
            if (!readCount && !_collectionReferences)
                return new CollectionHeader(-1, referenceId, false, false);

            ushort encodedCount = ReadUInt16();
            if (!_collectionReferences && encodedCount == ushort.MaxValue)
                return new CollectionHeader(0, -1, true, false);
            int count = encodedCount;
            if (count > _maxCollectionCount)
                throw new FormatException(
                    $"Collection count cannot exceed {_maxCollectionCount}.");
            if (readCount) CountNodes(count);
            return new CollectionHeader(count, referenceId, false, false);
        }

        private bool HasCollectionReferenceMetadata()
        {
            const string marker = BufferScan.ReferenceMetadata;
            if (_buffer == null || _index < 0 || _limit - _index < marker.Length + 6)
                return false;
            int metaCount = _buffer[_index] | _buffer[_index + 1] << 8;
            if (metaCount == 0) return false;
            int length = _buffer[_index + 2] |
                _buffer[_index + 3] << 8 |
                _buffer[_index + 4] << 16 |
                _buffer[_index + 5] << 24;
            if (length != marker.Length) return false;
            for (int i = 0; i < marker.Length; i++)
                if (_buffer[_index + 6 + i] != marker[i]) return false;
            return true;
        }

        public T? ReadNullable<T>(BuffConverter<T> converter) where T : struct
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (!ReadBool()) return null;
            CountNodes(1);
            return ReadPrecounted(converter);
        }

        public KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>(
            BuffConverter<TKey> keyConverter, BuffConverter<TValue> valueConverter)
        {
            if (keyConverter == null) throw new ArgumentNullException(nameof(keyConverter));
            if (valueConverter == null) throw new ArgumentNullException(nameof(valueConverter));
            CountNodes(2);
            return new KeyValuePair<TKey, TValue>(ReadPrecounted(keyConverter),
                ReadPrecounted(valueConverter));
        }
        private List<string> metas;

        private void EnsureMetas()
        {
            if (metas != null) return;

            metas = ClassPool.GetList<string>();
            var previousSuppression = _suppressNodeCounting;
            _suppressNodeCounting = true;
            List<string> values;
            try
            {
                values = ReadIEnumerable(metas, MetadataConverter);
            }
            finally
            {
                _suppressNodeCounting = previousSuppression;
            }
            if (values == null)
                throw new FormatException("The binary metadata table cannot be null.");
            for (int i = 0; i < metas.Count; i++)
            {
                if (metas[i] == null)
                    throw new FormatException("The binary metadata table cannot contain null values.");
            }
            _supportReferences = metas.Count > 0 &&
                (metas[0] == BufferScan.ReferenceMetadata ||
                 metas[0] == BufferScan.LegacyReferenceMetadata);
            _collectionReferences = metas.Count > 0 &&
                metas[0] == BufferScan.ReferenceMetadata;
        }

        private string ReadMeta()
        {
            int metaIndex = ReadInt32();
            if (metaIndex < 0 || metaIndex >= metas.Count)
                throw new FormatException($"Invalid binary metadata index '{metaIndex}'.");
            return metas[metaIndex];
        }

        private int ReadEndIndex(string name)
        {
            int end = ReadInt32();
            if (end < _index || end > _limit)
                throw new FormatException($"Invalid binary {name} end index '{end}'.");
            return end;
        }

        private void ReadFields(object instance, TypeHelper.TypeFields fields, int objectEnd)
        {
            int parentLimit = _limit;
            int fieldCount = 0;
            var previousObject = _currentObject;
            var presentFields = ClassPool.GetHashSet<TypeHelper.TypeFields.Field>();
            _currentObject = instance;
            _limit = objectEnd;
            try
            {
                while (_index < objectEnd)
                {
                    if (++fieldCount > _maxObjectFieldCount)
                        throw new FormatException(
                            $"Binary object field count cannot exceed {_maxObjectFieldCount}.");
                    CountNodes(1);
                    if (objectEnd - _index < 12)
                        throw new FormatException("Incomplete binary field header.");

                    int fieldEnd = ReadInt32();
                    string fieldName = ReadMeta();
                    string serializedTypeName = BuffSerializer.GetSerializedTypeName(ReadMeta());
                    if (fieldEnd < _index || fieldEnd > objectEnd)
                        throw new FormatException($"Invalid binary field end index '{fieldEnd}'.");

                    var field = fields.FindField(fieldName);
                    if (field == null)
                    {
                        _index = fieldEnd;
                        continue;
                    }
                    if (!presentFields.Add(field))
                        throw new FormatException(
                            $"Binary object contains duplicate field '{fieldName}'.");
                    if (field.FieldType.FullName != serializedTypeName)
                        throw new FormatException(
                            $"Binary field '{fieldName}' changed type from '{serializedTypeName}' to '{field.FieldType.FullName}'.");

                    int objectLimit = _limit;
                    _limit = fieldEnd;
                    try
                    {
                        var fieldType = field.FieldType;
                        var convert = ConverterResolver.Get(field.FieldType, _settings);
                        _precountedReadDepth++;
                        try
                        {
                            field.ReadAndSet(this, instance, convert);
                        }
                        finally
                        {
                            _precountedReadDepth--;
                        }
                        if (_index != fieldEnd)
                            throw new FormatException(
                                $"Binary field '{fieldName}' consumed {_index} bytes but ends at {fieldEnd}.");
                    }
                    finally
                    {
                        _limit = objectLimit;
                    }
                }
                fields.SetMissingDefaultValues(instance, presentFields);
            }
            finally
            {
                _currentObject = previousObject;
                _limit = parentLimit;
                ClassPool.BackHashSet(presentFields);
            }
        }

        public T ReadObject<T>()
        {
            EnterNode();
            _objectReadDepth++;
            try
            {
                var result = ReadNewObject<T>();
                if (_objectReadDepth == 1 && !_deferCallbacks)
                {
                    EnsureReferencesResolved();
                    InvokeAfterReadCallbacks();
                }
                return result;
            }
            finally
            {
                _objectReadDepth--;
                if (_objectReadDepth == 0 && !_deferCallbacks)
                    _afterReadCallbacks.Clear();
                ExitNode();
            }
        }

        bool IPolymorphicReader.TryReadPolymorphic(Type declaredType, out object value)
        {
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
            EnsureMetas();
            int payloadStart = _index;
            int typeIndex = ReadInt32();
            if (typeIndex < 0)
            {
                _index = payloadStart;
                value = null;
                return false;
            }
            if (typeIndex >= metas.Count)
                throw new FormatException($"Invalid binary metadata index '{typeIndex}'.");

            string typeName = metas[typeIndex];
            string assemblyName = ReadMeta();
            var actualType = BuffSerializer.ResolveSerializedType(
                declaredType, typeName, assemblyName, _settings);
            var converter = ConverterResolver.Get(actualType, _settings);
            if (converter.UsesObjectLayout)
            {
                _index = payloadStart;
                value = null;
                return false;
            }
            value = converter.Read(this, actualType);
            return true;
        }

        private T ReadNewObject<T>()
        {
            EnsureMetas();

            int typeIndex = ReadInt32();
            if (typeIndex == -1) return default;
            if (typeIndex == -2)
            {
                if (!_supportReferences)
                    throw new FormatException("Object reference marker requires reference metadata.");
                return (T)GetExistingReference(ReadInt32(), typeof(T));
            }
            if (typeIndex < 0 || typeIndex >= metas.Count)
                throw new FormatException($"Invalid binary metadata index '{typeIndex}'.");

            string typeName = metas[typeIndex];
            string assemblyName = ReadMeta();
            Type type = BuffSerializer.ResolveSerializedType(
                typeof(T), typeName, assemblyName, _settings);
            int objectEnd = ReadEndIndex("object");

            int referenceId = _supportReferences ? ReadInt32() : -1;
            object t = referenceId >= 0
                ? GetOrCreateReference(referenceId, type, true)
                : TypeHelper.CreateInstance(type);
            var typeField = TypeHelper.GetTypeFields(type);
            ReadFields(t, typeField, objectEnd);
            _index = objectEnd;
            if (t is IBufferObject callback)
                _afterReadCallbacks.Add(callback);

            return (T)t;
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

        public void EnsureFullyConsumed()
        {
            if (_buffer == null)
                throw new InvalidOperationException("The reader has not been initialized.");
            if (_index != _buffer.Length)
                throw new FormatException($"Binary data contains {_buffer.Length - _index} trailing bytes.");
        }

        private void EnterNode()
        {
            int maxDepth = _maxDepth;
            if (_depth >= maxDepth)
                throw new FormatException($"Binary serialization depth cannot exceed {maxDepth}.");
            if (_precountedReadDepth == 0)
                CountNodes(1);
            _depth++;
        }

        private void CountNodes(int count)
        {
            if (_suppressNodeCounting) return;
            if (count < 0 || _nodeCount > _maxNodeCount - count)
                throw new FormatException(
                    $"Binary node count cannot exceed {_maxNodeCount}.");
            _nodeCount += count;
        }

        private T ReadPrecounted<T>(BuffConverter<T> converter)
        {
            _precountedReadDepth++;
            try
            {
                return converter.ReadValue(this, typeof(T));
            }
            finally
            {
                _precountedReadDepth--;
            }
        }

        private void InvokeAfterReadCallbacks()
        {
            for (int i = 0; i < _afterReadCallbacks.Count; i++)
                _afterReadCallbacks[i].AfterReadBuffer();
        }

        void IReferenceResolver.EnsureReferencesResolved() => EnsureReferencesResolved();

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

        private void ExitNode()
        {
            _depth--;
        }


    }
}
