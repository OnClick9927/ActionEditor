using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ActionEditor
{
    /// <summary>Specifies the serialized file extension used by an Asset type.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class AssetFileExtensionAttribute : Attribute
    {
        public string Extension { get; }

        public AssetFileExtensionAttribute(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException("File extension cannot be empty.",
                    nameof(extension));
            Extension = extension.Trim().TrimStart(new[] { '.' });
        }
    }

    public static class AssetFileExtensionUtility
    {
        private static readonly Dictionary<Type, string> Extensions = new();

        public static string Get(Type assetType, string fallback = "bytes")
        {
            if (assetType == null) return Normalize(fallback);
            if (Extensions.TryGetValue(assetType, out string extension))
                return extension;
            extension = assetType.GetCustomAttribute<
                AssetFileExtensionAttribute>(true)?.Extension;
            if (string.IsNullOrEmpty(extension)) extension = Normalize(fallback);
            Extensions[assetType] = extension;
            return extension;
        }

        public static bool Matches(string path, Type assetType,
            string fallback = "bytes") => !string.IsNullOrEmpty(path) &&
            path.EndsWith("." + Get(assetType, fallback),
                StringComparison.OrdinalIgnoreCase);

        public static string WithExtension(string path, Type assetType,
            string fallback = "bytes")
        {
            string extension = Get(assetType, fallback);
            string without = WithoutExtension(path, extension);
            return without + "." + extension;
        }

        public static string WithoutExtension(string path, string extension)
        {
            if (string.IsNullOrEmpty(path)) return string.Empty;
            extension = Normalize(extension);
            string suffix = "." + extension;
            return path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? path.Substring(0, path.Length - suffix.Length)
                : Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(path));
        }

        private static string Normalize(string extension) =>
            string.IsNullOrWhiteSpace(extension)
                ? "bytes"
                : TrimLeadingDots(extension.Trim());

        private static string TrimLeadingDots(string extension)
        {
            int start = 0;
            while (start < extension.Length && extension[start] == '.') start++;
            return start == 0 ? extension : extension.Substring(start);
        }
    }
}
