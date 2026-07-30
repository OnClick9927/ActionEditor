using System;
using System.IO;
using System.Xml;

namespace ActionBuffer
{
    public sealed class XmlReader : StructuredTextReader
    {
        public void Init(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Clear();
            if (data.Length > BufferSerializer.MaxTextLength)
                throw new FormatException(
                    $"XML length cannot exceed {BufferSerializer.MaxTextLength} characters.");
            var root = default(StructuredNode);
            try
            {
                root = Parse(data);
                SetRoot(root);
                root = default;
            }
            finally
            {
                StructuredNode.Release(ref root);
            }
        }

        private static StructuredNode Parse(string xml)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                MaxCharactersInDocument = BufferSerializer.MaxTextLength
            };

            using (var stringReader = new StringReader(xml))
            using (var reader = System.Xml.XmlReader.Create(stringReader, settings))
            {
                if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "ActionBuffer")
                    throw Error(reader, "XML root element must be 'ActionBuffer'.");

                int nodeCount = 0;
                var root = ParseNode(reader, 0, ref nodeCount);
                try
                {
                    if (reader.MoveToContent() != XmlNodeType.None)
                        throw Error(reader, "Unexpected content after the ActionBuffer root element.");
                    return root;
                }
                catch
                {
                    StructuredNode.Release(ref root);
                    throw;
                }
            }
        }

        private static StructuredNode ParseNode(System.Xml.XmlReader reader, int depth, ref int nodeCount)
        {
            if (depth >= BufferScan.MaxDepth)
                throw Error(reader, $"XML depth cannot exceed {BufferScan.MaxDepth}.");
            if (nodeCount >= BufferSerializer.MaxNodeCount)
                throw Error(reader, $"XML node count cannot exceed {BufferSerializer.MaxNodeCount}.");
            nodeCount++;
            if (reader.NodeType != XmlNodeType.Element)
                throw Error(reader, "Expected an XML node element.");

            string kindName = reader.GetAttribute("kind");
            if (string.IsNullOrEmpty(kindName))
                throw Error(reader, "Missing required 'kind' attribute.");

            StructuredNodeKind kind;
            switch (kindName)
            {
                case "null": kind = StructuredNodeKind.Null; break;
                case "scalar": kind = StructuredNodeKind.Scalar; break;
                case "object": kind = StructuredNodeKind.Object; break;
                case "sequence": kind = StructuredNodeKind.Sequence; break;
                default: throw Error(reader, $"Unknown node kind '{kindName}'.");
            }

            if (kind == StructuredNodeKind.Scalar)
            {
                string scalar = reader.ReadElementContentAsString();
                EnsureScalarLength(scalar, reader);
                return StructuredNode.RentScalar(scalar, true);
            }

            var node = StructuredNode.Rent(kind);
            var fieldNames = kind == StructuredNodeKind.Object ? HashSetPool<string>.Get() : null;
            try
            {
                if (kind == StructuredNodeKind.Object)
                {
                    node.TypeName = reader.GetAttribute("type");
                    node.AssemblyName = reader.GetAttribute("assembly");
                    EnsureScalarLength(node.TypeName, reader);
                    EnsureScalarLength(node.AssemblyName, reader);
                }

                bool empty = reader.IsEmptyElement;
                reader.ReadStartElement();
                if (empty) return node;

                while (reader.MoveToContent() != XmlNodeType.EndElement)
                {
                    if (reader.NodeType != XmlNodeType.Element)
                        throw Error(reader, $"{kind} nodes cannot contain text content.");

                    if (kind == StructuredNodeKind.Object)
                    {
                        if (reader.LocalName != "Field")
                            throw Error(reader, "Object nodes may only contain Field elements.");
                        string name = reader.GetAttribute("name");
                        if (name == null)
                            throw Error(reader, "Field element is missing the 'name' attribute.");
                        if (node.FieldCount >= BufferSerializer.MaxObjectFieldCount)
                            throw Error(reader,
                                $"XML object field count cannot exceed {BufferSerializer.MaxObjectFieldCount}.");
                        EnsureScalarLength(name, reader);
                        if (!fieldNames.Add(name))
                            throw Error(reader, $"Duplicate field '{name}'.");
                        var value = ParseNode(reader, depth + 1, ref nodeCount);
                        try
                        {
                            node.AddField(name, value);
                            value = default;
                        }
                        finally
                        {
                            StructuredNode.Release(ref value);
                        }
                    }
                    else if (kind == StructuredNodeKind.Sequence)
                    {
                        if (reader.LocalName != "Item")
                            throw Error(reader, "Sequence nodes may only contain Item elements.");
                        if (node.ItemCount >= BufferSerializer.MaxCollectionCount)
                            throw Error(reader,
                                $"XML sequence count cannot exceed {BufferSerializer.MaxCollectionCount}.");
                        var value = ParseNode(reader, depth + 1, ref nodeCount);
                        try
                        {
                            node.AddItem(value);
                            value = default;
                        }
                        finally
                        {
                            StructuredNode.Release(ref value);
                        }
                    }
                    else
                    {
                        throw Error(reader, $"{kind} nodes cannot contain child elements.");
                    }
                }

                reader.ReadEndElement();
                return node;
            }
            catch
            {
                StructuredNode.Release(ref node);
                throw;
            }
            finally
            {
                HashSetPool<string>.Back(fieldNames);
            }
        }

        private static void EnsureScalarLength(string value, System.Xml.XmlReader reader)
        {
            if (value != null && value.Length > BufferSerializer.MaxScalarLength)
                throw Error(reader,
                    $"XML scalar length cannot exceed {BufferSerializer.MaxScalarLength} characters.");
        }

        private static FormatException Error(System.Xml.XmlReader reader, string message)
        {
            var info = reader as IXmlLineInfo;
            return info != null && info.HasLineInfo()
                ? new FormatException($"XML line {info.LineNumber}: {message}")
                : new FormatException(message);
        }
    }
}
