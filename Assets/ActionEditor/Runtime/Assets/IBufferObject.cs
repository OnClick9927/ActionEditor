using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;


namespace ActionEditor
{

    [AttributeUsage(AttributeTargets.Field)]
    public class BufferIgnoreAttribute : System.Attribute { }
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
                public readonly FieldInfo field;
                public readonly string name;

                public Field(FieldInfo field, string name)
                {
                    this.field = field;
                    this.name = name;
                }

            }
            public TypeFields(Type type)
            {
                this.type = type;
            }
            private List<Field> fields;
            private Dictionary<string, Field> map;
            public List<Field> GetFields() => fields;
            public Field FindField(string name)
            {
                if (map.TryGetValue(name, out var field)) return field;
                return null;
            }
            public void AddField(FieldInfo field)
            {

                if (field.IsDefined(typeof(BufferIgnoreAttribute))) return;
                fields = fields ?? new List<Field>();
                map = map ?? new Dictionary<string, Field>();
                var attr = field.GetCustomAttribute<BufferAttribute>();
                if (!field.IsPublic && attr == null) return;
                var name = attr?.bufferName ?? field.Name;
                if (map.TryGetValue(name, out var info))
                    throw new Exception($"{type}Exist Same Name Field {name}=> {info.field.DeclaringType}:{field.DeclaringType}");
                var _f = new Field(field, name);

                map[name] = _f;
                fields.Add(_f);
            }
        }
        private static Dictionary<Type, TypeFields> map = new Dictionary<Type, TypeFields>();
        public static TypeFields GetTypeFields(Type type)
        {
            if (map.TryGetValue(type, out var typefield)) return typefield;
            var _type = type;
            while (true)
            {
                var fields = _type.GetFields(BindingFlags.Public
                 | BindingFlags.NonPublic
                 | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                typefield = typefield ?? new TypeFields(type);
                for (int i = 0; i < fields.Length; i++)
                {
                    var _field = fields[i];
                    typefield.AddField(_field);
                }
                TypeHelper.map[_type] = typefield;
                if (_type.BaseType == typeof(System.Object))
                    break;
                _type = _type.BaseType;
            }
            return TypeHelper.map[type];
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
        public static Type GetTypeByFullName(string typeFullName, string assemblyName = null)
        {
            if (string.IsNullOrEmpty(typeFullName))
                return null;

            // 如果指定了程序集名称，拼接完整的类型标识
            string fullTypeName = string.IsNullOrEmpty(assemblyName)
                ? typeFullName
                : $"{typeFullName}, {assemblyName}";

            // 尝试直接获取类型
            Type type = Type.GetType(fullTypeName);

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

            return type;
        }

        public static T DeepCopyByBuffer<T>(this T value) => BuffConverter.ToObject<T>(BuffConverter.ToBytes(value));
        public static bool IsNullOrDefault<T>(T t)
        {
            Type type = typeof(T);
            if (type.IsEnum) return false;
            if (ReferenceEquals(t, null)) return true;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var hasValueProperty = type.GetProperty("HasValue");
                bool hasValue = (bool)hasValueProperty.GetValue(t);
                return !hasValue;
            }
            return EqualityComparer<T>.Default.Equals(t, default(T));
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
        void WriteField(string id, BufferWriter writer);
        void ReadField(string id, BufferReader reader);
        void AfterReadField();
        void BeforeWriteField();
    }
    public class BufferReader
    {
        internal static byte[] FieldEndFlag = new byte[2] { 0, byte.MaxValue };
        internal static byte[] FieldBeginFlag = new byte[2] { byte.MaxValue, 0 };
        internal static byte[] EndOfObjFlag = new byte[4] { 2, 0, 46, 46 };
        private readonly byte[] _buffer;
        private int _index = 0;

        public BufferReader(byte[] data)
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
            if (BitConverter.IsLittleEndian)
            {
                short value = (short)((_buffer[_index]) | (_buffer[_index + 1] << 8));
                _index += 2;
                return value;
            }
            else
            {
                short value = (short)((_buffer[_index] << 8) | (_buffer[_index + 1]));
                _index += 2;
                return value;
            }
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
            if (BitConverter.IsLittleEndian)
            {
                int value = (_buffer[_index]) | (_buffer[_index + 1] << 8) | (_buffer[_index + 2] << 16) | (_buffer[_index + 3] << 24);
                _index += 4;
                return value;
            }
            else
            {
                int value = (_buffer[_index] << 24) | (_buffer[_index + 1] << 16) | (_buffer[_index + 2] << 8) | (_buffer[_index + 3]);
                _index += 4;
                return value;
            }
        }
        public uint ReadUInt32()
        {
            return (uint)ReadInt32();
        }
        public long ReadInt64()
        {
            CheckReaderIndex(8);
            if (BitConverter.IsLittleEndian)
            {
                int i1 = (_buffer[_index]) | (_buffer[_index + 1] << 8) | (_buffer[_index + 2] << 16) | (_buffer[_index + 3] << 24);
                int i2 = (_buffer[_index + 4]) | (_buffer[_index + 5] << 8) | (_buffer[_index + 6] << 16) | (_buffer[_index + 7] << 24);
                _index += 8;
                return (uint)i1 | ((long)i2 << 32);
            }
            else
            {
                int i1 = (_buffer[_index] << 24) | (_buffer[_index + 1] << 16) | (_buffer[_index + 2] << 8) | (_buffer[_index + 3]);
                int i2 = (_buffer[_index + 4] << 24) | (_buffer[_index + 5] << 16) | (_buffer[_index + 6] << 8) | (_buffer[_index + 7]);
                _index += 8;
                return (uint)i2 | ((long)i1 << 32);
            }
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


        public List<T> ReadList<T>(Func<BufferReader, T> read)
        {
            ushort count = ReadUInt16();
            List<T> values = new List<T>();
            for (int i = 0; i < count; i++)
                values.Add(read(this));
            return values;
        }
        public T[] ReadArray<T>(Func<BufferReader, T> read)
        {
            ushort count = ReadUInt16();
            T[] values = new T[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = read(this);
            }
            return values;
        }


        private bool EqualsBuffer(int index, byte[] comare)
        {
            for (int i = 0; i < comare.Length; i++)
            {
                var src = this._buffer[index + i];
                if (src != comare[i])
                {
                    return false;
                }
            }
            return true;
        }
        private void MoveToFieldEnd()
        {
            var start = 0;
            var end = 0;

            for (int i = this._index; i < this._buffer.Length; i++)
            {
                if (EqualsBuffer(i, FieldEndFlag))
                    end++;
                else if (EqualsBuffer(i, FieldBeginFlag))
                    start++;
                if (end == start + 1)
                {
                    this._index = i + FieldEndFlag.Length;
                    break;
                }
            }
        }
        public T ReadObject<T>() where T : class
        {
            var typeName = ReadUTF8();
            var assemblyName = ReadUTF8();
            Type type = TypeHelper.GetTypeByFullName(typeName, assemblyName);
            T t = (T)Activator.CreateInstance(type);

            var typeField = TypeHelper.GetTypeFields(type);
            while (true)
            {
                if (!EqualsBuffer(this._index, EndOfObjFlag))
                {
                    this._index += FieldBeginFlag.Length;
                }
                else
                {
                    this._index += EndOfObjFlag.Length;
                    break;
                }
                //if (id == BufferReader.ObjEndFlag)
                //    break;
                var id = this.ReadUTF8();
                var TypeName = this.ReadUTF8();
                TypeName = TypeHelper.GetRealTypeName(TypeName);

                var field = typeField.FindField(id);
                if (field != null && field.field.FieldType.FullName != TypeName)
                {
                    field = null;
                }
                if (field != null)

                {
                    object value = null;
                    var fieldType = field.field.FieldType;
                    BuffConverter convert = null;

                    try
                    {
                        convert = BuffConverter.GetConverter(fieldType);
                        value = convert.Read(this, fieldType);
                        field.field.SetValue(t, value);

                    }
                    catch (Exception)
                    {
                        if (convert == null && t is IBufferObject _buff)
                            _buff.ReadField(field.name, this);
                        throw;
                    }


                }

                MoveToFieldEnd();



            }
            if (t is IBufferObject buff)
                buff.AfterReadField();

            return t;
        }


    }
    public class BufferWriter
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
            for (int i = 0; i < value.Length; i++)
            {
                WriteByte(value[i]);
            }
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


        public void WriteList<T>(List<T> values, Action<BufferWriter, T> write)
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
        public void WriteArray<T>(T[] values, Action<BufferWriter, T> write)
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
        public void WriteByteArray(byte[] values) => WriteArray(values, (_, value) => { WriteByte(value); });
        public void WriteUTF8(string value) => WriteByteArray(string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value));


        public void WriteObject<T>(T value) where T : class
        {
            var type = value.GetType();
            if (value is IBufferObject buff)
                buff.BeforeWriteField();
            WriteUTF8(type.FullName);
            WriteUTF8(type.Assembly.FullName);

            var fields = TypeHelper.GetTypeFields(type).GetFields();
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var fieldValue = field.field.GetValue(value);
                if (TypeHelper.IsNullOrDefault(fieldValue)) continue;
                WriteBytes(BufferReader.FieldBeginFlag);


                WriteUTF8(field.name);
                var fieldType = field.field.FieldType;
                WriteUTF8(TypeHelper.GetTypeName(fieldType));

                BuffConverter convert = null;
                try
                {
                    convert = BuffConverter.GetConverter(fieldType);
                    convert.Write(this, fieldValue);
                }
                catch (Exception)
                {
                    if (convert == null && value is IBufferObject _buff)
                        _buff.WriteField(field.name, this);
                    else
                        throw;
                }
                WriteBytes(BufferReader.FieldEndFlag);
            }


            WriteBytes(BufferReader.EndOfObjFlag);
        }

    }

}
namespace ActionEditor
{

    public abstract class BuffConverter
    {
        private static Dictionary<Type, Type> _nmap = new Dictionary<Type, Type>()
        {

            { typeof(byte),typeof(ByteConverter)},
            {typeof(bool),typeof(BoolConverter) },
            { typeof(char),typeof(CharConverter) },
            { typeof(short),typeof(ShortConverter)},
            { typeof(ushort),typeof(UShortConverter)},
            { typeof(int),typeof(IntConverter)},
            { typeof(uint),typeof(UIntConverter)},
            { typeof(long),typeof(LongConverter)},
            { typeof(ulong),typeof(ULongConverter)},
            { typeof(float),typeof(FloatConverter)},
            { typeof(double),typeof(DoubleConverter)},
            { typeof(string),typeof(StringConverter)},
            { typeof(DateTime),typeof(DateTimeConverter)},
            { typeof(TimeSpan),typeof(TimeSpanConverter)},
        };

        private static Dictionary<Type, Type> _fgenmap = new Dictionary<Type, Type>()
        {
            { typeof(List<>), typeof(ListConverter<>) }

        };
        private static Dictionary<Type, BuffConverter> map = new Dictionary<Type, BuffConverter>();
        public static event Func<Type, BuffConverter> OnGetConverter;
        private static BuffConverter Create(Type type)
        {
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
                    if (TypeHelper.IsSubclassOfGeneric(type, typeof(List<>)))
                    {
                        return Activator.CreateInstance(_fgenmap[item].MakeGenericType(type.GetGenericArguments())) as BuffConverter;
                    }
                }
            }
            var result = OnGetConverter?.Invoke(type);
            if (result != null) return result;
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

        public abstract object Read(BufferReader reader, Type type);
        public abstract void Write(BufferWriter writer, object value);
        public static byte[] ToBytes(object obj)
        {
            var type = obj.GetType();
            BufferWriter writer = new BufferWriter();
            var c = GetConverter(type);
            c.Write(writer, obj);
            return writer.GetValidBuffer();
        }
        public static object ToObject(byte[] bytes, Type type)
        {
            BufferReader reader = new BufferReader(bytes);
            var c = GetConverter(type);
            return c.Read(reader, type);
        }
        public static T ToObject<T>(byte[] bytes) => (T)ToObject(bytes, typeof(T));

    }
    public abstract class BuffConverter<T> : BuffConverter
    {
        public abstract void OnWrite(BufferWriter writer, T value);
        public abstract T OnRead(BufferReader reader, Type type);
        public sealed override object Read(BufferReader reader, Type type) => OnRead(reader, type);
        public sealed override void Write(BufferWriter writer, object value) => OnWrite(writer, (T)value);
    }

    class ByteConverter : BuffConverter<byte>
    {
        public override byte OnRead(BufferReader reader, Type type) => reader.ReadByte();
        public override void OnWrite(BufferWriter writer, byte value) => writer.WriteByte(value);
    }
    class BoolConverter : BuffConverter<bool>
    {
        public override bool OnRead(BufferReader reader, Type type) => reader.ReadBool();
        public override void OnWrite(BufferWriter writer, bool value) => writer.WriteBool(value);
    }

    class CharConverter : BuffConverter<char>
    {
        public override char OnRead(BufferReader reader, Type type) => reader.ReadChar();
        public override void OnWrite(BufferWriter writer, char value) => writer.WriteChar(value);
    }

    class ShortConverter : BuffConverter<short>
    {
        public override short OnRead(BufferReader reader, Type type) => reader.ReadInt16();
        public override void OnWrite(BufferWriter writer, short value) => writer.WriteInt16(value);
    }
    class IntConverter : BuffConverter<int>
    {
        public override int OnRead(BufferReader reader, Type type) => reader.ReadInt32();
        public override void OnWrite(BufferWriter writer, int value) => writer.WriteInt32(value);
    }
    class LongConverter : BuffConverter<long>
    {
        public override long OnRead(BufferReader reader, Type type) => reader.ReadInt64();
        public override void OnWrite(BufferWriter writer, long value) => writer.WriteInt64(value);
    }

    class UShortConverter : BuffConverter<ushort>
    {
        public override ushort OnRead(BufferReader reader, Type type) => reader.ReadUInt16();
        public override void OnWrite(BufferWriter writer, ushort value) => writer.WriteUInt16(value);
    }
    class UIntConverter : BuffConverter<uint>
    {
        public override uint OnRead(BufferReader reader, Type type) => reader.ReadUInt32();
        public override void OnWrite(BufferWriter writer, uint value) => writer.WriteUInt32(value);
    }
    class ULongConverter : BuffConverter<ulong>
    {
        public override ulong OnRead(BufferReader reader, Type type) => reader.ReadUInt64();
        public override void OnWrite(BufferWriter writer, ulong value) => writer.WriteUInt64(value);
    }

    class FloatConverter : BuffConverter<float>
    {
        public override float OnRead(BufferReader reader, Type type) => reader.ReadFloat();
        public override void OnWrite(BufferWriter writer, float value) => writer.WriteFloat(value);
    }
    class DoubleConverter : BuffConverter<double>
    {
        public override double OnRead(BufferReader reader, Type type) => reader.ReadDouble();
        public override void OnWrite(BufferWriter writer, double value) => writer.WriteDouble(value);
    }
    class StringConverter : BuffConverter<string>
    {
        public override string OnRead(BufferReader reader, Type type) => reader.ReadUTF8();
        public override void OnWrite(BufferWriter writer, string value) => writer.WriteUTF8(value);
    }
    class DateTimeConverter : BuffConverter<DateTime>
    {
        BuffConverter<long> converter = GetConverter<long>();
        public override DateTime OnRead(BufferReader reader, Type type)
        {
            return new DateTime(converter.OnRead(reader, typeof(long)));
        }

        public override void OnWrite(BufferWriter writer, DateTime value)
        {
            converter.OnWrite(writer, value.Ticks);
        }
    }
    class TimeSpanConverter : BuffConverter<TimeSpan>
    {
        BuffConverter<long> converter = GetConverter<long>();
        public override TimeSpan OnRead(BufferReader reader, Type type)
        {
            return TimeSpan.FromTicks(converter.OnRead(reader, typeof(long)));
        }

        public override void OnWrite(BufferWriter writer, TimeSpan value)
        {
            converter.OnWrite(writer, value.Ticks);
        }
    }
    class EnumConverter<T> : BuffConverter<T> where T : Enum
    {
        public override T OnRead(BufferReader reader, Type type) => (T)(Enum)reader.ReadEnum(type);
        public override void OnWrite(BufferWriter writer, T value) => writer.WriteEnum(value);
    }
    class ObjectConverter<T> : BuffConverter<T> where T : class
    {
        public override T OnRead(BufferReader reader, Type type) => reader.ReadObject<T>();
        public override void OnWrite(BufferWriter writer, T value) => writer.WriteObject(value);
    }
    class ListConverter<T> : BuffConverter<List<T>>
    {
        static BuffConverter<T> converter = GetConverter<T>();
        private T ReadOnce(BufferReader reader) => converter.OnRead(reader, typeof(T));
        private void WriteOnce(BufferWriter writer, T t) => converter.OnWrite(writer, t);

        public override List<T> OnRead(BufferReader reader, Type type) => reader.ReadList(ReadOnce);
        public override void OnWrite(BufferWriter writer, List<T> value) => writer.WriteList(value, WriteOnce);


    }
    class ArrayConverter<T> : BuffConverter<T[]>
    {
        static BuffConverter<T> converter = GetConverter<T>();
        public override T[] OnRead(BufferReader reader, Type type) => reader.ReadArray(ReadOnce);
        private void WriteOnce(BufferWriter writer, T t) => converter.OnWrite(writer, t);

        private T ReadOnce(BufferReader reader) => converter.OnRead(reader, typeof(T));

        public override void OnWrite(BufferWriter writer, T[] value) => writer.WriteArray(value, WriteOnce);
    }


}
