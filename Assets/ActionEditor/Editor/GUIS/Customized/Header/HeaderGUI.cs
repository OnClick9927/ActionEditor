using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    public class HeaderGUI : ICustomized
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(80));
            if (GUI.Button(rect, Lan.ins.CreateAsset, EditorStyles.toolbarButton))
                CreateAssetWindow.Show(rect);
            GUILayout.Space(10);


            DrawNowAssetName();


            DrawAssetsHeader();
            GUILayout.FlexibleSpace();

            DrawToolbarRight();

            GUILayout.EndHorizontal();
        }

        protected virtual void DrawAssetsHeader()
        {
            if (App.AssetData == null) return;
            var customAssetHeader = EditorCustomFactory.GetHeader(App.AssetData);
            customAssetHeader?.OnGUI();
        }

        protected virtual void DrawNowAssetName()
        {
            var gName = App.TextAsset != null ? App.TextAsset.name : "None";
            var size = GUI.skin.label.CalcSize(new GUIContent(gName));
            var width = size.x + 8;
            if (width < 80) width = 80;
            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width + 20));
            if (GUI.Button(rect, $"[{gName}]", EditorStyles.toolbarDropDown))
            {
                App.AutoSave();

                AssetPick.ShowObjectPicker(rect, "Assets", "t:TextAsset", (o) =>
                {
                    App.OnObjectPickerConfig(o);
                    GUIUtility.ExitGUI();
                }, (x) =>
                {
                    return x.EndsWith(Asset.FileEx);

                });
            }
        }


        protected virtual void DrawToolbarRight()
        {
            //显示保持状态

            if (App.AssetData != null)
            {
                GUI.skin.label.richText = true;
                GUILayout.Label(
                   string.Format(Lan.ins.HeaderLastSaveTime, App.LastSaveTime.ToString("HH:mm:ss")));

                if (GUILayout.Button(new GUIContent(EditorGUIUtility.TrIconContent("SaveActive").image, Lan.ins.Save), EditorStyles.toolbarButton,
                        GUILayout.Width(26)))
                {
                    App.AutoSave(); //先保存当前的
                }

            }


            var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
            if (GUI.Button(rect, EditorGUIUtility.TrIconContent("Settings", Lan.ins.OpenPreferencesTips),
                    EditorStyles.toolbarButton))
            {
                PreferencesWindow.Show(rect);
            }
        }
    }
}