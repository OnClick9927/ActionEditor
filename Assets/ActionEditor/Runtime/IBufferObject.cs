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
    static class TypeHelper
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


        public List<T> ReadList<T>(Func<T> read)
        {
            ushort count = ReadUInt16();
            List<T> values = new List<T>();
            for (int i = 0; i < count; i++)
                values.Add(read());
            return values;
        }
        public T[] ReadArray<T>(Func<T> read)
        {
            ushort count = ReadUInt16();
            T[] values = new T[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = read();
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
        public T ReadObject<T>() where T : class, IBufferObject
        {
            var typeName = ReadUTF8();
            Type type = Asset.GetType(typeName);
            T t = Activator.CreateInstance(type) as T;

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
                var field = typeField.FindField(id);
                if (field != null && field.field.FieldType.FullName != TypeName)
                {
                    field = null;
                }
                if (field != null)

                {
                    object value = null;
                    var fieldType = field.field.FieldType;

                    if (fieldType.IsEnum) value = this.ReadEnum(fieldType);
                    else if (fieldType == typeof(byte)) value = this.ReadByte();
                    else if (fieldType == typeof(char)) value = this.ReadChar();
                    else if (fieldType == typeof(bool)) value = this.ReadBool();
                    else if (fieldType == typeof(short)) value = this.ReadInt16();
                    else if (fieldType == typeof(ushort)) value = this.ReadUInt16();
                    else if (fieldType == typeof(int)) value = this.ReadInt32();
                    else if (fieldType == typeof(uint)) value = this.ReadUInt32();
                    else if (fieldType == typeof(long)) value = this.ReadInt64();
                    else if (fieldType == typeof(ulong)) value = this.ReadUInt64();
                    else if (fieldType == typeof(float)) value = this.ReadFloat();
                    else if (fieldType == typeof(double)) value = this.ReadDouble();
                    else if (fieldType == typeof(string)) value = this.ReadUTF8();
                    if (value != null)
                        field.field.SetValue(t, value);
                    else
                        t.ReadField(field.name, this);
                }

                MoveToFieldEnd();



            }
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


        public void WriteList<T>(List<T> values, Action<T> write)
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
                    write.Invoke(values[i]);
                }
            }
        }
        public void WriteArray<T>(T[] values, Action<T> write)
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
                    write.Invoke(values[i]);
                }
            }
        }
        public void WriteByteArray(byte[] values) => WriteArray(values, WriteByte);
        public void WriteUTF8(string value) => WriteByteArray(string.IsNullOrEmpty(value) ? null : Encoding.UTF8.GetBytes(value));


        private static bool IsNullOrDefault<T>(T t)
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
        public void WriteObject<T>(T value) where T : IBufferObject
        {
            var type = value.GetType();
            WriteUTF8(type.FullName);
            //value.WriteData(this);
            var fields = TypeHelper.GetTypeFields(type).GetFields();
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var fieldValue = field.field.GetValue(value);
                if (IsNullOrDefault(fieldValue)) continue;
                WriteBytes(BufferReader.FieldBeginFlag);


                WriteUTF8(field.name);
                var fieldType = field.field.FieldType;
                WriteUTF8(fieldType.FullName);

                if (fieldType.IsEnum) this.WriteEnum((Enum)fieldValue);
                else if (fieldType == typeof(byte)) this.WriteByte((byte)fieldValue);
                else if (fieldType == typeof(char)) this.WriteChar((char)fieldValue);
                else if (fieldType == typeof(bool)) this.WriteBool((bool)fieldValue);
                else if (fieldType == typeof(short)) this.WriteInt16((short)fieldValue);
                else if (fieldType == typeof(ushort)) this.WriteUInt16((ushort)fieldValue);
                else if (fieldType == typeof(int)) this.WriteInt32((int)fieldValue);
                else if (fieldType == typeof(uint)) this.WriteUInt32((uint)fieldValue);
                else if (fieldType == typeof(long)) this.WriteInt64((long)fieldValue);
                else if (fieldType == typeof(ulong)) this.WriteUInt64((ulong)fieldValue);
                else if (fieldType == typeof(float)) this.WriteFloat((float)fieldValue);
                else if (fieldType == typeof(double)) this.WriteDouble((double)fieldValue);
                else if (fieldType == typeof(string)) this.WriteUTF8((string)fieldValue);
                else
                {
                    value.WriteField(field.name, this);
                }
                WriteBytes(BufferReader.FieldEndFlag);
            }


            WriteBytes(BufferReader.EndOfObjFlag);
        }

    }

}
