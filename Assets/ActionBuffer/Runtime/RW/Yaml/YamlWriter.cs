using System;
using System.Globalization;
using System.Text;

namespace ActionBuffer
{
    public sealed class YamlWriter : StructuredTextWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();

        public string GetYaml()
        {
            _builder.Clear();
            Write(GetRoot(), _builder);
            ValidateTextLength(_builder.Length, "YAML");
            return _builder.ToString();
        }

        public override void Clear()
        {
            _builder.Clear();
            if (_builder.Capacity > BufferSerializer.RetainedTextCapacity)
                _builder.Capacity = 1024;
            base.Clear();
        }

        private static void Write(StructuredNode node, StringBuilder builder)
        {
            if (IsBlock(node))
                WriteBlock(node, builder, 0);
            else
            {
                WriteInline(node, builder);
                builder.Append('\n');
            }
            EnsureLength(builder);
        }

        private static bool IsBlock(StructuredNode node)
        {
            if (node.Kind == StructuredNodeKind.Object)
                return node.ReferenceId >= 0 || node.FieldCount > 0 || !string.IsNullOrEmpty(node.TypeName);
            return node.Kind == StructuredNodeKind.Sequence && node.ItemCount > 0;
        }

        private static void WriteBlock(StructuredNode node, StringBuilder builder, int indent)
        {
            if (node.Kind == StructuredNodeKind.Object)
            {
                if (node.IsReference)
                {
                    WriteScalarEntry("$ref", node.ReferenceId.ToString(CultureInfo.InvariantCulture),
                        builder, indent);
                    return;
                }
                if (node.ReferenceId >= 0)
                    WriteScalarEntry("$id", node.ReferenceId.ToString(CultureInfo.InvariantCulture),
                        builder, indent);
                if (!string.IsNullOrEmpty(node.TypeName))
                {
                    WriteScalarEntry("$type", node.TypeName, builder, indent);
                    WriteScalarEntry("$assembly", node.AssemblyName ?? string.Empty, builder, indent);
                }

                for (int i = 0; i < node.FieldCount; i++)
                {
                    var field = node.GetField(i);
                    WriteEntry(StructuredNode.EncodeTextFieldName(field.Name), field.Value, builder, indent);
                    EnsureLength(builder);
                }
                return;
            }

            if (node.Kind != StructuredNodeKind.Sequence)
                throw new InvalidOperationException($"Cannot write {node.Kind} as a YAML block.");

            for (int i = 0; i < node.ItemCount; i++)
            {
                AppendIndent(builder, indent);
                builder.Append('-');
                var item = node.GetItem(i);
                if (IsBlock(item))
                {
                    builder.Append('\n');
                    WriteBlock(item, builder, indent + 2);
                }
                else
                {
                    builder.Append(' ');
                    WriteInline(item, builder);
                    builder.Append('\n');
                }
                EnsureLength(builder);
            }
        }

        private static void WriteEntry(string name, StructuredNode value, StringBuilder builder, int indent)
        {
            AppendIndent(builder, indent);
            AppendQuoted(name, builder);
            builder.Append(':');
            if (IsBlock(value))
            {
                builder.Append('\n');
                WriteBlock(value, builder, indent + 2);
            }
            else
            {
                builder.Append(' ');
                WriteInline(value, builder);
                builder.Append('\n');
            }
        }

        private static void WriteScalarEntry(string name, string value, StringBuilder builder, int indent)
        {
            AppendIndent(builder, indent);
            AppendQuoted(name, builder);
            builder.Append(": ");
            AppendQuoted(value, builder);
            builder.Append('\n');
        }

        private static void WriteInline(StructuredNode node, StringBuilder builder)
        {
            switch (node.Kind)
            {
                case StructuredNodeKind.Null:
                    builder.Append("null");
                    break;
                case StructuredNodeKind.Scalar:
                    if (node.Quoted)
                        AppendQuoted(node.Scalar ?? string.Empty, builder);
                    else
                        builder.Append(node.Scalar);
                    EnsureLength(builder);
                    break;
                case StructuredNodeKind.Object:
                    builder.Append("{}");
                    break;
                case StructuredNodeKind.Sequence:
                    builder.Append("[]");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void AppendIndent(StringBuilder builder, int indent)
        {
            builder.Append(' ', indent);
        }

        private static void AppendQuoted(string value, StringBuilder builder)
        {
            builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\0': builder.Append("\\0"); break;
                    case '\a': builder.Append("\\a"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\t': builder.Append("\\t"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\v': builder.Append("\\v"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\r': builder.Append("\\r"); break;
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

        private static void EnsureLength(StringBuilder builder)
        {
        }
    }
}
