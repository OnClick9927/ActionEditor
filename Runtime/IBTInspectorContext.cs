using System;
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

        internal static bool IsDeterministicType(Type type)
        {
            BTVariableCondition.VariableType variableType =
                BTVariableCondition.GetVariableType(type);
            return variableType != BTVariableCondition.VariableType.None &&
                variableType != BTVariableCondition.VariableType.Float &&
                variableType != BTVariableCondition.VariableType.Double;
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
}
