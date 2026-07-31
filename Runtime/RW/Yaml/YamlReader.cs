using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ActionBuffer
{
    public sealed class YamlReader : StructuredTextReader
    {
        private struct Line
        {
            public int Number;
            public int Indent;
            public string Content;
        }

        private readonly List<Line> _lines = new List<Line>();
        private int _lineIndex;
        private int _nodeCount;

        public void Init(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Clear();
            if (data.Length > BufferSerializer.MaxTextLength)
                throw new FormatException(
                    $"YAML length cannot exceed {BufferSerializer.MaxTextLength} characters.");

            StructuredNode root = default;
            try
            {
                ReadLines(data);
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
            _lines.Clear();
            if (_lines.Capacity > BufferSerializer.RetainedListCapacity)
                _lines.Capacity = 0;
            _lineIndex = 0;
            _nodeCount = 0;
        }

        private StructuredNode ParseDocument()
        {
            if (_lines.Count == 0)
                throw new FormatException("YAML input is empty.");
            if (_lines[0].Indent != 0)
                throw Error(_lines[0], "The root value must start at indentation 0.");

            var root = ParseBlock(0, 0);
            if (_lineIndex != _lines.Count)
            {
                StructuredNode.Release(ref root);
                throw Error(_lines[_lineIndex], "Unexpected trailing content.");
            }
            return root;
        }

        private StructuredNode ParseBlock(int indent, int depth)
        {
            int maxDepth = BufferSerializerSettings.DefaultSetting.MaxDepth;
            if (depth >= maxDepth)
                throw Error(_lines[_lineIndex], $"YAML depth cannot exceed {maxDepth}.");
            var line = _lines[_lineIndex];
            if (line.Indent != indent)
                throw Error(line, $"Expected indentation {indent}, but found {line.Indent}.");

            if (IsSequenceLine(line.Content))
                return ParseSequence(indent, depth);
            if (FindMappingColon(line.Content) >= 0)
                return ParseObject(indent, depth);

            _lineIndex++;
            return ParseInline(line.Content, line);
        }

        private StructuredNode ParseObject(int indent, int depth)
        {
            CountNode(_lines[_lineIndex]);
            var node = StructuredNode.Rent(StructuredNodeKind.Object);
            var fieldNames = HashSetPool<string>.Get();
            int memberCount = 0;
            try
            {
                while (_lineIndex < _lines.Count)
                {
                    var line = _lines[_lineIndex];
                    if (line.Indent < indent) break;
                    if (line.Indent > indent)
                        throw Error(line, "Unexpected indentation in mapping.");
                    if (IsSequenceLine(line.Content)) break;

                    int colon = FindMappingColon(line.Content);
                    if (colon < 0) break;
                    if (++memberCount > BufferSerializer.MaxObjectFieldCount)
                        throw Error(line,
                            $"YAML object field count cannot exceed {BufferSerializer.MaxObjectFieldCount}.");
                    string key = ParseKey(line.Content.Substring(0, colon).Trim(), line);
                    string remainder = line.Content.Substring(colon + 1).Trim();
                    _lineIndex++;

                    StructuredNode value;
                    if (remainder.Length > 0)
                    {
                        value = ParseInline(remainder, line);
                    }
                    else
                    {
                        if (_lineIndex >= _lines.Count || _lines[_lineIndex].Indent <= indent)
                            throw Error(line, $"Mapping key '{key}' has no value.");
                        value = ParseBlock(_lines[_lineIndex].Indent, depth + 1);
                    }

                    if (key == "$type" || key == "$assembly" || key == "$id" || key == "$ref")
                    {
                        try
                        {
                            if (value.Kind != StructuredNodeKind.Scalar)
                                throw Error(line, $"Metadata '{key}' must be a scalar.");
                            if (key == "$id" || key == "$ref")
                            {
                                if (node.ReferenceId >= 0)
                                    throw Error(line, "Duplicate object reference metadata.");
                                node.ReferenceId = ParseReferenceId(key, value.Scalar, line);
                                node.IsReference = key == "$ref";
                            }
                            else if (key == "$type")
                            {
                                if (node.TypeName != null) throw Error(line, "Duplicate metadata '$type'.");
                                node.TypeName = value.Scalar;
                            }
                            else
                            {
                                if (node.AssemblyName != null) throw Error(line, "Duplicate metadata '$assembly'.");
                                node.AssemblyName = value.Scalar;
                            }
                        }
                        finally
                        {
                            StructuredNode.Release(ref value);
                        }
                    }
                    else
                    {
                        try
                        {
                            key = StructuredNode.DecodeTextFieldName(key);
                            if (!fieldNames.Add(key))
                                throw Error(line, $"Duplicate mapping key '{key}'.");
                            node.AddField(key, value);
                            value = default;
                        }
                        catch
                        {
                            StructuredNode.Release(ref value);
                            throw;
                        }
                    }
                }
                if (node.IsReference && (node.FieldCount != 0 || node.TypeName != null ||
                                         node.AssemblyName != null))
                    throw Error(_lines[Math.Max(0, _lineIndex - 1)],
                        "A '$ref' mapping cannot contain fields or type metadata.");
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

        private static int ParseReferenceId(string name, string value, Line line)
        {
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int result) ||
                result < 0)
                throw Error(line, $"Metadata '{name}' must be a non-negative integer.");
            return result;
        }

        private StructuredNode ParseSequence(int indent, int depth)
        {
            CountNode(_lines[_lineIndex]);
            var node = StructuredNode.Rent(StructuredNodeKind.Sequence);
            try
            {
                while (_lineIndex < _lines.Count)
                {
                    var line = _lines[_lineIndex];
                    if (line.Indent < indent) break;
                    if (line.Indent > indent)
                        throw Error(line, "Unexpected indentation in sequence.");
                    if (!IsSequenceLine(line.Content)) break;
                    if (node.ItemCount >= BufferSerializer.MaxCollectionCount)
                        throw Error(line,
                            $"YAML sequence count cannot exceed {BufferSerializer.MaxCollectionCount}.");

                    string remainder = line.Content.Length == 1
                        ? string.Empty
                        : line.Content.Substring(2).Trim();
                    _lineIndex++;

                    StructuredNode value;
                    if (remainder.Length > 0)
                    {
                        if (FindMappingColon(remainder) >= 0)
                            throw Error(line, "Inline mapping sequence items are not supported; place the mapping on the next indented line.");
                        value = ParseInline(remainder, line);
                    }
                    else
                    {
                        if (_lineIndex >= _lines.Count || _lines[_lineIndex].Indent <= indent)
                            throw Error(line, "Sequence item has no value.");
                        value = ParseBlock(_lines[_lineIndex].Indent, depth + 1);
                    }

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
                return node;
            }
            catch
            {
                StructuredNode.Release(ref node);
                throw;
            }
        }

        private StructuredNode ParseInline(string text, Line line)
        {
            CountNode(line);
            text = text.Trim();
            if (text == "null" || text == "Null" || text == "NULL" || text == "~")
                return StructuredNode.Rent(StructuredNodeKind.Null);
            if (text == "{}")
                return StructuredNode.Rent(StructuredNodeKind.Object);
            if (text == "[]")
                return StructuredNode.Rent(StructuredNodeKind.Sequence);
            if (text.Length == 0)
                throw Error(line, "Expected a value.");

            EnsureScalarLength(text.Length, line);

            if (text[0] == '"')
                return StructuredNode.RentScalar(ParseDoubleQuoted(text, line), true);
            if (text[0] == '\'')
                return StructuredNode.RentScalar(ParseSingleQuoted(text, line), true);
            if (text[0] == '&' || text[0] == '*' || text[0] == '!' || text[0] == '|' || text[0] == '>')
                throw Error(line, "YAML anchors, aliases, tags, and multiline scalars are not supported.");
            return StructuredNode.RentScalar(text, false);
        }

        private string ParseKey(string text, Line line)
        {
            if (text.Length == 0) throw Error(line, "Mapping key cannot be empty.");
            EnsureScalarLength(text.Length, line);
            if (text[0] == '"') return ParseDoubleQuoted(text, line);
            if (text[0] == '\'') return ParseSingleQuoted(text, line);
            return text;
        }

        private string ParseDoubleQuoted(string text, Line line)
        {
            var builder = ClassPool<StringBuilder>.Get();
            builder.Clear();
            try
            {
                int index = 1;
                bool closed = false;
                while (index < text.Length)
                {
                    char c = text[index++];
                    if (c == '"')
                    {
                        closed = true;
                        break;
                    }
                    if (c != '\\')
                    {
                        builder.Append(c);
                        EnsureScalarLength(builder.Length, line);
                        continue;
                    }
                    if (index >= text.Length) throw Error(line, "Incomplete escape sequence.");
                    c = text[index++];
                    switch (c)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case '0': builder.Append('\0'); break;
                        case 'a': builder.Append('\a'); break;
                        case 'b': builder.Append('\b'); break;
                        case 't': builder.Append('\t'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'v': builder.Append('\v'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'r': builder.Append('\r'); break;
                        case 'u':
                            if (index + 4 > text.Length) throw Error(line, "Incomplete Unicode escape sequence.");
                            int code = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                char hex = text[index + i];
                                int digit = hex >= '0' && hex <= '9' ? hex - '0'
                                    : hex >= 'a' && hex <= 'f' ? hex - 'a' + 10
                                    : hex >= 'A' && hex <= 'F' ? hex - 'A' + 10
                                    : -1;
                                if (digit < 0)
                                    throw Error(line, "Invalid Unicode escape sequence.");
                                code = (code << 4) | digit;
                            }
                            builder.Append((char)code);
                            index += 4;
                            break;
                        default:
                            throw Error(line, $"Unsupported escape sequence '\\{c}'.");
                    }
                    EnsureScalarLength(builder.Length, line);
                }
                while (index < text.Length && char.IsWhiteSpace(text[index]))
                    index++;
                if (!closed || index != text.Length)
                    throw Error(line, "Invalid double-quoted scalar.");
                return builder.ToString();
            }
            finally
            {
                builder.Clear();
                ClassPool<StringBuilder>.Back(builder);
            }
        }

        private string ParseSingleQuoted(string text, Line line)
        {
            if (text.Length < 2 || text[text.Length - 1] != '\'')
                throw Error(line, "Invalid single-quoted scalar.");
            var result = text.Substring(1, text.Length - 2).Replace("''", "'");
            EnsureScalarLength(result.Length, line);
            return result;
        }

        private static bool IsSequenceLine(string content)
        {
            return content == "-" || (content.Length > 1 && content[0] == '-' && char.IsWhiteSpace(content[1]));
        }

        private static int FindMappingColon(string content)
        {
            bool single = false;
            bool doubleQuoted = false;
            bool escaped = false;
            for (int i = 0; i < content.Length; i++)
            {
                char c = content[i];
                if (doubleQuoted && escaped)
                {
                    escaped = false;
                    continue;
                }
                if (doubleQuoted && c == '\\')
                {
                    escaped = true;
                    continue;
                }
                if (!doubleQuoted && c == '\'') single = !single;
                else if (!single && c == '"') doubleQuoted = !doubleQuoted;
                else if (!single && !doubleQuoted && c == ':' &&
                         (i + 1 == content.Length || char.IsWhiteSpace(content[i + 1])))
                    return i;
            }
            return -1;
        }

        private void ReadLines(string yaml)
        {
            int start = 0;
            int lineNumber = 1;
            while (start <= yaml.Length)
            {
                int end = start;
                while (end < yaml.Length && yaml[end] != '\r' && yaml[end] != '\n')
                    end++;
                AddLine(yaml, start, end, lineNumber);

                if (end == yaml.Length) break;
                if (yaml[end] == '\r' && end + 1 < yaml.Length && yaml[end + 1] == '\n')
                    end++;
                start = end + 1;
                lineNumber++;
            }
        }

        private void AddLine(string yaml, int start, int end, int lineNumber)
        {
            end = FindCommentStart(yaml, start, end);
            while (end > start && char.IsWhiteSpace(yaml[end - 1]))
                end--;
            if (end == start) return;

            int contentStart = start;
            while (contentStart < end && yaml[contentStart] == ' ')
                contentStart++;
            if (contentStart < end && yaml[contentStart] == '\t')
                throw new FormatException($"YAML line {lineNumber}: tabs cannot be used for indentation.");

            string content = yaml.Substring(contentStart, end - contentStart);
            int indent = contentStart - start;
            if (indent == 0 && (content == "---" || content == "...")) return;
            if (_lines.Count >= BufferSerializer.MaxNodeCount)
                throw new FormatException(
                    $"YAML line count cannot exceed {BufferSerializer.MaxNodeCount}.");
            _lines.Add(new Line { Number = lineNumber, Indent = indent, Content = content });
        }

        private void CountNode(Line line)
        {
            if (_nodeCount >= BufferSerializer.MaxNodeCount)
                throw Error(line, $"YAML node count cannot exceed {BufferSerializer.MaxNodeCount}.");
            _nodeCount++;
        }

        private static void EnsureScalarLength(int length, Line line)
        {
            if (length > BufferSerializer.MaxScalarLength)
                throw Error(line,
                    $"YAML scalar length cannot exceed {BufferSerializer.MaxScalarLength} characters.");
        }

        private static int FindCommentStart(string value, int start, int end)
        {
            bool single = false;
            bool doubleQuoted = false;
            bool escaped = false;
            for (int i = start; i < end; i++)
            {
                char c = value[i];
                if (doubleQuoted && escaped)
                {
                    escaped = false;
                    continue;
                }
                if (doubleQuoted && c == '\\')
                {
                    escaped = true;
                    continue;
                }
                if (!doubleQuoted && c == '\'') single = !single;
                else if (!single && c == '"') doubleQuoted = !doubleQuoted;
                else if (!single && !doubleQuoted && c == '#' &&
                         (i == start || char.IsWhiteSpace(value[i - 1])))
                    return i;
            }
            return end;
        }

        private static FormatException Error(Line line, string message)
        {
            return new FormatException($"YAML line {line.Number}: {message}");
        }
    }
}
