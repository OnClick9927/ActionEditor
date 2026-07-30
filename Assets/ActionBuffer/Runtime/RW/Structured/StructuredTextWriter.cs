using System;
using System.Collections.Generic;
using System.Globalization;

namespace ActionBuffer
{
    public abstract class StructuredTextWriter : IBufferWriter
    {
        private StructuredNode _value;
        private bool _hasValue;
        private bool _typeInfo;
        private bool _fullField;

        public bool CollectMeta => false;
        public bool FullField => _fullField;

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

        public void Init()
        {
            ResetValue();
        }

        public virtual void Clear()
        {
            ResetValue();
            _typeInfo = false;
            _fullField = false;
        }

        internal StructuredNode GetRoot()
        {
            if (!_hasValue)
                throw new InvalidOperationException("The writer has no serialized value.");
            return _value;
        }

        private void ResetValue()
        {
            if (!_hasValue) return;
            StructuredNode.Release(ref _value);
            _hasValue = false;
        }

        private void SetValue(StructuredNode value)
        {
            ResetValue();
            _value = value;
            _hasValue = true;
        }

        private StructuredNode TakeValue()
        {
            if (!_hasValue)
                throw new InvalidOperationException("A converter did not write a value.");

            var value = _value;
            _value = default;
            _hasValue = false;
            return value;
        }

        public void WriteObject<T>(BufferScan scan, T value, TypeHelper.TypeFields fields)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            var cached = scan.ReadObject();
            if (cached.Value == null)
            {
                SetValue(StructuredNode.Rent(StructuredNodeKind.Null));
                return;
            }
            var node = StructuredNode.Rent(StructuredNodeKind.Object);
            try
            {
                if (!_typeInfo && cached.Type != typeof(T))
                    throw new InvalidOperationException(
                        $"Writing runtime type '{cached.Type}' through '{typeof(T)}' requires typeInfo=true.");
                if (_typeInfo)
                {
                    node.TypeName = cached.Type.FullName;
                    node.AssemblyName = cached.Type.Assembly.FullName;
                }

                for (int i = 0; i < cached.FieldCount; i++)
                {
                    var cachedField = cached.GetField(i);
                    ResetValue();
                    cachedField.Converter.Write(this, scan, cachedField.Value);
                    var valueNode = TakeValue();
                    try
                    {
                        node.AddField(cachedField.Field.name, valueNode);
                        valueNode = default;
                    }
                    finally
                    {
                        StructuredNode.Release(ref valueNode);
                    }
                }
            }
            catch
            {
                ResetValue();
                StructuredNode.Release(ref node);
                throw;
            }
            SetValue(node);
        }

        public void WriteIEnumerable<T>(BufferScan scan, IEnumerable<T> values,
            Action<IBufferWriter, BufferScan, T> write)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            var cachedValues = scan.ReadEnumerable<T>();
            if (cachedValues == null)
            {
                SetValue(StructuredNode.Rent(StructuredNodeKind.Null));
                return;
            }

            var node = StructuredNode.Rent(StructuredNodeKind.Sequence);
            try
            {
                for (int i = 0; i < cachedValues.Count; i++)
                {
                    ResetValue();
                    write(this, scan, cachedValues[i]);
                    var valueNode = TakeValue();
                    try
                    {
                        node.AddItem(valueNode);
                        valueNode = default;
                    }
                    finally
                    {
                        StructuredNode.Release(ref valueNode);
                    }
                }
            }
            catch
            {
                ResetValue();
                StructuredNode.Release(ref node);
                throw;
            }
            SetValue(node);
        }

        public void WriteArray2D<T>(BufferScan scan, T[,] values,
            Action<IBufferWriter, BufferScan, T> write)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (write == null) throw new ArgumentNullException(nameof(write));
            var cachedValues = scan.ReadArray2D<T>(out int rows, out int columns);
            if (cachedValues == null)
            {
                SetValue(StructuredNode.Rent(StructuredNodeKind.Null));
                return;
            }
            if (rows == 0 && columns != 0)
            {
                WriteEmptyArrayDimensions(columns);
                return;
            }

            var node = StructuredNode.Rent(StructuredNodeKind.Sequence);
            int valueIndex = 0;
            try
            {
                for (int row = 0; row < rows; row++)
                {
                    var rowNode = StructuredNode.Rent(StructuredNodeKind.Sequence);
                    try
                    {
                        for (int column = 0; column < columns; column++)
                        {
                            ResetValue();
                            write(this, scan, cachedValues[valueIndex++]);
                            var valueNode = TakeValue();
                            try
                            {
                                rowNode.AddItem(valueNode);
                                valueNode = default;
                            }
                            finally
                            {
                                StructuredNode.Release(ref valueNode);
                            }
                        }
                        node.AddItem(rowNode);
                        rowNode = default;
                    }
                    finally
                    {
                        StructuredNode.Release(ref rowNode);
                    }
                }
            }
            catch
            {
                ResetValue();
                StructuredNode.Release(ref node);
                throw;
            }
            SetValue(node);
        }

        private void WriteEmptyArrayDimensions(int columns)
        {
            var node = StructuredNode.Rent(StructuredNodeKind.Object);
            try
            {
                var rowsNode = StructuredNode.RentScalar("0", false);
                try
                {
                    node.AddField("$rows", rowsNode);
                    rowsNode = default;
                }
                finally
                {
                    StructuredNode.Release(ref rowsNode);
                }

                var columnsNode = StructuredNode.RentScalar(
                    columns.ToString(CultureInfo.InvariantCulture), false);
                try
                {
                    node.AddField("$columns", columnsNode);
                    columnsNode = default;
                }
                finally
                {
                    StructuredNode.Release(ref columnsNode);
                }
            }
            catch
            {
                StructuredNode.Release(ref node);
                throw;
            }
            SetValue(node);
        }

        public void WriteNullable<T>(BufferScan scan, T? value,
            Action<IBufferWriter, BufferScan, T> write) where T : struct
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (write == null) throw new ArgumentNullException(nameof(write));
            if (!value.HasValue)
            {
                SetValue(StructuredNode.Rent(StructuredNodeKind.Null));
                return;
            }
            write(this, scan, value.Value);
        }

        public void WriteKeyValuePair<TKey, TValue>(BufferScan scan, KeyValuePair<TKey, TValue> value,
            Action<IBufferWriter, BufferScan, TKey> writeKey,
            Action<IBufferWriter, BufferScan, TValue> writeValue)
        {
            if (scan == null) throw new ArgumentNullException(nameof(scan));
            if (writeKey == null) throw new ArgumentNullException(nameof(writeKey));
            if (writeValue == null) throw new ArgumentNullException(nameof(writeValue));

            var node = StructuredNode.Rent(StructuredNodeKind.Object);
            try
            {
                ResetValue();
                writeKey(this, scan, value.Key);
                var keyNode = TakeValue();
                try
                {
                    node.AddField("key", keyNode);
                    keyNode = default;
                }
                finally
                {
                    StructuredNode.Release(ref keyNode);
                }

                ResetValue();
                writeValue(this, scan, value.Value);
                var valueNode = TakeValue();
                try
                {
                    node.AddField("value", valueNode);
                    valueNode = default;
                }
                finally
                {
                    StructuredNode.Release(ref valueNode);
                }
            }
            catch
            {
                ResetValue();
                StructuredNode.Release(ref node);
                throw;
            }
            SetValue(node);
        }

        private void WriteScalar(string value, bool quoted)
        {
            SetValue(value == null
                ? StructuredNode.Rent(StructuredNodeKind.Null)
                : StructuredNode.RentScalar(value, quoted));
        }

        public void WriteBool(bool value) => WriteScalar(value ? "true" : "false", false);
        public void WriteByte(byte value) => WriteScalar(value.ToString(CultureInfo.InvariantCulture), false);
        public void WriteChar(char value) => WriteScalar(value.ToString(), true);
        public void WriteDouble(double value) => WriteScalar(value.ToString("R", CultureInfo.InvariantCulture), false);
        public void WriteEnum(Enum data) => WriteScalar(data?.ToString(), true);
        public void WriteFloat(float value) => WriteScalar(value.ToString("R", CultureInfo.InvariantCulture), false);
        public void WriteInt16(short value) => WriteScalar(value.ToString(CultureInfo.InvariantCulture), false);
        public void WriteInt32(int value) => WriteScalar(value.ToString(CultureInfo.InvariantCulture), false);
        public void WriteInt64(long value) => WriteScalar(value.ToString(CultureInfo.InvariantCulture), false);
        public void WriteUInt16(ushort value) => WriteScalar(value.ToString(CultureInfo.InvariantCulture), false);
        public void WriteUInt32(uint value) => WriteScalar(value.ToString(CultureInfo.InvariantCulture), false);
        public void WriteUInt64(ulong value) => WriteScalar(value.ToString(CultureInfo.InvariantCulture), false);
        public void WriteUTF8(string value) => WriteScalar(value, true);
    }
}
