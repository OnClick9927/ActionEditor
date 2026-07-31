using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ActionBuffer
{
    public static class BufferSerializer
    {
        private static readonly Dictionary<Type, Type> ConverterTypes = new Dictionary<Type, Type>
        {
            { typeof(bool), typeof(BoolConverter) }, { typeof(byte), typeof(ByteConverter) },
            { typeof(char), typeof(CharConverter) }, { typeof(DateTime), typeof(DateTimeConverter) },
            { typeof(decimal), typeof(DecimalConverter) }, { typeof(double), typeof(DoubleConverter) },
            { typeof(float), typeof(FloatConverter) }, { typeof(Guid), typeof(GuidConverter) },
            { typeof(int), typeof(IntConverter) }, { typeof(long), typeof(LongConverter) },
            { typeof(sbyte), typeof(SByteConverter) }, { typeof(short), typeof(ShortConverter) },
            { typeof(string), typeof(StringConverter) }, { typeof(TimeSpan), typeof(TimeSpanConverter) },
            { typeof(uint), typeof(UIntConverter) }, { typeof(ulong), typeof(ULongConverter) },
            { typeof(ushort), typeof(UShortConverter) }
        };

        private static readonly Dictionary<Type, Type> GenericConverterTypes = new Dictionary<Type, Type>
        {
            { typeof(Nullable<>), typeof(NullableConverter<>) },
            { typeof(List<>), typeof(ListConverter<>) },
            { typeof(IEnumerable<>), typeof(EnumerableInterfaceConverter<>) },
            { typeof(ICollection<>), typeof(CollectionInterfaceConverter<>) },
            { typeof(IList<>), typeof(ListInterfaceConverter<>) },
            { typeof(IReadOnlyCollection<>), typeof(ReadOnlyCollectionInterfaceConverter<>) },
            { typeof(IReadOnlyList<>), typeof(ReadOnlyListInterfaceConverter<>) },
            { typeof(Dictionary<,>), typeof(DictionaryConverter<,>) },
            { typeof(ConcurrentDictionary<,>), typeof(ConcurrentDictionaryConverter<,>) },
            { typeof(IDictionary<,>), typeof(DictionaryInterfaceConverter<,>) },
            { typeof(IReadOnlyDictionary<,>), typeof(ReadOnlyDictionaryInterfaceConverter<,>) },
            { typeof(HashSet<>), typeof(HashSetConverter<>) },
            { typeof(ISet<>), typeof(SetInterfaceConverter<>) },
            { typeof(Queue<>), typeof(QueueConverter<>) },
            { typeof(Stack<>), typeof(StackConverter<>) },
            { typeof(KeyValuePair<,>), typeof(KeyValuePairConverter<,>) },
            { typeof(ArraySegment<>), typeof(ArraySegmentConverter<>) }
        };

        private static readonly Dictionary<Type, BuffConverter> Converters =
            new Dictionary<Type, BuffConverter>();
        internal static int ConverterVersion { get; private set; }

        public static int MaxDepth
        {
            get => BufferSerializerSettings.DefaultSetting.MaxDepth;
            set => BufferSerializerSettings.DefaultSetting.MaxDepth = value;
        }

        public static int MaxTextLength
        {
            get => BufferSerializerSettings.DefaultSetting.MaxTextLength;
            set => BufferSerializerSettings.DefaultSetting.MaxTextLength = value;
        }

        public static int MaxBinaryLength
        {
            get => BufferSerializerSettings.DefaultSetting.MaxBinaryLength;
            set => BufferSerializerSettings.DefaultSetting.MaxBinaryLength = value;
        }

        public static int MaxNodeCount
        {
            get => BufferSerializerSettings.DefaultSetting.MaxNodeCount;
            set => BufferSerializerSettings.DefaultSetting.MaxNodeCount = value;
        }

        public static int MaxCollectionCount
        {
            get => BufferSerializerSettings.DefaultSetting.MaxCollectionCount;
            set => BufferSerializerSettings.DefaultSetting.MaxCollectionCount = value;
        }

        public static int MaxObjectFieldCount
        {
            get => BufferSerializerSettings.DefaultSetting.MaxObjectFieldCount;
            set => BufferSerializerSettings.DefaultSetting.MaxObjectFieldCount = value;
        }

        public static int MaxScalarLength
        {
            get => BufferSerializerSettings.DefaultSetting.MaxScalarLength;
            set => BufferSerializerSettings.DefaultSetting.MaxScalarLength = value;
        }

        internal const int PoolLimit = 16;
        internal const int RetainedListCapacity = 4096;
        internal const int RetainedTextCapacity = 64 * 1024;
        internal const int RetainedBinaryCapacity = 1024 * 1024;

        public static BuffConverter GetConverter(Type type) =>
            GetConverter(type, BufferSerializerSettings.DefaultSetting);

        private static BuffConverter GetRegisteredConverter(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (Converters.TryGetValue(type, out var converter)) return converter;

            converter = CreateConverter(type);
            if (converter == null)
                throw new NotSupportedException($"Unhandled type '{type}'.");
            Converters[type] = converter;
            return converter;
        }

        internal static BuffConverter GetConverter(Type type, BufferSerializerSettings settings)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (settings != null && settings.TryGetConverter(type, out var converter))
                return converter;
            var defaultSetting = BufferSerializerSettings.DefaultSetting;
            if (!ReferenceEquals(settings, defaultSetting) &&
                defaultSetting.TryGetConverter(type, out converter))
                return converter;
            return GetRegisteredConverter(type);
        }

        public static BuffConverter<T> GetConverter<T>()
        {
            var converter = GetConverter(typeof(T));
            if (converter is BuffConverter<T> typedConverter) return typedConverter;
            throw new InvalidOperationException(
                $"Converter '{converter.GetType()}' cannot serialize target type '{typeof(T)}'.");
        }

        internal static BuffConverter<T> GetConverter<T>(BufferSerializerSettings settings)
        {
            var converter = GetConverter(typeof(T), settings);
            if (converter is BuffConverter<T> typedConverter) return typedConverter;
            throw new InvalidOperationException(
                $"Converter '{converter.GetType()}' cannot serialize target type '{typeof(T)}'.");
        }

        internal static long GetResolverVersion(BufferSerializerSettings settings)
        {
            unchecked
            {
                long version = ConverterVersion;
                version = version * 397 ^ BufferSerializerSettings.DefaultSetting.ResolverVersion;
                if (!ReferenceEquals(settings, BufferSerializerSettings.DefaultSetting))
                    version = version * 397 ^ (settings?.ResolverVersion ?? 0);
                return version;
            }
        }

        public static void RegisterConverter<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            Converters[typeof(T)] = converter;
            ConverterVersion++;
        }

        public static void RegisterConverter(Type targetType, Type converterType)
        {
            if (targetType == null) throw new ArgumentNullException(nameof(targetType));
            if (converterType == null) throw new ArgumentNullException(nameof(converterType));
            if (!typeof(BuffConverter).IsAssignableFrom(converterType))
                throw new ArgumentException($"'{converterType}' is not a BuffConverter.", nameof(converterType));

            if (targetType.IsGenericTypeDefinition)
            {
                GenericConverterTypes[targetType] = converterType;
                RemoveCachedGenericConverters(targetType);
                ConverterVersion++;
                return;
            }

            ConverterTypes[targetType] = converterType;
            Converters.Remove(targetType);
            ConverterVersion++;
        }

        // Referencing these closed generic types from player code makes them available to IL2CPP.
        public static void RegisterAot<T>()
        {
            var type = typeof(T);
            bool changed = false;
            if (!Converters.ContainsKey(type))
            {
                if (type.IsEnum)
                    Converters[type] = new EnumConverter<T>();
                else if (UsesObjectConverter(type))
                    Converters[type] = new ObjectConverter<T>();
                else
                    GetConverter(type);
                changed = true;
            }

            if (!Converters.ContainsKey(typeof(T[]))) { Converters[typeof(T[])] = new ArrayConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(T[,]))) { Converters[typeof(T[,])] = new MultiDimensionalArrayConverter<T[,], T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(T[,,]))) { Converters[typeof(T[,,])] = new MultiDimensionalArrayConverter<T[,,], T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(T[,,,]))) { Converters[typeof(T[,,,])] = new MultiDimensionalArrayConverter<T[,,,], T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(T[,,,,]))) { Converters[typeof(T[,,,,])] = new MultiDimensionalArrayConverter<T[,,,,], T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(List<T>))) { Converters[typeof(List<T>)] = new ListConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(IEnumerable<T>))) { Converters[typeof(IEnumerable<T>)] = new EnumerableInterfaceConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(ICollection<T>))) { Converters[typeof(ICollection<T>)] = new CollectionInterfaceConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(IList<T>))) { Converters[typeof(IList<T>)] = new ListInterfaceConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(IReadOnlyCollection<T>))) { Converters[typeof(IReadOnlyCollection<T>)] = new ReadOnlyCollectionInterfaceConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(IReadOnlyList<T>))) { Converters[typeof(IReadOnlyList<T>)] = new ReadOnlyListInterfaceConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(HashSet<T>))) { Converters[typeof(HashSet<T>)] = new HashSetConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(ISet<T>))) { Converters[typeof(ISet<T>)] = new SetInterfaceConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(Queue<T>))) { Converters[typeof(Queue<T>)] = new QueueConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(Stack<T>))) { Converters[typeof(Stack<T>)] = new StackConverter<T>(); changed = true; }
            if (!Converters.ContainsKey(typeof(ArraySegment<T>))) { Converters[typeof(ArraySegment<T>)] = new ArraySegmentConverter<T>(); changed = true; }
            if (changed) ConverterVersion++;
        }

        public static void RegisterAot<T>(T value) => RegisterAot<T>();

        public static void RegisterAot<TKey, TValue>()
        {
            RegisterAot<TKey>();
            RegisterAot<TValue>();
            bool changed = false;
            if (!Converters.ContainsKey(typeof(KeyValuePair<TKey, TValue>))) { Converters[typeof(KeyValuePair<TKey, TValue>)] = new KeyValuePairConverter<TKey, TValue>(); changed = true; }
            if (!Converters.ContainsKey(typeof(Dictionary<TKey, TValue>))) { Converters[typeof(Dictionary<TKey, TValue>)] = new DictionaryConverter<TKey, TValue>(); changed = true; }
            if (!Converters.ContainsKey(typeof(ConcurrentDictionary<TKey, TValue>))) { Converters[typeof(ConcurrentDictionary<TKey, TValue>)] = new ConcurrentDictionaryConverter<TKey, TValue>(); changed = true; }
            if (!Converters.ContainsKey(typeof(IDictionary<TKey, TValue>))) { Converters[typeof(IDictionary<TKey, TValue>)] = new DictionaryInterfaceConverter<TKey, TValue>(); changed = true; }
            if (!Converters.ContainsKey(typeof(IReadOnlyDictionary<TKey, TValue>))) { Converters[typeof(IReadOnlyDictionary<TKey, TValue>)] = new ReadOnlyDictionaryInterfaceConverter<TKey, TValue>(); changed = true; }
            if (changed) ConverterVersion++;
        }

        public static void RegisterAotNullable<T>() where T : struct
        {
            RegisterAot<T>();
            if (Converters.ContainsKey(typeof(T?))) return;
            Converters[typeof(T?)] = new NullableConverter<T>();
            ConverterVersion++;
        }

        public static void RegisterAotDelegate<TDelegate>() where TDelegate : Delegate
        {
            if (Converters.ContainsKey(typeof(TDelegate))) return;
            Converters[typeof(TDelegate)] = new DelegateConverter<TDelegate>();
            ConverterVersion++;
        }

        public static void WriteObject(IBufferWriter writer, object obj,
            BufferSerializerSettings settings = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            if (obj == null) throw new ArgumentNullException(nameof(obj));

            settings ??= BufferSerializerSettings.DefaultSetting;
            var converter = GetConverter(obj.GetType(), settings);
            var scan = BufferScan.Rent(settings, writer.CollectMeta, settings.FullField);
            try
            {
                converter.Scan(scan, obj);
                scan.ValidateReferences();
                scan.ResetRead();
                writer.Init(scan);
                converter.Write(writer, scan, obj);
            }
            finally
            {
                BufferScan.Back(scan);
            }
        }

        public static void WriteObject<T>(IBufferWriter writer, T obj,
            BufferSerializerSettings settings = null)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            settings ??= BufferSerializerSettings.DefaultSetting;
            var converter = GetConverter<T>(settings);
            var scan = BufferScan.Rent(settings, writer.CollectMeta, settings.FullField);
            try
            {
                converter.ScanValue(scan, obj);
                scan.ValidateReferences();
                scan.ResetRead();
                writer.Init(scan);
                converter.WriteValue(writer, scan, obj);
            }
            finally
            {
                BufferScan.Back(scan);
            }
        }

        public static object ReadObject(IBufferReader reader, Type type)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));
            if (type == null) throw new ArgumentNullException(nameof(type));
            var result = GetConverter(type, BufferSerializerSettings.DefaultSetting).Read(reader, type);
            if (reader is BufferReader binaryReader)
                binaryReader.EnsureReferencesResolved();
            else if (reader is StructuredTextReader textReader)
                textReader.EnsureReferencesResolved();
            return result;
        }

        public static string ToJson(object obj, BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<JsonWriter>.Get();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetJson();
            }
            finally
            {
                writer.Clear();
                ClassPool<JsonWriter>.Back(writer);
            }
        }

        public static object ToObject(string data, Type type)
        {
            var reader = ClassPool<JsonReader>.Get();
            try
            {
                reader.Init(data);
                return ReadObject(reader, type);
            }
            finally
            {
                reader.Clear();
                ClassPool<JsonReader>.Back(reader);
            }
        }

        public static T ToObject<T>(string data) => (T)ToObject(data, typeof(T));

        public static string ToJson<T>(ReadOnlySpan<T> value,
            BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<JsonWriter>.Get();
            try
            {
                WriteSpan(writer, value, settings);
                return writer.GetJson();
            }
            finally
            {
                writer.Clear();
                ClassPool<JsonWriter>.Back(writer);
            }
        }

        public static Span<T> ToSpan<T>(string data) => ToObject<T[]>(data);

        public static string ToJson<T>(T? value, BufferSerializerSettings settings = null)
            where T : struct
        {
            var writer = ClassPool<JsonWriter>.Get();
            try
            {
                WriteObject<T?>(writer, value, settings);
                return writer.GetJson();
            }
            finally
            {
                writer.Clear();
                ClassPool<JsonWriter>.Back(writer);
            }
        }

        public static string ToYaml(object obj, BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<YamlWriter>.Get();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetYaml();
            }
            finally
            {
                writer.Clear();
                ClassPool<YamlWriter>.Back(writer);
            }
        }

        public static object FromYaml(string data, Type type)
        {
            var reader = ClassPool<YamlReader>.Get();
            try
            {
                reader.Init(data);
                return ReadObject(reader, type);
            }
            finally
            {
                reader.Clear();
                ClassPool<YamlReader>.Back(reader);
            }
        }

        public static T FromYaml<T>(string data) => (T)FromYaml(data, typeof(T));

        public static string ToYaml<T>(ReadOnlySpan<T> value,
            BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<YamlWriter>.Get();
            try
            {
                WriteSpan(writer, value, settings);
                return writer.GetYaml();
            }
            finally
            {
                writer.Clear();
                ClassPool<YamlWriter>.Back(writer);
            }
        }

        public static Span<T> FromYamlSpan<T>(string data) => FromYaml<T[]>(data);

        public static string ToYaml<T>(T? value, BufferSerializerSettings settings = null)
            where T : struct
        {
            var writer = ClassPool<YamlWriter>.Get();
            try
            {
                WriteObject<T?>(writer, value, settings);
                return writer.GetYaml();
            }
            finally
            {
                writer.Clear();
                ClassPool<YamlWriter>.Back(writer);
            }
        }

        public static string ToXml(object obj, BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<XmlWriter>.Get();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetXml();
            }
            finally
            {
                writer.Clear();
                ClassPool<XmlWriter>.Back(writer);
            }
        }

        public static object FromXml(string data, Type type)
        {
            var reader = ClassPool<XmlReader>.Get();
            try
            {
                reader.Init(data);
                return ReadObject(reader, type);
            }
            finally
            {
                reader.Clear();
                ClassPool<XmlReader>.Back(reader);
            }
        }

        public static T FromXml<T>(string data) => (T)FromXml(data, typeof(T));

        public static string ToXml<T>(ReadOnlySpan<T> value,
            BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<XmlWriter>.Get();
            try
            {
                WriteSpan(writer, value, settings);
                return writer.GetXml();
            }
            finally
            {
                writer.Clear();
                ClassPool<XmlWriter>.Back(writer);
            }
        }

        public static Span<T> FromXmlSpan<T>(string data) => FromXml<T[]>(data);

        public static string ToXml<T>(T? value, BufferSerializerSettings settings = null)
            where T : struct
        {
            var writer = ClassPool<XmlWriter>.Get();
            try
            {
                WriteObject<T?>(writer, value, settings);
                return writer.GetXml();
            }
            finally
            {
                writer.Clear();
                ClassPool<XmlWriter>.Back(writer);
            }
        }

        public static byte[] ToBytes(object obj, BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<BufferWriter>.Get();
            try
            {
                WriteObject(writer, obj, settings);
                return writer.GetValidBuffer();
            }
            finally
            {
                writer.Clear();
                ClassPool<BufferWriter>.Back(writer);
            }
        }

        public static object ToObject(byte[] bytes, Type type)
        {
            var reader = ClassPool<BufferReader>.Get();
            try
            {
                reader.Init(bytes);
                var result = ReadObject(reader, type);
                reader.EnsureFullyConsumed();
                return result;
            }
            finally
            {
                reader.Clear();
                ClassPool<BufferReader>.Back(reader);
            }
        }

        public static T ToObject<T>(byte[] bytes) => (T)ToObject(bytes, typeof(T));

        public static byte[] ToBytes<T>(ReadOnlySpan<T> value,
            BufferSerializerSettings settings = null)
        {
            var writer = ClassPool<BufferWriter>.Get();
            try
            {
                WriteSpan(writer, value, settings);
                return writer.GetValidBuffer();
            }
            finally
            {
                writer.Clear();
                ClassPool<BufferWriter>.Back(writer);
            }
        }

        public static Span<T> ToSpan<T>(byte[] bytes) => ToObject<T[]>(bytes);

        public static byte[] ToBytes<T>(T? value, BufferSerializerSettings settings = null)
            where T : struct
        {
            var writer = ClassPool<BufferWriter>.Get();
            try
            {
                WriteObject<T?>(writer, value, settings);
                return writer.GetValidBuffer();
            }
            finally
            {
                writer.Clear();
                ClassPool<BufferWriter>.Back(writer);
            }
        }

        private static void WriteSpan<T>(IBufferWriter writer, ReadOnlySpan<T> value,
            BufferSerializerSettings settings)
        {
            settings ??= BufferSerializerSettings.DefaultSetting;
            var scan = BufferScan.Rent(settings, writer.CollectMeta, settings.FullField);
            try
            {
                scan.CountNode();
                scan.ScanSpan(value, SpanSerialization<T>.GetConverter(settings));
                scan.ValidateReferences();
                scan.ResetRead();
                writer.Init(scan);
                writer.WriteIEnumerable<T>(scan, null, SpanSerialization<T>.WriteElement);
            }
            finally
            {
                BufferScan.Back(scan);
            }
        }

        private static class SpanSerialization<T>
        {
            private static BuffConverter<T> _converter;
            private static long _converterVersion = -1;
            internal static readonly Action<IBufferWriter, BufferScan, T> WriteElement = Write;

            internal static BuffConverter<T> GetConverter(BufferSerializerSettings settings)
            {
                long version = GetResolverVersion(settings);
                if (_converterVersion == version) return _converter;
                _converter = BufferSerializer.GetConverter<T>(settings);
                _converterVersion = version;
                return _converter;
            }

            private static void Write(IBufferWriter writer, BufferScan scan, T value) =>
                GetConverter(scan.Settings).WriteValue(writer, scan, value);
        }

        private static BuffConverter CreateConverter(Type type)
        {
            if (ConverterTypes.TryGetValue(type, out var converterType))
                return CreateConverterInstance(converterType);
            if (type.IsEnum)
                return CreateConverterInstance(typeof(EnumConverter<>).MakeGenericType(type));
            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                if (rank == 1)
                    return CreateConverterInstance(typeof(ArrayConverter<>).MakeGenericType(type.GetElementType()));
                if (rank >= 2 && rank <= 5)
                    return CreateConverterInstance(typeof(MultiDimensionalArrayConverter<,>)
                        .MakeGenericType(type, type.GetElementType()));
                throw new NotSupportedException(
                    $"Array type '{type}' has rank {rank}; only arrays up to rank five are supported.");
            }
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (GenericConverterTypes.TryGetValue(definition, out converterType))
                    return CreateConverterInstance(converterType.MakeGenericType(type.GetGenericArguments()));
            }

            if (FindSupportedGenericAncestor(type) != null)
                throw new NotSupportedException(
                    $"Collection type '{type}' must be declared as a directly supported collection or interface.");

            if (typeof(Delegate).IsAssignableFrom(type))
                return CreateConverterInstance(typeof(DelegateConverter<>).MakeGenericType(type));
            if (type != typeof(ValueTuple) && type.IsValueType &&
                TypeHelper.GetTypeFields(type).GetFields().Count == 0)
                throw new NotSupportedException(
                    $"Value type '{type}' has no registered converter or serializable fields.");
            return CreateConverterInstance(typeof(ObjectConverter<>).MakeGenericType(type));
        }

        private static BuffConverter CreateConverterInstance(Type converterType) =>
            Activator.CreateInstance(converterType) as BuffConverter;

        private static bool UsesObjectConverter(Type type)
        {
            if (ConverterTypes.ContainsKey(type) || type.IsArray ||
                typeof(Delegate).IsAssignableFrom(type)) return false;
            if (type.IsGenericType && GenericConverterTypes.ContainsKey(type.GetGenericTypeDefinition()))
                return false;
            if (FindSupportedGenericAncestor(type) != null) return false;
            return true;
        }

        private static Type FindSupportedGenericAncestor(Type type)
        {
            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
            {
                var current = interfaces[i];
                if (current.IsGenericType &&
                    GenericConverterTypes.ContainsKey(current.GetGenericTypeDefinition()))
                    return current.GetGenericTypeDefinition();
            }

            for (var current = type.BaseType;
                 current != null && current != typeof(object);
                 current = current.BaseType)
            {
                if (current.IsGenericType &&
                    GenericConverterTypes.ContainsKey(current.GetGenericTypeDefinition()))
                    return current.GetGenericTypeDefinition();
            }
            return null;
        }

        private static void RemoveCachedGenericConverters(Type genericDefinition)
        {
            var cachedTypes = ListPool<Type>.Get();
            try
            {
                foreach (var cachedType in Converters.Keys)
                {
                    if (cachedType.IsGenericType && cachedType.GetGenericTypeDefinition() == genericDefinition)
                        cachedTypes.Add(cachedType);
                }
                for (int i = 0; i < cachedTypes.Count; i++)
                    Converters.Remove(cachedTypes[i]);
            }
            finally
            {
                cachedTypes.Clear();
                ListPool<Type>.Back(cachedTypes);
            }
        }

        private static void SetPositiveLimit(ref int target, int value) =>
            SetLimit(ref target, value, 1, int.MaxValue);

        private static void SetLimit(ref int target, int value, int min, int max)
        {
            if (value < min || value > max) throw new ArgumentOutOfRangeException(nameof(value));
            target = value;
        }
    }
}
