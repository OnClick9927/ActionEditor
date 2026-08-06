using System;
using UnityEngine;
using UnityEngine.Events;

namespace ActionBuffer.Unity
{
    public static class UnityObjectSerialization
    {
        public static BuffSettings CreateRuntimeSettings() =>
            CreateSettings(new RuntimeUnityObjectResolver());

        public static BuffSettings CreateRuntimeSettings(
            RuntimeUnityObjectResolver resolver) =>
            CreateSettings(resolver ?? throw new ArgumentNullException(
                nameof(resolver)));

        public static BuffSettings CreateSettings(IUnityObjectResolver resolver)
        {
            var settings = new BuffSettings();
            RegisterConverters(settings, resolver);
            return settings;
        }

        public static BuffSettings RegisterUnityConverters(
            this BuffSettings settings, IUnityObjectResolver resolver)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            RegisterConverters(settings, resolver);
            return settings;
        }

        public static BuffSettings RegisterRuntimeUnityConverters(
            this BuffSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            RegisterConverters(settings, new RuntimeUnityObjectResolver());
            return settings;
        }

        public static BuffSettings RegisterUnityValueConverters(
            this BuffSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            UnityValueBuffConverters.Register(settings);
            return settings;
        }

        public static bool RemoveUnityConverters(this BuffSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            bool removedObjects = settings.RemoveConverterFactory(
                typeof(UnityEngine.Object));
            bool removedEvents = settings.RemoveConverterFactory(
                typeof(UnityEventBase));
            bool removedValues = UnityValueBuffConverters.Remove(settings);
            return removedObjects || removedEvents || removedValues;
        }

        private static void RegisterConverters(BuffSettings settings,
            IUnityObjectResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            UnityValueBuffConverters.Register(settings);
            settings.RegisterConverterFactory(typeof(UnityEngine.Object), type =>
            {
                if (!typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                    type.ContainsGenericParameters)
                    throw new NotSupportedException(
                        $"'{type}' is not a concrete Unity object reference type.");
                Type converterType = typeof(UnityObjectBuffConverter<>).MakeGenericType(type);
                return (BuffConverter)Activator.CreateInstance(converterType, resolver);
            });
            settings.RegisterConverterFactory(typeof(UnityEventBase), type =>
            {
                if (!typeof(UnityEventBase).IsAssignableFrom(type) || type.IsAbstract ||
                    type.ContainsGenericParameters)
                    throw new NotSupportedException(
                        $"'{type}' is not a concrete UnityEvent type.");
                Type converterType = typeof(UnityEventBuffConverter<>).MakeGenericType(type);
                return (BuffConverter)Activator.CreateInstance(converterType, resolver);
            });
        }
    }
}
