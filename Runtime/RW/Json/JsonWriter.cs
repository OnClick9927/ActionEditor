using System;
using System.Globalization;
using System.Text;

namespace ActionBuffer
{
    public class JsonWriter : StructuredTextWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private bool _prettyPrint;

        public string GetJson()
        {
            _builder.Clear();
            Write(GetRoot(), _builder, _prettyPrint);
            ValidateTextLength(_builder.Length, "JSON");
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
            _prettyPrint = false;
            base.Clear();
        }

        private static void Write(StructuredNode node, StringBuilder builder, bool prettyPrint)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            WriteNode(node, builder, prettyPrint, 0);
        }

        private static void WriteNode(StructuredNode node, StringBuilder builder, bool prettyPrint, int indent)
        {
            switch (node.Kind)
            {
                case StructuredNodeKind.Null:
                    builder.Append("null");
                    break;
                case StructuredNodeKind.Scalar:
                    WriteScalar(node, builder);
                    break;
                case StructuredNodeKind.Object:
                    WriteObject(node, builder, prettyPrint, indent);
                    break;
                case StructuredNodeKind.Sequence:
                    WriteSequence(node, builder, prettyPrint, indent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
            EnsureLength(builder);
        }

        private static void WriteObject(StructuredNode node, StringBuilder builder, bool prettyPrint, int indent)
        {
            builder.Append('{');
            int memberIndex = 0;

            if (node.IsReference)
            {
                WritePropertyPrefix("$ref", memberIndex++, builder, prettyPrint, indent);
                builder.Append(node.ReferenceId.ToString(CultureInfo.InvariantCulture));
            }
            else if (node.ReferenceId >= 0)
            {
                WritePropertyPrefix("$id", memberIndex++, builder, prettyPrint, indent);
                builder.Append(node.ReferenceId.ToString(CultureInfo.InvariantCulture));
            }

            if (!node.IsReference && !string.IsNullOrEmpty(node.TypeName))
            {
                WritePropertyPrefix("$type", memberIndex++, builder, prettyPrint, indent);
                WriteString(node.TypeName, builder);
                WritePropertyPrefix("$assembly", memberIndex++, builder, prettyPrint, indent);
                WriteString(node.AssemblyName ?? string.Empty, builder);
            }

            for (int i = 0; !node.IsReference && i < node.FieldCount; i++)
            {
                var field = node.GetField(i);
                WritePropertyPrefix(StructuredNode.EncodeTextFieldName(field.Name), memberIndex++, builder, prettyPrint, indent);
                WriteNode(field.Value, builder, prettyPrint, indent + 1);
            }

            if (prettyPrint && memberIndex > 0)
            {
                builder.Append('\n');
                AppendIndent(builder, indent);
            }
            builder.Append('}');
        }

        private static void WritePropertyPrefix(string name, int index, StringBuilder builder, bool prettyPrint, int indent)
        {
            if (index > 0)
                builder.Append(',');
            if (prettyPrint)
            {
                builder.Append('\n');
                AppendIndent(builder, indent + 1);
            }
            WriteString(name, builder);
            builder.Append(':');
            if (prettyPrint)
                builder.Append(' ');
        }

        private static void WriteSequence(StructuredNode node, StringBuilder builder, bool prettyPrint, int indent)
        {
            builder.Append('[');
            for (int i = 0; i < node.ItemCount; i++)
            {
                if (i > 0)
                    builder.Append(',');
                if (prettyPrint)
                {
                    builder.Append('\n');
                    AppendIndent(builder, indent + 1);
                }
                WriteNode(node.GetItem(i), builder, prettyPrint, indent + 1);
            }
            if (prettyPrint && node.ItemCount > 0)
            {
                builder.Append('\n');
                AppendIndent(builder, indent);
            }
            builder.Append(']');
        }

        private static void WriteScalar(StructuredNode node, StringBuilder builder)
        {
            if (node.Quoted || !IsJsonLiteral(node.Scalar))
                WriteString(node.Scalar ?? string.Empty, builder);
            else
                builder.Append(node.Scalar);
        }

        private static bool IsJsonLiteral(string value)
        {
            if (value == "true" || value == "false") return true;
            if (string.IsNullOrEmpty(value)) return false;

            int index = 0;
            if (value[index] == '-')
            {
                index++;
                if (index == value.Length) return false;
            }

            if (value[index] == '0')
            {
                index++;
            }
            else
            {
                if (value[index] < '1' || value[index] > '9') return false;
                while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                    index++;
            }

            if (index < value.Length && value[index] == '.')
            {
                index++;
                int fractionStart = index;
                while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                    index++;
                if (index == fractionStart) return false;
            }

            if (index < value.Length && (value[index] == 'e' || value[index] == 'E'))
            {
                index++;
                if (index < value.Length && (value[index] == '+' || value[index] == '-'))
                    index++;
                int exponentStart = index;
                while (index < value.Length && value[index] >= '0' && value[index] <= '9')
                    index++;
                if (index == exponentStart) return false;
            }

            return index == value.Length;
        }

        private static void WriteString(string value, StringBuilder builder)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            builder.Append(c);
                        }
                        break;
                }
                EnsureLength(builder);
            }
            builder.Append('"');
            EnsureLength(builder);
        }

        private static void AppendIndent(StringBuilder builder, int indent)
        {
            builder.Append(' ', indent * 2);
        }

        private static void EnsureLength(StringBuilder builder)
        {
        }
    }
}
