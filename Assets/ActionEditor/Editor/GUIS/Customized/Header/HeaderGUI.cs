using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    public class HeaderGUI : ICustomized
    {
        public void OnGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button(Lan.ins.CreateAsset, EditorStyles.toolbarButton))
                CreateAssetWindow.Show();
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
            if (GUILayout.Button($"[{gName}]", EditorStyles.toolbarDropDown, GUILayout.Width(150)))
            {
                App.AutoSave();
                var rect = GUILayoutUtility.GetLastRect();
                rect.x = 200;
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
                GUI.color = Color.white.WithAlpha(0.3f);
                GUI.skin.label.richText = true;
                GUILayout.Label(
                    $"<size=11>{string.Format(Lan.ins.HeaderLastSaveTime, App.LastSaveTime.ToString("HH:mm:ss"))}</size>");
                GUI.color = Color.white;

                if (GUILayout.Button(new GUIContent(EditorGUIUtility.TrIconContent("SaveActive").image, Lan.ins.Save), EditorStyles.toolbarButton,
                        GUILayout.Width(26)))
                {
                    App.AutoSave(); //先保存当前的
                }

            }



            if (GUILayout.Button(new GUIContent(EditorGUIUtility.TrIconContent("Settings").image, Lan.ins.OpenPreferencesTips),
                    EditorStyles.toolbarButton, GUILayout.Width(26)))
            {
                PreferencesWindow.Show(new Rect(Styles.ScreenWidth - 5 - 400 - Styles.TimelineLeftTotalWidth, 25, 400,
                    Styles.ScreenHeight - 70));


            }
        }
    }
}