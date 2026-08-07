using System;
using System.Collections.Generic;
using System.Text;

namespace ActionBuffer
{
    public sealed class YamlReader : StructuredTextReader
    {
        private struct Line
        {
            public int Number;
            public int Indent;
            public int Start;
            public int Length;
        }

        private readonly List<Line> _lines = new List<Line>();
        private string _yaml;
        private int _lineIndex;
        private int _nodeCount;
        private int _maxDepth;
        private int _maxNodeCount;
        private int _maxCollectionCount;
        private int _maxObjectFieldCount;
        private int _maxScalarLength;

        public static YamlReader Get()
        {
            var result = ClassPool.Get<YamlReader>();
            result.Clear();
            return result;
        }

        public static void Back(YamlReader value)
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
                    $"YAML length cannot exceed {maxTextLength} characters.");
            _maxDepth = BuffSettings.MaxDepth;
            _maxNodeCount = BuffSettings.MaxNodeCount;
            _maxCollectionCount = BuffSettings.MaxCollectionCount;
            _maxObjectFieldCount = BuffSettings.MaxObjectFieldCount;
            _maxScalarLength = BuffSettings.MaxScalarLength;
            _yaml = data;

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
            if (_lines.Capacity > BuffSettings.RetainedListCapacity)
                _lines.Capacity = 0;
            _lineIndex = 0;
            _nodeCount = 0;
            _yaml = null;
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
            int maxDepth = _maxDepth;
            if (depth >= maxDepth)
                throw Error(_lines[_lineIndex], $"YAML depth cannot exceed {maxDepth}.");
            var line = _lines[_lineIndex];
            if (line.Indent != indent)
                throw Error(line, $"Expected indentation {indent}, but found {line.Indent}.");

            if (IsSequenceLine(line))
                return ParseSequence(indent, depth);
            if (FindMappingColon(line) >= 0)
                return ParseObject(indent, depth);

            _lineIndex++;
            return ParseInline(line.Start, line.Length, line);
        }

        private StructuredNode ParseObject(int indent, int depth)
        {
            CountNode(_lines[_lineIndex]);
            var node = RentNode(StructuredNodeKind.Object);
            var fieldNames = ClassPool.GetHashSet<string>();
            StructuredNode collectionValues = default;
            bool hasCollectionValues = false;
            int memberCount = 0;
            try
            {
                while (_lineIndex < _lines.Count)
                {
                    var line = _lines[_lineIndex];
                    if (line.Indent < indent) break;
                    if (line.Indent > indent)
                        throw Error(line, "Unexpected indentation in mapping.");
                    if (IsSequenceLine(line)) break;

                    int colon = FindMappingColon(line);
                    if (colon < 0) break;
                    if (++memberCount > _maxObjectFieldCount)
                        throw Error(line,
                            $"YAML object field count cannot exceed {_maxObjectFieldCount}.");
                    int keyStart = line.Start;
                    int keyLength = colon - keyStart;
                    TrimRange(_yaml, ref keyStart, ref keyLength);
                    string key = ParseKey(keyStart, keyLength, line);
                    int remainderStart = colon + 1;
                    int remainderLength = line.Start + line.Length - remainderStart;
                    TrimRange(_yaml, ref remainderStart, ref remainderLength);
                    _lineIndex++;

                    StructuredNode value;
                    if (remainderLength > 0)
                    {
                        value = ParseInline(remainderStart, remainderLength, line);
                    }
                    else
                    {
                        if (_lineIndex >= _lines.Count || _lines[_lineIndex].Indent <= indent)
                            throw Error(line, $"Mapping key '{key}' has no value.");
                        value = ParseBlock(_lines[_lineIndex].Indent, depth + 1);
                    }

                    if (key == "$type" || key == "$assembly" || key == "$id" || key == "$ref" ||
                        key == "$values")
                    {
                        try
                        {
                            if (key != "$values" && value.Kind != StructuredNodeKind.Scalar)
                                throw Error(line, $"Metadata '{key}' must be a scalar.");
                            if (key == "$values")
                            {
                                if (hasCollectionValues || value.Kind != StructuredNodeKind.Sequence)
                                    throw Error(line,
                                        "Metadata '$values' must contain one sequence.");
                                collectionValues = value;
                                value = default;
                                hasCollectionValues = true;
                            }
                            else if (key == "$id" || key == "$ref")
                            {
                                if (node.ReferenceId >= 0)
                                    throw Error(line, "Duplicate object reference metadata.");
                                node.ReferenceId = ParseReferenceId(key, value, line);
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
                if (hasCollectionValues)
                {
                    if (node.IsReference || node.ReferenceId < 0 || node.TypeName != null ||
                        node.AssemblyName != null || node.FieldCount != 0)
                        throw Error(_lines[Math.Max(0, _lineIndex - 1)],
                            "A collection wrapper must contain only '$id' and '$values'.");
                    collectionValues.ReferenceId = node.ReferenceId;
                    node = collectionValues;
                    collectionValues = default;
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
                StructuredNode.Release(ref collectionValues);
                ClassPool.BackHashSet(fieldNames);
            }
        }

        private static int ParseReferenceId(string name, StructuredNode value, Line line)
        {
            ulong parsed;
            try
            {
                parsed = ParseUnsignedScalar(value);
            }
            catch (Exception exception) when (exception is FormatException ||
                                               exception is OverflowException)
            {
                throw Error(line, $"Metadata '{name}' must be a non-negative integer.");
            }
            if (parsed > int.MaxValue)
                throw Error(line, $"Metadata '{name}' must be a non-negative integer.");
            return (int)parsed;
        }

        private StructuredNode ParseSequence(int indent, int depth)
        {
            CountNode(_lines[_lineIndex]);
            var node = RentNode(StructuredNodeKind.Sequence);
            try
            {
                while (_lineIndex < _lines.Count)
                {
                    var line = _lines[_lineIndex];
                    if (line.Indent < indent) break;
                    if (line.Indent > indent)
                        throw Error(line, "Unexpected indentation in sequence.");
                    if (!IsSequenceLine(line)) break;
                    if (node.ItemCount >= _maxCollectionCount)
                        throw Error(line,
                            $"YAML sequence count cannot exceed {_maxCollectionCount}.");

                    int remainderStart = line.Start + 1;
                    int remainderLength = line.Length - 1;
                    TrimRange(_yaml, ref remainderStart, ref remainderLength);
                    _lineIndex++;

                    StructuredNode value;
                    if (remainderLength > 0)
                    {
                        if (FindMappingColon(_yaml, remainderStart, remainderLength) >= 0)
                            throw Error(line, "Inline mapping sequence items are not supported; place the mapping on the next indented line.");
                        value = ParseInline(remainderStart, remainderLength, line);
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

        private StructuredNode ParseInline(int start, int length, Line line)
        {
            CountNode(line);
            TrimRange(_yaml, ref start, ref length);
            if (length == 0)
                throw Error(line, "Expected a value.");
            if (EqualsRange(_yaml, start, length, "null") ||
                EqualsRange(_yaml, start, length, "Null") ||
                EqualsRange(_yaml, start, length, "NULL") ||
                EqualsRange(_yaml, start, length, "~"))
                return RentNode(StructuredNodeKind.Null);
            if (EqualsRange(_yaml, start, length, "{}"))
                return RentNode(StructuredNodeKind.Object);
            if (EqualsRange(_yaml, start, length, "[]"))
                return RentNode(StructuredNodeKind.Sequence);

            EnsureScalarLength(length, line);
            char first = _yaml[start];
            if (first == '"')
                return RentScalar(ParseDoubleQuoted(start, length, line), true);
            if (first == '\'')
                return RentScalar(ParseSingleQuoted(start, length, line), true);
            if (first == '&' || first == '*' || first == '!' || first == '|' || first == '>')
                throw Error(line, "YAML anchors, aliases, tags, and multiline scalars are not supported.");
            return RentScalarSlice(_yaml, start, length, false);
        }

        private string ParseKey(int start, int length, Line line)
        {
            if (length == 0) throw Error(line, "Mapping key cannot be empty.");
            EnsureScalarLength(length, line);
            if (_yaml[start] == '"') return ParseDoubleQuoted(start, length, line);
            if (_yaml[start] == '\'') return ParseSingleQuoted(start, length, line);
            return _yaml.Substring(start, length);
        }

        private string ParseDoubleQuoted(int start, int length, Line line)
        {
            var builder = ClassPool.Get<StringBuilder>();
            builder.Clear();
            try
            {
                int index = start + 1;
                int end = start + length;
                bool closed = false;
                while (index < end)
                {
                    char c = _yaml[index++];
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
                    if (index >= end) throw Error(line, "Incomplete escape sequence.");
                    c = _yaml[index++];
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
                            if (index + 4 > end) throw Error(line, "Incomplete Unicode escape sequence.");
                            int code = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                char hex = _yaml[index + i];
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
                while (index < end && char.IsWhiteSpace(_yaml[index]))
                    index++;
                if (!closed || index != end)
                    throw Error(line, "Invalid double-quoted scalar.");
                return builder.ToString();
            }
            finally
            {
                builder.Clear();
                ClassPool.Back(builder);
            }
        }

        private string ParseSingleQuoted(int start, int length, Line line)
        {
            int end = start + length;
            if (length < 2 || _yaml[end - 1] != '\'')
                throw Error(line, "Invalid single-quoted scalar.");
            var builder = ClassPool.Get<StringBuilder>();
            builder.Clear();
            try
            {
                for (int i = start + 1; i < end - 1; i++)
                {
                    char value = _yaml[i];
                    if (value == '\'' && i + 1 < end - 1 && _yaml[i + 1] == '\'')
                        i++;
                    builder.Append(value);
                    EnsureScalarLength(builder.Length, line);
                }
                return builder.ToString();
            }
            finally
            {
                builder.Clear();
                ClassPool.Back(builder);
            }
        }

        private bool IsSequenceLine(Line line)
        {
            return line.Length == 1 && _yaml[line.Start] == '-' ||
                   line.Length > 1 && _yaml[line.Start] == '-' &&
                   char.IsWhiteSpace(_yaml[line.Start + 1]);
        }

        private int FindMappingColon(Line line) =>
            FindMappingColon(_yaml, line.Start, line.Length);

        private static int FindMappingColon(string source, int start, int length)
        {
            bool single = false;
            bool doubleQuoted = false;
            bool escaped = false;
            int end = start + length;
            for (int i = start; i < end; i++)
            {
                char c = source[i];
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
                         (i + 1 == end || char.IsWhiteSpace(source[i + 1])))
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

            int indent = contentStart - start;
            int contentLength = end - contentStart;
            if (indent == 0 && (EqualsRange(yaml, contentStart, contentLength, "---") ||
                                EqualsRange(yaml, contentStart, contentLength, "..."))) return;
            if (_lines.Count >= _maxNodeCount)
                throw new FormatException(
                    $"YAML line count cannot exceed {_maxNodeCount}.");
            _lines.Add(new Line
            {
                Number = lineNumber,
                Indent = indent,
                Start = contentStart,
                Length = contentLength
            });
        }

        private void CountNode(Line line)
        {
            if (_nodeCount >= _maxNodeCount)
                throw Error(line, $"YAML node count cannot exceed {_maxNodeCount}.");
            _nodeCount++;
        }

        private void EnsureScalarLength(int length, Line line)
        {
            if (length > _maxScalarLength)
                throw Error(line,
                    $"YAML scalar length cannot exceed {_maxScalarLength} characters.");
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

        private static void TrimRange(string source, ref int start, ref int length)
        {
            int end = start + length;
            while (start < end && char.IsWhiteSpace(source[start])) start++;
            while (end > start && char.IsWhiteSpace(source[end - 1])) end--;
            length = end - start;
        }

        private static bool EqualsRange(string source, int start, int length,
            string expected)
        {
            if (length != expected.Length) return false;
            for (int i = 0; i < length; i++)
                if (source[start + i] != expected[i]) return false;
            return true;
        }

        private static FormatException Error(Line line, string message)
        {
            return new FormatException($"YAML line {line.Number}: {message}");
        }
    }
}
