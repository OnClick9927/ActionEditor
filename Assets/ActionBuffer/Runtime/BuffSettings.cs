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

        public void ClearConverters()
        {
            lock (_converterSync)
            {
                EnsureSettingsMutable();
                _converters = null;
            }
        }

        internal bool TryGetConverter(Type type, out BuffConverter converter)
        {
            var converters = _converters;
            if (converters != null) return converters.TryGetValue(type, out converter);
            converter = null;
            return false;
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
