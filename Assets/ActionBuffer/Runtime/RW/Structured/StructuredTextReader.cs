using System;
using System.Collections.Generic;
using System.Globalization;

namespace ActionBuffer
{
    public abstract class StructuredTextReader : IBufferReader
    {
        private StructuredNode _root;
        private StructuredNode _current;

        internal void SetRoot(StructuredNode root)
        {
            Clear();
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _current = root;
        }

        public virtual void Clear()
        {
            var root = _root;
            _root = null;
            _current = null;
            StructuredNode.Release(root);
        }

        private StructuredNode RequireCurrent()
        {
            if (_current == null)
                throw new InvalidOperationException("The reader has not been initialized.");
            return _current;
        }

        private static void RequireKind(StructuredNode node, StructuredNodeKind kind)
        {
            if (node.Kind != kind)
                throw new FormatException($"Expected {kind}, but found {node.Kind}.");
        }

        private Type ResolveType(Type declaredType, StructuredNode node)
        {
            if (string.IsNullOrEmpty(node.TypeName)) return declaredType;

            var actualType = TypeHelper.GetTypeByFullName(node.TypeName, node.AssemblyName);
            if (actualType == null)
                throw new FormatException($"Cannot resolve type '{node.TypeName}'.");
            if (!declaredType.IsAssignableFrom(actualType))
                throw new FormatException($"Type '{actualType}' is not assignable to '{declaredType}'.");
            return actualType;
        }

        public T ReadObject<T>()
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return default;
            RequireKind(node, StructuredNodeKind.Object);

            var actualType = ResolveType(typeof(T), node);
            var instance = TypeHelper.CreateInstance(actualType);
            ReadFields(node, instance, TypeHelper.GetTypeFields(actualType));
            InvokeAfterRead(instance);
            return (T)instance;
        }

        public T ReadObject<T>(object instance, TypeHelper.TypeFields fields)
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return default;
            RequireKind(node, StructuredNodeKind.Object);
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (fields == null) throw new ArgumentNullException(nameof(fields));

            ReadFields(node, instance, fields);
            InvokeAfterRead(instance);
            return (T)instance;
        }

        private void ReadFields(StructuredNode node, object instance, TypeHelper.TypeFields fields)
        {
            for (int i = 0; i < node.Fields.Count; i++)
            {
                var serializedField = node.Fields[i];
                var field = fields.FindField(serializedField.Name);
                if (field == null) continue;

                var previous = _current;
                _current = serializedField.Value;
                try
                {
                    var converter = BuffConverter.GetConverter(field.FieldType);
                    field.SetValue(instance, converter.Read(this, field.FieldType));
                }
                finally
                {
                    _current = previous;
                }
            }
        }

        private static void InvokeAfterRead(object instance)
        {
            if (instance is IBufferObject bufferObject)
                bufferObject.AfterReadBuffer();
        }

        public List<T> ReadIEnumerable<T>(List<T> result, Func<IBufferReader, T> read)
        {
            var node = RequireCurrent();
            if (node.Kind == StructuredNodeKind.Null) return null;
            RequireKind(node, StructuredNodeKind.Sequence);

            for (int i = 0; i < node.Items.Count; i++)
            {
                var previous = _current;
                _current = node.Items[i];
                try
                {
                    result.Add(read(this));
                }
                finally
                {
                    _current = previous;
                }
            }
            return result;
        }

        private string ReadScalar(bool allowNull = false)
        {
            var node = RequireCurrent();
            if (allowNull && node.Kind == StructuredNodeKind.Null) return null;
            RequireKind(node, StructuredNodeKind.Scalar);
            return node.Scalar;
        }

        public bool ReadBool() => bool.Parse(ReadScalar());
        public byte ReadByte() => byte.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);

        public char ReadChar()
        {
            var value = ReadScalar();
            if (value.Length != 1) throw new FormatException("Expected a single character.");
            return value[0];
        }

        public double ReadDouble() => double.Parse(ReadScalar(), NumberStyles.Float, CultureInfo.InvariantCulture);
        public float ReadFloat() => float.Parse(ReadScalar(), NumberStyles.Float, CultureInfo.InvariantCulture);
        public short ReadInt16() => short.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public int ReadInt32() => int.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public long ReadInt64() => long.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public ushort ReadUInt16() => ushort.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public uint ReadUInt32() => uint.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public ulong ReadUInt64() => ulong.Parse(ReadScalar(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        public string ReadUTF8() => ReadScalar(true);
        public Enum ReadEnum(Type type) => (Enum)Enum.Parse(type, ReadScalar());
    }
}
