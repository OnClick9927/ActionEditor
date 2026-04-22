using UnityEditor;

namespace ActionEditor.Nodes.BT
{
    [InitializeOnLoad]
    static class Editorhelp
    {
        static Editorhelp()
        {
            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;

            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;
        }

        private static void EditorApplication_playModeStateChanged(PlayModeStateChange obj)
        {
            if (obj == PlayModeStateChange.ExitingPlayMode)
            {

                BTTree.ClearInstance();
            }
        }
    }





}
