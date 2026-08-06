using ActionAttribute;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{
    class PreferencesWindow : PopupWindowContent
    {
        private static Vector2 win_size = new Vector2(400, 400);
        private static GUIStyle _titleStyle;
        private static readonly string[] TagNames = Enum.GetNames(typeof(Tag));
        private static System.Collections.Generic.List<string> _languageNames;
        public static void Show(Rect rect)
        {


            win_size.y = App.window.position.height - 20;
            rect.x = rect.x - win_size.x + rect.width;
            //_myRect = rect;
            UnityEditor.PopupWindow.Show(rect, new PreferencesWindow());
        }

        public override Vector2 GetWindowSize() => win_size;
        System.Collections.Generic.List<string> assetNames = new System.Collections.Generic.List<string>();

        public override void OnGUI(Rect rect)
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 22
                };
            }
            if (_languageNames == null)
                _languageNames = Lan.AllLanguages.Keys.ToList();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Lan.ins.Preferences, _titleStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Lan.ins.Save, GUILayout.Width(50)))
            {
                Prefs.Save();
                App.RebuildCurrentView();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2);


            Prefs.pickListType = (AssetPickListType)EditorGUILayout.EnumPopup(Lan.ins.AssetPickListType, Prefs.pickListType);

            var lan = EditorEX.CleanPopup<string>(Lan.ins.Language, Lan.Language,
               _languageNames);



            if (lan != Lan.Language)
            {
                Lan.SetLanguage(lan);
            }








            Prefs.NodePrettySpacing = EditorGUILayout.Vector2Field(
               "NodePrettySpacing", Prefs.NodePrettySpacing);

            Prefs.autoSaveSeconds = EditorGUILayout.IntSlider(
                new GUIContent(Lan.ins.AutoSaveTime), Prefs.autoSaveSeconds, 5,
                120);
            GUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(true))

                EditorGUILayout.TextField(
            Lan.ins.SavePath, Prefs.savePath);
            if (GUILayout.Button(Lan.ins.Select, GUILayout.Width(50)))
            {
                CreateAssetWindow.SelectSavePath();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);


            if (App.AssetNames.Length == 0) return;
            tag = (Tag)GUILayout.Toolbar((int)tag, TagNames);
            assetIndex = GUILayout.Toolbar(assetIndex, App.AssetNames);
            var assetType = App.AssetTypes[App.AssetNames[assetIndex]];
            if (_selectedAssetType != assetType)
            {
                _selectedAssetType = assetType;
                assetNames.Clear();
                var temp = assetType;
                while (temp != typeof(object))
                {
                    assetNames.Add(temp.FullName);
                    temp = temp.BaseType;
                }
            }

        
            if(tag== Tag.Asset)
            {
            scroll = GUILayout.BeginScrollView(scroll);
            for (int i = 0; i < Prefs.data.nodes.Count; i++)
            {
                var node = Prefs.data.nodes[i];
                if (!CanAttachToSelectedAsset(node.attach)) continue;
                node.color = EditorGUILayout.ColorField(
                    EditorEX.GetTypeName(node.GetRealType()), node.color);
            }
            GUILayout.EndScrollView();

            }
            else
            {
            scroll2 = GUILayout.BeginScrollView(scroll2);
            foreach (var node in Prefs.data.other)
                node.color = EditorGUILayout.ColorField(EditorEX.GetTypeName(node.GetRealType()), node.color);
            GUILayout.EndScrollView();

            }




            //if (EditorGUI.EndChangeCheck())
            //{

            //    //if (window == null)
            //    //    window = App.window;
            //    App.UpdateGraphColor();
            //    App.window.Repaint();
            //}
        }

        //private static GraphWindow window;
        private bool CanAttachToSelectedAsset(System.Collections.Generic.List<string> attach)
        {
            if (attach == null) return false;
            for (int i = 0; i < attach.Count; i++)
            {
                for (int j = 0; j < assetNames.Count; j++)
                {
                    if (attach[i] == assetNames[j]) return true;
                }
            }
            return false;
        }

        private Vector2 scroll;
        private Vector2 scroll2;
        private enum Tag { 
        Asset,
        Port,
        
        }
        private Tag tag
            ;

        private int assetIndex;
        private Type _selectedAssetType;
    }

}
