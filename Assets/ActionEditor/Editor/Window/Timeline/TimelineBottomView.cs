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
            //GUI.color = Color.cyan + Color.blue;
            if (AppInternal.CopyAsset != null)
                if (EditorGUILayout.LinkButton(Lan.ins.ClearCopy))
                    AppInternal.SetCopyAsset(null, false);
            GUILayout.Space(2);
            if (AppInternal.SelectCount != 0)
                if (EditorGUILayout.LinkButton(Lan.ins.ClearSelect))
                    AppInternal.Select();
            //GUI.color = Color.white;
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
                GUILayout.Label($"{Lan.ins.HeaderLastSaveTime}  {AppInternal.LastSaveTime.ToString("HH:mm:ss.fffffff")}",EditorStyles.linkLabel);
            }
            //GUILayout.Space(2);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}