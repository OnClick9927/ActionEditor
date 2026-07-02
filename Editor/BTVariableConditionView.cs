using ActionBuffer;
using UnityEditor;
using System;
using static ActionEditor.Nodes.BT.BTVariableCondition;

namespace ActionEditor.Nodes.BT
{
    class BTVariableConditionView : BTConditionView<BTVariableCondition>
    {
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

            var fields = TypeHelper.GetTypeFields(tree.Blackboard.GetType());
            var result = fields.GetFields()
                .FindAll(x => x.DeclaringType != typeof(Blackboard)
                    && IsSupportType(x.FieldType));
            var names = result.ConvertAll(x => x.name);
            var index = names.IndexOf(data.fieldName);
            index = index < 0 ? 0 : index;
            index = EditorGUILayout.Popup("Variable", index, names.ToArray());
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
