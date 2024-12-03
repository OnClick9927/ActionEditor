using ActionEditor;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public abstract class TrackInspector<T> : TrackInspector where T : Track
    {
        protected T action => (T)target;
    }

    [CustomInspectors(typeof(Track), true)]
    public class TrackInspector : InspectorsBase
    {
        private Track action => (Track)target;

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