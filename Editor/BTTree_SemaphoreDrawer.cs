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

            const float spacing = 4f;
            float maxWidth = Mathf.Min(90f, position.width * 0.35f);
            var nameRect = new Rect(position.x, position.y,
                Mathf.Max(0f, position.width - maxWidth - spacing),
                position.height);
            var maxRect = new Rect(nameRect.xMax + spacing, position.y,
                maxWidth, position.height);
            name.stringValue = EditorGUI.TextField(nameRect, name.stringValue);
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 30;
            max.intValue = EditorGUI.IntField(maxRect, "Max", max.intValue);
            max.intValue = Mathf.Max(max.intValue, 1);
            EditorGUIUtility.labelWidth = labelWidth;

        }
    }





}
