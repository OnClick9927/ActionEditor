using System;
using System.Collections.Generic;
using System.Globalization;

namespace ActionBuffer
{
    public sealed class YamlReader : StructuredTextReader
    {
        public void Init(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Clear();
            var root = YamlParsing.Parse(data);
            SetRoot(root);
        }
    }

    internal static class YamlParsing
    {
        private struct Line
        {
            public int Number;
            public int Indent;
            public string Content;
        }

        private sealed class Parser
        {
            private readonly List<Line> _lines;
            private int _index;

            public Parser(string yaml)
            {
                _lines = ReadLines(yaml);
            }

            public StructuredNode Parse()
            {
                if (_lines.Count == 0)
                    throw new FormatException("YAML input is empty.");
                if (_lines[0].Indent != 0)
                    throw Error(_lines[0], "The root value must start at indentation 0.");

                var root = ParseBlock(0);
                if (_index != _lines.Count)
                {
                    StructuredNode.Release(root);
                    throw Error(_lines[_index], "Unexpected trailing content.");
                }
                return root;
            }

            private StructuredNode ParseBlock(int indent)
            {
                var line = _lines[_index];
                if (line.Indent != indent)
                    throw Error(line, $"Expected indentation {indent}, but found {line.Indent}.");

                if (IsSequenceLine(line.Content))
                    return ParseSequence(indent);
                if (FindMappingColon(line.Content) >= 0)
                    return ParseObject(indent);

                _index++;
                return ParseInline(line.Content, line);
            }

            private StructuredNode ParseObject(int indent)
            {
                var node = StructuredNode.Rent(StructuredNodeKind.Object);
                try
                {
                    while (_index < _lines.Count)
                    {
                        var line = _lines[_index];
                        if (line.Indent < indent) break;
                        if (line.Indent > indent)
                            throw Error(line, "Unexpected indentation in mapping.");
                        if (IsSequenceLine(line.Content)) break;

                        int colon = FindMappingColon(line.Content);
                        if (colon < 0) break;
                        string key = ParseKey(line.Content.Substring(0, colon).Trim(), line);
                        string remainder = line.Content.Substring(colon + 1).Trim();
                        _index++;

                        StructuredNode value;
                        if (remainder.Length > 0)
                        {
                            value = ParseInline(remainder, line);
                        }
                        else
                        {
                            if (_index >= _lines.Count || _lines[_index].Indent <= indent)
                                throw Error(line, $"Mapping key '{key}' has no value.");
                            value = ParseBlock(_lines[_index].Indent);
                        }

                        if (key == "$type" || key == "$assembly")
                        {
                            try
                            {
                                if (value.Kind != StructuredNodeKind.Scalar)
                                    throw Error(line, $"Metadata '{key}' must be a scalar.");
                                if (key == "$type")
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
                                StructuredNode.Release(value);
                            }
                        }
                        else
                        {
                            try
                            {
                                EnsureUniqueField(node, key, line);
                                node.AddField(key, value);
                            }
                            catch
                            {
                                StructuredNode.Release(value);
                                throw;
                            }
                        }
                    }
                    return node;
                }
                catch
                {
                    StructuredNode.Release(node);
                    throw;
                }
            }

            private StructuredNode ParseSequence(int indent)
            {
                var node = StructuredNode.Rent(StructuredNodeKind.Sequence);
                try
                {
                    while (_index < _lines.Count)
                    {
                        var line = _lines[_index];
                        if (line.Indent < indent) break;
                        if (line.Indent > indent)
                            throw Error(line, "Unexpected indentation in sequence.");
                        if (!IsSequenceLine(line.Content)) break;

                        string remainder = line.Content.Length == 1
                            ? string.Empty
                            : line.Content.Substring(2).Trim();
                        _index++;

                        if (remainder.Length > 0)
                        {
                            if (FindMappingColon(remainder) >= 0)
                                throw Error(line, "Inline mapping sequence items are not supported; place the mapping on the next indented line.");
                            node.AddItem(ParseInline(remainder, line));
                        }
                        else
                        {
                            if (_index >= _lines.Count || _lines[_index].Indent <= indent)
                                throw Error(line, "Sequence item has no value.");
                            node.AddItem(ParseBlock(_lines[_index].Indent));
                        }
                    }
                    return node;
                }
                catch
                {
                    StructuredNode.Release(node);
                    throw;
                }
            }

            private static StructuredNode ParseInline(string text, Line line)
            {
                text = text.Trim();
                if (text == "null" || text == "Null" || text == "NULL" || text == "~")
                    return StructuredNode.Rent(StructuredNodeKind.Null);
                if (text == "{}")
                    return StructuredNode.Rent(StructuredNodeKind.Object);
                if (text == "[]")
                    return StructuredNode.Rent(StructuredNodeKind.Sequence);
                if (text.Length == 0)
                    throw Error(line, "Expected a value.");

                if (text[0] == '"')
                    return StructuredNode.RentScalar(ParseDoubleQuoted(text, line), true);
                if (text[0] == '\'')
                    return StructuredNode.RentScalar(ParseSingleQuoted(text, line), true);
                if (text[0] == '&' || text[0] == '*' || text[0] == '!' || text[0] == '|' || text[0] == '>')
                    throw Error(line, "YAML anchors, aliases, tags, and multiline scalars are not supported.");
                return StructuredNode.RentScalar(text, false);
            }

            private static string ParseKey(string text, Line line)
            {
                if (text.Length == 0) throw Error(line, "Mapping key cannot be empty.");
                if (text[0] == '"') return ParseDoubleQuoted(text, line);
                if (text[0] == '\'') return ParseSingleQuoted(text, line);
                return text;
            }

            private static string ParseDoubleQuoted(string text, Line line)
            {
                var chars = ClassPool<List<char>>.Get();
                chars.Clear();
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
                            chars.Add(c);
                            continue;
                        }
                        if (index >= text.Length) throw Error(line, "Incomplete escape sequence.");
                        c = text[index++];
                        switch (c)
                        {
                            case '"': chars.Add('"'); break;
                            case '\\': chars.Add('\\'); break;
                            case '/': chars.Add('/'); break;
                            case '0': chars.Add('\0'); break;
                            case 'a': chars.Add('\a'); break;
                            case 'b': chars.Add('\b'); break;
                            case 't': chars.Add('\t'); break;
                            case 'n': chars.Add('\n'); break;
                            case 'v': chars.Add('\v'); break;
                            case 'f': chars.Add('\f'); break;
                            case 'r': chars.Add('\r'); break;
                            case 'u':
                                if (index + 4 > text.Length) throw Error(line, "Incomplete Unicode escape sequence.");
                                string hex = text.Substring(index, 4);
                                if (!ushort.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code))
                                    throw Error(line, $"Invalid Unicode escape '\\u{hex}'.");
                                chars.Add((char)code);
                                index += 4;
                                break;
                            default:
                                throw Error(line, $"Unsupported escape sequence '\\{c}'.");
                        }
                    }
                    if (!closed || text.Substring(index).Trim().Length != 0)
                        throw Error(line, "Invalid double-quoted scalar.");
                    return new string(chars.ToArray());
                }
                finally
                {
                    chars.Clear();
                    ClassPool<List<char>>.Back(chars);
                }
            }

            private static string ParseSingleQuoted(string text, Line line)
            {
                if (text.Length < 2 || text[text.Length - 1] != '\'')
                    throw Error(line, "Invalid single-quoted scalar.");
                return text.Substring(1, text.Length - 2).Replace("''", "'");
            }

            private static void EnsureUniqueField(StructuredNode node, string name, Line line)
            {
                for (int i = 0; i < node.Fields.Count; i++)
                    if (node.Fields[i].Name == name)
                        throw Error(line, $"Duplicate mapping key '{name}'.");
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

            private static List<Line> ReadLines(string yaml)
            {
                var result = new List<Line>();
                string[] source = yaml.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
                for (int i = 0; i < source.Length; i++)
                {
                    string raw = StripComment(source[i]).TrimEnd();
                    if (string.IsNullOrWhiteSpace(raw)) continue;

                    int indent = 0;
                    while (indent < raw.Length && raw[indent] == ' ') indent++;
                    if (indent < raw.Length && raw[indent] == '\t')
                        throw new FormatException($"YAML line {i + 1}: tabs cannot be used for indentation.");

                    string content = raw.Substring(indent);
                    if (indent == 0 && (content == "---" || content == "...")) continue;
                    result.Add(new Line { Number = i + 1, Indent = indent, Content = content });
                }
                return result;
            }

            private static string StripComment(string value)
            {
                bool single = false;
                bool doubleQuoted = false;
                bool escaped = false;
                for (int i = 0; i < value.Length; i++)
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
                             (i == 0 || char.IsWhiteSpace(value[i - 1])))
                        return value.Substring(0, i);
                }
                return value;
            }

            private static FormatException Error(Line line, string message)
            {
                return new FormatException($"YAML line {line.Number}: {message}");
            }
        }

        public static StructuredNode Parse(string yaml)
        {
            return new Parser(yaml).Parse();
        }
    }
}
