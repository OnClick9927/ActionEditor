using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ActionBuffer
{
    public sealed class BufferScan : IDisposable
    {
        internal const string LegacyReferenceMetadata = "$ActionBuffer.Reference.v1";
        internal const string ReferenceMetadata = "$ActionBuffer.Reference.v2";
        internal int MaxDepth { get; private set; }
        internal int MaxTextLength { get; private set; }
        internal int MaxCollectionCount { get; private set; }
        internal int MaxObjectFieldCount { get; private set; }
        internal int MaxScalarLength { get; private set; }
        internal int MaxNodeCount { get; private set; }
        internal bool SupportReferences { get; private set; }
        internal bool TypeInfo { get; private set; }
        internal bool PrettyPrint { get; private set; }
        internal bool DeterministicCollectionOrder { get; private set; }
        private bool _invokeBeforeWriteCallbacks;
        private bool _serializeEvents;

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }

        internal readonly struct CachedField
        {
            public TypeHelper.TypeFields.Field Field { get; }
            private readonly IFieldValueCache _values;
            private readonly BuffConverter _converter;
            private readonly int _index;

            internal CachedField(TypeHelper.TypeFields.Field field, IFieldValueCache values,
                BuffConverter converter, int index)
            {
                Field = field;
                _values = values;
                _converter = converter;
                _index = index;
            }

            internal void Scan(BufferScan scan) => _values.Scan(scan, _index, _converter);
            internal void Write(IBufferWriter writer, BufferScan scan) =>
                _values.Write(writer, scan, _index, _converter);
        }

        internal interface IFieldValueCache
        {
            void Scan(BufferScan scan, int index, BuffConverter converter);
            void Write(IBufferWriter writer, BufferScan scan, int index,
                BuffConverter converter);
            void Clear();
        }

        private sealed class FieldValueCache<T> : IFieldValueCache
        {
            private readonly List<T> _values = new List<T>();

            internal CachedField Add(TypeHelper.TypeFields.Field field, BuffConverter converter,
                T value)
            {
                int index = _values.Count;
                _values.Add(value);
                return new CachedField(field, this, converter, index);
            }

            public void Scan(BufferScan scan, int index, BuffConverter converter) =>
                RequireConverter(converter).ScanValue(scan, _values[index]);

            public void Write(IBufferWriter writer, BufferScan scan, int index,
                BuffConverter converter) =>
                RequireConverter(converter).WriteValue(writer, scan, _values[index]);

            public void Clear()
            {
                _values.Clear();
                if (_values.Capacity > BuffSettings.RetainedListCapacity)
                    _values.Capacity = 0;
            }

            private static BuffConverter<T> RequireConverter(BuffConverter converter)
            {
                if (converter is BuffConverter<T> typed) return typed;
                throw new InvalidOperationException(
                    $"Converter '{converter?.GetType()}' cannot serialize field type '{typeof(T)}'.");
            }
        }

        private sealed class BoxedFieldValueCache : IFieldValueCache
        {
            private readonly List<object> _values = new List<object>();

            internal CachedField Add(TypeHelper.TypeFields.Field field, BuffConverter converter,
                object value)
            {
                int index = _values.Count;
                _values.Add(value);
                return new CachedField(field, this, converter, index);
            }

            public void Scan(BufferScan scan, int index, BuffConverter converter) =>
                converter.Scan(scan, _values[index]);

            public void Write(IBufferWriter writer, BufferScan scan, int index,
                BuffConverter converter) => converter.Write(writer, scan, _values[index]);

            public void Clear()
            {
                _values.Clear();
                if (_values.Capacity > BuffSettings.RetainedListCapacity)
                    _values.Capacity = 0;
            }
        }

        internal struct CachedObject
        {
            public object Value { get; private set; }
            public Type Type { get; private set; }
            public int ReferenceId { get; private set; }
            public bool IsReference { get; private set; }
            internal int FieldStart { get; private set; }

            public int FieldCount { get; private set; }

            public CachedObject(object value, Type type, int referenceId = -1,
                bool isReference = false)
            {
                Value = value;
                Type = type;
                ReferenceId = referenceId;
                IsReference = isReference;
                FieldStart = 0;
                FieldCount = 0;
            }

            public void SetFieldRange(int start, int count)
            {
                FieldStart = start;
                FieldCount = count;
            }
        }

        internal readonly struct ArrayShape
        {
            internal int Rank { get; }
            internal int Length0 { get; }
            internal int Length1 { get; }
            internal int Length2 { get; }
            internal int Length3 { get; }
            internal int Length4 { get; }

            internal ArrayShape(int rank, int length0, int length1, int length2 = 0,
                int length3 = 0, int length4 = 0)
            {
                Rank = rank;
                Length0 = length0;
                Length1 = length1;
                Length2 = length2;
                Length3 = length3;
                Length4 = length4;
            }

            internal int GetLength(int dimension)
            {
                switch (dimension)
                {
                    case 0: return Length0;
                    case 1: return Length1;
                    case 2 when Rank > 2: return Length2;
                    case 3 when Rank > 3: return Length3;
                    case 4 when Rank > 4: return Length4;
                    default: throw new ArgumentOutOfRangeException(nameof(dimension));
                }
            }

        }

        internal struct CachedEnumerable
        {
            private ICachedEnumerableValues _values;
            private Type _elementType;
            private byte _rank;
            private int _length0;
            private int _length1;
            private int _length2;
            private int _length3;
            private int _length4;
            private int _referenceId;
            private bool _isReference;

            internal int ReferenceId => _referenceId;
            internal bool IsReference => _isReference;

            public static CachedEnumerable Capture<T>(IEnumerable<T> source, IComparer<T> comparer,
                int maxCollectionCount)
            {
                var cached = new CachedEnumerable
                {
                    _elementType = typeof(T),
                    _rank = 1,
                    _referenceId = -1
                };
                if (source == null) return cached;

                var collection = source as ICollection<T>;
                int knownCount = collection?.Count ?? 0;
                if (knownCount > maxCollectionCount)
                    throw new FormatException(
                        $"Collection count cannot exceed {maxCollectionCount}.");

                var values = ClassPool.GetList<T>(knownCount);
                var holder = ClassPool.Get<CachedEnumerableValues<T>>();
                holder.Values = values;
                cached._values = holder;
                try
                {
                    if (collection != null)
                    {
                        values.AddRange(collection);
                        if (values.Count > maxCollectionCount)
                            throw new FormatException(
                                $"Collection count cannot exceed {maxCollectionCount}.");
                    }
                    else
                    {
                        foreach (var value in source)
                        {
                            values.Add(value);
                            if (values.Count > maxCollectionCount)
                                throw new FormatException(
                                    $"Collection count cannot exceed {maxCollectionCount}.");
                        }
                    }
                    if (comparer != null)
                    {
                        try
                        {
                            values.Sort(comparer);
                            for (int i = 1; i < values.Count; i++)
                            {
                                if (comparer.Compare(values[i - 1], values[i]) == 0 &&
                                    !EqualityComparer<T>.Default.Equals(values[i - 1], values[i]))
                                    throw new NotSupportedException(
                                        $"Collection element type '{typeof(T)}' does not have a unique deterministic order.");
                            }
                        }
                        catch (InvalidOperationException exception)
                        {
                            throw new NotSupportedException(
                                $"Collection element type '{typeof(T)}' cannot be ordered deterministically.", exception);
                        }
                    }
                    return cached;
                }
                catch
                {
                    cached.Release();
                    throw;
                }
            }

            public List<T> GetValues<T>()
            {
                if (_elementType != typeof(T) || _rank != 1)
                    throw new InvalidOperationException("The enumerable scan cache contains an unexpected element type.");
                return _values == null ? null : ((CachedEnumerableValues<T>)_values).Values;
            }

            public static CachedEnumerable CaptureMultiDimensional<T>(Array source, int rank,
                int maxCollectionCount)
            {
                if (rank < 2 || rank > 5)
                    throw new NotSupportedException("Only arrays up to rank five are supported.");
                var cached = new CachedEnumerable
                {
                    _elementType = typeof(T),
                    _rank = (byte)rank,
                    _referenceId = -1
                };
                if (source == null) return cached;
                if (source.Rank != rank || source.GetType().GetElementType() != typeof(T))
                    throw new InvalidOperationException("The array rank or element type does not match its converter.");

                bool hasZeroLength = false;
                for (int dimension = 0; dimension < rank; dimension++)
                {
                    if (source.GetLowerBound(dimension) != 0)
                        throw new NotSupportedException("Only zero-based arrays are supported.");
                    int length = source.GetLength(dimension);
                    if (length >= ushort.MaxValue)
                        throw new FormatException(
                            $"Array dimensions cannot exceed {ushort.MaxValue - 1}.");
                    hasZeroLength |= length == 0;
                }
                long count = hasZeroLength ? 0 : 1;
                if (!hasZeroLength)
                {
                    for (int dimension = 0; dimension < rank; dimension++)
                    {
                        int length = source.GetLength(dimension);
                        if (count > maxCollectionCount / length)
                            throw new FormatException(
                                $"Collection count cannot exceed {maxCollectionCount}.");
                        count *= length;
                    }
                }

                var values = ClassPool.GetList<T>((int)count);
                var holder = ClassPool.Get<CachedEnumerableValues<T>>();
                holder.Values = values;
                cached._values = holder;
                cached._length0 = source.GetLength(0);
                cached._length1 = source.GetLength(1);
                if (rank > 2) cached._length2 = source.GetLength(2);
                if (rank > 3) cached._length3 = source.GetLength(3);
                if (rank > 4) cached._length4 = source.GetLength(4);
                try
                {
                    CaptureValues(source, rank, values);
                    return cached;
                }
                catch
                {
                    cached.Release();
                    throw;
                }
            }

            private static void CaptureValues<T>(Array source, int rank, List<T> values)
            {
                switch (rank)
                {
                    case 2:
                        var array2 = (T[,])source;
                        for (int i0 = 0; i0 < array2.GetLength(0); i0++)
                        for (int i1 = 0; i1 < array2.GetLength(1); i1++)
                            values.Add(array2[i0, i1]);
                        break;
                    case 3:
                        var array3 = (T[,,])source;
                        for (int i0 = 0; i0 < array3.GetLength(0); i0++)
                        for (int i1 = 0; i1 < array3.GetLength(1); i1++)
                        for (int i2 = 0; i2 < array3.GetLength(2); i2++)
                            values.Add(array3[i0, i1, i2]);
                        break;
                    case 4:
                        var array4 = (T[,,,])source;
                        for (int i0 = 0; i0 < array4.GetLength(0); i0++)
                        for (int i1 = 0; i1 < array4.GetLength(1); i1++)
                        for (int i2 = 0; i2 < array4.GetLength(2); i2++)
                        for (int i3 = 0; i3 < array4.GetLength(3); i3++)
                            values.Add(array4[i0, i1, i2, i3]);
                        break;
                    case 5:
                        var array5 = (T[,,,,])source;
                        for (int i0 = 0; i0 < array5.GetLength(0); i0++)
                        for (int i1 = 0; i1 < array5.GetLength(1); i1++)
                        for (int i2 = 0; i2 < array5.GetLength(2); i2++)
                        for (int i3 = 0; i3 < array5.GetLength(3); i3++)
                        for (int i4 = 0; i4 < array5.GetLength(4); i4++)
                            values.Add(array5[i0, i1, i2, i3, i4]);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(rank));
                }
            }

            public List<T> GetMultiDimensionalValues<T>(int rank, out ArrayShape shape)
            {
                if (_elementType != typeof(T) || _rank != rank)
                    throw new InvalidOperationException(
                        "The array scan cache contains an unexpected element type or rank.");
                shape = new ArrayShape(_rank, _length0, _length1, _length2, _length3,
                    _length4);
                return _values == null ? null : ((CachedEnumerableValues<T>)_values).Values;
            }

            public void Release()
            {
                var values = _values;
                this = default;
                values?.Release();
            }

            internal void SetReference(int referenceId, bool isReference)
            {
                _referenceId = referenceId;
                _isReference = isReference;
            }
        }

        private interface ICachedEnumerableValues
        {
            void Release();
        }

        private sealed class CachedEnumerableValues<T> : ICachedEnumerableValues
        {
            internal List<T> Values;

            public void Release()
            {
                var values = Values;
                Values = null;
                ClassPool.BackList(values);
                ClassPool.Back(this);
            }
        }

        private struct ReferenceEntry
        {
            public int Id;
            public bool Defined;
            public bool RequiredByDelegate;
        }

        private readonly List<CachedObject> _objects = new();
        private readonly List<CachedField> _fields = new();
        private readonly List<CachedEnumerable> _enumerables = new();
        private readonly Dictionary<Type, IFieldValueCache> _fieldValueCaches = new();
        private readonly BoxedFieldValueCache _boxedFieldValues = new BoxedFieldValueCache();
        private Dictionary<string, int> _metaMap = new();
        private readonly List<string> _metas = new();
        private HashSet<object> _activeReferences = new HashSet<object>(ReferenceComparer.Instance);
        private Dictionary<object, ReferenceEntry> _references =
            new Dictionary<object, ReferenceEntry>(ReferenceComparer.Instance);
        private int _objectIndex;
        private int _enumerableIndex;
        private int _scanDepth;
        private int _nodeCount;
        private bool _collectMeta;
        private bool _fullField;
        private object _currentObject;

        internal object CurrentObject => _currentObject;
        internal BuffSettings Settings { get; private set; }
        public BufferScan()
        {
            Prepare(null, true, false);
        }

        public BufferScan(bool collectMeta, bool fullField)
        {
            Prepare(null, collectMeta, fullField);
        }

        internal static BufferScan Rent(BuffSettings settings, bool collectMeta, bool fullField)
        {
            var result = ClassPool.Get<BufferScan>();
            result.Prepare(settings, collectMeta, fullField);
            return result;
        }

        internal static void Back(BufferScan value)
        {
            if (value == null) return;
            value.Clear();
            ClassPool.Back(value);
        }

        private void Prepare(BuffSettings settings, bool collectMeta, bool fullField)
        {
            Clear();
            settings ??= BuffSettings.DefaultSetting;
            Settings = settings;
            MaxDepth = BuffSettings.MaxDepth;
            MaxTextLength = BuffSettings.MaxTextLength;
            MaxCollectionCount = BuffSettings.MaxCollectionCount;
            MaxObjectFieldCount = BuffSettings.MaxObjectFieldCount;
            MaxScalarLength = BuffSettings.MaxScalarLength;
            MaxNodeCount = BuffSettings.MaxNodeCount;
            SupportReferences = settings.SupportReferences;
            TypeInfo = settings.TypeInfo;
            PrettyPrint = settings.PrettyPrint;
            DeterministicCollectionOrder = settings.DeterministicCollectionOrder;
            _invokeBeforeWriteCallbacks = settings.InvokeBeforeWriteCallbacks;
            _serializeEvents = settings.SerializeEvents;
            _collectMeta = collectMeta;
            _fullField = fullField;
            if (SupportReferences)
                AddMeta(ReferenceMetadata);
        }

        public void ScanObject<T>(T value)
        {
            var fields = value == null ? null : TypeHelper.GetTypeFields(value.GetType());
            ScanObject(value, fields);
        }

        public void ScanObject<T>(T value, TypeHelper.TypeFields fields)
        {
            EnterNode();
            object objectValue = value;
            bool tracked = false;
            try
            {
                var type = objectValue?.GetType();
                int referenceId = -1;
                if (objectValue != null && !type.IsValueType &&
                    SupportReferences)
                {
                    if (_references.TryGetValue(objectValue, out var reference))
                    {
                        referenceId = reference.Id;
                        if (reference.Defined)
                        {
                            _objects.Add(new CachedObject(objectValue, type, referenceId, true));
                            return;
                        }
                        reference.Defined = true;
                        _references[objectValue] = reference;
                    }
                    else
                    {
                        referenceId = _references.Count;
                        _references.Add(objectValue, new ReferenceEntry
                        {
                            Id = referenceId,
                            Defined = true
                        });
                    }
                }
                if (objectValue != null && !type.IsValueType)
                {
                    if (!_activeReferences.Add(objectValue))
                        throw new InvalidOperationException(
                            $"Circular reference detected for object type '{type}'.");
                    tracked = true;
                }

                if (_invokeBeforeWriteCallbacks && objectValue is IBufferObject bufferObject)
                    bufferObject.BeforeWriteBuffer();

                if (type != null && type != typeof(T))
                {
                    if (!typeof(T).IsAssignableFrom(type))
                        throw new NotSupportedException($"'{type}' is not assignable to '{typeof(T)}'.");
                    if (!ConverterResolver.Get(type, Settings).UsesObjectLayout)
                        throw new NotSupportedException(
                            $"A field declared as '{typeof(T)}' cannot serialize runtime value type '{type}'. " +
                            "Declare the field with its concrete serializable type.");
                }
                var cachedObject = new CachedObject(objectValue, type, referenceId);
                int cachedObjectIndex = _objects.Count;
                _objects.Add(cachedObject);
                if (objectValue == null) return;

                AddMeta(type.FullName);
                AddMeta(type.Assembly.FullName);
                var objectFields = fields.GetFields();
                var previousObject = _currentObject;
                _currentObject = objectValue;
                try
                {
                    int fieldStart = _fields.Count;
                    for (int i = 0; i < objectFields.Count; i++)
                    {
                        var field = objectFields[i];
                        if (field.IsEvent && !_serializeEvents) continue;
                        int fieldCount = _fields.Count - fieldStart;
                        if (fieldCount >= MaxObjectFieldCount)
                            throw new FormatException(
                                $"Object field count cannot exceed {MaxObjectFieldCount}.");
                        var converter = ConverterResolver.Get(field.FieldType, Settings);
                        if (!field.Capture(this, objectValue, converter, _fullField,
                                out var cachedField))
                            continue;
                        _fields.Add(cachedField);
                        AddMeta(field.name);
                        AddMeta(BuffSerializer.GetSerializedTypeName(field.FieldType));
                    }
                    cachedObject.SetFieldRange(fieldStart, _fields.Count - fieldStart);
                    _objects[cachedObjectIndex] = cachedObject;
                    for (int i = 0; i < cachedObject.FieldCount; i++)
                    {
                        var cachedField = _fields[cachedObject.FieldStart + i];
                        cachedField.Scan(this);
                    }
                }
                finally
                {
                    _currentObject = previousObject;
                }
            }
            finally
            {
                if (tracked) _activeReferences.Remove(objectValue);
                ExitNode();
            }
        }

        public void ScanEnumerable<T>(IEnumerable<T> values, BuffConverter<T> converter,
            IComparer<T> comparer = null, bool trackReference = true)
        {
            EnterNode();
            bool tracked = false;
            try
            {
                int referenceId = -1;
                bool isReference = false;
                if (trackReference && values != null && !values.GetType().IsValueType &&
                    SupportReferences)
                {
                    isReference = TryDefineReference(values, out referenceId);
                    if (isReference)
                    {
                        var referenceCache = CachedEnumerable.Capture<T>(null, null,
                            MaxCollectionCount);
                        referenceCache.SetReference(referenceId, true);
                        _enumerables.Add(referenceCache);
                        return;
                    }
                }

                if (values != null && !values.GetType().IsValueType)
                {
                    if (!_activeReferences.Add(values))
                        throw new InvalidOperationException($"Circular reference detected for collection type '{values.GetType()}'.");
                    tracked = true;
                }

                var cached = CachedEnumerable.Capture(values, comparer,
                    MaxCollectionCount);
                cached.SetReference(referenceId, false);
                try
                {
                    _enumerables.Add(cached);
                }
                catch
                {
                    cached.Release();
                    throw;
                }

                var cachedValues = cached.GetValues<T>();
                if (cachedValues == null) return;
                for (int i = 0; i < cachedValues.Count; i++)
                    converter.ScanValue(this, cachedValues[i]);
            }
            finally
            {
                if (tracked) _activeReferences.Remove(values);
                ExitNode();
            }
        }

        internal void ScanMultiDimensionalArray<T>(Array values, int rank,
            BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            bool tracked = false;
            try
            {
                int referenceId = -1;
                bool isReference = false;
                if (values != null && SupportReferences)
                {
                    isReference = TryDefineReference(values, out referenceId);
                    if (isReference)
                    {
                        var referenceCache = CachedEnumerable.CaptureMultiDimensional<T>(null,
                            rank, MaxCollectionCount);
                        referenceCache.SetReference(referenceId, true);
                        _enumerables.Add(referenceCache);
                        return;
                    }
                }

                if (values != null)
                {
                    if (!_activeReferences.Add(values))
                        throw new InvalidOperationException(
                            $"Circular reference detected for array type '{values.GetType()}'.");
                    tracked = true;
                }

                var cached = CachedEnumerable.CaptureMultiDimensional<T>(values, rank,
                    MaxCollectionCount);
                cached.SetReference(referenceId, false);
                try
                {
                    _enumerables.Add(cached);
                }
                catch
                {
                    cached.Release();
                    throw;
                }

                var cachedValues = cached.GetMultiDimensionalValues<T>(rank, out _);
                if (cachedValues == null) return;
                for (int i = 0; i < rank + 2; i++)
                    CountNode();
                for (int i = 0; i < cachedValues.Count; i++)
                    converter.ScanValue(this, cachedValues[i]);
            }
            finally
            {
                if (tracked) _activeReferences.Remove(values);
                ExitNode();
            }
        }

        private void EnterNode()
        {
            if (_scanDepth >= MaxDepth)
                throw new InvalidOperationException($"Serialization depth cannot exceed {MaxDepth}.");
            _scanDepth++;
        }

        internal void CountNode()
        {
            if (_nodeCount >= MaxNodeCount)
                throw new InvalidOperationException(
                    $"Serialization node count cannot exceed {MaxNodeCount}.");
            _nodeCount++;
        }

        internal int RequireObjectReference(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!SupportReferences)
                throw new InvalidOperationException(
                    "Delegates bound to another object require SupportReferences=true.");
            if (value.GetType().IsValueType)
                throw new NotSupportedException("Delegate targets must be reference types.");

            if (_references.TryGetValue(value, out var reference))
            {
                reference.RequiredByDelegate = true;
                _references[value] = reference;
                return reference.Id;
            }

            int referenceId = _references.Count;
            _references.Add(value, new ReferenceEntry
            {
                Id = referenceId,
                RequiredByDelegate = true
            });
            return referenceId;
        }

        private bool TryDefineReference(object value, out int referenceId)
        {
            if (_references.TryGetValue(value, out var reference))
            {
                referenceId = reference.Id;
                if (reference.Defined) return true;
                reference.Defined = true;
                _references[value] = reference;
                return false;
            }

            referenceId = _references.Count;
            _references.Add(value, new ReferenceEntry
            {
                Id = referenceId,
                Defined = true
            });
            return false;
        }

        internal void ValidateReferences()
        {
            if (!SupportReferences) return;
            foreach (var item in _references)
            {
                var reference = item.Value;
                if (reference.RequiredByDelegate && !reference.Defined)
                    throw new InvalidOperationException(
                        $"Delegate target '{item.Key.GetType()}' is not part of the serialized object graph.");
            }
        }

        private void ExitNode()
        {
            _scanDepth--;
        }

        public void AddMeta(string value)
        {
            if (!_collectMeta) return;
            if (value != null && value.Length > MaxScalarLength)
                throw new FormatException(
                    $"Metadata length cannot exceed {MaxScalarLength} characters.");
            if (_metaMap.ContainsKey(value)) return;
            _metaMap.Add(value, _metas.Count);
            _metas.Add(value);
        }

        internal int MetaCount => _metas.Count;
        internal string GetMeta(int index) => _metas[index];
        internal int GetMetaIndex(string value) => _metaMap[value];

        internal CachedObject ReadObject()
        {
            if (_objectIndex >= _objects.Count)
                throw new InvalidOperationException("The object scan cache is out of sync.");
            return _objects[_objectIndex++];
        }

        internal CachedField ReadField(CachedObject cached, int index)
        {
            if (index < 0 || index >= cached.FieldCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _fields[cached.FieldStart + index];
        }

        internal CachedField CacheFieldValue<T>(TypeHelper.TypeFields.Field field,
            BuffConverter converter, T value)
        {
            if (!_fieldValueCaches.TryGetValue(typeof(T), out var values))
            {
                values = new FieldValueCache<T>();
                _fieldValueCaches.Add(typeof(T), values);
            }
            return ((FieldValueCache<T>)values).Add(field, converter, value);
        }

        internal CachedField CacheBoxedFieldValue(TypeHelper.TypeFields.Field field,
            BuffConverter converter, object value) =>
            _boxedFieldValues.Add(field, converter, value);

        internal List<T> ReadEnumerable<T>(out int referenceId, out bool isReference)
        {
            if (_enumerableIndex >= _enumerables.Count)
                throw new InvalidOperationException("The enumerable scan cache is out of sync.");
            var cached = _enumerables[_enumerableIndex++];
            referenceId = cached.ReferenceId;
            isReference = cached.IsReference;
            return cached.GetValues<T>();
        }

        internal List<T> ReadMultiDimensionalArray<T>(int rank, out ArrayShape shape,
            out int referenceId, out bool isReference)
        {
            if (_enumerableIndex >= _enumerables.Count)
                throw new InvalidOperationException("The array scan cache is out of sync.");
            var cached = _enumerables[_enumerableIndex++];
            referenceId = cached.ReferenceId;
            isReference = cached.IsReference;
            return cached.GetMultiDimensionalValues<T>(rank, out shape);
        }

        internal void ResetRead()
        {
            _objectIndex = 0;
            _enumerableIndex = 0;
        }

        private void Clear()
        {
            _objects.Clear();
            _fields.Clear();
            foreach (var values in _fieldValueCaches.Values)
                values.Clear();
            if (_fieldValueCaches.Count > BuffSettings.PoolLimit * 4)
                _fieldValueCaches.Clear();
            _boxedFieldValues.Clear();

            for (int i = 0; i < _enumerables.Count; i++)
            {
                var cachedEnumerable = _enumerables[i];
                cachedEnumerable.Release();
            }
            _enumerables.Clear();

            if (_metaMap.Count > BuffSettings.RetainedListCapacity)
                _metaMap = new Dictionary<string, int>();
            else
                _metaMap.Clear();
            _metas.Clear();
            TrimList(_objects);
            TrimList(_fields);
            TrimList(_enumerables);
            TrimList(_metas);
            if (_activeReferences.Count > BuffSettings.RetainedListCapacity)
                _activeReferences = new HashSet<object>(ReferenceComparer.Instance);
            else
                _activeReferences.Clear();
            if (_references.Count > BuffSettings.RetainedListCapacity)
                _references = new Dictionary<object, ReferenceEntry>(ReferenceComparer.Instance);
            else
                _references.Clear();
            _scanDepth = 0;
            _nodeCount = 0;
            _currentObject = null;
            Settings = null;
            MaxDepth = 0;
            MaxTextLength = 0;
            MaxCollectionCount = 0;
            MaxObjectFieldCount = 0;
            MaxScalarLength = 0;
            MaxNodeCount = 0;
            SupportReferences = false;
            TypeInfo = false;
            PrettyPrint = false;
            DeterministicCollectionOrder = false;
            _invokeBeforeWriteCallbacks = false;
            _serializeEvents = false;
            ResetRead();
        }

        private static void TrimList<T>(List<T> values)
        {
            if (values.Capacity > BuffSettings.RetainedListCapacity)
                values.Capacity = 0;
        }

        public void Dispose() => Clear();
    }
}
