using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ActionBuffer.Unity
{
    /// <summary>
    /// Resolves Unity object references in a Player. Unknown objects are kept
    /// alive by this resolver for same-process round trips. Resources assets
    /// can additionally be registered with a path that survives restarts.
    /// </summary>
    public sealed class RuntimeUnityObjectResolver : IUnityObjectResolver
    {
        private const string RuntimePrefix = "$ActionBuffer.Runtime:";
        private const string ResourcesPrefix = "$ActionBuffer.Resources:";

        private sealed class ReferenceComparer :
            IEqualityComparer<UnityEngine.Object>
        {
            internal static readonly ReferenceComparer Instance =
                new ReferenceComparer();

            public bool Equals(UnityEngine.Object x, UnityEngine.Object y) =>
                ReferenceEquals(x, y);

            public int GetHashCode(UnityEngine.Object value) =>
                RuntimeHelpers.GetHashCode(value);
        }

        private readonly Dictionary<string, UnityEngine.Object> objectsById =
            new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
        private readonly Dictionary<UnityEngine.Object, string> idsByObject =
            new Dictionary<UnityEngine.Object, string>(ReferenceComparer.Instance);
        private int generatedId;

        public int Count => objectsById.Count;
        public bool AutoRegister { get; set; } = true;

        public void Register(string referenceId, UnityEngine.Object value)
        {
            if (string.IsNullOrEmpty(referenceId))
                throw new ArgumentException(
                    "Unity object reference id cannot be null or empty.",
                    nameof(referenceId));
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (objectsById.TryGetValue(referenceId, out UnityEngine.Object byId) &&
                !ReferenceEquals(byId, value))
                throw new InvalidOperationException(
                    $"Unity object reference id '{referenceId}' is already registered.");
            if (idsByObject.TryGetValue(value, out string byObject) &&
                !string.Equals(byObject, referenceId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Unity object '{value}' is already registered as '{byObject}'.");
            objectsById[referenceId] = value;
            idsByObject[value] = referenceId;
        }

        public void RegisterResource(string resourcesPath,
            UnityEngine.Object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            string path = NormalizeResourcesPath(resourcesPath);
            UnityEngine.Object loaded = Resources.Load(path, value.GetType());
            if (!ReferenceEquals(loaded, value))
                throw new InvalidOperationException(
                    $"Unity object '{value}' is not the Resources asset at '{path}'.");
            Register(ResourcesPrefix + path, value);
        }

        public T RegisterResource<T>(string resourcesPath)
            where T : UnityEngine.Object
        {
            string path = NormalizeResourcesPath(resourcesPath);
            T value = Resources.Load<T>(path);
            if (value == null)
                throw new InvalidOperationException(
                    $"Resources asset '{path}' of type '{typeof(T)}' was not found.");
            Register(ResourcesPrefix + path, value);
            return value;
        }

        public bool Remove(string referenceId)
        {
            if (string.IsNullOrEmpty(referenceId) ||
                !objectsById.TryGetValue(referenceId, out UnityEngine.Object value))
                return false;
            objectsById.Remove(referenceId);
            idsByObject.Remove(value);
            return true;
        }

        public void Clear()
        {
            objectsById.Clear();
            idsByObject.Clear();
            generatedId = 0;
        }

        public string GetReferenceId(UnityEngine.Object value)
        {
            if (value == null) return null;
            if (idsByObject.TryGetValue(value, out string referenceId))
                return referenceId;
            if (!AutoRegister)
                throw new InvalidOperationException(
                    $"Unity object '{value}' has not been registered.");

            do
            {
                generatedId++;
                referenceId = RuntimePrefix +
                    value.GetInstanceID().ToString(CultureInfo.InvariantCulture) +
                    ":" + generatedId.ToString(CultureInfo.InvariantCulture);
            }
            while (objectsById.ContainsKey(referenceId));
            Register(referenceId, value);
            return referenceId;
        }

        public UnityEngine.Object ResolveReference(string referenceId,
            Type expectedType)
        {
            if (referenceId == null) return null;
            if (!objectsById.TryGetValue(referenceId,
                    out UnityEngine.Object value) || value == null)
            {
                if (!referenceId.StartsWith(ResourcesPrefix,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        $"Runtime Unity object id '{referenceId}' is not registered.");
                string path = referenceId.Substring(ResourcesPrefix.Length);
                Type loadType = expectedType != null &&
                    typeof(UnityEngine.Object).IsAssignableFrom(expectedType)
                    ? expectedType
                    : typeof(UnityEngine.Object);
                value = Resources.Load(path, loadType);
                if (value == null)
                    throw new InvalidOperationException(
                        $"Resources asset '{path}' of type '{loadType}' was not found.");
                Register(referenceId, value);
            }
            if (expectedType != null && !expectedType.IsInstanceOfType(value))
                throw new InvalidCastException(
                    $"Unity object '{referenceId}' is '{value.GetType()}', " +
                    $"not '{expectedType}'.");
            return value;
        }

        private static string NormalizeResourcesPath(string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
                throw new ArgumentException(
                    "Resources path cannot be null or empty.",
                    nameof(resourcesPath));
            string path = resourcesPath.Replace('\\', '/').Trim('/');
            const string marker = "/Resources/";
            int markerIndex = path.LastIndexOf(marker,
                StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
                path = path.Substring(markerIndex + marker.Length);
            int extension = path.LastIndexOf('.');
            int slash = path.LastIndexOf('/');
            if (extension > slash) path = path.Substring(0, extension);
            if (path.Length == 0)
                throw new ArgumentException("Resources path is invalid.",
                    nameof(resourcesPath));
            return path;
        }
    }
}
