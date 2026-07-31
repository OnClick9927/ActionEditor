using ActionBuffer;
using UnityEditor;
using System;
using System.Collections.Generic;
using static ActionEditor.Nodes.BT.BTVariableCondition;

namespace ActionEditor.Nodes.BT
{
    class BTVariableConditionView : BTConditionView<BTVariableCondition>
    {
        private sealed class SupportedFieldSet
        {
            internal TypeHelper.TypeFields.Field[] Fields;
            internal string[] Names;
        }

        private static readonly Dictionary<Type, SupportedFieldSet> SupportedFields =
            new Dictionary<Type, SupportedFieldSet>();

        public static bool IsSupportType(Type fieldType)
        {
            if (fieldType.IsEnum) return true;
            if (fieldType == typeof(bool)) return true;
            if (fieldType == typeof(float)) return true;
            if (fieldType == typeof(int)) return true;

            return false;
        }
        public static VariableType GetVariableType(Type fieldType)
        {
            if (fieldType.IsEnum) return VariableType.Enum;
            if (fieldType == typeof(bool)) return VariableType.Bool;
            if (fieldType == typeof(float)) return VariableType.FLoat;
            if (fieldType == typeof(int)) return VariableType.Int;

            return VariableType.None;
        }

        internal static void GetSupportedFields(Type blackboardType,
            out TypeHelper.TypeFields.Field[] fields, out string[] names)
        {
            if (SupportedFields.TryGetValue(blackboardType, out var cached))
            {
                fields = cached.Fields;
                names = cached.Names;
                return;
            }

            var allFields = TypeHelper.GetTypeFields(blackboardType).GetFields();
            int count = 0;
            for (int i = 0; i < allFields.Count; i++)
            {
                var field = allFields[i];
                if (field.DeclaringType != typeof(Blackboard) && IsSupportType(field.FieldType))
                    count++;
            }

            fields = new TypeHelper.TypeFields.Field[count];
            names = new string[count];
            int index = 0;
            for (int i = 0; i < allFields.Count; i++)
            {
                var field = allFields[i];
                if (field.DeclaringType == typeof(Blackboard) || !IsSupportType(field.FieldType))
                    continue;
                fields[index] = field;
                names[index++] = field.name;
            }
            SupportedFields.Add(blackboardType, new SupportedFieldSet
            {
                Fields = fields,
                Names = names
            });
        }

        public static CompareType Valid(Type fieldType, CompareType src, CompareType target)
        {
            //if (fieldType == typeof(int)) return target;
            //if (fieldType == typeof(float)) return target;
            if (fieldType.IsEnum || fieldType == typeof(bool))
            {
                if (target != CompareType.Equals && target != CompareType.NotEquals)
                {
                    return src;
                }
            }
            return target;

        }
        string label = "Compare";
        private void DrawField(Type fieldType)
        {
            if (fieldType == typeof(int))
                data.intValue = EditorGUILayout.IntField(label, data.intValue);
            else if (fieldType == typeof(float))
                data.floatValue = EditorGUILayout.FloatField(label, data.floatValue);
            else if (fieldType == typeof(bool))
                data.boolValue = EditorGUILayout.Toggle(label, data.boolValue);
            else if (fieldType.IsEnum)
                data.intValue = EditorGUILayout.Popup(label, data.intValue, Enum.GetNames(fieldType));
        }
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Guid", data.guid);
            BTTree tree = App.asset as BTTree;
            if (tree == null || tree.Blackboard == null) return;

            GetSupportedFields(tree.Blackboard.GetType(), out var result, out var names);
            if (result.Length == 0)
            {
                EditorGUILayout.HelpBox("No supported Blackboard variables.", MessageType.Info);
                return;
            }

            var index = Array.IndexOf(names, data.fieldName);
            index = index < 0 ? 0 : index;
            index = EditorGUILayout.Popup("Variable", index, names);
            var field = result[index];
            data.fieldName = names[index];
            data.variableType = GetVariableType(field.FieldType);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.EnumPopup("Type", data.variableType);
            var compare = (CompareType)EditorGUILayout.EnumPopup("CompareType", data.compareType);
            data.compareType = Valid(field.FieldType, data.compareType, compare);
            DrawField(field.FieldType);
        }
    }
}
