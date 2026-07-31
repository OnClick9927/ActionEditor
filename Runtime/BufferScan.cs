using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ActionBuffer
{
    public sealed class BufferScan : IDisposable
    {
        internal const string ReferenceMetadata = "$ActionBuffer.Reference.v1";
        internal int MaxDepth => Settings.MaxDepth;

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            public static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object x, object y) => ReferenceEquals(x, y);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }

        internal readonly struct CachedField
        {
            public TypeHelper.TypeFields.Field Field { get; }
            public BuffConverter Converter { get; }
            public object Value { get; }

            public CachedField(TypeHelper.TypeFields.Field field, BuffConverter converter, object value)
            {
                Field = field;
                Converter = converter;
                Value = value;
            }
        }

        internal struct CachedObject
        {
            public object Value { get; private set; }
            public Type Type { get; private set; }
            public int ReferenceId { get; private set; }
            public bool IsReference { get; private set; }
            private List<CachedField> _fields;

            public int FieldCount => _fields?.Count ?? 0;

            public CachedObject(object value, Type type, int referenceId = -1,
                bool isReference = false)
            {
                Value = value;
                Type = type;
                ReferenceId = referenceId;
                IsReference = isReference;
                _fields = null;
            }

            public void AddField(CachedField field)
            {
                if (_fields == null)
                {
                    _fields = ListPool<CachedField>.Get();
                }
                _fields.Add(field);
            }

            public CachedField GetField(int index)
            {
                return _fields[index];
            }

            public static void Release(ref CachedObject cached)
            {
                var fields = cached._fields;
                if (fields != null)
                {
                    fields.Clear();
                    ListPool<CachedField>.Back(fields);
                }
                cached = default;
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
            private object _values;
            private Type _elementType;
            private Action<object> _release;
            private byte _rank;
            private int _length0;
            private int _length1;
            private int _length2;
            private int _length3;
            private int _length4;

            public static CachedEnumerable Capture<T>(IEnumerable<T> source, IComparer<T> comparer,
                int maxCollectionCount)
            {
                var cached = new CachedEnumerable
                {
                    _elementType = typeof(T),
                    _release = CachedEnumerableValues<T>.Release,
                    _rank = 1
                };
                if (source == null) return cached;

                var collection = source as ICollection<T>;
                int knownCount = collection?.Count ?? 0;
                if (knownCount > maxCollectionCount)
                    throw new FormatException(
                        $"Collection count cannot exceed {maxCollectionCount}.");

                var values = ListPool<T>.Get(knownCount);
                cached._values = values;
                try
                {
                    if (collection != null)
                    {
                        values.AddRange(collection);
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

            public static CachedEnumerable Capture<T>(ReadOnlySpan<T> source,
                int maxCollectionCount)
            {
                if (source.Length > maxCollectionCount)
                    throw new FormatException(
                        $"Collection count cannot exceed {maxCollectionCount}.");
                var values = ListPool<T>.Get(source.Length);
                var cached = new CachedEnumerable
                {
                    _values = values,
                    _elementType = typeof(T),
                    _release = CachedEnumerableValues<T>.Release,
                    _rank = 1,
                    _length0 = source.Length
                };
                try
                {
                    for (int i = 0; i < source.Length; i++)
                        values.Add(source[i]);
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
                return (List<T>)_values;
            }

            public static CachedEnumerable CaptureMultiDimensional<T>(Array source, int rank,
                int maxCollectionCount)
            {
                if (rank < 2 || rank > 5)
                    throw new NotSupportedException("Only arrays up to rank five are supported.");
                var cached = new CachedEnumerable
                {
                    _elementType = typeof(T),
                    _release = CachedEnumerableValues<T>.Release,
                    _rank = (byte)rank
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

                var values = ListPool<T>.Get((int)count);
                cached._values = values;
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
                return (List<T>)_values;
            }

            public void Release()
            {
                var values = _values;
                var release = _release;
                this = default;
                if (values != null)
                    release(values);
            }
        }

        private static class CachedEnumerableValues<T>
        {
            public static readonly Action<object> Release = ReleaseValues;

            private static void ReleaseValues(object value)
            {
                var values = (List<T>)value;
                values.Clear();
                ListPool<T>.Back(values);
            }
        }

        private struct ReferenceEntry
        {
            public int Id;
            public bool Defined;
            public bool RequiredByDelegate;
        }

        private readonly List<CachedObject> _objects = new();
        private readonly List<CachedEnumerable> _enumerables = new();
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
        internal BufferSerializerSettings Settings { get; private set; }

        public BufferScan()
        {
            Prepare(null, true, false);
        }

        public BufferScan(bool collectMeta, bool fullField)
        {
            Prepare(null, collectMeta, fullField);
        }

        internal static BufferScan Rent(bool collectMeta, bool fullField)
        {
            return Rent(null, collectMeta, fullField);
        }

        internal static BufferScan Rent(BufferSerializerSettings settings, bool collectMeta, bool fullField)
        {
            var result = ClassPool<BufferScan>.Get();
            result.Prepare(settings, collectMeta, fullField);
            return result;
        }

        internal static void Back(BufferScan value)
        {
            if (value == null) return;
            value.Clear();
            ClassPool<BufferScan>.Back(value);
        }

        private void Prepare(BufferSerializerSettings settings, bool collectMeta, bool fullField)
        {
            Clear();
            Settings = settings ?? BufferSerializerSettings.DefaultSetting;
            _collectMeta = collectMeta;
            _fullField = fullField;
            if (Settings.SupportReferences)
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
                    Settings.SupportReferences)
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

                if (Settings.InvokeBeforeWriteCallbacks && objectValue is IBufferObject bufferObject)
                    bufferObject.BeforeWriteBuffer();

                if (type != null && type != typeof(T))
                {
                    if (!typeof(T).IsAssignableFrom(type))
                        throw new NotSupportedException($"'{type}' is not assignable to '{typeof(T)}'.");
                    if (!BufferSerializer.GetConverter(type, Settings).UsesObjectLayout)
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
                    for (int i = 0; i < objectFields.Count; i++)
                    {
                        var field = objectFields[i];
                        if (field.IsEvent && !Settings.SerializeEvents) continue;
                        var fieldValue = field.GetValue(objectValue);
                        if (!_fullField && TypeHelper.IsNullOrDefault(fieldValue, field.FieldType)) continue;
                        if (cachedObject.FieldCount >= Settings.MaxObjectFieldCount)
                            throw new FormatException(
                                $"Object field count cannot exceed {Settings.MaxObjectFieldCount}.");
                        var converter = field.GetConverter(Settings);
                        cachedObject.AddField(new CachedField(field, converter, fieldValue));
                        if (cachedObject.FieldCount == 1)
                            _objects[cachedObjectIndex] = cachedObject;
                        AddMeta(field.name);
                        AddMeta(TypeHelper.GetTypeName(field.FieldType));
                        converter.Scan(this, fieldValue);
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
            IComparer<T> comparer = null)
        {
            EnterNode();
            bool tracked = false;
            try
            {
                if (values != null && !values.GetType().IsValueType)
                {
                    if (!_activeReferences.Add(values))
                        throw new InvalidOperationException($"Circular reference detected for collection type '{values.GetType()}'.");
                    tracked = true;
                }

                var cached = CachedEnumerable.Capture(values, comparer,
                    Settings.MaxCollectionCount);
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

        internal void ScanSpan<T>(ReadOnlySpan<T> values, BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterNode();
            try
            {
                var cached = CachedEnumerable.Capture(values, Settings.MaxCollectionCount);
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
                for (int i = 0; i < cachedValues.Count; i++)
                    converter.ScanValue(this, cachedValues[i]);
            }
            finally
            {
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
                if (values != null)
                {
                    if (!_activeReferences.Add(values))
                        throw new InvalidOperationException(
                            $"Circular reference detected for array type '{values.GetType()}'.");
                    tracked = true;
                }

                var cached = CachedEnumerable.CaptureMultiDimensional<T>(values, rank,
                    Settings.MaxCollectionCount);
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
            if (_nodeCount >= Settings.MaxNodeCount)
                throw new InvalidOperationException(
                    $"Serialization node count cannot exceed {Settings.MaxNodeCount}.");
            _nodeCount++;
        }

        internal int RequireObjectReference(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (!Settings.SupportReferences)
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

        internal void ValidateReferences()
        {
            if (!Settings.SupportReferences) return;
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
            if (value != null && value.Length > Settings.MaxScalarLength)
                throw new FormatException(
                    $"Metadata length cannot exceed {Settings.MaxScalarLength} characters.");
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

        internal List<T> ReadEnumerable<T>()
        {
            if (_enumerableIndex >= _enumerables.Count)
                throw new InvalidOperationException("The enumerable scan cache is out of sync.");
            return _enumerables[_enumerableIndex++].GetValues<T>();
        }

        internal List<T> ReadMultiDimensionalArray<T>(int rank, out ArrayShape shape)
        {
            if (_enumerableIndex >= _enumerables.Count)
                throw new InvalidOperationException("The array scan cache is out of sync.");
            return _enumerables[_enumerableIndex++].GetMultiDimensionalValues<T>(rank, out shape);
        }

        internal void ResetRead()
        {
            _objectIndex = 0;
            _enumerableIndex = 0;
        }

        private void Clear()
        {
            for (int i = 0; i < _objects.Count; i++)
            {
                var cachedObject = _objects[i];
                CachedObject.Release(ref cachedObject);
            }
            _objects.Clear();
            if (_objects.Capacity > BufferSerializer.RetainedListCapacity)
                _objects.Capacity = 0;

            for (int i = 0; i < _enumerables.Count; i++)
            {
                var cachedEnumerable = _enumerables[i];
                cachedEnumerable.Release();
            }
            _enumerables.Clear();
            if (_enumerables.Capacity > BufferSerializer.RetainedListCapacity)
                _enumerables.Capacity = 0;

            if (_metaMap.Count > BufferSerializer.RetainedListCapacity)
                _metaMap = new Dictionary<string, int>();
            else
                _metaMap.Clear();
            _metas.Clear();
            if (_metas.Capacity > BufferSerializer.RetainedListCapacity)
                _metas.Capacity = 0;
            if (_activeReferences.Count > BufferSerializer.RetainedListCapacity)
                _activeReferences = new HashSet<object>(ReferenceComparer.Instance);
            else
                _activeReferences.Clear();
            if (_references.Count > BufferSerializer.RetainedListCapacity)
                _references = new Dictionary<object, ReferenceEntry>(ReferenceComparer.Instance);
            else
                _references.Clear();
            _scanDepth = 0;
            _nodeCount = 0;
            _currentObject = null;
            Settings = null;
            ResetRead();
        }

        public void Dispose() => Clear();
    }
}
