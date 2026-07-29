using System;
using System.Collections.Generic;
using System.Text;
namespace ActionBuffer
{
    public class BufferWriter : IBufferWriter
    {
        private BufferScan scan;
        private bool metasWritten;

        public bool CollectMeta => true;
        public bool FullField => false;

        public byte[] GetValidBuffer()
        {
            var data = new byte[_index];
            Buffer.BlockCopy(_buffer, 0, data, 0, _index);
            return data;
        }
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
        public int length => _index;
        public byte[] buffer => _buffer;
        public BufferWriter() : this(1024) { }

        public BufferWriter(int capacity)
        {
            _buffer = new byte[capacity];
        }

        public void Init(BufferScan scan)
        {
            Clear();
            this.scan = scan ?? throw new ArgumentNullException(nameof(scan));
            scan.ResetRead();
        }

        public int Capacity
        {
            get { return _buffer.Length; }
        }

        public void Clear()
        {
            var currentScan = scan;
            scan = null;
            _index = 0;
            metasWritten = false;
            BufferScan.Back(currentScan);
        }
        private void CheckWriterIndex(int length)
        {
            var requiredLength = checked(_index + length);
            if (requiredLength <= _buffer.Length) return;

            var newCapacity = _buffer.Length;
            while (newCapacity < requiredLength)
            {
                var doubled = newCapacity * 2L;
                newCapacity = doubled > int.MaxValue ? requiredLength : (int)doubled;
            }
            Array.Resize(ref _buffer, newCapacity);
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

        public void WriteIEnumerable<T>(IEnumerable<T> values, Action<IBufferWriter, T> write)
        {
            var cachedValues = RequireScan().ReadEnumerable<T>();
            if (cachedValues == null)
            {
                WriteUInt16(0);
                return;
            }

            WriteUInt16((ushort)cachedValues.Count);
            for (int i = 0; i < cachedValues.Count; i++)
                write(this, cachedValues[i]);
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

        private BufferScan RequireScan()
        {
            if (scan == null)
                throw new InvalidOperationException("BufferWriter requires a completed BufferScan before writing objects or collections.");
            return scan;
        }

        private void WriteMetas()
        {
            var scan = RequireScan();
            if (scan.MetaCount > ushort.MaxValue)
                throw new FormatException($"Write meta count cannot be greater than {ushort.MaxValue} !");
            WriteUInt16((ushort)scan.MetaCount);
            for (int i = 0; i < scan.MetaCount; i++)
                WriteUTF8(scan.GetMeta(i));
            metasWritten = true;
        }


        public void WriteObject<T>(T value, TypeHelper.TypeFields _fields)
        {
            var scan = RequireScan();
            var cached = scan.ReadObject();
            if (cached.Value == null) return;
            if (!metasWritten)
                WriteMetas();

            WriteInt32(scan.GetMetaIndex(cached.Type.FullName));
            WriteInt32(scan.GetMetaIndex(cached.Type.Assembly.FullName));
            var ObjStart = this._index;
            WriteInt32(0);
            for (int i = 0; i < cached.Fields.Count; i++)
            {
                var cachedField = cached.Fields[i];
                var FieldStart = this._index;
                WriteInt32(0);
                WriteInt32(scan.GetMetaIndex(cachedField.Field.name));
                WriteInt32(scan.GetMetaIndex(TypeHelper.GetTypeName(cachedField.Field.FieldType)));
                cachedField.Converter.Write(this, cachedField.Value);
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
