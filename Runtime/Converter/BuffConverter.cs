using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ActionBuffer
{
    public abstract class BuffConverter
    {
        internal abstract object Read(IBufferReader reader, Type type);
        internal abstract void Scan(BufferScan scan, object value);
        internal abstract void Write(IBufferWriter writer, BufferScan scan, object value);
        internal virtual bool UsesObjectLayout => false;
    }

    public abstract class BuffConverter<T> : BuffConverter
    {
        protected abstract void OnScan(BufferScan scan, T value);
        protected abstract void OnWrite(IBufferWriter writer, BufferScan scan, T value);
        protected abstract T OnRead(IBufferReader reader, Type type);

        internal T ReadValue(IBufferReader reader, Type type) => OnRead(reader, type);

        internal void ScanValue(BufferScan scan, T value)
        {
            scan.CountNode();
            OnScan(scan, value);
        }

        internal void WriteValue(IBufferWriter writer, BufferScan scan, T value) =>
            OnWrite(writer, scan, value);

        internal sealed override object Read(IBufferReader reader, Type type) => ReadValue(reader, type);
        internal sealed override void Scan(BufferScan scan, object value) => ScanValue(scan, (T)value);
        internal sealed override void Write(IBufferWriter writer, BufferScan scan, object value) =>
            WriteValue(writer, scan, (T)value);
    }

    internal static class ConverterResolver
    {
        private static readonly object Sync = new object();
        private static readonly Dictionary<Type, Type> ConverterTypes =
            new Dictionary<Type, Type>
            {
                { typeof(bool), typeof(BoolConverter) },
                { typeof(byte), typeof(ByteConverter) },
                { typeof(char), typeof(CharConverter) },
                { typeof(DateTime), typeof(DateTimeConverter) },
                { typeof(decimal), typeof(DecimalConverter) },
                { typeof(double), typeof(DoubleConverter) },
                { typeof(float), typeof(FloatConverter) },
                { typeof(Guid), typeof(GuidConverter) },
                { typeof(int), typeof(IntConverter) },
                { typeof(long), typeof(LongConverter) },
                { typeof(sbyte), typeof(SByteConverter) },
                { typeof(short), typeof(ShortConverter) },
                { typeof(string), typeof(StringConverter) },
                { typeof(TimeSpan), typeof(TimeSpanConverter) },
                { typeof(uint), typeof(UIntConverter) },
                { typeof(ulong), typeof(ULongConverter) },
                { typeof(ushort), typeof(UShortConverter) }
            };
        private static readonly Dictionary<Type, Type> GenericConverterTypes =
            new Dictionary<Type, Type>
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
        private static readonly Dictionary<Type, BuffConverter> Cache =
            new Dictionary<Type, BuffConverter>();

        internal static BuffConverter Get(Type type, BuffSettings settings)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            settings ??= BuffSettings.DefaultSetting;
            return settings.TryGetConverter(type, out var converter)
                ? converter
                : GetBuiltIn(type);
        }

        internal static BuffConverter<T> Get<T>(BuffSettings settings)
        {
            var converter = Get(typeof(T), settings);
            if (converter is BuffConverter<T> typedConverter) return typedConverter;
            throw new InvalidOperationException(
                $"Converter '{converter.GetType()}' cannot serialize target type '{typeof(T)}'.");
        }

        internal static BuffConverter<T> GetBuiltIn<T>()
        {
            var converter = GetBuiltIn(typeof(T));
            if (converter is BuffConverter<T> typedConverter) return typedConverter;
            throw new InvalidOperationException(
                $"Converter '{converter.GetType()}' cannot serialize target type '{typeof(T)}'.");
        }

        private static BuffConverter GetBuiltIn(Type type)
        {
            lock (Sync)
            {
                if (Cache.TryGetValue(type, out var cached)) return cached;
                var converter = Create(type);
                if (converter == null)
                    throw new NotSupportedException($"Unhandled type '{type}'.");
                Cache.Add(type, converter);
                return converter;
            }
        }

        private static BuffConverter Create(Type type)
        {
            if (ConverterTypes.TryGetValue(type, out var converterType))
                return CreateInstance(converterType);
            if (type.IsEnum)
                return CreateInstance(typeof(EnumConverter<>).MakeGenericType(type));
            if (type.IsArray)
            {
                int rank = type.GetArrayRank();
                if (rank == 1)
                    return CreateInstance(
                        typeof(ArrayConverter<>).MakeGenericType(type.GetElementType()));
                if (rank >= 2 && rank <= 5)
                    return CreateInstance(typeof(MultiDimensionalArrayConverter<,>)
                        .MakeGenericType(type, type.GetElementType()));
                throw new NotSupportedException(
                    $"Array type '{type}' has rank {rank}; only arrays up to rank five are supported.");
            }
            if (typeof(ArrayList).IsAssignableFrom(type))
                return CreateInstance(typeof(ArrayListConverter<>).MakeGenericType(type));
            if (typeof(Hashtable).IsAssignableFrom(type))
                return CreateInstance(typeof(HashtableConverter<>).MakeGenericType(type));
            if (type.IsGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                if (GenericConverterTypes.TryGetValue(definition, out converterType))
                    return CreateInstance(
                        converterType.MakeGenericType(type.GetGenericArguments()));
            }
            var collectionConverter = CreateConcreteCollectionConverter(type);
            if (collectionConverter != null) return collectionConverter;
            if (typeof(Delegate).IsAssignableFrom(type))
                return CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type));
            if (type != typeof(ValueTuple) && type.IsValueType &&
                TypeHelper.GetTypeFields(type).GetFields().Count == 0)
                throw new NotSupportedException(
                    $"Value type '{type}' has no registered converter or serializable fields.");
            return CreateInstance(typeof(ObjectConverter<>).MakeGenericType(type));
        }

        private static BuffConverter CreateInstance(Type converterType) =>
            Activator.CreateInstance(converterType) as BuffConverter;

        private static BuffConverter CreateConcreteCollectionConverter(Type type)
        {
            if (type.IsAbstract || type.IsInterface) return null;

            var ancestor = FindGenericAncestor(type, typeof(IDictionary<,>));
            if (ancestor != null)
            {
                var arguments = ancestor.GetGenericArguments();
                return CreateInstance(typeof(ConcreteDictionaryConverter<,,>).MakeGenericType(
                    type, arguments[0], arguments[1]));
            }
            ancestor = FindGenericAncestor(type, typeof(ISet<>));
            if (ancestor != null)
                return CreateInstance(typeof(ConcreteSetConverter<,>).MakeGenericType(
                    type, ancestor.GetGenericArguments()[0]));
            ancestor = FindGenericAncestor(type, typeof(Stack<>));
            if (ancestor != null)
                return CreateInstance(typeof(ConcreteStackConverter<,>).MakeGenericType(
                    type, ancestor.GetGenericArguments()[0]));
            ancestor = FindGenericAncestor(type, typeof(Queue<>));
            if (ancestor != null)
                return CreateInstance(typeof(ConcreteSequenceConverter<,>).MakeGenericType(
                    type, ancestor.GetGenericArguments()[0]));
            ancestor = FindGenericAncestor(type, typeof(ICollection<>));
            return ancestor == null
                ? null
                : CreateInstance(typeof(ConcreteSequenceConverter<,>).MakeGenericType(
                    type, ancestor.GetGenericArguments()[0]));
        }

        private static Type FindGenericAncestor(Type type, Type definition)
        {
            for (var current = type; current != null && current != typeof(object);
                 current = current.BaseType)
            {
                if (current.IsGenericType &&
                    current.GetGenericTypeDefinition() == definition)
                    return current;
            }
            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
                if (interfaces[i].IsGenericType &&
                    interfaces[i].GetGenericTypeDefinition() == definition)
                    return interfaces[i];
            return null;
        }
    }

    internal static class ConverterCache<T>
    {
        private static readonly BuffConverter<T> BuiltInConverter =
            ConverterResolver.GetBuiltIn<T>();

        internal static BuffConverter<T> Get(BuffSettings settings)
        {
            settings ??= BuffSettings.DefaultSetting;
            if (!settings.TryGetConverter(typeof(T), out var converter))
                return BuiltInConverter;
            if (converter is BuffConverter<T> typedConverter)
                return typedConverter;
            throw new InvalidOperationException(
                $"Converter '{converter.GetType()}' cannot serialize target type '{typeof(T)}'.");
        }

        internal static BuffConverter<T> Get(IBufferReader reader)
        {
            var context = reader as IBuffSerializerContext;
            return Get(context?.Settings);
        }

        internal static BuffConverter<T> Get(BufferScan scan)
        {
            if (scan == null)
            {
                throw new ArgumentNullException(nameof(scan));
            }
            return Get(scan.Settings);
        }
    }
}
