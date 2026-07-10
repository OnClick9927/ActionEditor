using UnityEditor;
using UnityEngine;

namespace ActionEditor.Nodes.BT
{
    class BTPerformInterruptView : BTActionView<BTPerformInterrupt>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var flags = (App.asset as BTTree).Blackboard.interruptFlags;
            var index = EditorGUILayout.Popup(nameof(data.flag),Mathf.Max(flags.IndexOf(data.flag), 0), flags.ToArray());
            if (flags.Count == 0)
                data.flag = string.Empty;
            else
                data.flag = flags[index];
        }
    }
}
