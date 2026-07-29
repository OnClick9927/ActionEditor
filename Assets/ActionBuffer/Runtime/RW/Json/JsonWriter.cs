using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace ActionBuffer
{
    public class JsonWriter : IBufferWriter
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly Stack<WriteContext> _contexts = new Stack<WriteContext>();
        private BufferScan scan;
        private bool _prettyPrint, _typeInfo, _fullField;
        private int _indentLevel;

        public bool CollectMeta => false;
        public bool FullField => _fullField;

        public JsonWriter() { }

        public void Init(BufferScan scan)
        {
            ResetOutput();
            ReleaseScan();
            this.scan = scan ?? throw new ArgumentNullException(nameof(scan));
            scan.ResetRead();
        }

        public void Clear()
        {
            ResetOutput();
            ReleaseScan();
            _prettyPrint = false;
            _typeInfo = false;
            _fullField = false;
        }

        private void ResetOutput()
        {
            _sb.Clear();
            _contexts.Clear();
            _indentLevel = 0;
        }

        private void ReleaseScan()
        {
            var currentScan = scan;
            scan = null;
            BufferScan.Back(currentScan);
        }

        private BufferScan RequireScan()
        {
            if (scan == null)
                throw new InvalidOperationException("JsonWriter requires a completed BufferScan before writing objects or collections.");
            return scan;
        }
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
                //WriteIndent();
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

        public void WriteRaw(string value)
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

        private void WriteTypeInfo(Type type)
        {
            if (!typeInfo) return;
            WriteString("$type");
            WriteRaw(":");
            WriteSpaceIfPretty();
            WriteString(type.FullName);
            WriteRaw(",");

            //WriteCommaIfNeeded();
            WriteIndent();
            WriteString("$assembly");
            WriteRaw(":");
            WriteSpaceIfPretty();
            WriteString(type.Assembly.FullName);
            //WriteRaw(",");
            WriteCommaIfNeeded();

        }

        //public void WriteObject<T>(T value)
        //{
        //    WriteObject(value, value == null ? null : TypeHelper.GetTypeFields(value.GetType()));

        //}
        public void WriteObject<T>(T value, TypeHelper.TypeFields _fields)
        {
            var cached = RequireScan().ReadObject();
            if (cached.Value == null)
            {
                WriteRaw("null");
                return;
            }

            PushContext(false);
            WriteRaw("{");
            if (_prettyPrint) WriteIndent();

            WriteTypeInfo(cached.Type);

            int _count = 0;
            for (int i = 0; i < cached.Fields.Count; i++)
            {
                var cachedField = cached.Fields[i];

                WriteCommaIfNeeded();
                if (typeInfo || _count++ != 0)
                    WriteIndent();
                WriteString(cachedField.Field.name);
                WriteRaw(":");
                WriteSpaceIfPretty();
                cachedField.Converter.Write(this, cachedField.Value);
            }

            PopContext();
            WriteRaw("}");
        }

        public void WriteIEnumerable<T>(IEnumerable<T> values, Action<IBufferWriter, T> write)
        {
            var cachedValues = RequireScan().ReadEnumerable<T>();
            if (cachedValues == null)
            {
                WriteRaw("null");
                return;
            }

            PushContext(true);
            WriteRaw("[");
            if (_prettyPrint && cachedValues.Count > 0) WriteIndent();
            for (int i = 0; i < cachedValues.Count; i++)
            {
                if (i > 0)
                {
                    WriteRaw(",");
                    if (_prettyPrint) WriteIndent();
                }
                write(this, cachedValues[i]);
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
}
