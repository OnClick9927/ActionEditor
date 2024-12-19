using ActionEditor;
using UnityEditor;
using UnityEngine;

namespace ActionEditor.Design.Editor.Inspectors
{
    public abstract class GroupInspector<T> : ActionClipInspector where T : Group
    {
        protected T action => (T)target;
    }

    [CustomInspectors(typeof(Group), true)]
    public class GroupInspector : InspectorsBase
    {
        private Group action => (Group)target;

        public override void OnInspectorGUI()
        {
            GUI.enabled = !action.IsLocked;

            ShowCommonInspector();
        }

        protected void ShowCommonInspector(bool showBaseInspector = true)
        {
            action.Name = EditorGUILayout.TextField("Name", action.Name);
            if (showBaseInspector)
            {
                base.OnInspectorGUI();
            }
        }
    }
}