using System;
using System.Globalization;
using UnityEditor;

namespace ActionBuffer.Unity.Editor
{
    public sealed class AssetDatabaseUnityObjectResolver : IUnityObjectResolver
    {
        public string GetReferenceId(UnityEngine.Object value)
        {
            if (value == null) return null;
            if (!EditorUtility.IsPersistent(value))
                throw new InvalidOperationException(
                    $"Unity object '{value}' is not a persistent project asset. " +
                    "Use UnityObjectRegistry for scene and runtime objects.");
            if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(value,
                    out string guid, out long localId) || string.IsNullOrEmpty(guid))
                throw new InvalidOperationException(
                    $"Cannot obtain an asset GUID for Unity object '{value}'.");
            return guid + ":" + localId.ToString(CultureInfo.InvariantCulture);
        }

        public UnityEngine.Object ResolveReference(string referenceId,
            Type expectedType)
        {
            if (referenceId == null) return null;
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
                if (expectedType != null && !expectedType.IsInstanceOfType(asset))
                    throw new InvalidCastException(
                        $"Unity asset '{referenceId}' is '{asset.GetType()}', " +
                        $"not '{expectedType}'.");
                return asset;
            }
            throw new InvalidOperationException(
                $"Unity asset '{referenceId}' no longer exists at '{path}'.");
        }
    }
}
