using System;
using System.Text;

namespace ActionBuffer
{
    public sealed class YamlWriter : StructuredTextWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private int[] _containerIndentDeltas = new int[16];
        private int _containerDepth;
        private int _indent;
        private bool _pendingValue;

        public static YamlWriter Get()
        {
            var result = ClassPool.Get<YamlWriter>();
            result.Clear();
            return result;
        }

        public static void Back(YamlWriter value)
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

        public string GetYaml()
        {
            RequireResult();
            ValidateTextLength(_builder.Length, "YAML");
            return _builder.ToString();
        }

        public override void Clear()
        {
            _builder.Clear();
            _containerDepth = 0;
            _indent = 0;
            _pendingValue = false;
            base.Clear();
        }

        protected override void WriteNullValue()
        {
            WriteInline("null", false);
        }

        protected override void WriteScalarValue(string value, bool quoted)
        {
            if (quoted)
            {
                BeginInlineValue();
                AppendQuoted(value ?? string.Empty);
                _builder.Append('\n');
                EnsureLength();
                return;
            }
            WriteInline(value, false);
        }

        protected override void WriteBooleanValue(bool value)
        {
            WriteInline(value ? "true" : "false", false);
        }

        protected override void WriteCharacterValue(char value)
        {
            BeginInlineValue();
            EnsureAppend(value < 0x20 ? 8 : 4);
            _builder.Append('"');
            switch (value)
            {
                case '"': _builder.Append("\\\""); break;
                case '\\': _builder.Append("\\\\"); break;
                case '\0': _builder.Append("\\0"); break;
                case '\a': _builder.Append("\\a"); break;
                case '\b': _builder.Append("\\b"); break;
                case '\t': _builder.Append("\\t"); break;
                case '\n': _builder.Append("\\n"); break;
                case '\v': _builder.Append("\\v"); break;
                case '\f': _builder.Append("\\f"); break;
                case '\r': _builder.Append("\\r"); break;
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
            _builder.Append('"').Append('\n');
            EnsureLength();
        }

        protected override void WriteSignedIntegerValue(long value)
        {
            BeginInlineValue();
            ulong magnitude = value < 0
                ? unchecked((ulong)(-(value + 1))) + 1
                : (ulong)value;
            EnsureAppend(DigitCount(magnitude) + (value < 0 ? 1 : 0) + 1);
            TextIntegerWriter.Append(_builder, value);
            _builder.Append('\n');
        }

        protected override void WriteUnsignedIntegerValue(ulong value)
        {
            BeginInlineValue();
            EnsureAppend(DigitCount(value) + 1);
            TextIntegerWriter.Append(_builder, value);
            _builder.Append('\n');
        }

        protected override void BeginObjectValue(int referenceId, bool isReference,
            string typeName, string assemblyName, int fieldCount)
        {
            if (!isReference && referenceId < 0 && typeName == null && fieldCount == 0)
            {
                WriteInline("{}", false);
                PushContainer(0);
                return;
            }

            int delta = BeginBlockValue();
            PushContainer(delta);
            if (isReference)
            {
                WriteIntegerEntry("$ref", referenceId);
                return;
            }
            if (referenceId >= 0)
                WriteIntegerEntry("$id", referenceId);
            if (typeName != null)
            {
                WriteQuotedEntry("$type", typeName);
                WriteQuotedEntry("$assembly", assemblyName ?? string.Empty);
            }
        }

        protected override void BeginObjectField(string name)
        {
            BeginEntry(StructuredNode.EncodeTextFieldName(name));
        }

        protected override void EndObjectField()
        {
            if (_pendingValue)
                throw new InvalidOperationException("A YAML mapping value was not written.");
        }

        protected override void EndObjectValue()
        {
            PopContainer();
        }

        protected override void BeginSequenceValue(int referenceId, bool isReference,
            int count)
        {
            if (isReference)
            {
                int referenceDelta = BeginBlockValue();
                PushContainer(referenceDelta);
                WriteIntegerEntry("$ref", referenceId);
                return;
            }
            if (referenceId >= 0)
            {
                int wrapperDelta = BeginBlockValue();
                WriteIntegerEntry("$id", referenceId);
                AppendIndent();
                AppendQuoted("$values");
                if (count == 0)
                {
                    _builder.Append(": []\n");
                    EnsureLength();
                    PushContainer(wrapperDelta);
                    return;
                }
                _builder.Append(":\n");
                _indent += 2;
                PushContainer(wrapperDelta + 2);
                EnsureLength();
                return;
            }
            if (count == 0)
            {
                WriteInline("[]", false);
                PushContainer(0);
                return;
            }
            PushContainer(BeginBlockValue());
        }

        protected override void BeginSequenceItem()
        {
            AppendIndent();
            _builder.Append('-');
            _pendingValue = true;
            EnsureLength();
        }

        protected override void EndSequenceItem()
        {
            if (_pendingValue)
                throw new InvalidOperationException("A YAML sequence value was not written.");
        }

        protected override void EndSequenceValue()
        {
            PopContainer();
        }

        private void BeginEntry(string name)
        {
            if (_pendingValue)
                throw new InvalidOperationException("YAML entries cannot overlap.");
            AppendIndent();
            AppendQuoted(name);
            _builder.Append(':');
            _pendingValue = true;
            EnsureLength();
        }

        private void WriteIntegerEntry(string name, int value)
        {
            BeginEntry(name);
            _builder.Append(' ');
            TextIntegerWriter.Append(_builder, value);
            _builder.Append('\n');
            _pendingValue = false;
            EnsureLength();
        }

        private void WriteQuotedEntry(string name, string value)
        {
            BeginEntry(name);
            _builder.Append(' ');
            AppendQuoted(value);
            _builder.Append('\n');
            _pendingValue = false;
            EnsureLength();
        }

        private void WriteInline(string value, bool quoted)
        {
            BeginInlineValue();
            if (quoted) AppendQuoted(value);
            else Append(value);
            _builder.Append('\n');
            EnsureLength();
        }

        private void BeginInlineValue()
        {
            if (_pendingValue)
            {
                _builder.Append(' ');
                _pendingValue = false;
            }
        }

        private int BeginBlockValue()
        {
            if (!_pendingValue) return 0;
            _builder.Append('\n');
            _pendingValue = false;
            _indent += 2;
            EnsureLength();
            return 2;
        }

        private void PushContainer(int indentDelta)
        {
            if (_containerDepth == _containerIndentDeltas.Length)
                Array.Resize(ref _containerIndentDeltas,
                    checked(_containerIndentDeltas.Length * 2));
            _containerIndentDeltas[_containerDepth++] = indentDelta;
        }

        private void PopContainer()
        {
            if (_containerDepth <= 0)
                throw new InvalidOperationException("YAML container state is unbalanced.");
            _indent -= _containerIndentDeltas[--_containerDepth];
            if (_indent < 0)
                throw new InvalidOperationException("YAML indentation state is unbalanced.");
        }

        private void AppendIndent()
        {
            _builder.Append(' ', _indent);
        }

        private void AppendQuoted(string value)
        {
            int outputLength = 2;
            bool requiresEscaping = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                int characterLength = c == '"' || c == '\\' || c == '\0' || c == '\a' ||
                                      c == '\b' || c == '\t' || c == '\n' || c == '\v' ||
                                      c == '\f' || c == '\r'
                    ? 2
                    : c < 0x20 ? 6 : 1;
                requiresEscaping |= characterLength != 1;
                if (outputLength > int.MaxValue - characterLength)
                    throw new FormatException("YAML string is too long.");
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
                    case '\0': _builder.Append("\\0"); break;
                    case '\a': _builder.Append("\\a"); break;
                    case '\b': _builder.Append("\\b"); break;
                    case '\t': _builder.Append("\\t"); break;
                    case '\n': _builder.Append("\\n"); break;
                    case '\v': _builder.Append("\\v"); break;
                    case '\f': _builder.Append("\\f"); break;
                    case '\r': _builder.Append("\\r"); break;
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

        private void Append(string value)
        {
            EnsureAppend(value.Length);
            _builder.Append(value);
        }

        private void EnsureLength()
        {
            if (_builder.Length > MaxTextLength)
                throw new FormatException($"YAML output length cannot exceed {MaxTextLength} characters.");
        }

        private void EnsureAppend(int count)
        {
            if (count > MaxTextLength - _builder.Length)
                throw new FormatException($"YAML output length cannot exceed {MaxTextLength} characters.");
        }
    }
}
