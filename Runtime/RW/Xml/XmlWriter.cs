using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace ActionBuffer
{
    public sealed class XmlWriter : StructuredTextWriter
    {
        private readonly StringBuilder _builder = new StringBuilder();
        private bool _prettyPrint = true;

        public bool prettyPrint
        {
            get { return _prettyPrint; }
            set { _prettyPrint = value; }
        }

        public string GetXml()
        {
            _builder.Clear();
            XmlWriting.Write(GetRoot(), _builder, _prettyPrint);
            return _builder.ToString();
        }

        public override void Clear()
        {
            _builder.Clear();
            _prettyPrint = true;
            base.Clear();
        }
    }

    internal static class XmlWriting
    {
        public static void Write(StructuredNode node, StringBuilder builder, bool prettyPrint)
        {
            var settings = new System.Xml.XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = prettyPrint,
                IndentChars = "  ",
                NewLineChars = "\n",
                NewLineHandling = System.Xml.NewLineHandling.Replace
            };

            using (var stringWriter = new StringWriter(builder, CultureInfo.InvariantCulture))
            using (var writer = System.Xml.XmlWriter.Create(stringWriter, settings))
            {
                WriteNode(writer, "ActionBuffer", null, node);
            }
        }

        private static void WriteNode(System.Xml.XmlWriter writer, string elementName, string fieldName, StructuredNode node)
        {
            writer.WriteStartElement(elementName);
            if (fieldName != null)
                writer.WriteAttributeString("name", fieldName);
            writer.WriteAttributeString("kind", GetKindName(node.Kind));

            if (node.Kind == StructuredNodeKind.Object)
            {
                if (!string.IsNullOrEmpty(node.TypeName))
                {
                    writer.WriteAttributeString("type", node.TypeName);
                    writer.WriteAttributeString("assembly", node.AssemblyName ?? string.Empty);
                }

                for (int i = 0; i < node.Fields.Count; i++)
                {
                    var field = node.Fields[i];
                    WriteNode(writer, "Field", field.Name, field.Value);
                }
            }
            else if (node.Kind == StructuredNodeKind.Sequence)
            {
                for (int i = 0; i < node.Items.Count; i++)
                    WriteNode(writer, "Item", null, node.Items[i]);
            }
            else if (node.Kind == StructuredNodeKind.Scalar)
            {
                writer.WriteString(node.Scalar ?? string.Empty);
            }

            writer.WriteEndElement();
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
    }
}
