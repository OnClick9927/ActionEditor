using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ActionBuffer.Unity
{
    public sealed class UnityObjectRegistry : IUnityObjectResolver
    {
        private sealed class ObjectReferenceComparer :
            IEqualityComparer<UnityEngine.Object>
        {
            internal static readonly ObjectReferenceComparer Instance =
                new ObjectReferenceComparer();

            public bool Equals(UnityEngine.Object x, UnityEngine.Object y) =>
                ReferenceEquals(x, y);

            public int GetHashCode(UnityEngine.Object value) =>
                RuntimeHelpers.GetHashCode(value);
        }

        private readonly Dictionary<string, UnityEngine.Object> objectsById =
            new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
        private readonly Dictionary<UnityEngine.Object, string> idsByObject =
            new Dictionary<UnityEngine.Object, string>(ObjectReferenceComparer.Instance);

        public int Count => objectsById.Count;

        public void Register(string referenceId, UnityEngine.Object value)
        {
            if (string.IsNullOrEmpty(referenceId))
                throw new ArgumentException(
                    "Unity object reference id cannot be null or empty.",
                    nameof(referenceId));
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (objectsById.TryGetValue(referenceId, out var idValue) &&
                !ReferenceEquals(idValue, value))
                throw new InvalidOperationException(
                    $"Unity object reference id '{referenceId}' is already registered.");
            if (idsByObject.TryGetValue(value, out string objectId) &&
                objectId != referenceId)
                throw new InvalidOperationException(
                    $"Unity object '{value}' is already registered as '{objectId}'.");

            objectsById[referenceId] = value;
            idsByObject[value] = referenceId;
        }

        public bool Remove(string referenceId)
        {
            if (string.IsNullOrEmpty(referenceId) ||
                !objectsById.TryGetValue(referenceId, out var value)) return false;
            objectsById.Remove(referenceId);
            idsByObject.Remove(value);
            return true;
        }

        public void Clear()
        {
            objectsById.Clear();
            idsByObject.Clear();
        }

        public string GetReferenceId(UnityEngine.Object value)
        {
            if (value == null) return null;
            if (idsByObject.TryGetValue(value, out string referenceId))
                return referenceId;
            throw new InvalidOperationException(
                $"Unity object '{value}' has not been registered.");
        }

        public UnityEngine.Object ResolveReference(string referenceId,
            Type expectedType)
        {
            if (referenceId == null) return null;
            if (!objectsById.TryGetValue(referenceId, out var value) || value == null)
                throw new InvalidOperationException(
                    $"Unity object reference id '{referenceId}' is not registered.");
            if (expectedType != null && !expectedType.IsInstanceOfType(value))
                throw new InvalidCastException(
                    $"Unity object '{referenceId}' is '{value.GetType()}', not '{expectedType}'.");
            return value;
        }
    }
}
