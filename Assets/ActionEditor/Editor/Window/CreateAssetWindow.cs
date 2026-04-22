using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class CreateAssetWindow : PopupWindowContent
    {
        private static string _selectType;
        private static string _createName;

        public static void Show(Rect rect) => PopupWindow.Show(rect, new CreateAssetWindow());

        public override Vector2 GetWindowSize() => new Vector2(300, 130);
        public override void OnGUI(Rect rect)
        {
            GUILayout.Label(Lan.ins.CreateAsset, new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 30
            });
            GUILayout.Space(2);

            if (AppInternal.AssetTypes.Count == 0)
            {
                EditorGUILayout.HelpBox(Lan.ins.NoAssetExtendType, MessageType.Error);
            }
            else
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.TextField(Lan.ins.SavePath, Prefs.savePath);


                if (string.IsNullOrEmpty(_selectType))
                    _selectType = AppInternal.AssetNames.FirstOrDefault();
                _selectType = EditorEX.CleanPopup(Lan.ins.CrateAssetType, _selectType, AppInternal.AssetNames);
                _createName = EditorGUILayout.TextField(new GUIContent(Lan.ins.CrateAssetName, Lan.ins.CreateAssetFileName),
                    _createName);
                GUILayout.FlexibleSpace();
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(Lan.ins.CreateAssetConfirm)))
                {
                    CreateConfirm(false);
                }
                if (GUILayout.Button(new GUIContent(Lan.ins.CreateAssetConfirmBySelectPath)))
                {
                    CreateConfirm(true);
                }
                GUILayout.EndHorizontal();
                GUILayout.Space(5);
            }


        }

        public static string SelectSavePath()
        {
            var defaut_ = Prefs.savePath;


            var path = EditorUtility.SaveFolderPanel(Lan.ins.SelectFolder, defaut_, "");

            if (string.IsNullOrEmpty(path)) return string.Empty;

            var index = path.IndexOf("Assets");
            path = path.Remove(0, index).Replace("\\", "/");

            Prefs.savePath = path;
            return path;
        }
        void CreateConfirm(bool select)
        {
            //var path = $"{Prefs.savePath}/{_createName}.json";

            var path = Path.Combine(Prefs.savePath, $"{_createName}.{Asset.FileEx}");

            if (select)
            {
                var s = SelectSavePath();
                if (string.IsNullOrEmpty(s))
                    return;
                path = Path.Combine(s, $"{_createName}.{Asset.FileEx}");
            }



            if (string.IsNullOrEmpty(_createName))
            {
                EditorUtility.DisplayDialog(Lan.ins.TipsTitle, Lan.ins.CreateAssetTipsNameNull, Lan.ins.TipsConfirm);
            }
            else if (AssetDatabase.LoadAssetAtPath<TextAsset>(path) != null)
            {
                EditorUtility.DisplayDialog(Lan.ins.TipsTitle, Lan.ins.CreateAssetTipsRepetitive, Lan.ins.TipsConfirm);
            }
            else
            {

                var t = AppInternal.AssetTypes[_selectType];
                var inst = Activator.CreateInstance(t) as Asset;
                if (inst != null)
                {
                    var json = inst.ToBytes();
                    System.IO.File.WriteAllBytes(path, json);
                    AssetDatabase.Refresh();
                    AppInternal.OnObjectPickerConfig(path);
                    editorWindow.Close();
                }
            }
        }
    }
}