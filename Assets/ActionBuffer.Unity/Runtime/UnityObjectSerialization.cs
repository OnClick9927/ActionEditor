using System;
using UnityEngine;

namespace ActionBuffer.Unity
{
    public static class UnityObjectSerialization
    {
        public static BuffSettings CreateSettings(IUnityObjectResolver resolver)
        {
            var settings = new BuffSettings();
            Register(settings, resolver);
            return settings;
        }

        public static BuffSettings RegisterUnityObjectConverters(
            this BuffSettings settings, IUnityObjectResolver resolver)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            Register(settings, resolver);
            return settings;
        }

        public static bool RemoveUnityObjectConverters(this BuffSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return settings.RemoveConverterFactory(typeof(UnityEngine.Object));
        }

        private static void Register(BuffSettings settings,
            IUnityObjectResolver resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            settings.RegisterConverterFactory(typeof(UnityEngine.Object), type =>
            {
                if (!typeof(UnityEngine.Object).IsAssignableFrom(type) ||
                    type.ContainsGenericParameters)
                    throw new NotSupportedException(
                        $"'{type}' is not a concrete Unity object reference type.");
                Type converterType = typeof(UnityObjectBuffConverter<>).MakeGenericType(type);
                return (BuffConverter)Activator.CreateInstance(converterType, resolver);
            });
        }
    }
}
