using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEditor;

namespace ActionBuffer.Unity.Editor
{
    public sealed class AssetDatabaseUnityObjectResolver : IUnityObjectResolver
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

        private readonly Dictionary<UnityEngine.Object, string> idsByObject =
            new Dictionary<UnityEngine.Object, string>(
                ObjectReferenceComparer.Instance);
        private readonly Dictionary<string, UnityEngine.Object> objectsById =
            new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);

        public string GetReferenceId(UnityEngine.Object value)
        {
            if (value == null) return null;
            if (idsByObject.TryGetValue(value, out string cachedId))
                return cachedId;
            if (!EditorUtility.IsPersistent(value))
                throw new InvalidOperationException(
                    $"Unity object '{value}' is not a persistent project asset. " +
                    "Use UnityObjectRegistry for scene and runtime objects.");
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,
                    out string guid, out long localId) || string.IsNullOrEmpty(guid))
                throw new InvalidOperationException(
                    $"Cannot obtain an asset GUID for Unity object '{value}'.");
            string referenceId = guid + ":" +
                localId.ToString(CultureInfo.InvariantCulture);
            idsByObject[value] = referenceId;
            objectsById[referenceId] = value;
            return referenceId;
        }

        public UnityEngine.Object ResolveReference(string referenceId,
            Type expectedType)
        {
            if (referenceId == null) return null;
            if (objectsById.TryGetValue(referenceId,
                    out UnityEngine.Object cached) && cached != null)
            {
                EnsureExpectedType(referenceId, cached, expectedType);
                return cached;
            }
            int separator = referenceId.LastIndexOf(':');
            if (separator <= 0 || separator == referenceId.Length - 1 ||
                !long.TryParse(referenceId.Substring(separator + 1),
                    NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out long localId))
                throw new FormatException(
                    $"Invalid Unity asset reference id '{referenceId}'.");

            string guid = referenceId.Substring(0, separator);
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                throw new InvalidOperationException(
                    $"Unity asset GUID '{guid}' cannot be resolved.");

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            for (int i = 0; i < assets.Length; i++)
            {
                var asset = assets[i];
                if (asset == null ||
                    !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,
                        out string assetGuid, out long assetLocalId) ||
                    assetGuid != guid || assetLocalId != localId) continue;
                EnsureExpectedType(referenceId, asset, expectedType);
                idsByObject[asset] = referenceId;
                objectsById[referenceId] = asset;
                return asset;
            }
            throw new InvalidOperationException(
                $"Unity asset '{referenceId}' no longer exists at '{path}'.");
        }

        private static void EnsureExpectedType(string referenceId,
            UnityEngine.Object value, Type expectedType)
        {
            if (expectedType != null && !expectedType.IsInstanceOfType(value))
                throw new InvalidCastException(
                    $"Unity asset '{referenceId}' is '{value.GetType()}', " +
                    $"not '{expectedType}'.");
        }
    }
}
