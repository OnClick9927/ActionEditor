using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ActionBuffer
{
    internal static class TextIntegerWriter
    {
        internal static void Append(StringBuilder builder, long value)
        {
            if (value >= 0)
            {
                Append(builder, (ulong)value);
                return;
            }
            builder.Append('-');
            Append(builder, unchecked((ulong)(-(value + 1))) + 1);
        }

        internal static void Append(StringBuilder builder, ulong value)
        {
            if (value >= 10)
                Append(builder, value / 10);
            builder.Append((char)('0' + value % 10));
        }
    }

    public abstract class StructuredTextWriter : IBufferWriter, ITypedEnumWriter
    {
        private bool _typeInfo;
        private bool _initialized;
        private bool _hasRoot;
        private int _valueDepth;
        protected int MaxTextLength { get; private set; }

        public bool CollectMeta => false;

        public void Init(BufferScan scan)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            Clear();
            _typeInfo = scan.TypeInfo;
            MaxTextLength = scan.MaxTextLength;
            _initialized = true;
            OnInit(scan);
        }

        protected virtual void OnInit(BufferScan scan) { }

        protected void RequireResult()
        {
            if (!_initialized || !_hasRoot || _valueDepth != 0)
                throw new InvalidOperationException("The writer has no complete serialized value.");
        }

        protected void ValidateTextLength(int length, string format)
        {
            if (length > MaxTextLength)
                throw new FormatException(
                    $"{format} output length cannot exceed {MaxTextLength} characters.");
        }

        public virtual void Clear()
        {
            _typeInfo = false;
            _initialized = false;
            _hasRoot = false;
            _valueDepth = 0;
            MaxTextLength = 0;
        }

        private void EnterValue()
        {
            if (!_initialized) throw new InvalidOperationException("The writer is not initialized.");
            if (_valueDepth == 0)
            {
                if (_hasRoot)
                    throw new InvalidOperationException("A converter wrote more than one root value.");
                _hasRoot = true;
            }
            _valueDepth++;
        }

        private void ExitValue()
        {
            _valueDepth--;
        }

        public void WriteObject<T>(BufferScan scan, T value, TypeHelper.TypeFields fields)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            EnterValue();
            try
            {
                var cached = scan.ReadObject();
                if (cached.Value == null)
                {
                    WriteNullValue();
                    return;
                }
                if (cached.IsReference)
                {
                    BeginObjectValue(cached.ReferenceId, true, null, null, 0);
                    EndObjectValue();
                    return;
                }
                if (!_typeInfo && cached.Type != typeof(T))
                    throw new InvalidOperationException(
                        $"Writing runtime type '{cached.Type}' through '{typeof(T)}' requires typeInfo=true.");

                string typeName = _typeInfo && cached.Type != typeof(T)
                    ? cached.Type.FullName
                    : null;
                string assemblyName = typeName == null ? null : cached.Type.Assembly.FullName;
                BeginObjectValue(cached.ReferenceId, false, typeName, assemblyName,
                    cached.FieldCount);
                for (int i = 0; i < cached.FieldCount; i++)
                {
                    var field = scan.ReadField(cached, i);
                    BeginObjectField(field.Field.name);
                    field.Write(this, scan);
                    EndObjectField();
                }
                EndObjectValue();
            }
            finally
            {
                ExitValue();
            }
        }

        public void WriteIEnumerable<T>(BufferScan scan, BuffConverter<T> converter)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterValue();
            try
            {
                var values = scan.ReadEnumerable<T>(out int referenceId, out bool isReference);
                if (values == null && !isReference)
                {
                    WriteNullValue();
                    return;
                }
                BeginSequenceValue(referenceId, isReference, values?.Count ?? 0);
                if (!isReference)
                {
                    for (int i = 0; i < values.Count; i++)
                    {
                        BeginSequenceItem();
                        converter.WriteValue(this, scan, values[i]);
                        EndSequenceItem();
                    }
                }
                EndSequenceValue();
            }
            finally
            {
                ExitValue();
            }
        }

        public void WriteMultiDimensionalArray<T>(BufferScan scan, int rank,
            BuffConverter<T> converter)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            EnterValue();
            try
            {
                var values = scan.ReadMultiDimensionalArray<T>(rank, out var shape,
                    out int referenceId, out bool isReference);
                if (values == null && !isReference)
                {
                    WriteNullValue();
                    return;
                }
                if (isReference)
                {
                    BeginObjectValue(referenceId, true, null, null, 0);
                    EndObjectValue();
                    return;
                }

                BeginObjectValue(referenceId, false, null, null, 2);
                BeginObjectField("dimensions");
                BeginSequenceValue(-1, false, rank);
                for (int dimension = 0; dimension < rank; dimension++)
                {
                    BeginSequenceItem();
                    WriteInt32(shape.GetLength(dimension));
                    EndSequenceItem();
                }
                EndSequenceValue();
                EndObjectField();

                BeginObjectField("values");
                BeginSequenceValue(-1, false, values.Count);
                for (int i = 0; i < values.Count; i++)
                {
                    BeginSequenceItem();
                    converter.WriteValue(this, scan, values[i]);
                    EndSequenceItem();
                }
                EndSequenceValue();
                EndObjectField();
                EndObjectValue();
            }
            finally
            {
                ExitValue();
            }
        }

        public void WriteNullable<T>(BufferScan scan, T? value,
            BuffConverter<T> converter) where T : struct
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (!value.HasValue)
            {
                WriteAtomic(null, false);
                return;
            }
            converter.WriteValue(this, scan, value.Value);
        }

        public void WriteKeyValuePair<TKey, TValue>(BufferScan scan,
            KeyValuePair<TKey, TValue> value, BuffConverter<TKey> keyConverter,
            BuffConverter<TValue> valueConverter)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (keyConverter == null) throw new ArgumentNullException(nameof(keyConverter));
            if (valueConverter == null) throw new ArgumentNullException(nameof(valueConverter));
            EnterValue();
            try
            {
                BeginObjectValue(-1, false, null, null, 2);
                BeginObjectField("key");
                keyConverter.WriteValue(this, scan, value.Key);
                EndObjectField();
                BeginObjectField("value");
                valueConverter.WriteValue(this, scan, value.Value);
                EndObjectField();
                EndObjectValue();
            }
            finally
            {
                ExitValue();
            }
        }

        private void WriteAtomic(string value, bool quoted)
        {
            EnterValue();
            try
            {
                if (value == null) WriteNullValue();
                else WriteScalarValue(value, quoted);
            }
            finally
            {
                ExitValue();
            }
        }

        public void WriteBool(bool value)
        {
            EnterValue();
            try { WriteBooleanValue(value); }
            finally { ExitValue(); }
        }

        public void WriteByte(byte value) => WriteUnsignedInteger(value);

        public void WriteChar(char value)
        {
            EnterValue();
            try { WriteCharacterValue(value); }
            finally { ExitValue(); }
        }

        public void WriteDouble(double value) => WriteAtomic(value.ToString("R", CultureInfo.InvariantCulture), false);
        public void WriteEnum(Enum data) => WriteAtomic(data?.ToString(), true);
        void ITypedEnumWriter.WriteEnumValue<T>(T value) =>
            WriteAtomic(value.ToString(), true);
        public void WriteFloat(float value) => WriteAtomic(value.ToString("R", CultureInfo.InvariantCulture), false);
        public void WriteGuid(Guid value) => WriteAtomic(value.ToString("D"), true);
        public void WriteInt16(short value) => WriteSignedInteger(value);
        public void WriteInt32(int value) => WriteSignedInteger(value);
        public void WriteInt64(long value) => WriteSignedInteger(value);
        public void WriteUInt16(ushort value) => WriteUnsignedInteger(value);
        public void WriteUInt32(uint value) => WriteUnsignedInteger(value);
        public void WriteUInt64(ulong value) => WriteUnsignedInteger(value);
        public void WriteUTF8(string value) => WriteAtomic(value, true);

        private void WriteSignedInteger(long value)
        {
            EnterValue();
            try { WriteSignedIntegerValue(value); }
            finally { ExitValue(); }
        }

        private void WriteUnsignedInteger(ulong value)
        {
            EnterValue();
            try { WriteUnsignedIntegerValue(value); }
            finally { ExitValue(); }
        }

        protected abstract void WriteNullValue();
        protected abstract void WriteScalarValue(string value, bool quoted);
        protected abstract void WriteBooleanValue(bool value);
        protected abstract void WriteCharacterValue(char value);
        protected abstract void WriteSignedIntegerValue(long value);
        protected abstract void WriteUnsignedIntegerValue(ulong value);
        protected abstract void BeginObjectValue(int referenceId, bool isReference,
            string typeName, string assemblyName, int fieldCount);
        protected abstract void BeginObjectField(string name);
        protected abstract void EndObjectField();
        protected abstract void EndObjectValue();
        protected abstract void BeginSequenceValue(int referenceId, bool isReference,
            int count);
        protected abstract void BeginSequenceItem();
        protected abstract void EndSequenceItem();
        protected abstract void EndSequenceValue();
    }
}
