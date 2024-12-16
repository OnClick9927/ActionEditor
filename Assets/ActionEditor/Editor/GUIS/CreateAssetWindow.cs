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

        public override Vector2 GetWindowSize() => new Vector2(300, 120);

        public override void OnGUI(Rect rect)
        {
            GUILayout.Label($"<size=30><b>{Lan.ins.CreateAsset}</b></size>", new GUIStyle(EditorStyles.label)
            {
                richText = true,
            });
            GUILayout.Space(2);

            if (EditorEX.AssetNames.Count == 0)
            {
                EditorGUILayout.HelpBox(Lan.ins.NoAssetExtendType, MessageType.Error);
            }
            else
            {
                if (string.IsNullOrEmpty(_selectType))
                    _selectType = EditorEX.AssetNames.FirstOrDefault();
                _selectType = EditorEX.CleanPopup(Lan.ins.CrateAssetType, _selectType, EditorEX.AssetNames);
                _createName = EditorGUILayout.TextField(new GUIContent(Lan.ins.CrateAssetName, Lan.ins.CreateAssetFileName),
                    _createName);
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

                var t = EditorEX.AssetTypes[_selectType];
                var inst = Activator.CreateInstance(t) as Asset;
                if (inst != null)
                {
                    var json = inst.Serialize();
                    System.IO.File.WriteAllText(path, json);
                    AssetDatabase.Refresh();
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset != null)
                    {
                        App.OnObjectPickerConfig(textAsset);
                    }
                    editorWindow.Close();
                }
            }
        }
    }
}