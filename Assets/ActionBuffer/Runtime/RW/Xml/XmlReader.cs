using System;
using System.IO;
using System.Xml;

namespace ActionBuffer
{
    public sealed class XmlReader : StructuredTextReader
    {
        private readonly struct ParseLimits
        {
            internal readonly int MaxTextLength;
            internal readonly int MaxDepth;
            internal readonly int MaxNodeCount;
            internal readonly int MaxCollectionCount;
            internal readonly int MaxObjectFieldCount;
            internal readonly int MaxScalarLength;

            internal ParseLimits(int maxTextLength, int maxDepth, int maxNodeCount,
                int maxCollectionCount, int maxObjectFieldCount, int maxScalarLength)
            {
                MaxTextLength = maxTextLength;
                MaxDepth = maxDepth;
                MaxNodeCount = maxNodeCount;
                MaxCollectionCount = maxCollectionCount;
                MaxObjectFieldCount = maxObjectFieldCount;
                MaxScalarLength = maxScalarLength;
            }
        }

        public void Init(string data, BuffSettings settings = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Prepare(settings);
            var limits = new ParseLimits(
                BuffSettings.MaxTextLength,
                BuffSettings.MaxDepth,
                BuffSettings.MaxNodeCount,
                BuffSettings.MaxCollectionCount,
                BuffSettings.MaxObjectFieldCount,
                BuffSettings.MaxScalarLength);
            if (data.Length > limits.MaxTextLength)
                throw new FormatException(
                    $"XML length cannot exceed {limits.MaxTextLength} characters.");
            var root = default(StructuredNode);
            try
            {
                root = Parse(data, limits);
                SetRoot(root);
                root = default;
            }
            finally
            {
                StructuredNode.Release(ref root);
            }
        }

        private StructuredNode Parse(string xml, ParseLimits limits)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                MaxCharactersInDocument = limits.MaxTextLength
            };

            using (var stringReader = new StringReader(xml))
            using (var reader = System.Xml.XmlReader.Create(stringReader, settings))
            {
                if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "ActionBuffer")
                    throw Error(reader, "XML root element must be 'ActionBuffer'.");

                int nodeCount = 0;
                var root = ParseNode(reader, 0, ref nodeCount, limits);
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

        private StructuredNode ParseNode(System.Xml.XmlReader reader, int depth,
            ref int nodeCount, ParseLimits limits)
        {
            int maxDepth = limits.MaxDepth;
            if (depth >= maxDepth)
                throw Error(reader, $"XML depth cannot exceed {maxDepth}.");
            if (nodeCount >= limits.MaxNodeCount)
                throw Error(reader, $"XML node count cannot exceed {limits.MaxNodeCount}.");
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
                EnsureScalarLength(scalar, reader, limits.MaxScalarLength);
                return RentScalar(scalar, true);
            }

            var node = RentNode(kind);
            var fieldNames = kind == StructuredNodeKind.Object
                ? ClassPool.GetHashSet<string>()
                : null;
            try
            {
                if (kind == StructuredNodeKind.Object || kind == StructuredNodeKind.Sequence)
                {
                    string id = reader.GetAttribute("id");
                    string reference = reader.GetAttribute("ref");
                    if (id != null && reference != null)
                        throw Error(reader, "An object cannot contain both 'id' and 'ref'.");
                    if (reference != null)
                    {
                        node.ReferenceId = ParseReferenceId(reference, reader);
                        node.IsReference = true;
                    }
                    else if (id != null)
                    {
                        node.ReferenceId = ParseReferenceId(id, reader);
                    }
                    if (kind == StructuredNodeKind.Object)
                    {
                        node.TypeName = reader.GetAttribute("type");
                        node.AssemblyName = reader.GetAttribute("assembly");
                        EnsureScalarLength(node.TypeName, reader, limits.MaxScalarLength);
                        EnsureScalarLength(node.AssemblyName, reader, limits.MaxScalarLength);
                    }
                }

                bool empty = reader.IsEmptyElement;
                reader.ReadStartElement();
                if (empty) return node;
                if (node.IsReference)
                    throw Error(reader, "A reference object cannot contain child elements.");

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
                        if (node.FieldCount >= limits.MaxObjectFieldCount)
                            throw Error(reader,
                                $"XML object field count cannot exceed {limits.MaxObjectFieldCount}.");
                        EnsureScalarLength(name, reader, limits.MaxScalarLength);
                        if (!fieldNames.Add(name))
                            throw Error(reader, $"Duplicate field '{name}'.");
                        var value = ParseNode(reader, depth + 1, ref nodeCount, limits);
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
                        if (node.ItemCount >= limits.MaxCollectionCount)
                            throw Error(reader,
                                $"XML sequence count cannot exceed {limits.MaxCollectionCount}.");
                        var value = ParseNode(reader, depth + 1, ref nodeCount, limits);
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
                ClassPool.BackHashSet(fieldNames);
            }
        }

        private static int ParseReferenceId(string value, System.Xml.XmlReader reader)
        {
            if (!int.TryParse(value, out int result) || result < 0)
                throw Error(reader, $"Invalid object reference id '{value}'.");
            return result;
        }

        private static void EnsureScalarLength(string value, System.Xml.XmlReader reader,
            int maxScalarLength)
        {
            if (value != null && value.Length > maxScalarLength)
                throw Error(reader,
                    $"XML scalar length cannot exceed {maxScalarLength} characters.");
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
