using System;
using System.Collections.Generic;

namespace ActionBuffer
{
    public sealed class BufferSerializerSettings
    {
        private static int _nextResolverVersion;
        private Dictionary<Type, BuffConverter> _converters;
        private int _maxDepth = 256;
        private int _maxTextLength = 16 * 1024 * 1024;
        private int _maxBinaryLength = 64 * 1024 * 1024;
        private int _maxNodeCount = 100000;
        private int _maxCollectionCount = ushort.MaxValue - 1;
        private int _maxObjectFieldCount = 4096;
        private int _maxScalarLength = 4 * 1024 * 1024;

        public static BufferSerializerSettings DefaultSetting { get; } =
            new BufferSerializerSettings();

        internal int ResolverVersion { get; private set; } = ++_nextResolverVersion;

        public bool TypeInfo { get; set; } = true;
        public bool FullField { get; set; }
        public bool SupportReferences { get; set; }
        public bool SerializeEvents { get; set; } = true;
        public bool PrettyPrint { get; set; }
        public bool InvokeBeforeWriteCallbacks { get; set; } = true;

        public int MaxDepth
        {
            get => _maxDepth;
            set => SetLimit(ref _maxDepth, value, 1, 1024, nameof(MaxDepth));
        }

        public int MaxTextLength
        {
            get => _maxTextLength;
            set => SetPositiveLimit(ref _maxTextLength, value, nameof(MaxTextLength));
        }

        public int MaxBinaryLength
        {
            get => _maxBinaryLength;
            set => SetPositiveLimit(ref _maxBinaryLength, value, nameof(MaxBinaryLength));
        }

        public int MaxNodeCount
        {
            get => _maxNodeCount;
            set => SetPositiveLimit(ref _maxNodeCount, value, nameof(MaxNodeCount));
        }

        public int MaxCollectionCount
        {
            get => _maxCollectionCount;
            set => SetLimit(ref _maxCollectionCount, value, 1, ushort.MaxValue - 1,
                nameof(MaxCollectionCount));
        }

        public int MaxObjectFieldCount
        {
            get => _maxObjectFieldCount;
            set => SetPositiveLimit(ref _maxObjectFieldCount, value, nameof(MaxObjectFieldCount));
        }

        public int MaxScalarLength
        {
            get => _maxScalarLength;
            set => SetPositiveLimit(ref _maxScalarLength, value, nameof(MaxScalarLength));
        }

        public void RegisterConverter<T>(BuffConverter<T> converter)
        {
            if (converter == null) throw new ArgumentNullException(nameof(converter));
            if (_converters == null)
                _converters = new Dictionary<Type, BuffConverter>();
            _converters[typeof(T)] = converter;
            ResolverVersion = ++_nextResolverVersion;
        }

        public bool RemoveConverter<T>()
        {
            if (_converters == null || !_converters.Remove(typeof(T))) return false;
            ResolverVersion = ++_nextResolverVersion;
            return true;
        }

        public void ClearConverters()
        {
            if (_converters == null || _converters.Count == 0) return;
            _converters.Clear();
            ResolverVersion = ++_nextResolverVersion;
        }

        internal bool TryGetConverter(Type type, out BuffConverter converter)
        {
            if (_converters != null)
                return _converters.TryGetValue(type, out converter);
            converter = null;
            return false;
        }

        private static void SetPositiveLimit(ref int target, int value, string name)
        {
            if (value <= 0) throw new ArgumentOutOfRangeException(name);
            target = value;
        }

        private static void SetLimit(ref int target, int value, int minimum, int maximum,
            string name)
        {
            if (value < minimum || value > maximum)
                throw new ArgumentOutOfRangeException(name, value,
                    $"Value must be between {minimum} and {maximum}.");
            target = value;
        }
    }
}
