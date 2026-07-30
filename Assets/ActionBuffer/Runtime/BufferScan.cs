using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ActionBuffer
{
    public sealed class BufferScan : IDisposable
    {
        internal static int MaxDepth => BufferSerializer.MaxDepth;

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
            private List<CachedField> _fields;

            public int FieldCount => _fields?.Count ?? 0;

            public CachedObject(object value, Type type)
            {
                Value = value;
                Type = type;
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

        internal struct CachedEnumerable
        {
            private object _values;
            private Type _elementType;
            private Action<object> _release;

            public static CachedEnumerable Capture<T>(IEnumerable<T> source, IComparer<T> comparer)
            {
                var cached = new CachedEnumerable
                {
                    _elementType = typeof(T),
                    _release = CachedEnumerableValues<T>.Release
                };
                if (source == null) return cached;

                var collection = source as ICollection<T>;
                int knownCount = collection?.Count ?? 0;
                if (knownCount > BufferSerializer.MaxCollectionCount)
                    throw new FormatException(
                        $"Collection count cannot exceed {BufferSerializer.MaxCollectionCount}.");

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
                            if (values.Count > BufferSerializer.MaxCollectionCount)
                                throw new FormatException(
                                    $"Collection count cannot exceed {BufferSerializer.MaxCollectionCount}.");
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

            public static CachedEnumerable Capture<T>(ReadOnlySpan<T> source)
            {
                if (source.Length > BufferSerializer.MaxCollectionCount)
                    throw new FormatException(
                        $"Collection count cannot exceed {BufferSerializer.MaxCollectionCount}.");
                var values = ListPool<T>.Get(source.Length);
                var cached = new CachedEnumerable
                {
                    _values = values,
                    _elementType = typeof(T),
                    _release = CachedEnumerableValues<T>.Release
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
                if (_elementType != typeof(T))
                    throw new InvalidOperationException("The enumerable scan cache contains an unexpected element type.");
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

        private readonly List<CachedObject> _objects = new();
        private readonly List<CachedEnumerable> _enumerables = new();
        private Dictionary<string, int> _metaMap = new();
        private readonly List<string> _metas = new();
        private HashSet<object> _activeReferences = new HashSet<object>(ReferenceComparer.Instance);
        private int _objectIndex;
        private int _enumerableIndex;
        private int _scanDepth;
        private int _nodeCount;
        private bool _collectMeta;
        private bool _fullField;

        public BufferScan()
        {
            Prepare(true, false);
        }

        public BufferScan(bool collectMeta, bool fullField)
        {
            Prepare(collectMeta, fullField);
        }

        internal static BufferScan Rent(bool collectMeta, bool fullField)
        {
            var result = ClassPool<BufferScan>.Get();
            result.Prepare(collectMeta, fullField);
            return result;
        }

        internal static void Back(BufferScan value)
        {
            if (value == null) return;
            value.Clear();
            ClassPool<BufferScan>.Back(value);
        }

        private void Prepare(bool collectMeta, bool fullField)
        {
            Clear();
            _collectMeta = collectMeta;
            _fullField = fullField;
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
                if (objectValue != null && !type.IsValueType)
                {
                    if (!_activeReferences.Add(objectValue))
                        throw new InvalidOperationException(
                            $"Circular reference detected for object type '{type}'.");
                    tracked = true;
                }

                if (objectValue is IBufferObject bufferObject)
                    bufferObject.BeforeWriteBuffer();

                if (type != null && type != typeof(T))
                {
                    if (!typeof(T).IsAssignableFrom(type))
                        throw new NotSupportedException($"'{type}' is not assignable to '{typeof(T)}'.");
                    if (!BufferSerializer.GetConverter(type).UsesObjectLayout)
                        throw new NotSupportedException(
                            $"A field declared as '{typeof(T)}' cannot serialize runtime value type '{type}'. " +
                            "Declare the field with its concrete serializable type.");
                }
                var cachedObject = new CachedObject(objectValue, type);
                int cachedObjectIndex = _objects.Count;
                _objects.Add(cachedObject);
                if (objectValue == null) return;

                AddMeta(type.FullName);
                AddMeta(type.Assembly.FullName);
                var objectFields = fields.GetFields();
                if (objectFields.Count > BufferSerializer.MaxObjectFieldCount)
                    throw new FormatException(
                        $"Object field count cannot exceed {BufferSerializer.MaxObjectFieldCount}.");
                for (int i = 0; i < objectFields.Count; i++)
                {
                    var field = objectFields[i];
                    var fieldValue = field.GetValue(objectValue);
                    if (!_fullField && TypeHelper.IsNullOrDefault(fieldValue, field.FieldType)) continue;
                    var converter = field.GetConverter();
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

                var cached = CachedEnumerable.Capture(values, comparer);
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
                var cached = CachedEnumerable.Capture(values);
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

        private void EnterNode()
        {
            if (_scanDepth >= MaxDepth)
                throw new InvalidOperationException($"Serialization depth cannot exceed {MaxDepth}.");
            _scanDepth++;
        }

        internal void CountNode()
        {
            if (_nodeCount >= BufferSerializer.MaxNodeCount)
                throw new InvalidOperationException(
                    $"Serialization node count cannot exceed {BufferSerializer.MaxNodeCount}.");
            _nodeCount++;
        }

        private void ExitNode()
        {
            _scanDepth--;
        }

        public void AddMeta(string value)
        {
            if (value != null && value.Length > BufferSerializer.MaxScalarLength)
                throw new FormatException(
                    $"Metadata length cannot exceed {BufferSerializer.MaxScalarLength} characters.");
            if (!_collectMeta || _metaMap.ContainsKey(value)) return;
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
            _scanDepth = 0;
            _nodeCount = 0;
            ResetRead();
        }

        public void Dispose() => Clear();
    }
}
