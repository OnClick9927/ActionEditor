using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public class CreateAssetWindow : PopupWindowContent
    {
        private static Rect _myRect;
        private static string _selectType;
        private static string _createName;

        public static void Show()
        {
            var rect = new Rect(20, 25, 300, 130);
            _myRect = rect;
            PopupWindow.Show(new Rect(rect.x, rect.y, 0, 0), new CreateAssetWindow());
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_myRect.width, _myRect.height);
        }

        public override void OnGUI(Rect rect)
        {



            GUILayout.Label($"<size=30><b>{Lan.ins.CreateAsset}</b></size>");
            GUILayout.Space(2);

            if (Prefs.AssetNames.Count == 0)
            {
                EditorGUILayout.HelpBox("None Type Sub Class of Asset", MessageType.Error);
            }
            else
            {

                GUILayout.BeginVertical("box");
                if (string.IsNullOrEmpty(_selectType))
                    _selectType = Prefs.AssetNames.FirstOrDefault();
                _selectType = EditorTools.CleanPopup(Lan.ins.CrateAssetType, _selectType, Prefs.AssetNames);
                _createName = EditorGUILayout.TextField(new GUIContent(Lan.ins.CrateAssetName, Lan.ins.CreateAssetFileName),
                    _createName);
                if (GUILayout.Button(new GUIContent(Lan.ins.CreateAssetConfirm)))
                {
                    CreateConfirm();
                }
            }


        }

        void CreateConfirm()
        {
            //var path = $"{Prefs.savePath}/{_createName}.json";

            var path = EditorUtility.SaveFolderPanel("SelectFolder", "Assets", "");

            if (string.IsNullOrEmpty(path)) return;
            path = Path.Combine(path, $"{_createName}.{Asset.FileEx}");

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

                var t = Prefs.AssetTypes[_selectType];
                var inst = Activator.CreateInstance(t) as Asset;
                if (inst != null)
                {
                    inst.AddGroup(typeof(Group));
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