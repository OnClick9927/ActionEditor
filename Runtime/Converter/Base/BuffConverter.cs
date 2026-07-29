using System;
using System.Collections.Generic;
using System.Reflection;


namespace ActionBuffer
{
    public abstract class BuffConverter
    {
        private static Dictionary<Type, Type> _nmap;

        private static Dictionary<Type, Type> _fgenmap;
        private static Dictionary<Type, BuffConverter> map = new Dictionary<Type, BuffConverter>();
        private static BuffConverter Create(Type type)
        {
            if (_nmap == null)
            {
                _nmap = new Dictionary<Type, Type>();
                _fgenmap = new Dictionary<Type, Type>();
                for (int i = 0; i < TypeHelper.Types.Count; i++)
                {
                    var item = TypeHelper.Types[i];
                    if (item.IsAbstract || item.BaseType == null || !item.BaseType.IsGenericType ||
                       !typeof(BuffConverter).IsAssignableFrom(item))
                        continue;

                    var attr = item.GetCustomAttribute<BuffConverterAttribute>(false);
                    if (attr == null) continue;
                    var _target = attr.type;
                    if (_target.IsGenericType)
                        _fgenmap.Add(_target, item);
                    else
                        _nmap.Add(_target, item);
                }
            }


            if (_nmap.TryGetValue(type, out var target))
                return TypeHelper.CreateInstance(target) as BuffConverter;
            if (type.IsEnum)
                return TypeHelper.CreateInstance(typeof(EnumConverter<>).MakeGenericType(type)) as BuffConverter;
            if (type.IsArray)
                return TypeHelper.CreateInstance(typeof(ArrayConverter<>).MakeGenericType(type.GetElementType())) as BuffConverter;
            if (type.IsGenericType)
            {
                foreach (var item in _fgenmap.Keys)
                {
                    if (TypeHelper.IsSubclassOfGeneric(type, item))
                    {
                        return TypeHelper.CreateInstance(_fgenmap[item].MakeGenericType(type.GetGenericArguments())) as BuffConverter;
                    }
                }
            }
            //if (!type.IsGenericType)
            return TypeHelper.CreateInstance(typeof(ObjectConverter<>).MakeGenericType(type)) as BuffConverter;
            //return null;
        }
        public static BuffConverter GetConverter(Type type)
        {
            if (!map.TryGetValue(type, out var convert))
            {
                convert = Create(type);
                if (convert == null)
                {
                    throw new Exception($"UnHandled Type {type}");
                }
                map.Add(type, convert);
            }
            return convert;
        }
        public static BuffConverter<T> GetConverter<T>() => GetConverter(typeof(T)) as BuffConverter<T>;

        internal abstract object Read(IBufferReader reader, Type type);
        internal abstract void Scan(BufferScan scan, object value);
        internal abstract void Write(IBufferWriter writer, object value);

        public static void WriteObject(IBufferWriter writer, object obj)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            var converter = GetConverter(obj.GetType());
            var scan = BufferScan.Rent(writer.CollectMeta, writer.FullField);
            try
            {
                converter.Scan(scan, obj);
                writer.Init(scan);
                scan = null;
                converter.Write(writer, obj);
            }
            finally
            {
                BufferScan.Back(scan);
            }
        }

        public static object ReadObject(IBufferReader reader, Type type)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (type == null) throw new ArgumentNullException(nameof(type));
            return GetConverter(type).Read(reader, type);
        }

        public static string ToJson(object obj, bool pretty = false, bool typeInfo = true, bool fullField = false)
        {
            var writer = ClassPool<JsonWriter>.Get();
            try
            {
                writer.typeInfo = typeInfo;
                writer.prettyPrint = pretty;
                writer.fullField = fullField;
                WriteObject(writer, obj);
                return writer.GetJson();
            }
            finally
            {
                writer.Clear();
                ClassPool<JsonWriter>.Back(writer);
            }
        }
        public static object ToObject(string data, Type type)
        {
            var reader = ClassPool<JsonReader>.Get();
            try
            {
                reader.Init(data);
                return ReadObject(reader, type);
            }
            finally
            {
                reader.Clear();
                ClassPool<JsonReader>.Back(reader);
            }
        }
        public static T ToObject<T>(string data) => (T)ToObject(data, typeof(T));

        public static byte[] ToBytes(object obj)
        {
            var writer = ClassPool<BufferWriter>.Get();
            try
            {
                WriteObject(writer, obj);
                return writer.GetValidBuffer();
            }
            finally
            {
                writer.Clear();
                ClassPool<BufferWriter>.Back(writer);
            }
        }
        public static object ToObject(byte[] bytes, Type type)
        {
            var reader = ClassPool<BufferReader>.Get();
            try
            {
                reader.Init(bytes);
                return ReadObject(reader, type);
            }
            finally
            {
                reader.Clear();
                ClassPool<BufferReader>.Back(reader);
            }
        }
        public static T ToObject<T>(byte[] bytes) => (T)ToObject(bytes, typeof(T));
    }
    public abstract class BuffConverter<T> : BuffConverter
    {
        protected abstract void OnScan(BufferScan scan, T value);
        protected abstract void OnWrite(IBufferWriter writer, T value);
        protected abstract T OnRead(IBufferReader reader, Type type);
        internal T ReadValue(IBufferReader reader, Type type) => OnRead(reader, type);
        internal void ScanValue(BufferScan scan, T value) => OnScan(scan, value);
        internal void WriteValue(IBufferWriter writer, T value) => OnWrite(writer, value);
        internal sealed override object Read(IBufferReader reader, Type type) => ReadValue(reader, type);
        internal sealed override void Scan(BufferScan scan, object value) => ScanValue(scan, (T)value);
        internal sealed override void Write(IBufferWriter writer, object value) => WriteValue(writer, (T)value);
    }

}
