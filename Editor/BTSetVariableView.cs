using ActionBuffer;
using UnityEditor;
using System;
using static ActionEditor.Nodes.BT.BTSetVariable;

namespace ActionEditor.Nodes.BT
{
    class BTSetVariableView : BTActionView<BTSetVariable>
    {
        public override void OnInspectorGUI()
        {
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.TextField("Guid", data.guid);

            BTTree tree = App.asset as BTTree;
            if (tree == null || tree.Blackboard == null) return;

            var fields = TypeHelper.GetTypeFields(tree.Blackboard.GetType());
            var result = fields.GetFields()
                .FindAll(x => x.DeclaringType != typeof(Blackboard)
                    && BTVariableConditionView.IsSupportType(x.FieldType));
           
            var names = result.ConvertAll(x => x.name);
            var index = names.IndexOf(data.fieldName);
            index = index < 0 ? 0 : index;
            index = EditorGUILayout.Popup("Variable", index, names.ToArray());
            var field = result[index];
            data.fieldName = names[index];
            data.variableType = BTVariableConditionView.GetVariableType(field.FieldType);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.EnumPopup("Type", data.variableType);
            var compare = (SetVariableType)EditorGUILayout.EnumPopup("SetType", data.setType);
            data.setType = Valid(field.FieldType, data.setType, compare);
            if (data.setType == SetVariableType.Not 
                || data.setType==  SetVariableType.Abs 
                || data.setType== SetVariableType.Round
                || data.setType == SetVariableType.Ceil
                || data.setType == SetVariableType.Floor) return;
            DrawField(field.FieldType);
        }
        public static SetVariableType Valid(Type fieldType, SetVariableType src, SetVariableType target)
        {
            if (fieldType == typeof(int) || fieldType == typeof(float)) return target == SetVariableType.Not ? src : target;
            if (fieldType == typeof(bool))
            {
                if (target != SetVariableType.Not && target != SetVariableType.Set)
                    return src;
                return target;
            }
            if (fieldType.IsEnum || fieldType == typeof(bool))
            {
                return SetVariableType.Set;
            }
            return target;

        }
        string label = "Value";
        private void DrawField(Type fieldType)
        {

            //if (fieldType == typeof(int))
            //{
            //    if (data.setType == SetVariableType.Divide && data.intValue == 0)
            //        EditorGUILayout.HelpBox("Can' be Zero", MessageType.Error);

            //    data.intValue = EditorGUILayout.IntField(label, data.intValue);

            //}
            //else
            if (fieldType == typeof(float) || fieldType == typeof(int))
            {
                if (data.setType == SetVariableType.Divide && data.floatValue == 0)
                    EditorGUILayout.HelpBox("Can' be Zero", MessageType.Error);
                data.floatValue = EditorGUILayout.FloatField(label, data.floatValue);
            }
            else if (fieldType == typeof(bool))
                data.boolValue = EditorGUILayout.Toggle(label, data.boolValue);
            else if (fieldType.IsEnum)
                data.floatValue = EditorGUILayout.Popup(label, (int)data.floatValue, Enum.GetNames(fieldType));
        }
    }
}
