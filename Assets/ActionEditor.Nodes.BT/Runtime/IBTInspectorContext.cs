using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ActionAttribute;
using ActionBuffer;

namespace ActionEditor.Nodes.BT
{
    public interface IBTInspectorContext
    {
        void SetInspectorBlackboard(Type blackboardType);
    }

    internal static class BTInspectorVariableUtility
    {
        internal static ValueDropdownList<string> GetFields(Type blackboardType)
        {
            var result = new ValueDropdownList<string>();
            if (blackboardType == null) return result;
            var fields = TypeHelper.GetTypeFields(blackboardType).GetFields();
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field.DeclaringType == typeof(Blackboard) ||
                    BTVariableCondition.GetVariableType(field.FieldType) ==
                    BTVariableCondition.VariableType.None) continue;
                result.Add(field.name, field.name);
            }
            return result;
        }

        internal static Type GetFieldType(Type blackboardType, string fieldName)
        {
            if (blackboardType == null || string.IsNullOrEmpty(fieldName))
                return null;
            return TypeHelper.GetTypeFields(blackboardType)
                .FindField(fieldName)?.FieldType;
        }

        internal static ValueDropdownList<string> GetEnumValues(
            Type blackboardType, string fieldName)
        {
            var result = new ValueDropdownList<string>();
            Type enumType = GetFieldType(blackboardType, fieldName);
            if (enumType == null || !enumType.IsEnum) return result;
            string[] names = Enum.GetNames(enumType);
            for (int i = 0; i < names.Length; i++)
                result.Add(names[i], names[i]);
            return result;
        }

        internal static bool IsDeterministicType(Type type)
        {
            return type == typeof(bool) ||
                type == typeof(sbyte) || type == typeof(byte) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong) ||
                type == typeof(decimal) || type == typeof(char) ||
                type == typeof(string) || (type != null && type.IsEnum);
        }

        internal static ValueDropdownList<string> GetDeterministicFields(
            Type blackboardType)
        {
            var result = new ValueDropdownList<string>();
            if (blackboardType == null) return result;
            var fields = TypeHelper.GetTypeFields(blackboardType).GetFields();
            for (int i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                if (field.DeclaringType == typeof(Blackboard) ||
                    !IsDeterministicType(field.FieldType)) continue;
                result.Add(field.name, field.name);
            }
            return result;
        }
    }

    internal static class BTEnumValueUtility
    {
        private static readonly ConcurrentDictionary<Type,
            IReadOnlyDictionary<string, object>> Values = new();

        internal static bool TryGetValue(Type enumType, string name,
            out object value)
        {
            value = null;
            if (enumType == null || !enumType.IsEnum ||
                string.IsNullOrEmpty(name)) return false;
            return Values.GetOrAdd(enumType, CreateValues)
                .TryGetValue(name, out value);
        }

        private static IReadOnlyDictionary<string, object> CreateValues(
            Type enumType)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            string[] names = Enum.GetNames(enumType);
            for (int i = 0; i < names.Length; i++)
                result.Add(names[i], Enum.Parse(enumType, names[i], false));
            return result;
        }
    }
}
