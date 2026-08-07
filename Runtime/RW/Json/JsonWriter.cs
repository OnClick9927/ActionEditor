using System;
using System.Globalization;
using System.Text;

namespace ActionBuffer
{
    public sealed class JsonWriter : StructuredTextWriter
    {
        private const byte ObjectContainer = 1;
        private const byte ArrayContainer = 2;
        private const byte PlainSequence = 0;
        private const byte WrappedSequence = 1;
        private const byte ReferenceSequence = 2;

        private readonly StringBuilder _builder = new StringBuilder();
        private byte[] _containerKinds = new byte[16];
        private int[] _memberCounts = new int[16];
        private byte[] _sequenceModes = new byte[16];
        private bool _prettyPrint;
        private int _containerDepth;
        private int _sequenceDepth;

        public static JsonWriter Get()
        {
            var result = ClassPool.Get<JsonWriter>();
            result.Clear();
            return result;
        }

        public static void Back(JsonWriter value)
        {
            if (value == null) return;
            value.Clear();
            ClassPool.Back(value);
        }

        internal int Capacity => _builder.Capacity;

        internal void TrimCapacity()
        {
            if (_builder.Capacity > BuffSettings.RetainedTextCapacity)
            {
                _builder.Clear();
                _builder.Capacity = 1024;
            }
        }

        public string GetJson()
        {
            RequireResult();
            ValidateTextLength(_builder.Length, "JSON");
            return _builder.ToString();
        }

        protected override void OnInit(BufferScan scan)
        {
            _prettyPrint = scan.PrettyPrint;
        }

        public override void Clear()
        {
            _builder.Clear();
            _prettyPrint = false;
            _containerDepth = 0;
            _sequenceDepth = 0;
            base.Clear();
        }

        protected override void WriteNullValue()
        {
            Append("null");
        }

        protected override void WriteScalarValue(string value, bool quoted)
        {
            if (quoted || !IsJsonLiteral(value))
                WriteString(value ?? string.Empty);
            else
                Append(value);
        }

        protected override void WriteBooleanValue(bool value)
        {
            Append(value ? "true" : "false");
        }

        protected override void WriteCharacterValue(char value)
        {
            EnsureAppend(value < 0x20 ? 8 : 4);
            _builder.Append('"');
            switch (value)
            {
                case '"': _builder.Append("\\\""); break;
                case '\\': _builder.Append("\\\\"); break;
                case '\b': _builder.Append("\\b"); break;
                case '\f': _builder.Append("\\f"); break;
                case '\n': _builder.Append("\\n"); break;
                case '\r': _builder.Append("\\r"); break;
                case '\t': _builder.Append("\\t"); break;
                default:
                    if (value < 0x20)
                    {
                        _builder.Append("\\u");
                        AppendHex4(value);
                    }
                    else
                    {
                        _builder.Append(value);
                    }
                    break;
            }
            _builder.Append('"');
        }

        protected override void WriteSignedIntegerValue(long value)
        {
            ulong magnitude = value < 0
                ? unchecked((ulong)(-(value + 1))) + 1
                : (ulong)value;
            EnsureAppend(DigitCount(magnitude) + (value < 0 ? 1 : 0));
            TextIntegerWriter.Append(_builder, value);
        }

        protected override void WriteUnsignedIntegerValue(ulong value)
        {
            EnsureAppend(DigitCount(value));
            TextIntegerWriter.Append(_builder, value);
        }

        private static int DigitCount(ulong value)
        {
            int count = 1;
            while (value >= 10)
            {
                value /= 10;
                count++;
            }
            return count;
        }

        protected override void BeginObjectValue(int referenceId, bool isReference,
            string typeName, string assemblyName, int fieldCount)
        {
            BeginContainer('{', ObjectContainer);
            if (isReference)
            {
                WriteIntegerProperty("$ref", referenceId);
                return;
            }
            if (referenceId >= 0)
                WriteIntegerProperty("$id", referenceId);
            if (typeName != null)
            {
                WriteStringProperty("$type", typeName);
                WriteStringProperty("$assembly", assemblyName ?? string.Empty);
            }
        }

        protected override void BeginObjectField(string name)
        {
            WritePropertyPrefix(StructuredNode.EncodeTextFieldName(name));
        }

        protected override void EndObjectField()
        {
            EnsureLength();
        }

        protected override void EndObjectValue()
        {
            EndContainer('}', ObjectContainer);
        }

        protected override void BeginSequenceValue(int referenceId, bool isReference,
            int count)
        {
            EnsureSequenceCapacity();
            if (isReference)
            {
                _sequenceModes[_sequenceDepth++] = ReferenceSequence;
                BeginContainer('{', ObjectContainer);
                WriteIntegerProperty("$ref", referenceId);
                return;
            }
            if (referenceId >= 0)
            {
                _sequenceModes[_sequenceDepth++] = WrappedSequence;
                BeginContainer('{', ObjectContainer);
                WriteIntegerProperty("$id", referenceId);
                WritePropertyPrefix("$values");
                BeginContainer('[', ArrayContainer);
                return;
            }
            _sequenceModes[_sequenceDepth++] = PlainSequence;
            BeginContainer('[', ArrayContainer);
        }

        protected override void BeginSequenceItem()
        {
            WriteArrayPrefix();
        }

        protected override void EndSequenceItem()
        {
            EnsureLength();
        }

        protected override void EndSequenceValue()
        {
            if (_sequenceDepth <= 0)
                throw new InvalidOperationException("JSON sequence state is unbalanced.");
            byte mode = _sequenceModes[--_sequenceDepth];
            if (mode == ReferenceSequence)
            {
                EndContainer('}', ObjectContainer);
                return;
            }
            EndContainer(']', ArrayContainer);
            if (mode == WrappedSequence)
                EndContainer('}', ObjectContainer);
        }

        private void BeginContainer(char token, byte kind)
        {
            EnsureContainerCapacity();
            _builder.Append(token);
            _containerKinds[_containerDepth] = kind;
            _memberCounts[_containerDepth] = 0;
            _containerDepth++;
            EnsureLength();
        }

        private void EndContainer(char token, byte expectedKind)
        {
            if (_containerDepth <= 0 || _containerKinds[_containerDepth - 1] != expectedKind)
                throw new InvalidOperationException("JSON container state is unbalanced.");
            int count = _memberCounts[_containerDepth - 1];
            _containerDepth--;
            if (_prettyPrint && count > 0)
            {
                _builder.Append('\n');
                AppendIndent(_containerDepth);
            }
            _builder.Append(token);
            EnsureLength();
        }

        private void WritePropertyPrefix(string name)
        {
            if (_containerDepth <= 0 || _containerKinds[_containerDepth - 1] != ObjectContainer)
                throw new InvalidOperationException("A JSON property must be written inside an object.");
            int index = _containerDepth - 1;
            if (_memberCounts[index]++ > 0)
                _builder.Append(',');
            if (_prettyPrint)
            {
                _builder.Append('\n');
                AppendIndent(_containerDepth);
            }
            WriteString(name);
            _builder.Append(':');
            if (_prettyPrint)
                _builder.Append(' ');
            EnsureLength();
        }

        private void WriteArrayPrefix()
        {
            if (_containerDepth <= 0 || _containerKinds[_containerDepth - 1] != ArrayContainer)
                throw new InvalidOperationException("A JSON item must be written inside an array.");
            int index = _containerDepth - 1;
            if (_memberCounts[index]++ > 0)
                _builder.Append(',');
            if (_prettyPrint)
            {
                _builder.Append('\n');
                AppendIndent(_containerDepth);
            }
            EnsureLength();
        }

        private void WriteIntegerProperty(string name, int value)
        {
            WritePropertyPrefix(name);
            TextIntegerWriter.Append(_builder, value);
            EnsureLength();
        }

        private void WriteStringProperty(string name, string value)
        {
            WritePropertyPrefix(name);
            WriteString(value);
        }

        private static bool IsJsonLiteral(string value)
        {
            if (value == "true" || value == "false") return true;
            if (string.IsNullOrEmpty(value)) return false;

            int index = value[0] == '-' ? 1 : 0;
            if (index == value.Length) return false;
            if (value[index] == '0')
                index++;
            else
            {
                if (value[index] < '1' || value[index] > '9') return false;
                while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                    index++;
            }
            if (index < value.Length && value[index] == '.')
            {
                int start = ++index;
                while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                    index++;
                if (index == start) return false;
            }
            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-'))
                    index++;
                int start = index;
                while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                    index++;
                if (index == start) return false;
            }
            return index == value.Length;
        }

        private void WriteString(string value)
        {
            if (value == null)
            {
                Append("null");
                return;
            }

            int outputLength = 2;
            bool requiresEscaping = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                int characterLength = c == '"' || c == '\\' || c == '\b' || c == '\f' ||
                                      c == '\n' || c == '\r' || c == '\t'
                    ? 2
                    : c < 0x20 ? 6 : 1;
                requiresEscaping |= characterLength != 1;
                if (outputLength > int.MaxValue - characterLength)
                    throw new FormatException("JSON string is too long.");
                outputLength += characterLength;
            }
            EnsureAppend(outputLength);

            _builder.Append('"');
            if (!requiresEscaping)
            {
                _builder.Append(value).Append('"');
                return;
            }
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': _builder.Append("\\\""); break;
                    case '\\': _builder.Append("\\\\"); break;
                    case '\b': _builder.Append("\\b"); break;
                    case '\f': _builder.Append("\\f"); break;
                    case '\n': _builder.Append("\\n"); break;
                    case '\r': _builder.Append("\\r"); break;
                    case '\t': _builder.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            _builder.Append("\\u");
                            AppendHex4(c);
                        }
                        else
                        {
                            _builder.Append(c);
                        }
                        break;
                }
            }
            _builder.Append('"');
        }

        private void AppendHex4(int value)
        {
            const string Hex = "0123456789ABCDEF";
            _builder.Append(Hex[(value >> 12) & 15]);
            _builder.Append(Hex[(value >> 8) & 15]);
            _builder.Append(Hex[(value >> 4) & 15]);
            _builder.Append(Hex[value & 15]);
        }

        private void AppendIndent(int indent)
        {
            _builder.Append(' ', indent * 2);
        }

        private void Append(string value)
        {
            EnsureAppend(value.Length);
            _builder.Append(value);
        }

        private void EnsureLength()
        {
            if (_builder.Length > MaxTextLength)
                throw new FormatException($"JSON output length cannot exceed {MaxTextLength} characters.");
        }

        private void EnsureAppend(int count)
        {
            if (count > MaxTextLength - _builder.Length)
                throw new FormatException($"JSON output length cannot exceed {MaxTextLength} characters.");
        }

        private void EnsureContainerCapacity()
        {
            if (_containerDepth < _containerKinds.Length) return;
            int size = checked(_containerKinds.Length * 2);
            Array.Resize(ref _containerKinds, size);
            Array.Resize(ref _memberCounts, size);
        }

        private void EnsureSequenceCapacity()
        {
            if (_sequenceDepth < _sequenceModes.Length) return;
            Array.Resize(ref _sequenceModes, checked(_sequenceModes.Length * 2));
        }
    }
}
