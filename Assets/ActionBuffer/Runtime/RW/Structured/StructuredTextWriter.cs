using System;
using System.Collections.Generic;
using System.Globalization;

namespace ActionBuffer
{
    public abstract class StructuredTextWriter : IBufferWriter
    {
        private BufferScan _scan;
        private StructuredNode _value;
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

        public void Init(BufferScan scan)
        {
            ResetValue();
            ReleaseScan();
            _scan = scan ?? throw new ArgumentNullException(nameof(scan));
            scan.ResetRead();
        }

        public virtual void Clear()
        {
            ResetValue();
            ReleaseScan();
            _typeInfo = false;
            _fullField = false;
        }

        internal StructuredNode GetRoot()
        {
            if (_value == null)
                throw new InvalidOperationException("The writer has no serialized value.");
            return _value;
        }

        private BufferScan RequireScan()
        {
            if (_scan == null)
                throw new InvalidOperationException("The writer requires a completed BufferScan.");
            return _scan;
        }

        private void ReleaseScan()
        {
            var scan = _scan;
            _scan = null;
            BufferScan.Back(scan);
        }

        private void ResetValue()
        {
            var value = _value;
            _value = null;
            StructuredNode.Release(value);
        }

        private void SetValue(StructuredNode value)
        {
            if (_value != null)
                StructuredNode.Release(_value);
            _value = value;
        }

        private StructuredNode TakeValue()
        {
            var value = _value;
            _value = null;
            if (value == null)
                throw new InvalidOperationException("A converter did not write a value.");
            return value;
        }

        public void WriteObject<T>(T value, TypeHelper.TypeFields fields)
        {
            var cached = RequireScan().ReadObject();
            if (cached.Value == null)
            {
                SetValue(StructuredNode.Rent(StructuredNodeKind.Null));
                return;
            }

            var node = StructuredNode.Rent(StructuredNodeKind.Object);
            try
            {
                if (_typeInfo)
                {
                    node.TypeName = cached.Type.FullName;
                    node.AssemblyName = cached.Type.Assembly.FullName;
                }

                for (int i = 0; i < cached.Fields.Count; i++)
                {
                    var cachedField = cached.Fields[i];
                    ResetValue();
                    cachedField.Converter.Write(this, cachedField.Value);
                    node.AddField(cachedField.Field.name, TakeValue());
                }
            }
            catch
            {
                ResetValue();
                StructuredNode.Release(node);
                throw;
            }
            SetValue(node);
        }

        public void WriteIEnumerable<T>(IEnumerable<T> values, Action<IBufferWriter, T> write)
        {
            var cachedValues = RequireScan().ReadEnumerable<T>();
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
                    write(this, cachedValues[i]);
                    node.AddItem(TakeValue());
                }
            }
            catch
            {
                ResetValue();
                StructuredNode.Release(node);
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
