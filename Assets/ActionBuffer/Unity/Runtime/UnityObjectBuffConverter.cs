using System;
using UnityEngine;

namespace ActionBuffer.Unity
{
    internal sealed class UnityObjectBuffConverter<T> : BuffConverter<T>
        where T : UnityEngine.Object
    {
        private readonly IUnityObjectResolver resolver;

        public UnityObjectBuffConverter(IUnityObjectResolver resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        protected override void OnScan(BufferScan scan, T value)
        {
            string referenceId = value == null
                ? null
                : resolver.GetReferenceId(value);
            if (value != null && string.IsNullOrEmpty(referenceId))
                throw new InvalidOperationException(
                    $"Resolver returned an empty id for Unity object '{value}'.");
            if (referenceId != null && referenceId.Length > BuffSettings.MaxScalarLength)
                throw new FormatException(
                    $"Unity object reference id cannot exceed " +
                    $"{BuffSettings.MaxScalarLength} characters.");
            scan.CacheConverterValue(referenceId);
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            T value) => writer.WriteUTF8(scan.ReadConverterValue<string>());

        protected override T OnRead(IBufferReader reader, Type type)
        {
            string referenceId = reader.ReadUTF8();
            if (referenceId == null) return null;
            var value = resolver.ResolveReference(referenceId, type);
            if (value == null)
                throw new InvalidOperationException(
                    $"Resolver returned null for Unity object id '{referenceId}'.");
            if (!(value is T typed))
                throw new InvalidCastException(
                    $"Unity object '{referenceId}' is '{value.GetType()}', not '{typeof(T)}'.");
            return typed;
        }
    }
}
