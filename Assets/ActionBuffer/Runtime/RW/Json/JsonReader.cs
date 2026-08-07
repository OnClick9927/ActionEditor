using System;
using System.Text;

namespace ActionBuffer
{
    public class JsonReader : StructuredTextReader
    {
        private string _json;
        private int _position;
        private int _nodeCount;
        private int _maxDepth;
        private int _maxNodeCount;
        private int _maxCollectionCount;
        private int _maxObjectFieldCount;
        private int _maxScalarLength;

        public static JsonReader Get()
        {
            var result = ClassPool.Get<JsonReader>();
            result.Clear();
            return result;
        }

        public static void Back(JsonReader value)
        {
            if (value == null) return;
            value.Clear();
            ClassPool.Back(value);
        }

        public void Init(string data, BuffSettings settings = null)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Prepare(settings);
            int maxTextLength = BuffSettings.MaxTextLength;
            if (data.Length > maxTextLength)
                throw new FormatException(
                    $"JSON length cannot exceed {maxTextLength} characters.");
            _maxDepth = BuffSettings.MaxDepth;
            _maxNodeCount = BuffSettings.MaxNodeCount;
            _maxCollectionCount = BuffSettings.MaxCollectionCount;
            _maxObjectFieldCount = BuffSettings.MaxObjectFieldCount;
            _maxScalarLength = BuffSettings.MaxScalarLength;
            _json = data;

            StructuredNode root = default;
            try
            {
                root = ParseDocument();
                SetRoot(root);
                root = default;
            }
            finally
            {
                StructuredNode.Release(ref root);
                ResetParser();
            }
        }

        public override void Clear()
        {
            ResetParser();
            base.Clear();
        }

        private void ResetParser()
        {
            _json = null;
            _position = 0;
            _nodeCount = 0;
        }

        private StructuredNode ParseDocument()
        {
            SkipWhitespace();
            if (IsEnd)
                throw Error("JSON input is empty.");

            var root = ParseValue(0);
            try
            {
                SkipWhitespace();
                if (!IsEnd)
                    throw Error("Unexpected trailing content.");
                return root;
            }
            catch
            {
                StructuredNode.Release(ref root);
                throw;
            }
        }

        private StructuredNode ParseValue(int depth)
        {
            int maxDepth = _maxDepth;
            if (depth >= maxDepth)
                throw Error($"JSON depth cannot exceed {maxDepth}.");
            CountNode();
            SkipWhitespace();
            if (IsEnd)
                throw Error("Expected a JSON value.");

            switch (Peek())
            {
                case '{': return ParseObject(depth);
                case '[': return ParseSequence(depth);
                case '"': return RentScalar(ReadString(), true);
                case 't':
                    ReadLiteral("true");
                    return RentScalar("true", false);
                case 'f':
                    ReadLiteral("false");
                    return RentScalar("false", false);
                case 'n':
                    ReadLiteral("null");
                    return RentNode(StructuredNodeKind.Null);
                default:
                    return ReadNumber();
            }
        }

        private StructuredNode ParseObject(int depth)
        {
            Expect('{');
            var node = RentNode(StructuredNodeKind.Object);
            var fieldNames = ClassPool.GetHashSet<string>();
            StructuredNode collectionValues = default;
            bool hasCollectionValues = false;
            try
            {
                SkipWhitespace();
                if (TryRead('}'))
                    return node;

                while (true)
                {
                    if (node.FieldCount >= _maxObjectFieldCount)
                        throw Error(
                            $"JSON object field count cannot exceed {_maxObjectFieldCount}.");
                    string name = ReadString();
                    Expect(':');
                    var value = ParseValue(depth + 1);
                    try
                    {
                        if (name == "$id" || name == "$ref")
                        {
                            RequireReferenceScalar(name, value);
                            if (node.ReferenceId >= 0)
                                throw Error("Duplicate object reference metadata.");
                            node.ReferenceId = ParseReferenceId(name, value);
                            node.IsReference = name == "$ref";
                        }
                        else if (name == "$type")
                        {
                            RequireMetadataScalar(name, value);
                            if (node.TypeName != null)
                                throw Error("Duplicate metadata '$type'.");
                            node.TypeName = value.Scalar;
                        }
                        else if (name == "$assembly")
                        {
                            RequireMetadataScalar(name, value);
                            if (node.AssemblyName != null)
                                throw Error("Duplicate metadata '$assembly'.");
                            node.AssemblyName = value.Scalar;
                        }
                        else if (name == "$values")
                        {
                            if (hasCollectionValues || value.Kind != StructuredNodeKind.Sequence)
                                throw Error("Metadata '$values' must contain one sequence.");
                            collectionValues = value;
                            value = default;
                            hasCollectionValues = true;
                        }
                        else
                        {
                            name = StructuredNode.DecodeTextFieldName(name);
                            if (!fieldNames.Add(name))
                                throw Error($"Duplicate JSON field '{name}'.");
                            node.AddField(name, value);
                            value = default;
                        }
                    }
                    finally
                    {
                        StructuredNode.Release(ref value);
                    }

                    SkipWhitespace();
                    if (TryRead('}'))
                    {
                        if (hasCollectionValues)
                        {
                            if (node.IsReference || node.ReferenceId < 0 ||
                                node.TypeName != null || node.AssemblyName != null ||
                                node.FieldCount != 0)
                                throw Error("A collection wrapper must contain only '$id' and '$values'.");
                            collectionValues.ReferenceId = node.ReferenceId;
                            node = collectionValues;
                            collectionValues = default;
                        }
                        ValidateReferenceNode(node);
                        return node;
                    }
                    Expect(',');
                }
            }
            catch
            {
                StructuredNode.Release(ref node);
                throw;
            }
            finally
            {
                StructuredNode.Release(ref collectionValues);
                ClassPool.BackHashSet(fieldNames);
            }
        }

        private int ParseReferenceId(string name, StructuredNode value)
        {
            ulong parsed;
            try
            {
                parsed = ParseUnsignedScalar(value);
            }
            catch (Exception exception) when (exception is FormatException ||
                                               exception is OverflowException)
            {
                throw Error($"Metadata '{name}' must be a non-negative integer.");
            }
            if (parsed > int.MaxValue)
                throw Error($"Metadata '{name}' must be a non-negative integer.");
            return (int)parsed;
        }

        private void ValidateReferenceNode(StructuredNode node)
        {
            if (node.IsReference && (node.FieldCount != 0 || node.TypeName != null ||
                                     node.AssemblyName != null))
                throw Error("A '$ref' object cannot contain fields or type metadata.");
        }

        private StructuredNode ParseSequence(int depth)
        {
            Expect('[');
            var node = RentNode(StructuredNodeKind.Sequence);
            try
            {
                SkipWhitespace();
                if (TryRead(']'))
                    return node;

                while (true)
                {
                    if (node.ItemCount >= _maxCollectionCount)
                        throw Error(
                            $"JSON sequence count cannot exceed {_maxCollectionCount}.");
                    var value = ParseValue(depth + 1);
                    try
                    {
                        node.AddItem(value);
                        value = default;
                    }
                    finally
                    {
                        StructuredNode.Release(ref value);
                    }
                    SkipWhitespace();
                    if (TryRead(']'))
                        return node;
                    Expect(',');
                }
            }
            catch
            {
                StructuredNode.Release(ref node);
                throw;
            }
        }

        private string ReadString()
        {
            SkipWhitespace();
            if (IsEnd || Read() != '"')
                throw Error("Expected a JSON string.");

            int start = _position;
            while (!IsEnd)
            {
                char current = Peek();
                if (current == '"')
                {
                    int length = _position - start;
                    EnsureScalarLength(length);
                    _position++;
                    return length == 0 ? string.Empty : _json.Substring(start, length);
                }
                if (current < 0x20)
                    throw Error("JSON strings cannot contain unescaped control characters.");
                if (current == '\\') break;
                _position++;
            }
            if (IsEnd)
                throw Error("Unterminated JSON string.");

            var builder = ClassPool.Get<StringBuilder>();
            builder.Clear();
            try
            {
                builder.Append(_json, start, _position - start);
                while (!IsEnd)
                {
                    char c = Read();
                    if (c == '"')
                        return builder.ToString();
                    if (c < 0x20)
                        throw Error("JSON strings cannot contain unescaped control characters.");
                    if (c != '\\')
                    {
                        builder.Append(c);
                        EnsureScalarLength(builder.Length);
                        continue;
                    }

                    if (IsEnd)
                        throw Error("Incomplete JSON escape sequence.");
                    c = Read();
                    switch (c)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u': builder.Append(ReadUnicodeEscape()); break;
                        default: throw Error($"Unsupported JSON escape sequence '\\{c}'.");
                    }
                    EnsureScalarLength(builder.Length);
                }
                throw Error("Unterminated JSON string.");
            }
            finally
            {
                builder.Clear();
                ClassPool.Back(builder);
            }
        }

        private char ReadUnicodeEscape()
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                if (IsEnd)
                    throw Error("Incomplete JSON Unicode escape sequence.");
                char c = Read();
                int digit = c >= '0' && c <= '9' ? c - '0'
                    : c >= 'a' && c <= 'f' ? c - 'a' + 10
                    : c >= 'A' && c <= 'F' ? c - 'A' + 10
                    : -1;
                if (digit < 0)
                    throw Error("Invalid JSON Unicode escape sequence.");
                value = (value << 4) | digit;
            }
            return (char)value;
        }

        private StructuredNode ReadNumber()
        {
            SkipWhitespace();
            int start = _position;

            if (TryRead('-') && IsEnd)
                throw Error("Incomplete JSON number.");

            if (TryRead('0'))
            {
                if (!IsEnd && IsDigit(Peek()))
                    throw Error("JSON numbers cannot contain leading zeroes.");
            }
            else
            {
                if (IsEnd || Peek() < '1' || Peek() > '9')
                    throw Error("Expected a JSON number.");
                while (!IsEnd && IsDigit(Peek()))
                    Read();
            }

            if (TryRead('.'))
            {
                int fractionStart = _position;
                while (!IsEnd && IsDigit(Peek()))
                    Read();
                if (_position == fractionStart)
                    throw Error("JSON fractions require at least one digit.");
            }

            if (TryRead('e') || TryRead('E'))
            {
                if (!TryRead('+'))
                    TryRead('-');
                int exponentStart = _position;
                while (!IsEnd && IsDigit(Peek()))
                    Read();
                if (_position == exponentStart)
                    throw Error("JSON exponents require at least one digit.");
            }

            if (!IsEnd && !IsValueTerminator(Peek()))
                throw Error("Invalid character after JSON number.");
            int length = _position - start;
            EnsureScalarLength(length);
            return RentScalarSlice(_json, start, length, false);
        }

        private void CountNode()
        {
            if (_nodeCount >= _maxNodeCount)
                throw Error($"JSON node count cannot exceed {_maxNodeCount}.");
            _nodeCount++;
        }

        private void EnsureScalarLength(int length)
        {
            if (length > _maxScalarLength)
                throw Error(
                    $"JSON scalar length cannot exceed {_maxScalarLength} characters.");
        }

        private void ReadLiteral(string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                if (IsEnd || Read() != value[i])
                    throw Error($"Expected JSON literal '{value}'.");
            }
            if (!IsEnd && !IsValueTerminator(Peek()))
                throw Error($"Invalid character after JSON literal '{value}'.");
        }

        private static void RequireMetadataScalar(string name, StructuredNode value)
        {
            if (value.Kind != StructuredNodeKind.Scalar || !value.Quoted)
                throw new FormatException($"JSON metadata '{name}' must be a string.");
        }

        private static void RequireReferenceScalar(string name, StructuredNode value)
        {
            if (value.Kind != StructuredNodeKind.Scalar || value.Quoted)
                throw new FormatException(
                    $"JSON metadata '{name}' must be a non-negative integer.");
        }

        private void Expect(char expected)
        {
            SkipWhitespace();
            if (IsEnd || Read() != expected)
                throw Error($"Expected '{expected}'.");
        }

        private bool TryRead(char value)
        {
            if (!IsEnd && Peek() == value)
            {
                _position++;
                return true;
            }
            return false;
        }

        private void SkipWhitespace()
        {
            while (!IsEnd)
            {
                char c = Peek();
                if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                    break;
                _position++;
            }
        }

        private static bool IsDigit(char value) => value >= '0' && value <= '9';

        private static bool IsValueTerminator(char value)
        {
            return value == ',' || value == ']' || value == '}' ||
                   value == ' ' || value == '\t' || value == '\r' || value == '\n';
        }

        private bool IsEnd => _position >= _json.Length;
        private char Peek() => _json[_position];
        private char Read() => _json[_position++];
        private FormatException Error(string message) =>
            new FormatException($"JSON position {_position}: {message}");
    }
}
