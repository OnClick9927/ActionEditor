using UnityEditor;
using UnityEngine;

namespace ActionEditor.Nodes.BT
{
    [CustomPropertyDrawer(typeof(BTTree.Semaphore))]
    class BTTree_SemaphoreDrawer : UnityEditor.PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var max = property.FindPropertyRelative(nameof(BTTree.Semaphore.max));
            var name = property.FindPropertyRelative(nameof(BTTree.Semaphore.name));

            name.stringValue = EditorGUI.TextField(new Rect(position.x, position.y, 150, position.height), name.stringValue);
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 30;
            max.intValue = EditorGUI.IntField(new Rect(position.x + 150, position.y, position.x + position.width - 150, position.height)
                , "Max", max.intValue);
            max.intValue = Mathf.Max(max.intValue, 1);
            EditorGUIUtility.labelWidth = labelWidth;

        }
    }





}
