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
            rect.x = rect.x - win_size.x + rect.width;
            //_myRect = rect;
            PopupWindow.Show(rect, new PreferencesWindow());
        }

        public override Vector2 GetWindowSize() => win_size;

        public override void OnGUI(Rect rect)
        {

            GUILayout.BeginHorizontal();
            GUILayout.Label(Lan.ins.PreferencesTitle, new GUIStyle(EditorStyles.label)
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
            var lan = EditorEX.CleanPopup<string>("Language", Lan.Language,
                Lan.AllLanguages.Keys.ToList());
            if (lan != Lan.Language)
            {
                Lan.SetLanguage(lan);
            }


            Prefs.timeStepMode =
                (Prefs.TimeStepMode)EditorGUILayout.EnumPopup(Lan.ins.PreferencesTimeStepMode, Prefs.timeStepMode);
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Seconds)
            {
                Prefs.SnapInterval = EditorEX.CleanPopup<float>(Lan.ins.PreferencesSnapInterval, Prefs.SnapInterval,
                    Prefs.snapIntervals.ToList());
            }
            else
            {
                Prefs.FrameRate = EditorEX.CleanPopup<int>(Lan.ins.PreferencesFrameRate, Prefs.FrameRate,
                    Prefs.frameRates.ToList());
            }


            Prefs.MagnetSnapping =
                EditorGUILayout.Toggle(new GUIContent(Lan.ins.PreferencesMagnetSnapping, Lan.ins.PreferencesMagnetSnappingTips),
                    Prefs.MagnetSnapping);








            Prefs.autoSaveSeconds = EditorGUILayout.IntSlider(
                new GUIContent(Lan.ins.PreferencesAutoSaveTime, Lan.ins.PreferencesAutoSaveTimeTips), Prefs.autoSaveSeconds, 5,
                120);
            GUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.TextField(
            new GUIContent(Lan.ins.PreferencesSavePath, Lan.ins.PreferencesSavePathTips), Prefs.savePath);
            GUI.enabled = true;
            if (GUILayout.Button(Lan.ins.Select, GUILayout.Width(50)))
            {
                CreateAssetWindow.SelectSavePath();
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.LabelField(Lan.ins.Track, EditorStyles.boldLabel);
            //EditorGUI.BeginChangeCheck();
            foreach (var item in Prefs.data.tracks)
                item.color = EditorGUILayout.ColorField(EditorEX.GetName(item.GetRealType()), item.color);

            EditorGUILayout.LabelField(Lan.ins.Clip, EditorStyles.boldLabel);
            foreach (var item in Prefs.data.clips)
                item.color = EditorGUILayout.ColorField(EditorEX.GetName(item.GetRealType()), item.color);

            GUILayout.EndScrollView();
            if (EditorGUI.EndChangeCheck())
            {

                if (window == null)
                    window = Resources.FindObjectsOfTypeAll<ActionEditor.ActionEditorWindow>().FirstOrDefault();
                window.Repaint();
            }
        }
        private ActionEditorWindow window;
        private Vector2 scroll;

    }
}