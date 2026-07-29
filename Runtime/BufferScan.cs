using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    public sealed class BufferScan : IDisposable
    {
        internal sealed class CachedField
        {
            public TypeHelper.TypeFields.Field Field { get; private set; }
            public BuffConverter Converter { get; private set; }
            public object Value { get; private set; }

            public CachedField() { }

            public void Set(TypeHelper.TypeFields.Field field, BuffConverter converter, object value)
            {
                Field = field;
                Converter = converter;
                Value = value;
            }

            public void Clear()
            {
                Field = null;
                Converter = null;
                Value = null;
            }
        }

        internal sealed class CachedObject
        {
            public object Value { get; private set; }
            public Type Type { get; private set; }
            public readonly List<CachedField> Fields = new();

            public CachedObject() { }

            public void Set(object value, Type type)
            {
                Value = value;
                Type = type;
            }

            public void Clear()
            {
                Value = null;
                Type = null;
                for (int i = 0; i < Fields.Count; i++)
                {
                    var field = Fields[i];
                    field.Clear();
                    ClassPool<CachedField>.Back(field);
                }
                Fields.Clear();
            }
        }

        internal abstract class CachedEnumerable
        {
            public abstract void Release();
        }

        internal sealed class CachedEnumerable<T> : CachedEnumerable
        {
            public List<T> Values { get; private set; }
            public bool IsNull { get; private set; }

            public CachedEnumerable() { }

            public void Capture(IEnumerable<T> source, bool limitCount)
            {
                IsNull = source == null;
                if (IsNull) return;

                Values = ClassPool<List<T>>.Get();
                Values.Clear();
                foreach (var value in source)
                {
                    Values.Add(value);
                    if (limitCount && Values.Count > ushort.MaxValue)
                        throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue} !");
                }
            }

            public override void Release()
            {
                IsNull = false;
                if (Values != null)
                {
                    Values.Clear();
                    ClassPool<List<T>>.Back(Values);
                    Values = null;
                }
                ClassPool<CachedEnumerable<T>>.Back(this);
            }
        }

        private readonly List<CachedObject> _objects = new();
        private readonly List<CachedEnumerable> _enumerables = new();
        private readonly Dictionary<string, int> _metaMap = new();
        private readonly List<string> _metas = new();
        private int _objectIndex;
        private int _enumerableIndex;
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
            object objectValue = value;
            if (objectValue is IBufferObject bufferObject)
                bufferObject.BeforeWriteBuffer();

            var type = objectValue?.GetType();
            var cachedObject = ClassPool<CachedObject>.Get();
            cachedObject.Set(objectValue, type);
            _objects.Add(cachedObject);
            if (objectValue == null) return;

            AddMeta(type.FullName);
            AddMeta(type.Assembly.FullName);
            var objectFields = fields.GetFields();
            for (int i = 0; i < objectFields.Count; i++)
            {
                var field = objectFields[i];
                var fieldValue = field.GetValue(objectValue);
                if (!_fullField && TypeHelper.IsNullOrDefault(fieldValue)) continue;

                var converter = BuffConverter.GetConverter(field.FieldType);
                var cachedField = ClassPool<CachedField>.Get();
                cachedField.Set(field, converter, fieldValue);
                cachedObject.Fields.Add(cachedField);
                AddMeta(field.name);
                AddMeta(TypeHelper.GetTypeName(field.FieldType));
                converter.Scan(this, fieldValue);
            }
        }

        public void ScanEnumerable<T>(IEnumerable<T> values, BuffConverter<T> converter)
        {
            var cached = ClassPool<CachedEnumerable<T>>.Get();
            try
            {
                cached.Capture(values, _collectMeta);
                _enumerables.Add(cached);
            }
            catch
            {
                cached.Release();
                throw;
            }

            if (cached.IsNull) return;
            for (int i = 0; i < cached.Values.Count; i++)
                converter.ScanValue(this, cached.Values[i]);
        }

        public void AddMeta(string value)
        {
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
            if (!(_enumerables[_enumerableIndex++] is CachedEnumerable<T> cached))
                throw new InvalidOperationException("The enumerable scan cache contains an unexpected element type.");
            return cached.IsNull ? null : cached.Values;
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
                cachedObject.Clear();
                ClassPool<CachedObject>.Back(cachedObject);
            }
            _objects.Clear();

            for (int i = 0; i < _enumerables.Count; i++)
                _enumerables[i].Release();
            _enumerables.Clear();

            _metaMap.Clear();
            _metas.Clear();
            ResetRead();
        }

        public void Dispose() => Clear();
    }
}
