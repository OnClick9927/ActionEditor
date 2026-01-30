using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class PreferencesWindow : PopupWindowContent
    {
        //private static Rect _myRect;
        //private bool firstPass = true;
        private static Vector2 win_size = new Vector2(400, 400);
        public static void Show(Rect rect)
        {

            if (window == null)
                window = Resources.FindObjectsOfTypeAll<ActionEditor.ActionEditorWindow>().FirstOrDefault();
            win_size.y = window.position.height - 20;
            rect.x = rect.x - win_size.x + rect.width;
            //_myRect = rect;
            PopupWindow.Show(rect, new PreferencesWindow());
        }

        public override Vector2 GetWindowSize() => win_size;

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

            var lan = EditorEX.CleanPopup<string>(Lan.ins.Language, Lan.Language,
                Lan.AllLanguages.Keys.ToList());



            if (lan != Lan.Language)
            {
                Lan.SetLanguage(lan);
            }
            Prefs.pickListType = (AssetPickListType)EditorGUILayout.EnumPopup(Lan.ins.AssetPickListType, Prefs.pickListType);


            Prefs.timeStepMode =
                (Prefs.TimeStepMode)EditorGUILayout.EnumPopup(Lan.ins.StepMode, Prefs.timeStepMode);
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Seconds)
            {
                Prefs.SnapInterval = EditorEX.CleanPopup<float>(Lan.ins.SnapInterval, Prefs.SnapInterval,
                    Prefs.snapIntervals.ToList());
            }
            else
            {
                Prefs.FrameRate = EditorEX.CleanPopup<int>(Lan.ins.FrameRate, Prefs.FrameRate,
                    Prefs.frameRates.ToList());
            }


            Prefs.MagnetSnapping =
                EditorGUILayout.Toggle(Lan.ins.MagnetSnapping,
                    Prefs.MagnetSnapping);








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


            if (AppInternal.AssetNames.Count == 0) return;
            scroll = GUILayout.BeginScrollView(scroll);

            assetIndex = GUILayout.Toolbar(assetIndex, AppInternal.AssetNames.ToArray());
            var temp = AppInternal.AssetTypes[AppInternal.AssetNames[assetIndex]];

            assetNames.Clear();
            while (temp != typeof(object))
            {
                assetNames.Add(temp.FullName);
                temp = temp.BaseType;
            }


            //var assetName = App.AssetTypes[App.AssetNames[assetIndex]].FullName;


            var tracks = Prefs.data.tracks.Where(x => x.attach != null && x.attach.Intersect(assetNames).Any());

            //var tracks = Prefs.data.tracks.Where(x => x.asset != null && x.asset.Contains(assetName));
            foreach (var track in tracks)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                track.color = EditorGUILayout.ColorField(EditorEX.GetTypeName(track.GetRealType()), track.color);
                GUI.Label(GUILayoutUtility.GetLastRect(), "", EditorStyles.helpBox);
                var clips = Prefs.data.clips.FindAll(x => x.attach != null && x.attach.Contains(track.type));

                foreach (var clip in clips)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(30);
                    clip.color = EditorGUILayout.ColorField(EditorEX.GetTypeName(clip.GetRealType()), clip.color);

                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
            }

            GUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {

                if (window == null)
                    window = Resources.FindObjectsOfTypeAll<ActionEditor.ActionEditorWindow>().FirstOrDefault();
                window.Repaint();
            }
        }

        private static ActionEditorWindow window;
        private Vector2 scroll;
        private int assetIndex;
        private List<string> assetNames = new List<string>();
    }
}