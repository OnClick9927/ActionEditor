using System;
using System.Collections.Generic;
using System.Text;
namespace ActionBuffer
{
    public class BufferReader : IBufferReader
    {
        private byte[] _buffer;
        private int _index = 0;
        public int index
        {
            get { return _index; }
            set
            {

                if (value < 0) value = 0;
                if (value >= _buffer.Length) value = _buffer.Length + 1;
                _index = value;
            }
        }
        public void Init(byte[] data)
        {
            Clear();
            _buffer = data;
        }

        public void Clear()
        {
            _buffer = null;
            _index = 0;
            if (metas == null) return;
            metas.Clear();
            ClassPool<List<string>>.Back(metas);
            metas = null;
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
        public List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read)
        {
            ushort count = ReadUInt16();

            List<T> values = result;
            for (int i = 0; i < count; i++)
                values.Add(read(this));
            return values;
        }
        private List<string> metas;

        public T ReadObject<T>(object instance, TypeHelper.TypeFields fields)
        {
            if (metas == null)
            {
                metas = ClassPool<List<string>>.Get();
                metas.Clear();
                ReadIEnumerable(metas, (r) => r.ReadUTF8());
            }
            var typeName = metas[ReadInt32()];
            var assemblyName = metas[ReadInt32()];
            Type type = TypeHelper.GetTypeByFullName(typeName, assemblyName);
            var ObjEnd = ReadInt32();
            if (type == null)
            {
                this._index = ObjEnd;
                return default;
            }
            //object t = instance;

            //var typeField = TypeHelper.GetTypeFields(type);
            while (ObjEnd - this._index > 12)
            {
                var FieldEndIndex = ReadInt32();
                var fieldName = metas[this.ReadInt32()];
                var TypeName = metas[this.ReadInt32()];

                TypeName = TypeHelper.GetRealTypeName(TypeName);

                var field = fields.FindField(fieldName);
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
                    field.SetValue(instance, value);
                }
                this._index = FieldEndIndex;
            }
            this._index = ObjEnd;
            if (instance is IBufferObject buff)
                buff.AfterReadBuffer();

            return (T)instance;
        }

        public T ReadObject<T>()
        {
            if (metas == null)
            {
                metas = ClassPool<List<string>>.Get();
                metas.Clear();
                ReadIEnumerable(metas, (r) => r.ReadUTF8());
            }
            var typeName = metas[ReadInt32()];
            var assemblyName = metas[ReadInt32()];
            Type type = TypeHelper.GetTypeByFullName(typeName, assemblyName);
            var ObjEnd = ReadInt32();
            if (type == null)
            {
                this._index = ObjEnd;
                return default;
            }
            object t = TypeHelper.CreateInstance(type);

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

            return (T)t;
        }


    }
}
