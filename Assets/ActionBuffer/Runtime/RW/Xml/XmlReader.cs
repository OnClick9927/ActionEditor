using System;
using System.IO;
using System.Xml.Linq;

namespace ActionBuffer
{
    public sealed class XmlReader : StructuredTextReader
    {
        public void Init(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Clear();
            var root = XmlParsing.Parse(data);
            SetRoot(root);
        }
    }

    internal static class XmlParsing
    {
        public static StructuredNode Parse(string xml)
        {
            var settings = new System.Xml.XmlReaderSettings
            {
                DtdProcessing = System.Xml.DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true
            };

            XDocument document;
            using (var stringReader = new StringReader(xml))
            using (var reader = System.Xml.XmlReader.Create(stringReader, settings))
                document = XDocument.Load(reader, LoadOptions.SetLineInfo);

            if (document.Root == null || document.Root.Name.LocalName != "ActionBuffer")
                throw new FormatException("XML root element must be 'ActionBuffer'.");
            return ParseNode(document.Root);
        }

        private static StructuredNode ParseNode(XElement element)
        {
            string kindName = (string)element.Attribute("kind");
            if (string.IsNullOrEmpty(kindName))
                throw Error(element, "Missing required 'kind' attribute.");

            StructuredNodeKind kind;
            switch (kindName)
            {
                case "null": kind = StructuredNodeKind.Null; break;
                case "scalar": kind = StructuredNodeKind.Scalar; break;
                case "object": kind = StructuredNodeKind.Object; break;
                case "sequence": kind = StructuredNodeKind.Sequence; break;
                default: throw Error(element, $"Unknown node kind '{kindName}'.");
            }

            var node = kind == StructuredNodeKind.Scalar
                ? StructuredNode.RentScalar(element.Value, true)
                : StructuredNode.Rent(kind);
            try
            {
                if (kind == StructuredNodeKind.Object)
                {
                    node.TypeName = (string)element.Attribute("type");
                    node.AssemblyName = (string)element.Attribute("assembly");
                    foreach (var child in element.Elements())
                    {
                        if (child.Name.LocalName != "Field")
                            throw Error(child, "Object nodes may only contain Field elements.");
                        string name = (string)child.Attribute("name");
                        if (name == null)
                            throw Error(child, "Field element is missing the 'name' attribute.");
                        EnsureUniqueField(node, name, child);
                        node.AddField(name, ParseNode(child));
                    }
                }
                else if (kind == StructuredNodeKind.Sequence)
                {
                    foreach (var child in element.Elements())
                    {
                        if (child.Name.LocalName != "Item")
                            throw Error(child, "Sequence nodes may only contain Item elements.");
                        node.AddItem(ParseNode(child));
                    }
                }
                else if (element.HasElements)
                {
                    throw Error(element, $"{kind} nodes cannot contain child elements.");
                }
                return node;
            }
            catch
            {
                StructuredNode.Release(node);
                throw;
            }
        }

        private static void EnsureUniqueField(StructuredNode node, string name, XElement element)
        {
            for (int i = 0; i < node.Fields.Count; i++)
                if (node.Fields[i].Name == name)
                    throw Error(element, $"Duplicate field '{name}'.");
        }

        private static FormatException Error(XElement element, string message)
        {
            var info = (System.Xml.IXmlLineInfo)element;
            return info.HasLineInfo()
                ? new FormatException($"XML line {info.LineNumber}: {message}")
                : new FormatException(message);
        }
    }
}
