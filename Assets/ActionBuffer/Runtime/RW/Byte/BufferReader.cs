using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
namespace ActionBuffer
{
    public class BufferReader : IBufferReader, IObjectContextReader
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly Func<IBufferReader, string> ReadMetadataValue = ReadMetadata;
        private byte[] _buffer;
        private int _index = 0;
        private int _depth;
        private int _nodeCount;
        private int _limit;
        private int _precountedReadDepth;
        private bool _suppressNodeCounting;
        private int _objectReadDepth;
        private object _currentObject;
        private sealed class ReferenceEntry
        {
            internal object Value;
            internal Type Type;
            internal bool Defined;
        }
        private readonly Dictionary<int, ReferenceEntry> _references =
            new Dictionary<int, ReferenceEntry>();
        private bool _supportReferences;
        private readonly List<IBufferObject> _afterReadCallbacks = new List<IBufferObject>();
        object IObjectContextReader.CurrentObject => _currentObject;
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
        public void Init(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length > BufferSerializer.MaxBinaryLength)
                throw new FormatException(
                    $"Binary data length cannot exceed {BufferSerializer.MaxBinaryLength} bytes.");
            Clear();
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
            _supportReferences = false;
            _references.Clear();
            _afterReadCallbacks.Clear();
            if (_afterReadCallbacks.Capacity > BufferSerializer.RetainedListCapacity)
                _afterReadCallbacks.Capacity = 0;
            if (metas == null) return;
            metas.Clear();
            ListPool<string>.Back(metas);
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
            if (count > BufferSerializer.MaxScalarLength)
                throw new FormatException(
                    $"Binary scalar length cannot exceed {BufferSerializer.MaxScalarLength} bytes.");
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
        public List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read)
        {
            EnterNode();
            try
            {
                if (!TryReadCollectionCount(out int count))
                    return null;

                if (result == null) throw new ArgumentNullException(nameof(result));
                if (read == null) throw new ArgumentNullException(nameof(read));
                List<T> values = result;
                int requiredCapacity = checked(values.Count + count);
                if (values.Capacity < requiredCapacity)
                    values.Capacity = requiredCapacity;
                for (int i = 0; i < count; i++)
                    values.Add(ReadPrecounted(read));
                return values;
            }
            finally
            {
                ExitNode();
            }
        }

        public List<T> ReadList<T>(Func<IBufferReader, T> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            EnterNode();
            try
            {
                if (!TryReadCollectionCount(out int count)) return null;
                var result = new List<T>(count);
                for (int i = 0; i < count; i++) result.Add(ReadPrecounted(read));
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public T[] ReadArray<T>(Func<IBufferReader, T> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            EnterNode();
            try
            {
                if (!TryReadCollectionCount(out int count)) return null;
                var result = new T[count];
                for (int i = 0; i < count; i++) result[i] = ReadPrecounted(read);
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public Array ReadMultiDimensionalArray<T>(int rank, Func<IBufferReader, T> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            if (rank < 2 || rank > 5) throw new ArgumentOutOfRangeException(nameof(rank));
            EnterNode();
            try
            {
                ushort firstLength = ReadUInt16();
                if (firstLength == ushort.MaxValue) return null;
                int length1 = ReadArrayDimension();
                int length2 = rank > 2 ? ReadArrayDimension() : 0;
                int length3 = rank > 3 ? ReadArrayDimension() : 0;
                int length4 = rank > 4 ? ReadArrayDimension() : 0;
                var shape = new BufferScan.ArrayShape(rank, firstLength, length1, length2,
                    length3, length4);
                bool hasZeroLength = false;
                for (int dimension = 0; dimension < rank; dimension++)
                    hasZeroLength |= shape.GetLength(dimension) == 0;
                int maxCollectionCount = BufferSerializerSettings.DefaultSetting.MaxCollectionCount;
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
                CountNodes(checked(rank + 2 + count));

                var result = MultiDimensionalArrayHelper.Create<T>(shape);
                for (int index = 0; index < count; index++)
                    MultiDimensionalArrayHelper.SetValue(result, shape, index,
                        ReadPrecounted(read));
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

        public HashSet<T> ReadHashSet<T>(Func<IBufferReader, T> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            EnterNode();
            try
            {
                if (!TryReadCollectionCount(out int count)) return null;
                var result = new HashSet<T>();
                for (int i = 0; i < count; i++) result.Add(ReadPrecounted(read));
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public Queue<T> ReadQueue<T>(Func<IBufferReader, T> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            EnterNode();
            try
            {
                if (!TryReadCollectionCount(out int count)) return null;
                var result = new Queue<T>(count);
                for (int i = 0; i < count; i++) result.Enqueue(ReadPrecounted(read));
                return result;
            }
            finally
            {
                ExitNode();
            }
        }

        public Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>(
            Func<IBufferReader, KeyValuePair<TKey, TValue>> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            EnterNode();
            try
            {
                if (!TryReadCollectionCount(out int count)) return null;
                var result = new Dictionary<TKey, TValue>(count);
                for (int i = 0; i < count; i++)
                {
                    var item = ReadPrecounted(read);
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
            Func<IBufferReader, KeyValuePair<TKey, TValue>> read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            EnterNode();
            try
            {
                if (!TryReadCollectionCount(out int count)) return null;
                var result = new ConcurrentDictionary<TKey, TValue>();
                for (int i = 0; i < count; i++)
                {
                    var item = ReadPrecounted(read);
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

        private bool TryReadCollectionCount(out int count)
        {
            ushort encodedCount = ReadUInt16();
            if (encodedCount == ushort.MaxValue)
            {
                count = 0;
                return false;
            }
            count = encodedCount;
            if (count > BufferSerializer.MaxCollectionCount)
                throw new FormatException(
                    $"Collection count cannot exceed {BufferSerializer.MaxCollectionCount}.");
            CountNodes(count);
            return true;
        }

        public T? ReadNullable<T>(Func<IBufferReader, T> read) where T : struct
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            if (!ReadBool()) return null;
            CountNodes(1);
            return ReadPrecounted(read);
        }

        public KeyValuePair<TKey, TValue> ReadKeyValuePair<TKey, TValue>(
            Func<IBufferReader, TKey> readKey, Func<IBufferReader, TValue> readValue)
        {
            if (readKey == null) throw new ArgumentNullException(nameof(readKey));
            if (readValue == null) throw new ArgumentNullException(nameof(readValue));
            CountNodes(2);
            return new KeyValuePair<TKey, TValue>(ReadPrecounted(readKey), ReadPrecounted(readValue));
        }
        private List<string> metas;

        private void EnsureMetas()
        {
            if (metas != null) return;

            metas = ListPool<string>.Get();
            var previousSuppression = _suppressNodeCounting;
            _suppressNodeCounting = true;
            List<string> values;
            try
            {
                values = ReadIEnumerable(metas, ReadMetadataValue);
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
                if (metas[i] == BufferScan.ReferenceMetadata)
                    _supportReferences = true;
            }
        }

        private static string ReadMetadata(IBufferReader reader) => reader.ReadUTF8();

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
            _currentObject = instance;
            fields.SetDefaultValues(instance);
            _limit = objectEnd;
            try
            {
                while (_index < objectEnd)
                {
                    if (++fieldCount > BufferSerializer.MaxObjectFieldCount)
                        throw new FormatException(
                            $"Binary object field count cannot exceed {BufferSerializer.MaxObjectFieldCount}.");
                    CountNodes(1);
                    if (objectEnd - _index < 12)
                        throw new FormatException("Incomplete binary field header.");

                    int fieldEnd = ReadInt32();
                    string fieldName = ReadMeta();
                    string serializedTypeName = TypeHelper.GetRealTypeName(ReadMeta());
                    if (fieldEnd < _index || fieldEnd > objectEnd)
                        throw new FormatException($"Invalid binary field end index '{fieldEnd}'.");

                    var field = fields.FindField(fieldName);
                    if (field == null)
                    {
                        _index = fieldEnd;
                        continue;
                    }
                    if (field.FieldType.FullName != serializedTypeName)
                        throw new FormatException(
                            $"Binary field '{fieldName}' changed type from '{serializedTypeName}' to '{field.FieldType.FullName}'.");

                    int objectLimit = _limit;
                    _limit = fieldEnd;
                    try
                    {
                        var fieldType = field.FieldType;
                        var convert = field.GetConverter(BufferSerializerSettings.DefaultSetting);
                        _precountedReadDepth++;
                        try
                        {
                            field.SetValue(instance, convert.Read(this, fieldType));
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
            }
            finally
            {
                _currentObject = previousObject;
                _limit = parentLimit;
            }
        }

        public T ReadObject<T>()
        {
            EnterNode();
            _objectReadDepth++;
            try
            {
                var result = ReadNewObject<T>();
                if (_objectReadDepth == 1)
                    InvokeAfterReadCallbacks();
                return result;
            }
            finally
            {
                _objectReadDepth--;
                if (_objectReadDepth == 0)
                    _afterReadCallbacks.Clear();
                ExitNode();
            }
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
            Type type = TypeHelper.ResolveSerializedType(typeof(T), typeName, assemblyName);
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
            int maxDepth = BufferSerializerSettings.DefaultSetting.MaxDepth;
            if (_depth >= maxDepth)
                throw new FormatException($"Binary serialization depth cannot exceed {maxDepth}.");
            if (_precountedReadDepth == 0)
                CountNodes(1);
            _depth++;
        }

        private void CountNodes(int count)
        {
            if (_suppressNodeCounting) return;
            if (count < 0 || _nodeCount > BufferSerializer.MaxNodeCount - count)
                throw new FormatException(
                    $"Binary node count cannot exceed {BufferSerializer.MaxNodeCount}.");
            _nodeCount += count;
        }

        private T ReadPrecounted<T>(Func<IBufferReader, T> read)
        {
            _precountedReadDepth++;
            try
            {
                return read(this);
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

        private void ExitNode()
        {
            _depth--;
        }


    }
}
