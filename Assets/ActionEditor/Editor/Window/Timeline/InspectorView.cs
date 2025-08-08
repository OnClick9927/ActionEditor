using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class InspectorView : ViewBase
    {

        static Vector2 scroll;
        public override void OnDraw()
        {
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 25,
                    fontStyle = FontStyle.Bold
                };
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            bool change = false;
            EditorGUI.BeginChangeCheck();
            if (AppInternal.SelectCount < 1)
            {
                if (AppInternal.AssetData == null)
                {
                    EditorGUILayout.HelpBox(Lan.ins.NotSelectAsset, MessageType.Info);
                }
                else
                {
                    DoAssetInspector();

                }
            }
            else
            {
                DoSelectionInspector();
            }
            change = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndScrollView();
            if (change)
                AppInternal.Refresh();

        }
        GUIStyle _style;
        void DoAssetInspector()
        {
            if (AppInternal.AssetData == null) return;
            var assetData = AppInternal.AssetData;

            var title = EditorEX.GetTypeName(assetData.GetType());

            EditorGUILayout.LabelField(title, _style, GUILayout.Height(30));

            GUILayout.Space(2);
            ActonEditorView.GetEditor(assetData)?.OnInspectorGUI();


        }

        void DoSelectionInspector()
        {
            if (AppInternal.AssetData == null) return;

            var data = AppInternal.FistSelect;
            if (data == null) return;
            GUILayout.Space(4);
            var name = EditorEX.GetTypeName(data.GetType());
            GUILayout.Label(name, _style);
            GUILayout.Space(2);
            ActonEditorView.GetEditor(data)?.OnInspectorGUI();
        }


    }
}