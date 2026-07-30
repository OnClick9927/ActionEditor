using System;
using System.Collections.Generic;
using System.Text;
namespace ActionBuffer
{
    public class BufferWriter : IBufferWriter
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
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
                if (value < 0 || value > _buffer.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                _index = value;
            }
        }
        public int length => _index;
        public byte[] buffer => _buffer;
        public BufferWriter() : this(1024) { }

        public BufferWriter(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _buffer = new byte[capacity];
        }

        public void Init()
        {
            Clear();
        }

        public int Capacity
        {
            get { return _buffer.Length; }
        }

        public void Clear()
        {
            _index = 0;
            metasWritten = false;
            if (_buffer.Length > BufferSerializer.RetainedBinaryCapacity)
                _buffer = new byte[1024];
        }
        private void CheckWriterIndex(int length)
        {
            var requiredLength = checked(_index + length);
            if (requiredLength > BufferSerializer.MaxBinaryLength)
                throw new FormatException(
                    $"Binary data length cannot exceed {BufferSerializer.MaxBinaryLength} bytes.");
            if (requiredLength <= _buffer.Length) return;

            var newCapacity = Math.Max(_buffer.Length, 1);
            while (newCapacity < requiredLength)
            {
                var doubled = newCapacity * 2L;
                newCapacity = doubled > int.MaxValue ? requiredLength : (int)doubled;
            }
            Array.Resize(ref _buffer, newCapacity);
        }

        public void WriteEnum(Enum data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var underlyingType = Enum.GetUnderlyingType(data.GetType());
            bool signed = underlyingType == typeof(sbyte) || underlyingType == typeof(short) ||
                          underlyingType == typeof(int) || underlyingType == typeof(long);
            ulong value = signed
                ? unchecked((ulong)Convert.ToInt64(data))
                : Convert.ToUInt64(data);
            WriteUInt64(value);
        }

        public void WriteByte(byte value)
        {
            CheckWriterIndex(1);
            _buffer[_index++] = value;
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

        public void WriteGuid(Guid value)
        {
            CheckWriterIndex(16);
            if (!value.TryWriteBytes(new Span<byte>(_buffer, _index, 16)))
                throw new InvalidOperationException("Could not write Guid bytes.");
            _index += 16;
        }

        public void WriteIEnumerable<T>(BufferScan scan, IEnumerable<T> values,
            Action<IBufferWriter, BufferScan, T> write)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            var cachedValues = scan.ReadEnumerable<T>();
            if (cachedValues == null)
            {
                WriteUInt16(ushort.MaxValue);
                return;
            }

            if (cachedValues.Count >= ushort.MaxValue)
                throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue - 1} !");
            WriteUInt16((ushort)cachedValues.Count);
            for (int i = 0; i < cachedValues.Count; i++)
                write(this, scan, cachedValues[i]);
        }

        public void WriteArray2D<T>(BufferScan scan, T[,] values,
            Action<IBufferWriter, BufferScan, T> write)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (write == null) throw new ArgumentNullException(nameof(write));
            var cachedValues = scan.ReadArray2D<T>(out int rows, out int columns);
            if (cachedValues == null)
            {
                WriteUInt16(ushort.MaxValue);
                return;
            }

            WriteUInt16((ushort)rows);
            WriteUInt16((ushort)columns);
            for (int i = 0; i < cachedValues.Count; i++)
                write(this, scan, cachedValues[i]);
        }

        public void WriteNullable<T>(BufferScan scan, T? value,
            Action<IBufferWriter, BufferScan, T> write) where T : struct
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (write == null) throw new ArgumentNullException(nameof(write));
            WriteBool(value.HasValue);
            if (value.HasValue)
                write(this, scan, value.Value);
        }

        public void WriteKeyValuePair<TKey, TValue>(BufferScan scan, KeyValuePair<TKey, TValue> value,
            Action<IBufferWriter, BufferScan, TKey> writeKey,
            Action<IBufferWriter, BufferScan, TValue> writeValue)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (writeKey == null) throw new ArgumentNullException(nameof(writeKey));
            if (writeValue == null) throw new ArgumentNullException(nameof(writeValue));
            writeKey(this, scan, value.Key);
            writeValue(this, scan, value.Value);
        }
        public void WriteUTF8(string value)
        {
            if (value == null)
            {
                WriteInt32(-1);
                return;
            }

            int count;
            try
            {
                count = Utf8.GetByteCount(value);
            }
            catch (EncoderFallbackException exception)
            {
                throw new FormatException("String contains an unpaired UTF-16 surrogate.", exception);
            }
            if (count > BufferSerializer.MaxScalarLength)
                throw new FormatException(
                    $"UTF-8 byte count cannot exceed {BufferSerializer.MaxScalarLength}.");
            WriteInt32(count);
            CheckWriterIndex(count);
            _index += Utf8.GetBytes(value, 0, value.Length, _buffer, _index);
        }

        private void WriteMetas(BufferScan scan)
        {
            if (scan.MetaCount >= ushort.MaxValue)
                throw new FormatException($"Write meta count cannot be greater than {ushort.MaxValue - 1} !");
            WriteUInt16((ushort)scan.MetaCount);
            for (int i = 0; i < scan.MetaCount; i++)
                WriteUTF8(scan.GetMeta(i));
            metasWritten = true;
        }


        public void WriteObject<T>(BufferScan scan, T value, TypeHelper.TypeFields _fields)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            var cached = scan.ReadObject();
            if (!metasWritten)
                WriteMetas(scan);
            if (cached.Value == null)
            {
                WriteInt32(-1);
                return;
            }
            WriteInt32(scan.GetMetaIndex(cached.Type.FullName));
            WriteInt32(scan.GetMetaIndex(cached.Type.Assembly.FullName));
            var ObjStart = this._index;
            WriteInt32(0);
            for (int i = 0; i < cached.FieldCount; i++)
            {
                var cachedField = cached.GetField(i);
                var FieldStart = this._index;
                WriteInt32(0);
                WriteInt32(scan.GetMetaIndex(cachedField.Field.name));
                WriteInt32(scan.GetMetaIndex(TypeHelper.GetTypeName(cachedField.Field.FieldType)));
                cachedField.Converter.Write(this, scan, cachedField.Value);
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
