using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    public static class BuffSerializer
    {
        private static readonly Dictionary<Type, string> TypeAliases =
            new Dictionary<Type, string>
            {
                { typeof(byte), "a" },
                { typeof(bool), "b" },
                { typeof(char), "c" },
                { typeof(short), "d" },
                { typeof(ushort), "e" },
                { typeof(int), "f" },
                { typeof(uint), "g" },
                { typeof(long), "h" },
                { typeof(ulong), "i" },
                { typeof(float), "j" },
                { typeof(double), "k" },
                { typeof(string), "l" },
                { typeof(DateTime), "m" },
                { typeof(TimeSpan), "n" }
            };
        private static readonly Dictionary<string, string> TypesByAlias =
            CreateTypesByAlias();

        public static void WriteObject(IBufferWriter writer, object obj,
            BuffSettings settings = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            settings ??= BuffSettings.DefaultSetting;
            settings.BeginOperation();
            try
            {
                var converter = ConverterResolver.Get(obj.GetType(), settings);
                var scan = BufferScan.Rent(settings, writer.CollectMeta, settings.FullField);
                try
                {
                    converter.Scan(scan, obj);
                    scan.ResetRead();
                    writer.Init(scan);
                    converter.Write(writer, scan, obj);
                    scan.EnsureConverterValuesConsumed();
                }
                finally
                {
                    BufferScan.Back(scan);
                }
            }
            finally
            {
                settings.EndOperation();
            }
        }

        public static object ReadObject(IBufferReader reader, Type type,
            BuffSettings settings = null)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (type == null) throw new ArgumentNullException(nameof(type));
            settings ??= (reader as IBuffSerializerContext)?.Settings ??
                BuffSettings.DefaultSetting;
            settings.BeginOperation();
            try
            {
                return ReadObjectCore(reader, type, settings);
            }
            finally
            {
                settings.EndOperation();
            }
        }

        private static object ReadObjectCore(IBufferReader reader, Type type,
            BuffSettings settings)
        {
            var result = ConverterResolver.Get(type, settings).Read(reader, type);
            if (reader is IReferenceResolver referenceResolver)
                referenceResolver.EnsureReferencesResolved();
            return result;
        }

        public static string ToJson(object obj, BuffSettings settings = null)
        {
            var writer = ClassPool.Get<JsonWriter>();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetJson();
            }
            finally
            {
                writer.Clear();
                ClassPool.Back(writer);
            }
        }

        public static object FromJson(string data, Type type,
            BuffSettings settings = null)
        {
            var reader = ClassPool.Get<JsonReader>();
            try
            {
                reader.Init(data, settings);
                return ReadObject(reader, type, settings);
            }
            finally
            {
                reader.Clear();
                ClassPool.Back(reader);
            }
        }

        public static T FromJson<T>(string data, BuffSettings settings = null) =>
            (T)FromJson(data, typeof(T), settings);

        public static string ToYaml(object obj, BuffSettings settings = null)
        {
            var writer = ClassPool.Get<YamlWriter>();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetYaml();
            }
            finally
            {
                writer.Clear();
                ClassPool.Back(writer);
            }
        }

        public static object FromYaml(string data, Type type,
            BuffSettings settings = null)
        {
            var reader = ClassPool.Get<YamlReader>();
            try
            {
                reader.Init(data, settings);
                return ReadObject(reader, type, settings);
            }
            finally
            {
                reader.Clear();
                ClassPool.Back(reader);
            }
        }

        public static T FromYaml<T>(string data, BuffSettings settings = null) =>
            (T)FromYaml(data, typeof(T), settings);

        public static string ToXml(object obj, BuffSettings settings = null)
        {
            var writer = ClassPool.Get<XmlWriter>();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetXml();
            }
            finally
            {
                writer.Clear();
                ClassPool.Back(writer);
            }
        }

        public static object FromXml(string data, Type type,
            BuffSettings settings = null)
        {
            var reader = ClassPool.Get<XmlReader>();
            try
            {
                reader.Init(data, settings);
                return ReadObject(reader, type, settings);
            }
            finally
            {
                reader.Clear();
                ClassPool.Back(reader);
            }
        }

        public static T FromXml<T>(string data, BuffSettings settings = null) =>
            (T)FromXml(data, typeof(T), settings);

        public static byte[] ToBytes(object obj, BuffSettings settings = null)
        {
            var writer = ClassPool.Get<BufferWriter>();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetValidBuffer();
            }
            finally
            {
                writer.Clear();
                ClassPool.Back(writer);
            }
        }

        public static object FromBytes(byte[] bytes, Type type,
            BuffSettings settings = null)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            settings ??= BuffSettings.DefaultSetting;
            var reader = ClassPool.Get<BufferReader>();
            settings.BeginOperation();
            try
            {
                reader.Init(bytes, settings);
                reader.DeferCallbacks();
                var result = ReadObjectCore(reader, type, settings);
                reader.EnsureFullyConsumed();
                reader.CompleteCallbacks();
                return result;
            }
            finally
            {
                reader.Clear();
                ClassPool.Back(reader);
                settings.EndOperation();
            }
        }

        public static T FromBytes<T>(byte[] bytes, BuffSettings settings = null) =>
            (T)FromBytes(bytes, typeof(T), settings);

        public static T DeepCopyByBuffer<T>(this T value) =>
            FromBytes<T>(ToBytes(value));

        internal static Type ResolveSerializedType(Type declaredType, string typeFullName,
            string assemblyName, BuffSettings settings)
        {
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
            if (string.IsNullOrEmpty(typeFullName)) return declaredType;

            var actualType = TypeHelper.GetTypeByFullName(typeFullName, assemblyName);
            if (actualType == null)
                throw new FormatException($"Cannot resolve type '{typeFullName}, {assemblyName}'.");
            if (!declaredType.IsAssignableFrom(actualType))
                throw new FormatException($"Type '{actualType}' is not assignable to '{declaredType}'.");
            settings ??= BuffSettings.DefaultSetting;
            if (!settings.IsTypeAllowed(declaredType, actualType))
                throw new FormatException(
                    $"Type '{actualType}' is not registered in the serializer settings.");
            if (actualType.IsAbstract || actualType.IsInterface || actualType.ContainsGenericParameters)
                throw new FormatException($"Type '{actualType}' is not a concrete serializable type.");
            return actualType;
        }

        internal static string GetSerializedTypeName(Type type)
        {
            if (TypeAliases.TryGetValue(type, out var alias)) return alias;
            return type.FullName;
        }

        internal static string GetSerializedTypeName(string alias)
        {
            if (TypesByAlias.TryGetValue(alias, out var typeName)) return typeName;
            return alias;
        }

        private static Dictionary<string, string> CreateTypesByAlias()
        {
            var result = new Dictionary<string, string>();
            foreach (var item in TypeAliases)
                result.Add(item.Value, item.Key.FullName);
            return result;
        }

    }
}
