using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    public sealed class BuffSettings
    {
        internal const int PoolLimit = 16;
        internal const int RetainedListCapacity = 4096;
        internal const int RetainedTextCapacity = 64 * 1024;
        internal const int RetainedBinaryCapacity = 1024 * 1024;
        private static readonly object LimitSync = new object();
        private readonly object _converterSync = new object();
        private Dictionary<Type, BuffConverter> _converters;
        private List<ConverterFactoryRegistration> _converterFactories;
        private Dictionary<Type, BuffConverter> _factoryConverters;
        private HashSet<Type> _types;
        private int _activeOperations;
        private int _restrictTypes;
        private static int _maxDepth = 256;
        private static int _maxTextLength = 16 * 1024 * 1024;
        private static int _maxBinaryLength = 64 * 1024 * 1024;
        private static int _maxNodeCount = 100000;
        private static int _maxCollectionCount = ushort.MaxValue - 1;
        private static int _maxObjectFieldCount = 4096;
        private static int _maxScalarLength = 4 * 1024 * 1024;

        public static BuffSettings DefaultSetting { get; } =
            new BuffSettings();

        private sealed class ConverterFactoryRegistration
        {
            internal Type BaseType;
            internal Func<Type, BuffConverter> Factory;
        }

        public bool TypeInfo { get; set; } = true;
        public bool FullField { get; set; }
        public bool SupportReferences { get; set; }
        public bool SerializeEvents { get; set; } = true;
        public bool PrettyPrint { get; set; }
        public bool DeterministicCollectionOrder { get; set; }
        public bool InvokeBeforeWriteCallbacks { get; set; } = true;
        public bool RestrictTypes
        {
            get
            {
                lock (_converterSync) return _restrictTypes != 0;
            }
            set
            {
                lock (_converterSync)
                {
                    EnsureSettingsMutable();
                    _restrictTypes = value ? 1 : 0;
                }
            }
        }

        public static int MaxDepth
        {
            get { lock (LimitSync) return _maxDepth; }
            set => SetLimit(ref _maxDepth, value, 1, 1024, nameof(MaxDepth));
        }

        public static int MaxTextLength
        {
            get { lock (LimitSync) return _maxTextLength; }
            set => SetPositiveLimit(ref _maxTextLength, value, nameof(MaxTextLength));
        }

        public static int MaxBinaryLength
        {
            get { lock (LimitSync) return _maxBinaryLength; }
            set => SetPositiveLimit(ref _maxBinaryLength, value, nameof(MaxBinaryLength));
        }

        public static int MaxNodeCount
        {
            get { lock (LimitSync) return _maxNodeCount; }
            set => SetPositiveLimit(ref _maxNodeCount, value, nameof(MaxNodeCount));
        }

        public static int MaxCollectionCount
        {
            get { lock (LimitSync) return _maxCollectionCount; }
            set => SetLimit(ref _maxCollectionCount, value, 1, ushort.MaxValue - 1,
                nameof(MaxCollectionCount));
        }

        public static int MaxObjectFieldCount
        {
            get { lock (LimitSync) return _maxObjectFieldCount; }
            set => SetPositiveLimit(ref _maxObjectFieldCount, value, nameof(MaxObjectFieldCount));
        }

        public static int MaxScalarLength
        {
            get { lock (LimitSync) return _maxScalarLength; }
            set => SetPositiveLimit(ref _maxScalarLength, value, nameof(MaxScalarLength));
        }

        public void RegisterConverter<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                if (_converters == null)
                    _converters = new Dictionary<Type, BuffConverter>();
                _converters[typeof(T)] = converter;
                _factoryConverters?.Remove(typeof(T));
            }
        }

        public void RegisterConverterFactory<TBase>(
            Func<Type, BuffConverter> factory) =>
            RegisterConverterFactory(typeof(TBase), factory);

        public void RegisterConverterFactory(Type baseType,
            Func<Type, BuffConverter> factory)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                _converterFactories ??= new List<ConverterFactoryRegistration>();
                for (int i = 0; i < _converterFactories.Count; i++)
                {
                    if (_converterFactories[i].BaseType != baseType) continue;
                    _converterFactories[i].Factory = factory;
                    _factoryConverters = null;
                    return;
                }
                _converterFactories.Add(new ConverterFactoryRegistration
                {
                    BaseType = baseType,
                    Factory = factory
                });
                _factoryConverters = null;
            }
        }

        public bool RemoveConverter<T>()
        {
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                if (_converters == null || !_converters.Remove(typeof(T))) return false;
                if (_converters.Count == 0) _converters = null;
                return true;
            }
        }

        public bool RemoveConverterFactory<TBase>() =>
            RemoveConverterFactory(typeof(TBase));

        public bool RemoveConverterFactory(Type baseType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                if (_converterFactories == null) return false;
                for (int i = 0; i < _converterFactories.Count; i++)
                {
                    if (_converterFactories[i].BaseType != baseType) continue;
                    _converterFactories.RemoveAt(i);
                    if (_converterFactories.Count == 0) _converterFactories = null;
                    _factoryConverters = null;
                    return true;
                }
                return false;
            }
        }

        public void ClearConverters()
        {
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                _converters = null;
                _converterFactories = null;
                _factoryConverters = null;
            }
        }

        internal bool TryGetConverter(Type type, out BuffConverter converter)
        {
            lock (_converterSync)
            {
                if (_converters != null &&
                    _converters.TryGetValue(type, out converter)) return true;
                if (_factoryConverters != null &&
                    _factoryConverters.TryGetValue(type, out converter)) return true;
                var registration = FindConverterFactory(type);
                if (registration == null)
                {
                    converter = null;
                    return false;
                }

                converter = registration.Factory(type);
                if (converter == null)
                    throw new InvalidOperationException(
                        $"Converter factory for '{registration.BaseType}' returned null for '{type}'.");
                Type requiredConverter = typeof(BuffConverter<>).MakeGenericType(type);
                if (!requiredConverter.IsInstanceOfType(converter))
                    throw new InvalidOperationException(
                        $"Converter factory for '{registration.BaseType}' returned " +
                        $"'{converter.GetType()}', which cannot serialize '{type}'.");
                _factoryConverters ??= new Dictionary<Type, BuffConverter>();
                _factoryConverters[type] = converter;
                return true;
            }
        }

        private ConverterFactoryRegistration FindConverterFactory(Type type)
        {
            ConverterFactoryRegistration best = null;
            if (_converterFactories == null) return null;
            for (int i = _converterFactories.Count - 1; i >= 0; i--)
            {
                var candidate = _converterFactories[i];
                if (!candidate.BaseType.IsAssignableFrom(type)) continue;
                if (best == null || best.BaseType.IsAssignableFrom(candidate.BaseType))
                    best = candidate;
            }
            return best;
        }

        internal void BeginOperation()
        {
            lock (_converterSync)
            {
                _activeOperations++;
            }
        }

        internal void EndOperation()
        {
            lock (_converterSync)
            {
                if (_activeOperations <= 0)
                    throw new InvalidOperationException("The serializer settings operation count is invalid.");
                _activeOperations--;
            }
        }

        public void RegisterType<T>() => RegisterType(typeof(T));

        public void RegisterType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                if (_types == null) _types = new HashSet<Type>();
                _types.Add(type);
            }
        }

        public bool RemoveType<T>() => RemoveType(typeof(T));

        public bool RemoveType(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                if (_types == null || !_types.Remove(type)) return false;
                if (_types.Count == 0) _types = null;
                return true;
            }
        }

        public void ClearTypes()
        {
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                _types = null;
            }
        }

        internal bool IsTypeAllowed(Type declaredType, Type actualType)
        {
            if (declaredType == actualType || _restrictTypes == 0) return true;
            var types = _types;
            return types != null && types.Contains(actualType);
        }

        private void EnsureSettingsMutable()
        {
            if (_activeOperations != 0)
                throw new InvalidOperationException(
                    "Converters and registered types cannot be changed while the settings are being used by a serialization operation.");
        }

        private static void SetPositiveLimit(ref int target, int value, string name)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(name);
            lock (LimitSync) target = value;
        }

        private static void SetLimit(ref int target, int value, int minimum, int maximum,
            string name)
        {
            if (value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name, value,
                    $"Value must be between {minimum} and {maximum}.");
            lock (LimitSync) target = value;
        }
    }
}
