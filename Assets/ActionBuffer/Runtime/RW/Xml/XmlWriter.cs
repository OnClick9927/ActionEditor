using System;
using System.Text;
using System.Xml;

namespace ActionBuffer
{
    public sealed class XmlWriter : StructuredTextWriter
    {
        private const byte RootElement = 1;
        private const byte FieldElement = 2;
        private const byte ItemElement = 3;

        private readonly StringBuilder _builder = new StringBuilder();
        private byte[] _elementKinds = new byte[16];
        private bool[] _closedElements = new bool[16];
        private string _pendingField;
        private bool _pendingItem;
        private bool _rootWritten;
        private bool _prettyPrint = true;
        private int _elementDepth;
        private int _indent;

        internal int Capacity => _builder.Capacity;

        internal void TrimCapacity()
        {
            if (_builder.Capacity > BuffSettings.RetainedTextCapacity)
            {
                _builder.Clear();
                _builder.Capacity = 1024;
            }
            if (_elementKinds.Length > 256)
            {
                _elementKinds = new byte[16];
                _closedElements = new bool[16];
            }
        }

        public string GetXml()
        {
            RequireResult();
            if (_prettyPrint && _builder.Length > 0 && _builder[_builder.Length - 1] == '\n')
                _builder.Length--;
            ValidateTextLength(_builder.Length, "XML");
            return _builder.ToString();
        }

        protected override void OnInit(BufferScan scan)
        {
            _prettyPrint = scan.PrettyPrint;
        }

        public override void Clear()
        {
            _builder.Clear();
            _pendingField = null;
            _pendingItem = false;
            _rootWritten = false;
            _prettyPrint = true;
            _elementDepth = 0;
            _indent = 0;
            base.Clear();
        }

        protected override void WriteNullValue()
        {
            WriteEmptyNode("null");
        }

        protected override void WriteScalarValue(string value, bool quoted)
        {
            byte element = TakeElement(out string fieldName);
            AppendNodeStart(element, fieldName, "scalar");
            if (string.IsNullOrEmpty(value))
            {
                Append(" />");
                AppendLineEnd();
                return;
            }
            Append('>');
            AppendEscaped(value, false);
            Append("</");
            AppendElementName(element);
            Append('>');
            AppendLineEnd();
        }

        protected override void WriteBooleanValue(bool value)
        {
            byte element = BeginScalarNode();
            Append(value ? "true" : "false");
            EndScalarNode(element);
        }

        protected override void WriteCharacterValue(char value)
        {
            byte element = BeginScalarNode();
            AppendEscapedCharacter(value, false);
            EndScalarNode(element);
        }

        protected override void WriteSignedIntegerValue(long value)
        {
            byte element = BeginScalarNode();
            ulong magnitude = value < 0
                ? unchecked((ulong)(-(value + 1))) + 1
                : (ulong)value;
            EnsureAppend(DigitCount(magnitude) + (value < 0 ? 1 : 0));
            TextIntegerWriter.Append(_builder, value);
            EndScalarNode(element);
        }

        protected override void WriteUnsignedIntegerValue(ulong value)
        {
            byte element = BeginScalarNode();
            EnsureAppend(DigitCount(value));
            TextIntegerWriter.Append(_builder, value);
            EndScalarNode(element);
        }

        protected override void BeginObjectValue(int referenceId, bool isReference,
            string typeName, string assemblyName, int fieldCount)
        {
            byte element = TakeElement(out string fieldName);
            BeginContainer(element, fieldName, "object", referenceId, isReference,
                typeName, assemblyName, isReference || fieldCount == 0);
        }

        protected override void BeginObjectField(string name)
        {
            if (_pendingField != null || _pendingItem)
                throw new InvalidOperationException("XML writer already has a pending value.");
            _pendingField = name;
        }

        protected override void EndObjectField()
        {
            if (_pendingField != null)
                throw new InvalidOperationException("An XML field has no value.");
        }

        protected override void EndObjectValue()
        {
            EndContainer();
        }

        protected override void BeginSequenceValue(int referenceId, bool isReference,
            int count)
        {
            byte element = TakeElement(out string fieldName);
            BeginContainer(element, fieldName, "sequence", referenceId, isReference,
                null, null, isReference || count == 0);
        }

        protected override void BeginSequenceItem()
        {
            if (_pendingField != null || _pendingItem)
                throw new InvalidOperationException("XML writer already has a pending value.");
            _pendingItem = true;
        }

        protected override void EndSequenceItem()
        {
            if (_pendingItem)
                throw new InvalidOperationException("An XML sequence item has no value.");
        }

        protected override void EndSequenceValue()
        {
            EndContainer();
        }

        private void WriteEmptyNode(string kind)
        {
            byte element = TakeElement(out string fieldName);
            AppendNodeStart(element, fieldName, kind);
            Append(" />");
            AppendLineEnd();
        }

        private byte BeginScalarNode()
        {
            byte element = TakeElement(out string fieldName);
            AppendNodeStart(element, fieldName, "scalar");
            Append('>');
            return element;
        }

        private void EndScalarNode(byte element)
        {
            Append("</");
            AppendElementName(element);
            Append('>');
            AppendLineEnd();
        }

        private void BeginContainer(byte element, string fieldName, string kind,
            int referenceId, bool isReference, string typeName, string assemblyName,
            bool closed)
        {
            EnsureElementCapacity();
            AppendNodeStart(element, fieldName, kind);
            if (isReference)
                AppendIntegerAttribute("ref", referenceId);
            else if (referenceId >= 0)
                AppendIntegerAttribute("id", referenceId);
            if (!isReference && typeName != null)
            {
                AppendAttribute("type", typeName);
                AppendAttribute("assembly", assemblyName ?? string.Empty);
            }
            _elementKinds[_elementDepth] = element;
            _closedElements[_elementDepth] = closed;
            _elementDepth++;
            if (closed)
            {
                Append(" />");
                AppendLineEnd();
                return;
            }
            Append('>');
            AppendLineEnd();
            _indent++;
        }

        private void EndContainer()
        {
            if (_elementDepth == 0)
                throw new InvalidOperationException("XML element stack is empty.");
            int index = --_elementDepth;
            if (_closedElements[index])
                return;
            _indent--;
            AppendIndent();
            Append("</");
            AppendElementName(_elementKinds[index]);
            Append('>');
            AppendLineEnd();
        }

        private byte TakeElement(out string fieldName)
        {
            if (_pendingField != null)
            {
                fieldName = _pendingField;
                _pendingField = null;
                return FieldElement;
            }
            fieldName = null;
            if (_pendingItem)
            {
                _pendingItem = false;
                return ItemElement;
            }
            if (!_rootWritten)
            {
                _rootWritten = true;
                return RootElement;
            }
            throw new InvalidOperationException("XML value has no parent field or item.");
        }

        private void AppendNodeStart(byte element, string fieldName, string kind)
        {
            AppendIndent();
            Append('<');
            AppendElementName(element);
            if (fieldName != null)
                AppendAttribute("name", fieldName);
            AppendAttribute("kind", kind);
        }

        private void AppendIntegerAttribute(string name, int value)
        {
            Append(' ');
            Append(name);
            Append("=\"");
            EnsureAppend(DigitCount((uint)value));
            TextIntegerWriter.Append(_builder, value);
            Append('"');
        }

        private void AppendAttribute(string name, string value)
        {
            Append(' ');
            Append(name);
            Append("=\"");
            AppendEscaped(value, true);
            Append('"');
        }

        private void AppendEscaped(string value, bool attribute)
        {
            int outputLength = 0;
            bool requiresEscaping = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        throw new FormatException("XML strings cannot contain an unpaired surrogate.");
                    outputLength = checked(outputLength + 2);
                    i++;
                    continue;
                }
                if (char.IsLowSurrogate(c) || !XmlConvert.IsXmlChar(c))
                    throw new FormatException($"XML strings cannot contain character U+{(int)c:X4}.");

                int characterLength;
                switch (c)
                {
                    case '&': characterLength = 5; break;
                    case '<': characterLength = 4; break;
                    case '>': characterLength = 4; break;
                    case '"' when attribute: characterLength = 6; break;
                    case '\t' when attribute: characterLength = 5; break;
                    case '\n' when attribute: characterLength = 5; break;
                    case '\r': characterLength = 5; break;
                    default: characterLength = 1; break;
                }
                requiresEscaping |= characterLength != 1;
                outputLength = checked(outputLength + characterLength);
            }
            EnsureAppend(outputLength);
            if (!requiresEscaping)
            {
                _builder.Append(value);
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsHighSurrogate(c))
                {
                    _builder.Append(c).Append(value[++i]);
                    continue;
                }
                switch (c)
                {
                    case '&': _builder.Append("&amp;"); break;
                    case '<': _builder.Append("&lt;"); break;
                    case '>': _builder.Append("&gt;"); break;
                    case '"' when attribute: _builder.Append("&quot;"); break;
                    case '\t' when attribute: _builder.Append("&#x9;"); break;
                    case '\n' when attribute: _builder.Append("&#xA;"); break;
                    case '\r': _builder.Append("&#xD;"); break;
                    default: _builder.Append(c); break;
                }
            }
        }

        private void AppendEscapedCharacter(char value, bool attribute)
        {
            if (char.IsSurrogate(value) || !XmlConvert.IsXmlChar(value))
                throw new FormatException("XML strings contain an invalid character.");
            switch (value)
            {
                case '&': Append("&amp;"); break;
                case '<': Append("&lt;"); break;
                case '>': Append("&gt;"); break;
                case '"' when attribute: Append("&quot;"); break;
                case '\t' when attribute: Append("&#x9;"); break;
                case '\n' when attribute: Append("&#xA;"); break;
                case '\r': Append("&#xD;"); break;
                default: Append(value); break;
            }
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

        private void EnsureElementCapacity()
        {
            if (_elementDepth < _elementKinds.Length) return;
            int size = checked(_elementKinds.Length * 2);
            Array.Resize(ref _elementKinds, size);
            Array.Resize(ref _closedElements, size);
        }

        private void AppendElementName(byte kind)
        {
            switch (kind)
            {
                case RootElement: Append("ActionBuffer"); break;
                case FieldElement: Append("Field"); break;
                case ItemElement: Append("Item"); break;
                default: throw new InvalidOperationException("Unknown XML element kind.");
            }
        }

        private void AppendIndent()
        {
            if (!_prettyPrint) return;
            int count = checked(_indent * 2);
            EnsureAppend(count);
            _builder.Append(' ', count);
        }

        private void AppendLineEnd()
        {
            if (_prettyPrint)
                Append('\n');
            EnsureLength();
        }

        private void Append(string value)
        {
            EnsureAppend(value.Length);
            _builder.Append(value);
        }

        private void Append(char value)
        {
            EnsureAppend(1);
            _builder.Append(value);
        }

        private void EnsureLength()
        {
            if (_builder.Length > MaxTextLength)
                throw new FormatException($"XML output length cannot exceed {MaxTextLength} characters.");
        }

        private void EnsureAppend(int count)
        {
            if (count > MaxTextLength - _builder.Length)
                throw new FormatException($"XML output length cannot exceed {MaxTextLength} characters.");
        }
    }
}
