using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace ActionAttribute
{
    internal static class EnumDisplayUtility
    {
        private static readonly Dictionary<Type, string[]> Names = new();

        internal static string[] GetNames(Type enumType)
        {
            if (Names.TryGetValue(enumType, out string[] cached)) return cached;
            string[] names = Enum.GetNames(enumType);
            var result = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo field = enumType.GetField(names[i]);
                NameAttribute name = field?.GetCustomAttribute<NameAttribute>();
                result[i] = name?.name ??
                    ObjectNames.NicifyVariableName(names[i]);
            }
            Names.Add(enumType, result);
            return result;
        }
    }
}
