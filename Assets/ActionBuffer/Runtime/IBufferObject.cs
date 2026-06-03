using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ActionBuffer
{

    [AttributeUsage(AttributeTargets.Field)]
    public class BufferAttribute : System.Attribute
    {
        public readonly string bufferName;
        public BufferAttribute() { }
        public BufferAttribute(string bufferName)
        {
            this.bufferName = bufferName;
        }
    }
    public static class TypeHelper
    {
        public class TypeFields
        {
            private Type type;
            public class Field
            {
                public readonly string name;
                public Type FieldType;
                public Type DeclaringType;
                private Func<object, object> _getter;
                private Action<object, object> _setter;
                private void Build(FieldInfo field)
                {

                    var targetParam = Expression.Parameter(typeof(object), "target");
                    var valueParam = Expression.Parameter(typeof(object), "value");

                    // Getter: 将目标转换为声明类型，读取字段值并装箱为 object
                    var targetExpr = Expression.Convert(targetParam, field.DeclaringType);
                    var fieldExpr = Expression.Field(targetExpr, field);
                    var getterBody = Expression.Convert(fieldExpr, typeof(object));
                    _getter = Expression.Lambda<Func<object, object>>(getterBody, targetParam).Compile();

                    // Setter: 根据声明类型是否为值类型，选择 Convert 或 Unbox 获取实例引用
                    Expression instanceExpr;
                    if (field.DeclaringType.IsValueType)
                    {
                        // 值类型：通过 Unbox 获取对装箱对象中字段的引用，可直接修改
                        instanceExpr = Expression.Unbox(targetParam, field.DeclaringType);
                    }
                    else
                    {
                        // 引用类型：直接转换
                        instanceExpr = Expression.Convert(targetParam, field.DeclaringType);
                    }

                    var fieldAccess = Expression.Field(instanceExpr, field);
                    var valueCast = Expression.Convert(valueParam, field.FieldType);
                    var assign = Expression.Assign(fieldAccess, valueCast);   // 赋值表达式返回被赋的值
                    var setterLambda = Expression.Lambda<Action<object, object>>(assign, targetParam, valueParam);
                    _setter = setterLambda.Compile();
                }
                public object GetValue(object target) => _getter(target);
                public void SetValue(object target, object value) => _setter(target, value);
                public Field(FieldInfo field, string name)
                {
                    DeclaringType = field.DeclaringType;
                    FieldType = field.FieldType;
                    Build(field);
                    this.name = name;
                }

            }
            public TypeFields(Type type)
            {
                this.type = type;
            }
            private List<Field> fields = new();
            private Dictionary<string, Field> map = new();
            public List<Field> GetFields() => fields;
            public Field FindField(string name)
            {
                if (map.TryGetValue(name, out var field)) return field;
                return null;
            }
            public void AddField(Field field)
            {
                map[field.name] = field;
                fields.Add(field);
            }
            public void AddField(FieldInfo field)
            {

                if (field.IsDefined(typeof(System.NonSerializedAttribute))) return;
                var attr = field.GetCustomAttribute<BufferAttribute>();
                if (!field.IsPublic && attr == null) return;
                var name = attr?.bufferName ?? field.Name;
                if (map.TryGetValue(name, out var info))
                    throw new Exception($"{type}Exist Same Name Field {name}=> {info.DeclaringType}:{field.DeclaringType}");
                var _f = new Field(field, name);

                AddField(_f);
            }
        }
        private static Dictionary<Type, TypeFields> map = new Dictionary<Type, TypeFields>();
        public static TypeFields GetTypeFields(Type type)
        {
            if (map.TryGetValue(type, out var typefield)) return typefield;
            typefield = new TypeFields(type);
            var baseType = type.BaseType;
            // 处理基类（object 除外）
            if (baseType != null && baseType != typeof(object))
            {
                var baseFields = GetTypeFields(baseType); // 递归确保缓存
                var _fields = baseFields.GetFields();
                if (_fields != null && _fields.Count != 0)
                    for (int i = 0; i < _fields.Count; i++)
                    {
                        var field = _fields[i];
                        typefield.AddField(field);
                    }
            }

            // 添加当前类型声明的字段
            {
                var _fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                     BindingFlags.Instance | BindingFlags.DeclaredOnly);
                for (int i = 0; i < _fields.Length; i++)
                {
                    var field = _fields[i];
                    typefield.AddField(field);
                }

            }

            map[type] = typefield;
            return typefield;
        }

        public static bool IsSubclassOfGeneric(Type self, Type genericType)
        {
#if NETFX_CORE
                if (!genericTypeDefinition.GetTypeInfo().IsGenericTypeDefinition)
#else
            if (!genericType.IsGenericTypeDefinition)
#endif
                return false;

#if NETFX_CORE
                if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().Equals(genericTypeDefinition))
#else
            if (self.IsGenericType && self.GetGenericTypeDefinition().Equals(genericType))
#endif
                return true;

#if NETFX_CORE
                Type baseType = type.GetTypeInfo().BaseType;
#else
            Type baseType = self.BaseType;
#endif
            if (baseType != null && baseType != typeof(object))
            {
                if (IsSubclassOfGeneric(baseType, genericType))
                    return true;
            }

            foreach (Type t in self.GetInterfaces())
            {
                if (IsSubclassOfGeneric(t, genericType))
                    return true;
            }

            return false;
        }

        private static Dictionary<string, Type> _typeMap = new Dictionary<string, Type>();
        public static Type GetTypeByFullName(string typeFullName, string assemblyName = null)
        {
            if (string.IsNullOrEmpty(typeFullName))
                return null;

            if (_typeMap.TryGetValue(typeFullName, out var type)) return type;


            // 如果指定了程序集名称，拼接完整的类型标识
            string fullTypeName = string.IsNullOrEmpty(assemblyName)
                ? typeFullName
                : $"{typeFullName}, {assemblyName}";

            // 尝试直接获取类型
            type = Type.GetType(fullTypeName);

            // 如果获取失败，遍历当前加载的所有程序集查找
            if (type == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeFullName);
                    if (type != null)
                        break;
                }
            }
            //if (type != null)
            _typeMap[typeFullName] = type;
            return type;
        }

        public static T DeepCopyByBuffer<T>(this T value) => BuffConverter.ToObject<T>(BuffConverter.ToBytes(value));
        private static readonly Dictionary<Type, object> _defaultValues = new Dictionary<Type, object>();
        private static readonly object _lock = new object();
        public static bool IsNullOrDefault(object obj)
        {
            if (obj == null) return true;
            Type type = obj.GetType();
            if (!type.IsValueType) return false;

            object defaultValue;
            lock (_lock)
            {
                if (!_defaultValues.TryGetValue(type, out defaultValue))
                {
                    defaultValue = Activator.CreateInstance(type);
                    _defaultValues.Add(type, defaultValue);
                }
            }
            return obj.Equals(defaultValue);
        }
        private static Dictionary<Type, string> type_warp = new Dictionary<Type, string>()
        {
            { typeof(byte),"a"},
            {typeof(bool),"b" },
            { typeof(char),"c" },
            { typeof(short),"d"},
            { typeof(ushort),"e"},
            { typeof(int),"f"},
            { typeof(uint),"g"},
            { typeof(long),"h"},
            { typeof(ulong),"i"},
            { typeof(float),"j"},
            { typeof(double),"k"},
            { typeof(string),"l"},
            { typeof(DateTime),"m"},
            { typeof(TimeSpan),"n"},
        };
        private static Dictionary<string, string> type_warp_2;

        public static string GetTypeName(Type type)
        {
            if (type_warp.TryGetValue(type, out var result))
                return result;
            return type.FullName;
        }
        public static string GetRealTypeName(string src)
        {
            if (type_warp_2 == null)
            {
                type_warp_2 = new Dictionary<string, string>();
                foreach (var t in type_warp)
                    type_warp_2.Add(t.Value, t.Key.FullName);
            }

            if (type_warp_2.TryGetValue(src, out var result))
                return result;
            return src;
        }
    }





    [StructLayout(LayoutKind.Explicit, Size = 4)]
    struct FloatUnion
    {
        [FieldOffset(0)]
        public float value;
        [FieldOffset(0)]

        public int _int;
    }
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    struct DoubleUnion
    {
        [FieldOffset(0)]
        public double value;
        [FieldOffset(0)]

        public long _long;
    }

    public interface IBufferObject
    {
        void BeforeWriteBuffer();
        void AfterReadBuffer();
    }

    public interface IBufferReader
    {
        T[] ReadArray<T>(Func<IBufferReader, T> read);
        bool ReadBool();
        byte ReadByte();
        char ReadChar();
        double ReadDouble();
        Enum ReadEnum(Type type);
        float ReadFloat();
        short ReadInt16();
        int ReadInt32();
        long ReadInt64();
        List<T> ReadList<T>(Func<IBufferReader, T> read);
        T ReadObject<T>() where T : class;
        ushort ReadUInt16();
        uint ReadUInt32();
        ulong ReadUInt64();
        string ReadUTF8();
    }
    public interface IBufferWriter
    {
        void CollectMetas(object value);
        void WriteArray<T>(T[] values, Action<IBufferWriter, T> write);
        void WriteBool(bool value);
        void WriteByte(byte value);
        void WriteChar(char value);
        void WriteDouble(double value);
        void WriteEnum(Enum data);
        void WriteFloat(float value);
        void WriteInt16(short value);
        void WriteInt32(int value);
        void WriteInt64(long value);
        void WriteList<T>(List<T> values, Action<IBufferWriter, T> write);
        void WriteObject<T>(T value) where T : class;
        void WriteUInt16(ushort value);
        void WriteUInt32(uint value);
        void WriteUInt64(ulong value);
        void WriteUTF8(string value);
    }


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
                var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes()).Where(x =>
                {
                    return !x.IsAbstract && x.BaseType != null && x.BaseType.IsGenericType && typeof(BuffConverter).IsAssignableFrom(x);
                });
                foreach (var item in types)
                {
                    var args = item.BaseType.GetGenericArguments();
                    if (args.Length == 1)
                    {
                        var arg = args[0];
                        if (arg.IsGenericType)
                        {
                            _fgenmap.Add(arg.GetGenericTypeDefinition(), item);
                            Console.WriteLine();
                        }
                        else
                            _nmap.Add(args[0], item);
                    }
                }

            }


            if (_nmap.TryGetValue(type, out var target))
                return Activator.CreateInstance(target) as BuffConverter;
            if (type.IsEnum)
                return Activator.CreateInstance(typeof(EnumConverter<>).MakeGenericType(type)) as BuffConverter;
            if (type.IsArray)
                return Activator.CreateInstance(typeof(ArrayConverter<>).MakeGenericType(type.GetElementType())) as BuffConverter;
            if (type.IsGenericType)
            {
                foreach (var item in _fgenmap.Keys)
                {
                    if (TypeHelper.IsSubclassOfGeneric(type, item))
                    {
                        return Activator.CreateInstance(_fgenmap[item].MakeGenericType(type.GetGenericArguments())) as BuffConverter;
                    }
                }
            }
            if (!type.IsValueType && !type.IsGenericType)
                return Activator.CreateInstance(typeof(ObjectConverter<>).MakeGenericType(type)) as BuffConverter;
            return null;
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

        public abstract object Read(IBufferReader reader, Type type);
        public abstract void Write(IBufferWriter writer, object value);
        internal abstract void CollectMetas(IBufferWriter writer, object value);


        public enum DataType
        {
            Bytes, Json
        }
        public static IBufferReader CreateReader(DataType type)
        {
            if (type == DataType.Bytes) return new BufferReader();
            if (type == DataType.Json) return new JsonReader();

            return null;
        }
        public static IBufferWriter CreateWriter(DataType type)
        {
            if (type == DataType.Bytes) return new BufferWriter();
            if (type == DataType.Json) return new JsonWriter();

            return null;
        }

        public static string ToJson(object obj, bool pretty = false, bool typeInfo = true, bool fullField = false)
        {
            var type = obj.GetType();
            var writer = CreateWriter(DataType.Json);
            var js = writer as JsonWriter;
            js.typeInfo = typeInfo;
            js.prettyPrint = pretty;
            js.fullField = fullField;
            var c = GetConverter(type);
            c.Write(writer, obj);
            return js.GetJson();
        }
        public static object ToObject(string data, Type type)
        {
            var reader = CreateReader(DataType.Json);
            (reader as JsonReader).Init(data);
            var c = GetConverter(type);
            return c.Read(reader, type);
        }
        public static T ToObject<T>(string data) => (T)ToObject(data, typeof(T));




        public static byte[] ToBytes(object obj)
        {
            var type = obj.GetType();
            var writer = CreateWriter(DataType.Bytes);
            var c = GetConverter(type);
            c.Write(writer, obj);
            return (writer as BufferWriter).GetValidBuffer();
        }
        public static object ToObject(byte[] bytes, Type type)
        {
            var reader = CreateReader(DataType.Bytes);
            (reader as BufferReader).Init(bytes);
            var c = GetConverter(type);
            return c.Read(reader, type);
        }
        public static T ToObject<T>(byte[] bytes) => (T)ToObject(bytes, typeof(T));
    }
    public abstract class BuffConverter<T> : BuffConverter
    {
        public abstract void OnWrite(IBufferWriter writer, T value);
        public abstract T OnRead(IBufferReader reader, Type type);
        public sealed override object Read(IBufferReader reader, Type type) => OnRead(reader, type);
        public sealed override void Write(IBufferWriter writer, object value) => OnWrite(writer, (T)value);
        protected virtual void OnCollectMetas(IBufferWriter writer, T value) { }

        internal sealed override void CollectMetas(IBufferWriter writer, object value) => OnCollectMetas(writer, (T)value);
    }

}



namespace ActionBuffer
{

    public class BufferReader : IBufferReader
    {
        private byte[] _buffer;
        private int _index = 0;


        public void Init(byte[] data)
        {
            _buffer = data;
        }
        private void CheckReaderIndex(int length)
        {
            if (_index + length > Capacity)
            {
                throw new Exception("IndexOutOfRangeException");
            }
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
            get { return _buffer.Length; }
        }
        public Enum ReadEnum(Type type)
        {
            long value = ReadInt64();
            return Enum.ToObject(type, value) as Enum;
            //WriteInt64(value);
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
            return _buffer[_index++] == 1;
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
        public string ReadUTF8()
        {
            ushort count = ReadUInt16();
            if (count == 0)
                return string.Empty;
            CheckReaderIndex(count);
            string value = Encoding.UTF8.GetString(_buffer, _index, count);
            _index += count;
            return value;
        }


        public List<T> ReadList<T>(Func<IBufferReader, T> read)
        {
            ushort count = ReadUInt16();

            List<T> values = new List<T>();
            for (int i = 0; i < count; i++)
                values.Add(read(this));
            return values;
        }
        public T[] ReadArray<T>(Func<IBufferReader, T> read)
        {
            ushort count = ReadUInt16();
            T[] values = new T[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = read(this);
            }
            return values;
        }



        private string[] metas;
        public T ReadObject<T>() where T : class
        {
            if (metas == null)
                metas = ReadArray((r) => r.ReadUTF8());
            var typeName = metas[ReadInt32()];
            var assemblyName = metas[ReadInt32()];
            Type type = TypeHelper.GetTypeByFullName(typeName, assemblyName);
            var ObjEnd = ReadInt32();
            if (type == null)
            {
                this._index = ObjEnd;
                return null;
            }
            T t = (T)Activator.CreateInstance(type);

            var typeField = TypeHelper.GetTypeFields(type);
            while (ObjEnd - this._index > 12)
            {
                var FieldEndIndex = ReadInt32();
                var fieldName = metas[this.ReadInt32()];
                var TypeName = metas[this.ReadInt32()];

                TypeName = TypeHelper.GetRealTypeName(TypeName);

                var field = typeField.FindField(fieldName);
                if (field != null && field.FieldType.FullName != TypeName)
                {
                    field = null;
                }
                if (field != null)

                {
                    object value = null;
                    var fieldType = field.FieldType;
                    var convert = BuffConverter.GetConverter(fieldType);
                    value = convert.Read(this, fieldType);
                    field.SetValue(t, value);
                }
                this._index = FieldEndIndex;
            }
            this._index = ObjEnd;
            if (t is IBufferObject buff)
                buff.AfterReadBuffer();

            return t;
        }


    }


    public class BufferWriter : IBufferWriter
    {
        public byte[] GetValidBuffer()
        {
            var data = new byte[_index];
            Buffer.BlockCopy(_buffer, 0, data, 0, _index);
            return data;
        }
        private byte[] _buffer;
        private int _index = 0;
        public int length => _index;
        public byte[] buffer => _buffer;
        public BufferWriter(int capacity = 1024)
        {
            _buffer = new byte[capacity];
        }
        public int Capacity
        {
            get { return _buffer.Length; }
        }

        public void Clear()
        {
            _index = 0;
            metas = null;
            _index_meta = 0;
            _visited.Clear();
        }
        private void CheckWriterIndex(int length)
        {
            if (_index + length > _buffer.Length)
            {
                byte[] bytes = new byte[_buffer.Length * 2];
                Buffer.BlockCopy(_buffer, 0, bytes, 0, _buffer.Length);
                _buffer = bytes;
            }
        }

        public void WriteEnum(Enum data)
        {
            long value = Convert.ToInt64(data);
            WriteInt64(value);
        }

        public void WriteByte(byte value)
        {
            CheckWriterIndex(1);
            _buffer[_index++] = value;
        }
        private void WriteBytes(byte[] value)
        {
            var length = value.Length;
            CheckWriterIndex(length);

            Array.Copy(value, 0, this._buffer, _index, length);
            _index += length;
        }
        public void WriteChar(char value)
        {
            CheckWriterIndex(2);
            _buffer[_index++] = (byte)((value & 0xFF00) >> 8);
            _buffer[_index++] = (byte)(value & 0xFF);
        }
        public void WriteBool(bool value)
        {
            WriteByte((byte)(value ? 1 : 0));
        }
        public void WriteInt16(short value)
        {
            WriteUInt16((ushort)value);
        }
        public void WriteUInt16(ushort value)
        {
            CheckWriterIndex(2);
            _buffer[_index++] = (byte)value;
            _buffer[_index++] = (byte)(value >> 8);
        }
        public void WriteInt32(int value)
        {
            WriteUInt32((uint)value);
        }
        public void WriteUInt32(uint value)
        {
            CheckWriterIndex(4);
            _buffer[_index++] = (byte)value;
            _buffer[_index++] = (byte)(value >> 8);
            _buffer[_index++] = (byte)(value >> 16);
            _buffer[_index++] = (byte)(value >> 24);
        }
        public void WriteFloat(float value)
        {
            var _int = new FloatUnion() { value = value }._int;
            WriteInt32(_int);
        }
        public void WriteDouble(double value)
        {
            var _int = new DoubleUnion() { value = value }._long;
            WriteInt64(_int);

        }


        public void WriteInt64(long value)
        {
            WriteUInt64((ulong)value);
        }
        public void WriteUInt64(ulong value)
        {
            CheckWriterIndex(8);
            _buffer[_index++] = (byte)value;
            _buffer[_index++] = (byte)(value >> 8);
            _buffer[_index++] = (byte)(value >> 16);
            _buffer[_index++] = (byte)(value >> 24);
            _buffer[_index++] = (byte)(value >> 32);
            _buffer[_index++] = (byte)(value >> 40);
            _buffer[_index++] = (byte)(value >> 48);
            _buffer[_index++] = (byte)(value >> 56);
        }


        public void WriteList<T>(List<T> values, Action<IBufferWriter, T> write)
        {
            if (values == null)
                WriteUInt16(0);
            else
            {
                int count = values.Count;
                if (count > ushort.MaxValue)
                    throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue} !");
                WriteUInt16(Convert.ToUInt16(count));
                for (int i = 0; i < count; i++)
                {
                    write.Invoke(this, values[i]);
                }
            }
        }
        public void WriteArray<T>(T[] values, Action<IBufferWriter, T> write)
        {
            if (values == null)
                WriteUInt16(0);
            else
            {
                int count = values.Length;
                if (count > ushort.MaxValue)
                    throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue} !");
                WriteUInt16(Convert.ToUInt16(count));
                for (int i = 0; i < count; i++)
                {
                    write.Invoke(this, values[i]);
                }
            }
        }
        private void WriteByteArray(byte[] values)
        {


            if (values == null)
                WriteUInt16(0);
            else
            {
                int count = values.Length;
                if (count > ushort.MaxValue)
                    throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue} !");
                WriteUInt16(Convert.ToUInt16(count));
                //WriteBytes
                WriteBytes(values);

            }
            //WriteArray(values, (_, value) => { WriteByte(value); });
        }

        public void WriteUTF8(string value)
        {

            if (string.IsNullOrEmpty(value))
                WriteByteArray(null);

            else
                WriteByteArray(Encoding.UTF8.GetBytes(value));
        }


        private Dictionary<string, int> metas;
        private int _index_meta = 0;
        private void AddToMeta(string meta)
        {
            if (metas.ContainsKey(meta)) return;
            metas.Add(meta, _index_meta++);
        }
        private HashSet<Type> _visited = new HashSet<Type>();
        public void CollectMetas(object value)
        {
            if (value == null) return;

            var type = value.GetType();
            if (!_visited.Add(type)) return;

            AddToMeta(type.FullName);
            AddToMeta(type.Assembly.FullName);
            var fields = TypeHelper.GetTypeFields(type).GetFields();
            if (fields == null || fields.Count == 0) return;
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var fieldValue = field.GetValue(value);
                if (TypeHelper.IsNullOrDefault(fieldValue)) continue;


                var fieldType = field.FieldType;
                var fieldName = field.name;
                var fieldTypeName = TypeHelper.GetTypeName(fieldType);
                AddToMeta(fieldName);
                AddToMeta(fieldTypeName);


                var convert = BuffConverter.GetConverter(fieldType);
                convert.CollectMetas(this, fieldValue);
            }

        }

        public void WriteObject<T>(T value) where T : class
        {
            var type = value.GetType();
            if (value is IBufferObject buff)
                buff.BeforeWriteBuffer();
            if (metas == null)
            {
                metas = new();
                CollectMetas(value);
                WriteArray(metas.Keys.ToArray(), (w, val) => w.WriteUTF8(val));
            }

            WriteInt32(metas[type.FullName]);
            WriteInt32(metas[type.Assembly.FullName]);
            var ObjStart = this._index;
            WriteInt32(0);


            var fields = TypeHelper.GetTypeFields(type).GetFields();
            if (fields != null && fields.Count != 0)

                for (int i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var fieldValue = field.GetValue(value);
                    if (TypeHelper.IsNullOrDefault(fieldValue)) continue;
                    var FieldStart = this._index;
                    WriteInt32(0);

                    var fieldType = field.FieldType;
                    WriteInt32(metas[field.name]);
                    WriteInt32(metas[TypeHelper.GetTypeName(fieldType)]);


                    var convert = BuffConverter.GetConverter(fieldType);
                    convert.Write(this, fieldValue);
                    var FieldEnd = this._index;
                    this._index = FieldStart;
                    WriteInt32(FieldEnd);
                    this._index = FieldEnd;
                }




            var ObjEnd = this._index;
            this._index = ObjStart;
            WriteInt32(ObjEnd);
            this._index = ObjEnd;

        }

    }
}
namespace ActionBuffer
{
    public class JsonWriter : IBufferWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Stack<WriteContext> _contexts = new Stack<WriteContext>();
        private bool _prettyPrint, _typeInfo, _fullField;
        private int _indentLevel;

        public bool prettyPrint
        {
            get { return _prettyPrint; }
            set { _prettyPrint = value; }
        }
        public bool typeInfo
        {
            get { return _typeInfo; }
            set { _typeInfo = value; }
        }
        public bool fullField
        {
            get { return _fullField; }
            set { _fullField = value; }
        }


        private struct WriteContext
        {
            public bool IsArray;
            public bool HasElements;
        }

        private void WriteIndent()
        {
            if (!_prettyPrint) return;
            _sb.Append('\n');
            _sb.Append(' ', _indentLevel * 2);
        }

        private void WriteSpaceIfPretty()
        {
            if (_prettyPrint) _sb.Append(' ');
        }

        private void PushContext(bool isArray)
        {
            _contexts.Push(new WriteContext { IsArray = isArray, HasElements = false });
            if (_prettyPrint)
            {
                _indentLevel++;
                WriteIndent();
            }
        }

        private void PopContext()
        {
            if (_prettyPrint)
            {
                _indentLevel--;
                WriteIndent();
            }
            _contexts.Pop();
        }

        private void WriteCommaIfNeeded()
        {
            if (_contexts.Count == 0) return;
            WriteContext ctx = _contexts.Pop();
            if (ctx.HasElements)
                _sb.Append(',');
            else
                ctx.HasElements = true;
            _contexts.Push(ctx);
        }

        private void WriteRaw(string value)
        {
            _sb.Append(value);
        }

        private void WriteString(string value)
        {
            if (value == null)
            {
                WriteRaw("null");
                return;
            }
            _sb.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\b': _sb.Append("\\b"); break;
                    case '\f': _sb.Append("\\f"); break;
                    case '\n': _sb.Append("\\n"); break;
                    case '\r': _sb.Append("\\r"); break;
                    case '\t': _sb.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            _sb.Append(string.Format("\\u{0:X4}", (int)c));
                        else
                            _sb.Append(c);
                        break;
                }
            }
            _sb.Append('"');
        }

        public string GetJson()
        {
            return _sb.ToString();
        }

        public void CollectMetas(object value) { }

        private void WriteTypeInfo(Type type)
        {
            if (!typeInfo) return;
            WriteString("$type");
            WriteRaw(":");
            WriteSpaceIfPretty();
            WriteString(type.FullName);
            WriteCommaIfNeeded();
            WriteIndent();
            WriteString("$assembly");
            WriteRaw(":");
            WriteSpaceIfPretty();
            WriteString(type.Assembly.FullName);
        }

        public void WriteObject<T>(T value) where T : class
        {
            if (value == null)
            {
                WriteRaw("null");
                return;
            }

            IBufferObject buff = value as IBufferObject;
            if (buff != null)
                buff.BeforeWriteBuffer();

            PushContext(false);
            WriteRaw("{");
            if (_prettyPrint) WriteIndent();

            Type actualType = value.GetType();
            WriteTypeInfo(actualType);

            var fields = TypeHelper.GetTypeFields(actualType).GetFields();
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                object fieldValue = field.GetValue(value);
                if (!fullField && TypeHelper.IsNullOrDefault(fieldValue)) continue;

                WriteCommaIfNeeded();
                WriteIndent();
                WriteString(field.name);
                WriteRaw(":");
                WriteSpaceIfPretty();
                BuffConverter.GetConverter(field.FieldType).Write(this, fieldValue);
            }

            PopContext();
            WriteRaw("}");
        }

        public void WriteArray<T>(T[] values, Action<IBufferWriter, T> write)
        {
            if (values == null)
            {
                WriteRaw("null");
                return;
            }
            PushContext(true);
            WriteRaw("[");
            if (_prettyPrint && values.Length > 0) WriteIndent();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    WriteRaw(",");
                    if (_prettyPrint) WriteIndent();
                }
                write(this, values[i]);
            }
            PopContext();
            WriteRaw("]");
        }

        public void WriteList<T>(List<T> values, Action<IBufferWriter, T> write)
        {
            if (values == null)
            {
                WriteRaw("null");
                return;
            }
            PushContext(true);
            WriteRaw("[");
            if (_prettyPrint && values.Count > 0) WriteIndent();
            for (int i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    WriteRaw(",");
                    if (_prettyPrint) WriteIndent();
                }
                write(this, values[i]);
            }
            PopContext();
            WriteRaw("]");
        }

        public void WriteBool(bool value) { WriteRaw(value ? "true" : "false"); }
        public void WriteByte(byte value) { WriteRaw(value.ToString()); }
        public void WriteChar(char value) { WriteString(value.ToString()); }
        public void WriteDouble(double value) { WriteRaw(value.ToString("R", CultureInfo.InvariantCulture)); }
        public void WriteFloat(float value) { WriteRaw(value.ToString("R", CultureInfo.InvariantCulture)); }
        public void WriteInt16(short value) { WriteRaw(value.ToString()); }
        public void WriteInt32(int value) { WriteRaw(value.ToString()); }
        public void WriteInt64(long value) { WriteRaw(value.ToString()); }
        public void WriteUInt16(ushort value) { WriteRaw(value.ToString()); }
        public void WriteUInt32(uint value) { WriteRaw(value.ToString()); }
        public void WriteUInt64(ulong value) { WriteRaw(value.ToString()); }
        public void WriteUTF8(string value) { WriteString(value); }
        public void WriteEnum(Enum data) { WriteString(data.ToString()); }
    }

    public class JsonReader : IBufferReader
    {
        private string _json;
        private int _pos;

        public void Init(string data)
        {
            _json = data;
            _pos = 0;
        }

        private char Peek()
        {
            return _pos < _json.Length ? _json[_pos] : '\0';
        }

        private char Read()
        {
            return _pos < _json.Length ? _json[_pos++] : '\0';
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(Peek()))
                Read();
        }

        private void Expect(char expected)
        {
            SkipWhitespace();
            if (Peek() != expected)
                throw new FormatException(string.Format("Expected '{0}' at position {1}", expected, _pos));
            Read();
        }

        private string ReadNumber()
        {
            SkipWhitespace();
            int start = _pos;
            if (Peek() == '-') Read();
            while (char.IsDigit(Peek())) Read();
            if (Peek() == '.') { Read(); while (char.IsDigit(Peek())) Read(); }
            if (Peek() == 'e' || Peek() == 'E')
            {
                Read();
                if (Peek() == '+' || Peek() == '-') Read();
                while (char.IsDigit(Peek())) Read();
            }
            return _json.Substring(start, _pos - start);
        }

        private string ReadString()
        {
            SkipWhitespace();
            Expect('"');
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                char c = Read();
                if (c == '"') break;
                if (c == '\\')
                {
                    c = Read();
                    switch (c)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            char[] hexChars = new char[4];
                            hexChars[0] = Read();
                            hexChars[1] = Read();
                            hexChars[2] = Read();
                            hexChars[3] = Read();
                            string hex = new string(hexChars);
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            break;
                        default: throw new FormatException(string.Format("Invalid escape sequence \\{0}", c));
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void SkipValue()
        {
            SkipWhitespace();
            char c = Peek();
            if (c == '{')
            {
                Read();
                int depth = 1;
                while (depth > 0)
                {
                    c = Read();
                    if (c == '{') depth++;
                    else if (c == '}') depth--;
                    else if (c == '"') { while (Read() != '"') { } }
                }
            }
            else if (c == '[')
            {
                Read();
                int depth = 1;
                while (depth > 0)
                {
                    c = Read();
                    if (c == '[') depth++;
                    else if (c == ']') depth--;
                    else if (c == '"') { while (Read() != '"') { } }
                }
            }
            else if (c == '"')
            {
                ReadString();
            }
            else
            {
                // 数字、true、false、null
                while (!char.IsWhiteSpace(Peek()) && Peek() != ',' && Peek() != ']' && Peek() != '}')
                    Read();
            }
        }

        public T ReadObject<T>() where T : class
        {
            Expect('{');
            SkipWhitespace();

            // 空对象
            if (Peek() == '}')
            {
                Read();
                T empty = Activator.CreateInstance<T>();
                IBufferObject bufferObj = empty as IBufferObject;
                if (bufferObj != null) bufferObj.AfterReadBuffer();
                return empty;
            }

            // 保存快照，用于回退（无类型信息时）
            int snapshot = _pos;
            string firstKey = ReadString();
            Expect(':');

            Type actualType = typeof(T);
            object instance = null;

            // 情况1：第一个字段是 $type，则读取类型信息
            if (firstKey == "$type")
            {
                string typeFullName = ReadString();
                string assemblyName = null;

                // 读取 $assembly
                SkipWhitespace();
                if (Peek() == ',')
                {
                    Read();
                    SkipWhitespace();
                    string nextKey = ReadString();
                    if (nextKey == "$assembly")
                    {
                        Expect(':');
                        assemblyName = ReadString();
                        // 跳过 $assembly 后面的逗号（如果存在）
                        SkipWhitespace();
                        if (Peek() == ',')
                            Read();
                    }
                    else
                    {
                        throw new FormatException("Expected $assembly after $type");
                    }
                }

                actualType = TypeHelper.GetTypeByFullName(typeFullName, assemblyName);
                if (actualType == null)
                    throw new Exception(string.Format("Cannot resolve type: {0}, {1}", typeFullName, assemblyName));

                instance = Activator.CreateInstance(actualType);
            }
            else
            {
                // 情况2：没有 $type，回退到快照并使用泛型类型 T
                _pos = snapshot;
                instance = Activator.CreateInstance(actualType);
            }

            var typeFields = TypeHelper.GetTypeFields(actualType);
            bool firstField = true;

            // 循环读取剩余字段（普通字段）
            while (true)
            {
                SkipWhitespace();
                if (Peek() == '}')
                {
                    Read();
                    break;
                }
                if (!firstField) Expect(',');
                firstField = false;

                string fieldName = ReadString();
                Expect(':');
                var field = typeFields.FindField(fieldName);
                if (field == null)
                {
                    SkipValue();
                }
                else
                {
                    var converter = BuffConverter.GetConverter(field.FieldType);
                    object value = converter.Read(this, field.FieldType);
                    field.SetValue(instance, value);
                }
            }

            IBufferObject bufferObjFinal = instance as IBufferObject;
            if (bufferObjFinal != null)
                bufferObjFinal.AfterReadBuffer();
            return (T)instance;
        }

        // 其余方法保持不变（ReadArray, ReadList, 基础类型等）
        public T[] ReadArray<T>(Func<IBufferReader, T> read)
        {
            SkipWhitespace();
            Expect('[');
            List<T> list = new List<T>();
            bool first = true;
            while (true)
            {
                SkipWhitespace();
                if (Peek() == ']')
                {
                    Read();
                    break;
                }
                if (!first) Expect(',');
                first = false;
                list.Add(read(this));
            }
            return list.ToArray();
        }

        public List<T> ReadList<T>(Func<IBufferReader, T> read)
        {
            SkipWhitespace();
            Expect('[');
            List<T> list = new List<T>();
            bool first = true;
            while (true)
            {
                SkipWhitespace();
                if (Peek() == ']')
                {
                    Read();
                    break;
                }
                if (!first) Expect(',');
                first = false;
                list.Add(read(this));
            }
            return list;
        }

        public bool ReadBool()
        {
            SkipWhitespace();
            if (Peek() == 't')
            {
                Expect('t'); Expect('r'); Expect('u'); Expect('e');
                return true;
            }
            if (Peek() == 'f')
            {
                Expect('f'); Expect('a'); Expect('l'); Expect('s'); Expect('e');
                return false;
            }
            throw new FormatException("Expected boolean");
        }

        public byte ReadByte() { return (byte)ReadInt64(); }
        public char ReadChar() { return ReadUTF8()[0]; }
        public double ReadDouble() { return double.Parse(ReadNumber(), CultureInfo.InvariantCulture); }
        public float ReadFloat() { return float.Parse(ReadNumber(), CultureInfo.InvariantCulture); }
        public short ReadInt16() { return (short)ReadInt64(); }
        public int ReadInt32() { return (int)ReadInt64(); }
        public long ReadInt64() { return long.Parse(ReadNumber(), CultureInfo.InvariantCulture); }
        public ushort ReadUInt16() { return (ushort)ReadInt64(); }
        public uint ReadUInt32() { return (uint)ReadInt64(); }
        public ulong ReadUInt64() { return (ulong)ReadInt64(); }
        public string ReadUTF8() { return ReadString(); }

        public Enum ReadEnum(Type type)
        {
            string value = ReadString();
            return (Enum)Enum.Parse(type, value);
        }
    }
}
namespace ActionBuffer
{
    class ByteConverter : BuffConverter<byte>
    {
        public override byte OnRead(IBufferReader reader, Type type) => reader.ReadByte();
        public override void OnWrite(IBufferWriter writer, byte value) => writer.WriteByte(value);
    }
    class BoolConverter : BuffConverter<bool>
    {
        public override bool OnRead(IBufferReader reader, Type type) => reader.ReadBool();
        public override void OnWrite(IBufferWriter writer, bool value) => writer.WriteBool(value);
    }

    class CharConverter : BuffConverter<char>
    {
        public override char OnRead(IBufferReader reader, Type type) => reader.ReadChar();
        public override void OnWrite(IBufferWriter writer, char value) => writer.WriteChar(value);
    }

    class ShortConverter : BuffConverter<short>
    {
        public override short OnRead(IBufferReader reader, Type type) => reader.ReadInt16();
        public override void OnWrite(IBufferWriter writer, short value) => writer.WriteInt16(value);
    }
    class IntConverter : BuffConverter<int>
    {
        public override int OnRead(IBufferReader reader, Type type) => reader.ReadInt32();
        public override void OnWrite(IBufferWriter writer, int value) => writer.WriteInt32(value);
    }
    class LongConverter : BuffConverter<long>
    {
        public override long OnRead(IBufferReader reader, Type type) => reader.ReadInt64();
        public override void OnWrite(IBufferWriter writer, long value) => writer.WriteInt64(value);
    }

    class UShortConverter : BuffConverter<ushort>
    {
        public override ushort OnRead(IBufferReader reader, Type type) => reader.ReadUInt16();
        public override void OnWrite(IBufferWriter writer, ushort value) => writer.WriteUInt16(value);
    }
    class UIntConverter : BuffConverter<uint>
    {
        public override uint OnRead(IBufferReader reader, Type type) => reader.ReadUInt32();
        public override void OnWrite(IBufferWriter writer, uint value) => writer.WriteUInt32(value);
    }
    class ULongConverter : BuffConverter<ulong>
    {
        public override ulong OnRead(IBufferReader reader, Type type) => reader.ReadUInt64();
        public override void OnWrite(IBufferWriter writer, ulong value) => writer.WriteUInt64(value);
    }

    class FloatConverter : BuffConverter<float>
    {
        public override float OnRead(IBufferReader reader, Type type) => reader.ReadFloat();
        public override void OnWrite(IBufferWriter writer, float value) => writer.WriteFloat(value);
    }
    class DoubleConverter : BuffConverter<double>
    {
        public override double OnRead(IBufferReader reader, Type type) => reader.ReadDouble();
        public override void OnWrite(IBufferWriter writer, double value) => writer.WriteDouble(value);
    }
    class StringConverter : BuffConverter<string>
    {
        public override string OnRead(IBufferReader reader, Type type) => reader.ReadUTF8();
        public override void OnWrite(IBufferWriter writer, string value) => writer.WriteUTF8(value);
    }
    class DateTimeConverter : BuffConverter<DateTime>
    {
        BuffConverter<long> converter = GetConverter<long>();
        public override DateTime OnRead(IBufferReader reader, Type type)
        {
            return new DateTime(converter.OnRead(reader, typeof(long)));
        }

        public override void OnWrite(IBufferWriter writer, DateTime value)
        {
            converter.OnWrite(writer, value.Ticks);
        }
    }
    class TimeSpanConverter : BuffConverter<TimeSpan>
    {
        BuffConverter<long> converter = GetConverter<long>();
        public override TimeSpan OnRead(IBufferReader reader, Type type)
        {
            return TimeSpan.FromTicks(converter.OnRead(reader, typeof(long)));
        }

        public override void OnWrite(IBufferWriter writer, TimeSpan value)
        {
            converter.OnWrite(writer, value.Ticks);
        }
    }
    class EnumConverter<T> : BuffConverter<T> where T : Enum
    {
        public override T OnRead(IBufferReader reader, Type type) => (T)(Enum)reader.ReadEnum(type);
        public override void OnWrite(IBufferWriter writer, T value) => writer.WriteEnum(value);
    }
    class ObjectConverter<T> : BuffConverter<T> where T : class
    {
        public override T OnRead(IBufferReader reader, Type type) => reader.ReadObject<T>();
        public override void OnWrite(IBufferWriter writer, T value) => writer.WriteObject(value);
        protected override void OnCollectMetas(IBufferWriter writer, T value) => writer.CollectMetas(value);
    }
    class ListConverter<T> : BuffConverter<List<T>>
    {
        static BuffConverter<T> converter = GetConverter<T>();
        private T ReadOnce(IBufferReader reader) => converter.OnRead(reader, typeof(T));
        private void WriteOnce(IBufferWriter writer, T t) => converter.OnWrite(writer, t);

        public override List<T> OnRead(IBufferReader reader, Type type) => reader.ReadList(ReadOnce);
        public override void OnWrite(IBufferWriter writer, List<T> value) => writer.WriteList(value, WriteOnce);
        protected override void OnCollectMetas(IBufferWriter writer, List<T> value)
        {
            for (var i = 0; i < value.Count; i++)
            {
                var t = value[i];
                converter.CollectMetas(writer, t);
            }
        }

    }
    class ArrayConverter<T> : BuffConverter<T[]>
    {
        static BuffConverter<T> converter = GetConverter<T>();
        public override T[] OnRead(IBufferReader reader, Type type) => reader.ReadArray(ReadOnce);
        private void WriteOnce(IBufferWriter writer, T t) => converter.OnWrite(writer, t);

        private T ReadOnce(IBufferReader reader) => converter.OnRead(reader, typeof(T));

        public override void OnWrite(IBufferWriter writer, T[] value) => writer.WriteArray(value, WriteOnce);
        protected override void OnCollectMetas(IBufferWriter writer, T[] value)
        {
            for (var i = 0; i < value.Length; i++)
            {
                var t = value[i];
                converter.CollectMetas(writer, t);
            }
        }
    }



    class DictionaryConverter<Key, Value> : BuffConverter<Dictionary<Key, Value>>
    {
        static BuffConverter<Key> c_k = GetConverter<Key>();
        static BuffConverter<Value> c_v = GetConverter<Value>();

        public override Dictionary<Key, Value> OnRead(IBufferReader reader, Type type)
        {
            var list_key = reader.ReadList(ReadOnce_Key);
            var list_values = reader.ReadList(ReadOnce_Value);

            var dic = new Dictionary<Key, Value>();
            for (int i = 0; i < list_key.Count; i++)
            {
                dic.Add(list_key[i], list_values[i]);
            }

            return dic;

        }

        public override void OnWrite(IBufferWriter writer, Dictionary<Key, Value> value)
        {
            var list_key = value.Keys.ToList();
            var list_values = value.Values.ToList();

            writer.WriteList(list_key, WriteOnce_Key);
            writer.WriteList(list_values, WriteOnce_Value);

        }

        protected override void OnCollectMetas(IBufferWriter writer, Dictionary<Key, Value> value)
        {
            foreach (var kv in value)
            {
                var k = kv.Key;
                var v = kv.Value;
                c_k.CollectMetas(writer, k);
                c_v.CollectMetas(writer, v);
            }
            //base.OnCollectMetas(writer, value);
        }
        private Key ReadOnce_Key(IBufferReader reader) => c_k.OnRead(reader, typeof(Key));
        private void WriteOnce_Key(IBufferWriter writer, Key t) => c_k.OnWrite(writer, t);
        private Value ReadOnce_Value(IBufferReader reader) => c_v.OnRead(reader, typeof(Value));
        private void WriteOnce_Value(IBufferWriter writer, Value value) => c_v.OnWrite(writer, value);
    }
}
