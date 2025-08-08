using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    class TimelineBottomView_custom : ViewBase
    {
        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();

            if (AppInternal.AssetData != null)
                ActonEditorView.GetEditor(AppInternal.AssetData)?.OnAssetFooterGUI();

            GUILayout.FlexibleSpace();
            GUI.color = Color.cyan + Color.blue;
            if (AppInternal.SelectCount != 0)
                if (GUILayout.Button(Lan.ins.ClearSelect, EditorStyles.toolbarButton))
                    AppInternal.Select();
            GUILayout.Space(2);
            if (AppInternal.CopyAsset != null)
                if (GUILayout.Button(Lan.ins.ClearCopy, EditorStyles.toolbarButton))
                    AppInternal.SetCopyAsset(null, false);
            GUI.color = Color.white;
            GUILayout.Space(2);
            GUILayout.EndHorizontal();

        }
    }

    class TimelineBottomView : ViewBase
    {
        public override void OnDraw()
        {
            GUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (AppInternal.AssetData != null)
            {
                GUILayout.Label(
string.Format(Lan.ins.HeaderLastSaveTime, AppInternal.LastSaveTime.ToString("HH:mm:ss")));
            }
            //GUILayout.Space(2);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}