using System;
using System.Text;
using System.Xml;

namespace ActionBuffer
{
    public sealed class XmlWriter : StructuredTextWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private bool _prettyPrint = true;

        public string GetXml()
        {
            _builder.Clear();
            WriteNode(_builder, "ActionBuffer", null, GetRoot(), _prettyPrint, 0);
            if (_prettyPrint && _builder.Length > 0)
                _builder.Length--;
            ValidateTextLength(_builder.Length, "XML");
            return _builder.ToString();
        }

        protected override void OnInit(BufferScan scan)
        {
            _prettyPrint = scan.Settings.PrettyPrint;
        }

        public override void Clear()
        {
            _builder.Clear();
            if (_builder.Capacity > BufferSerializer.RetainedTextCapacity)
                _builder.Capacity = 1024;
            _prettyPrint = true;
            base.Clear();
        }

        private static void WriteNode(StringBuilder builder, string elementName, string fieldName,
            StructuredNode node, bool prettyPrint, int indent)
        {
            if (prettyPrint) AppendIndent(builder, indent);
            builder.Append('<').Append(elementName);
            if (fieldName != null)
                AppendAttribute(builder, "name", fieldName);
            AppendAttribute(builder, "kind", GetKindName(node.Kind));

            if (node.IsReference)
                AppendAttribute(builder, "ref", node.ReferenceId.ToString());
            else if (node.ReferenceId >= 0)
                AppendAttribute(builder, "id", node.ReferenceId.ToString());

            if (node.Kind == StructuredNodeKind.Object && !node.IsReference &&
                !string.IsNullOrEmpty(node.TypeName))
            {
                AppendAttribute(builder, "type", node.TypeName);
                AppendAttribute(builder, "assembly", node.AssemblyName ?? string.Empty);
            }
            bool hasChildren = node.Kind == StructuredNodeKind.Object && !node.IsReference &&
                               node.FieldCount > 0 ||
                               node.Kind == StructuredNodeKind.Sequence && node.ItemCount > 0;
            bool hasScalar = node.Kind == StructuredNodeKind.Scalar && !string.IsNullOrEmpty(node.Scalar);
            if (!hasChildren && !hasScalar)
            {
                builder.Append(" />");
                if (prettyPrint) builder.Append('\n');
                EnsureLength(builder);
                return;
            }

            builder.Append('>');
            if (node.Kind == StructuredNodeKind.Scalar)
            {
                AppendText(builder, node.Scalar);
            }
            else
            {
                if (prettyPrint) builder.Append('\n');
                if (node.Kind == StructuredNodeKind.Object)
                {
                    for (int i = 0; i < node.FieldCount; i++)
                    {
                        var field = node.GetField(i);
                        WriteNode(builder, "Field", field.Name, field.Value, prettyPrint, indent + 1);
                    }
                }
                else
                {
                    for (int i = 0; i < node.ItemCount; i++)
                        WriteNode(builder, "Item", null, node.GetItem(i), prettyPrint, indent + 1);
                }
                if (prettyPrint) AppendIndent(builder, indent);
            }

            builder.Append("</").Append(elementName).Append('>');
            if (prettyPrint) builder.Append('\n');
            EnsureLength(builder);
        }

        private static void AppendAttribute(StringBuilder builder, string name, string value)
        {
            builder.Append(' ').Append(name).Append("=\"");
            AppendEscaped(builder, value, true);
            builder.Append('"');
        }

        private static void AppendText(StringBuilder builder, string value)
        {
            AppendEscaped(builder, value, false);
        }

        private static void AppendEscaped(StringBuilder builder, string value, bool attribute)
        {
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsHighSurrogate(c))
                {
                    if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                        throw new FormatException("XML strings cannot contain an unpaired surrogate.");
                    builder.Append(c).Append(value[++i]);
                    continue;
                }
                if (char.IsLowSurrogate(c) || !XmlConvert.IsXmlChar(c))
                    throw new FormatException($"XML strings cannot contain character U+{(int)c:X4}.");

                switch (c)
                {
                    case '&': builder.Append("&amp;"); break;
                    case '<': builder.Append("&lt;"); break;
                    case '>': builder.Append("&gt;"); break;
                    case '"' when attribute: builder.Append("&quot;"); break;
                    case '\t' when attribute: builder.Append("&#x9;"); break;
                    case '\n' when attribute: builder.Append("&#xA;"); break;
                    case '\r': builder.Append("&#xD;"); break;
                    default: builder.Append(c); break;
                }
                EnsureLength(builder);
            }
        }

        private static void AppendIndent(StringBuilder builder, int indent)
        {
            builder.Append(' ', indent * 2);
        }

        private static string GetKindName(StructuredNodeKind kind)
        {
            switch (kind)
            {
                case StructuredNodeKind.Null: return "null";
                case StructuredNodeKind.Scalar: return "scalar";
                case StructuredNodeKind.Object: return "object";
                case StructuredNodeKind.Sequence: return "sequence";
                default: throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static void EnsureLength(StringBuilder builder)
        {
        }
    }
}
