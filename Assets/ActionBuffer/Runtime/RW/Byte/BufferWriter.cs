using System;
using System.Collections.Generic;
using System.Text;
namespace ActionBuffer
{
    public class BufferWriter : IBufferWriter, ITypedEnumWriter
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private bool metasWritten;
        private int _maxBinaryLength = BuffSettings.MaxBinaryLength;
        private int _maxScalarLength = BuffSettings.MaxScalarLength;

        public bool CollectMeta => true;

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

        public void Init(BufferScan scan)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            Clear();
            _maxBinaryLength = BuffSettings.MaxBinaryLength;
            _maxScalarLength = BuffSettings.MaxScalarLength;
        }

        public int Capacity
        {
            get { return _buffer.Length; }
        }

        public void Clear()
        {
            _index = 0;
            metasWritten = false;
        }
        private void CheckWriterIndex(int length)
        {
            var requiredLength = checked(_index + length);
            if (requiredLength > _maxBinaryLength)
                throw new FormatException(
                    $"Binary data length cannot exceed {_maxBinaryLength} bytes.");
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

        void ITypedEnumWriter.WriteEnumValue<T>(T value) =>
            WriteUInt64(EnumValue<T>.ToUInt64(value));

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

        public void WriteIEnumerable<T>(BufferScan scan, BuffConverter<T> converter)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var cachedValues = scan.ReadEnumerable<T>(out int referenceId,
                out bool isReference);
            if (!WriteCollectionHeader(scan, cachedValues?.Count ?? 0,
                    cachedValues == null && !isReference,
                    referenceId, isReference))
                return;

            for (int i = 0; i < cachedValues.Count; i++)
                converter.WriteValue(this, scan, cachedValues[i]);
        }

        internal void TrimCapacity()
        {
            if (_buffer.Length > BuffSettings.RetainedBinaryCapacity)
                _buffer = new byte[1024];
        }

        private bool WriteCollectionHeader(BufferScan scan, int count, bool isNull,
            int referenceId, bool isReference)
        {
            if (scan.SupportReferences)
            {
                WriteByte(isNull ? (byte)0 : isReference ? (byte)1 : (byte)2);
                if (isNull) return false;
                WriteInt32(referenceId);
                if (isReference) return false;
            }
            else if (isNull)
            {
                WriteUInt16(ushort.MaxValue);
                return false;
            }
            if (count >= ushort.MaxValue)
                throw new FormatException($"Write array length cannot be greater than {ushort.MaxValue - 1} !");
            WriteUInt16((ushort)count);
            return true;
        }

        public void WriteMultiDimensionalArray<T>(BufferScan scan, int rank,
            BuffConverter<T> converter)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            var cachedValues = scan.ReadMultiDimensionalArray<T>(rank, out var shape,
                out int referenceId, out bool isReference);
            if (scan.SupportReferences)
            {
                if (!WriteCollectionHeader(scan, cachedValues?.Count ?? 0,
                        cachedValues == null && !isReference,
                        referenceId, isReference))
                    return;
            }
            else if (cachedValues == null)
            {
                // The legacy multi-dimensional format uses the first dimension as its null marker.
                WriteUInt16(ushort.MaxValue);
                return;
            }

            for (int dimension = 0; dimension < rank; dimension++)
                WriteUInt16((ushort)shape.GetLength(dimension));
            for (int i = 0; i < cachedValues.Count; i++)
                converter.WriteValue(this, scan, cachedValues[i]);
        }

        public void WriteNullable<T>(BufferScan scan, T? value,
            BuffConverter<T> converter) where T : struct
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            WriteBool(value.HasValue);
            if (value.HasValue)
                converter.WriteValue(this, scan, value.Value);
        }

        public void WriteKeyValuePair<TKey, TValue>(BufferScan scan, KeyValuePair<TKey, TValue> value,
            BuffConverter<TKey> keyConverter, BuffConverter<TValue> valueConverter)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (keyConverter == null) throw new ArgumentNullException(nameof(keyConverter));
            if (valueConverter == null) throw new ArgumentNullException(nameof(valueConverter));
            keyConverter.WriteValue(this, scan, value.Key);
            valueConverter.WriteValue(this, scan, value.Value);
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
            if (count > _maxScalarLength)
                throw new FormatException(
                    $"UTF-8 byte count cannot exceed {_maxScalarLength}.");
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
            if (cached.IsReference)
            {
                WriteInt32(-2);
                WriteInt32(cached.ReferenceId);
                return;
            }
            WriteInt32(scan.GetMetaIndex(cached.Type.FullName));
            WriteInt32(scan.GetMetaIndex(cached.Type.Assembly.FullName));
            var ObjStart = this._index;
            WriteInt32(0);
            if (scan.SupportReferences)
                WriteInt32(cached.ReferenceId);
            for (int i = 0; i < cached.FieldCount; i++)
            {
                var cachedField = scan.ReadField(cached, i);
                var FieldStart = this._index;
                WriteInt32(0);
                WriteInt32(scan.GetMetaIndex(cachedField.Field.name));
                WriteInt32(scan.GetMetaIndex(
                    BuffSerializer.GetSerializedTypeName(cachedField.Field.FieldType)));
                cachedField.Write(this, scan);
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
