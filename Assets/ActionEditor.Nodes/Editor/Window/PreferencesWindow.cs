using ActionEditor;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor.Nodes
{
    class PreferencesWindow : PopupWindowContent
    {
        private static Vector2 win_size = new Vector2(400, 400);
        public static void Show(Rect rect)
        {
            

            if (window == null)
                window = Resources.FindObjectsOfTypeAll<GraphWindow>().FirstOrDefault();
            win_size.y = window.position.height - 20;
            rect.x = rect.x - win_size.x + rect.width;
            //_myRect = rect;
            UnityEditor.PopupWindow.Show(rect, new PreferencesWindow());
        }

        public override Vector2 GetWindowSize() => win_size;
        System.Collections.Generic.List<string> assetNames = new System.Collections.Generic.List<string>();

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(Lan.ins.Preferences, new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 22
            });
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(Lan.ins.Save, GUILayout.Width(50)))
            {
                Prefs.Save();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            scroll = GUILayout.BeginScrollView(scroll);

            Prefs.pickListType = (AssetPickListType)EditorGUILayout.EnumPopup(Lan.ins.AssetPickListType, Prefs.pickListType);

            var lan = EditorEX.CleanPopup<string>(Lan.ins.Language, Lan.Language,
               Lan.AllLanguages.Keys.ToList());



            if (lan != Lan.Language)
            {
                Lan.SetLanguage(lan);
            }










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



            assetIndex = GUILayout.Toolbar(assetIndex, App.AssetNames.ToArray());
            var assetType = App.AssetTypes[App.AssetNames[assetIndex]];
            var temp = assetType;
            assetNames.Clear();
            while (temp!=typeof(object))
            {
                assetNames.Add(temp.FullName);
                temp = temp.BaseType;
            }


            //var assetName = App.AssetTypes[App.AssetNames[assetIndex]].FullName;


            var nodes = Prefs.data.nodes.Where(x => x.attach != null && x.attach.Intersect(assetNames).Any());
            foreach (var node in nodes)
                node.color = EditorGUILayout.ColorField(EditorEX.GetTypeName(node.GetRealType()), node.color);
            GUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {

                if (window == null)
                    window = Resources.FindObjectsOfTypeAll<GraphWindow>().FirstOrDefault();
                window.Repaint();
            }
        }

        private static GraphWindow window;
        private Vector2 scroll;
        private int assetIndex;
    }

}