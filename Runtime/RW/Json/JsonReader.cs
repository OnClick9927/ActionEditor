using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
namespace ActionBuffer
{
    public class JsonReader : IBufferReader
    {
        private string _json;
        private int _pos;

        public void Init(string data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            Clear();
            _json = data;
        }

        public void Clear()
        {
            _json = null;
            _pos = 0;
        }

        private char Peek()
        {
            return _pos < _json.Length ? _json[_pos] : '\0';
        }

        private char Read()
        {
            return _pos < _json.Length ? _json[_pos++] : '\0';
        }

        private void SkipWhitespace()
        {
            while (char.IsWhiteSpace(Peek()))
                Read();
        }

        public void Expect(char expected)
        {
            SkipWhitespace();
            if (Peek() != expected)
                throw new FormatException(string.Format("Expected '{0}' at position {1}", expected, _pos));
            Read();
        }

        private string ReadNumber()
        {
            SkipWhitespace();
            int start = _pos;
            if (Peek() == '-') Read();
            while (char.IsDigit(Peek())) Read();
            if (Peek() == '.') { Read(); while (char.IsDigit(Peek())) Read(); }
            if (Peek() == 'e' || Peek() == 'E')
            {
                Read();
                if (Peek() == '+' || Peek() == '-') Read();
                while (char.IsDigit(Peek())) Read();
            }
            return _json.Substring(start, _pos - start);
        }

        private string ReadString()
        {
            SkipWhitespace();
            Expect('"');
            var sb = ClassPool<StringBuilder>.Get();
            sb.Clear();
            try
            {
                while (true)
                {
                    char c = Read();
                    if (c == '\0') throw new FormatException("Unterminated JSON string.");
                    if (c == '"') break;
                    if (c == '\\')
                    {
                        c = Read();
                        switch (c)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u': sb.Append(ReadUnicodeEscape()); break;
                            default: throw new FormatException(string.Format("Invalid escape sequence \\{0}", c));
                        }
                    }
                    else
                    {
                        if (c < 0x20) throw new FormatException("JSON strings cannot contain unescaped control characters.");
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
            finally
            {
                sb.Clear();
                ClassPool<StringBuilder>.Back(sb);
            }
        }

        private char ReadUnicodeEscape()
        {
            int value = 0;
            for (int i = 0; i < 4; i++)
            {
                char c = Read();
                int digit = c >= '0' && c <= '9' ? c - '0'
                    : c >= 'a' && c <= 'f' ? c - 'a' + 10
                    : c >= 'A' && c <= 'F' ? c - 'A' + 10
                    : -1;
                if (digit < 0) throw new FormatException("Invalid JSON Unicode escape sequence.");
                value = (value << 4) | digit;
            }
            return (char)value;
        }

        private bool TryReadNull()
        {
            SkipWhitespace();
            if (_pos + 4 > _json.Length ||
                _json[_pos] != 'n' || _json[_pos + 1] != 'u' ||
                _json[_pos + 2] != 'l' || _json[_pos + 3] != 'l')
                return false;

            int end = _pos + 4;
            if (end < _json.Length && !char.IsWhiteSpace(_json[end]) &&
                _json[end] != ',' && _json[end] != ']' && _json[end] != '}')
                return false;
            _pos = end;
            return true;
        }

        private void SkipValue()
        {
            SkipWhitespace();
            char c = Peek();
            if (c == '{')
            {
                Read();
                SkipWhitespace();
                if (Peek() == '}')
                {
                    Read();
                    return;
                }
                while (true)
                {
                    ReadString();
                    Expect(':');
                    SkipValue();
                    SkipWhitespace();
                    if (Peek() == '}') { Read(); return; }
                    Expect(',');
                }
            }
            else if (c == '[')
            {
                Read();
                SkipWhitespace();
                if (Peek() == ']')
                {
                    Read();
                    return;
                }
                while (true)
                {
                    SkipValue();
                    SkipWhitespace();
                    if (Peek() == ']') { Read(); return; }
                    Expect(',');
                }
            }
            else if (c == '"')
            {
                ReadString();
            }
            else
            {
                int start = _pos;
                while (!char.IsWhiteSpace(Peek()) && Peek() != ',' && Peek() != ']' && Peek() != '}')
                    Read();
                if (_pos == start) throw new FormatException($"Expected a JSON value at position {_pos}.");
            }
        }

        public T ReadObject<T>()
        {
            if (TryReadNull()) return default;
            Expect('{');
            SkipWhitespace();

            // 空对象
            if (Peek() == '}')
            {
                Read();
                T empty = (T)TypeHelper.CreateInstance(typeof(T));
                IBufferObject bufferObj = empty as IBufferObject;
                if (bufferObj != null) bufferObj.AfterReadBuffer();
                return empty;
            }

            // 保存快照，用于回退（无类型信息时）
            int snapshot = _pos;
            string firstKey = ReadString();
            Expect(':');

            Type actualType = typeof(T);
            object instance = null;

            // 情况1：第一个字段是 $type，则读取类型信息
            if (firstKey == "$type")
            {
                string typeFullName = ReadString();
                string assemblyName = null;

                // 读取 $assembly
                SkipWhitespace();
                if (Peek() == ',')
                {
                    Read();
                    SkipWhitespace();
                    string nextKey = ReadString();
                    if (nextKey == "$assembly")
                    {
                        Expect(':');
                        assemblyName = ReadString();
                        // 跳过 $assembly 后面的逗号（如果存在）
                        SkipWhitespace();
                        if (Peek() == ',')
                            Read();
                    }
                    else
                    {
                        throw new FormatException("Expected $assembly after $type");
                    }
                }

                actualType = TypeHelper.GetTypeByFullName(typeFullName, assemblyName);
                if (actualType == null)
                    throw new Exception(string.Format("Cannot resolve type: {0}, {1}", typeFullName, assemblyName));

                instance = TypeHelper.CreateInstance(actualType);
            }
            else
            {
                // 情况2：没有 $type，回退到快照并使用泛型类型 T
                _pos = snapshot;
                instance = TypeHelper.CreateInstance(actualType);
            }

            var typeFields = TypeHelper.GetTypeFields(actualType);
            bool firstField = true;

            // 循环读取剩余字段（普通字段）
            while (true)
            {
                SkipWhitespace();
                if (Peek() == '}')
                {
                    Read();
                    break;
                }
                if (!firstField) Expect(',');
                firstField = false;

                string fieldName = ReadString();
                Expect(':');
                var field = typeFields.FindField(fieldName);
                if (field == null)
                {
                    SkipValue();
                }
                else
                {
                    var converter = BuffConverter.GetConverter(field.FieldType);
                    object value = converter.Read(this, field.FieldType);
                    field.SetValue(instance, value);
                }
            }

            IBufferObject bufferObjFinal = instance as IBufferObject;
            if (bufferObjFinal != null)
                bufferObjFinal.AfterReadBuffer();
            return (T)instance;
        }
        public T ReadObject<T>(object instance, TypeHelper.TypeFields fields)
        {
            if (TryReadNull()) return default;
            Expect('{');
            SkipWhitespace();

            // 空对象
            if (Peek() == '}')
            {
                Read();
                //T empty = (T)TypeHelper.CreateInstance(typeof(T));
                IBufferObject bufferObj = instance as IBufferObject;
                if (bufferObj != null) bufferObj.AfterReadBuffer();
                return (T)instance;
            }

            // 保存快照，用于回退（无类型信息时）
            int snapshot = _pos;
            string firstKey = ReadString();
            Expect(':');

            Type actualType = typeof(T);
            //object instance = null;

            // 情况1：第一个字段是 $type，则读取类型信息
            if (firstKey == "$type")
            {
                string typeFullName = ReadString();
                string assemblyName = null;

                // 读取 $assembly
                SkipWhitespace();
                if (Peek() == ',')
                {
                    Read();
                    SkipWhitespace();
                    string nextKey = ReadString();
                    if (nextKey == "$assembly")
                    {
                        Expect(':');
                        assemblyName = ReadString();
                        // 跳过 $assembly 后面的逗号（如果存在）
                        SkipWhitespace();
                        if (Peek() == ',')
                            Read();
                    }
                    else
                    {
                        throw new FormatException("Expected $assembly after $type");
                    }
                }

                //actualType = TypeHelper.GetTypeByFullName(typeFullName, assemblyName);
                //if (actualType == null)
                //    throw new Exception(string.Format("Cannot resolve type: {0}, {1}", typeFullName, assemblyName));

                //instance = TypeHelper.CreateInstance(actualType);
            }
            else
            {
                // 情况2：没有 $type，回退到快照并使用泛型类型 T
                _pos = snapshot;
                //instance = TypeHelper.CreateInstance(actualType);
            }

            //var typeFields = TypeHelper.GetTypeFields(actualType);
            bool firstField = true;

            // 循环读取剩余字段（普通字段）
            while (true)
            {
                SkipWhitespace();
                if (Peek() == '}')
                {
                    Read();
                    break;
                }
                if (!firstField) Expect(',');
                firstField = false;

                string fieldName = ReadString();
                Expect(':');
                var field = fields.FindField(fieldName);
                if (field == null)
                {
                    SkipValue();
                }
                else
                {
                    var converter = BuffConverter.GetConverter(field.FieldType);
                    object value = converter.Read(this, field.FieldType);
                    field.SetValue(instance, value);
                }
            }

            IBufferObject bufferObjFinal = instance as IBufferObject;
            if (bufferObjFinal != null)
                bufferObjFinal.AfterReadBuffer();
            return (T)instance;
        }

        public List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read)
        {
            SkipWhitespace();
            if (TryReadNull()) return null;
            Expect('[');
            List<T> list = result;
            bool first = true;
            while (true)
            {
                SkipWhitespace();
                if (Peek() == ']')
                {
                    Read();
                    break;
                }
                if (!first) Expect(',');
                first = false;
                list.Add(read(this));
            }
            return list;
        }
        public bool ReadBool()
        {
            SkipWhitespace();
            if (Peek() == 't')
            {
                Expect('t'); Expect('r'); Expect('u'); Expect('e');
                return true;
            }
            if (Peek() == 'f')
            {
                Expect('f'); Expect('a'); Expect('l'); Expect('s'); Expect('e');
                return false;
            }
            throw new FormatException("Expected boolean");
        }

        public byte ReadByte() { return (byte)ReadInt64(); }
        public char ReadChar() { return ReadUTF8()[0]; }
        public double ReadDouble() { return double.Parse(ReadNumber(), CultureInfo.InvariantCulture); }
        public float ReadFloat() { return float.Parse(ReadNumber(), CultureInfo.InvariantCulture); }
        public short ReadInt16() { return (short)ReadInt64(); }
        public int ReadInt32() { return (int)ReadInt64(); }
        public long ReadInt64() { return long.Parse(ReadNumber(), CultureInfo.InvariantCulture); }
        public ushort ReadUInt16() { return (ushort)ReadInt64(); }
        public uint ReadUInt32() { return (uint)ReadInt64(); }
        public ulong ReadUInt64() { return (ulong)ReadInt64(); }
        public string ReadUTF8() { return TryReadNull() ? null : ReadString(); }

        public Enum ReadEnum(Type type)
        {
            string value = ReadString();
            return (Enum)Enum.Parse(type, value);
        }


    }
}
