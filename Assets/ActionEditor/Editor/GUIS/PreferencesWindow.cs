using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class PreferencesWindow : PopupWindowContent
    {
        //private static Rect _myRect;
        //private bool firstPass = true;

        public static void Show(Rect rect)
        {
            rect.x = rect.x - 300 + rect.width;
            //_myRect = rect;
            PopupWindow.Show(rect, new PreferencesWindow());
        }

        public override Vector2 GetWindowSize() => new Vector2(300, 150);

        public override void OnGUI(Rect rect)
        {
    

            GUILayout.Label($"<size=22><b>{Lan.ins.PreferencesTitle}</b></size>", new GUIStyle(EditorStyles.label)
            {
                richText = true,
            });
            GUILayout.Space(2);

            var lan = EditorTools.CleanPopup<string>("Language", Lan.Language,
                Lan.AllLanguages.Keys.ToList());
            if (lan != Lan.Language)
            {
                Lan.SetLanguage(lan);
            }


            Prefs.timeStepMode =
                (Prefs.TimeStepMode)EditorGUILayout.EnumPopup(Lan.ins.PreferencesTimeStepMode, Prefs.timeStepMode);
            if (Prefs.timeStepMode == Prefs.TimeStepMode.Seconds)
            {
                Prefs.SnapInterval = EditorTools.CleanPopup<float>(Lan.ins.PreferencesSnapInterval, Prefs.SnapInterval,
                    Prefs.snapIntervals.ToList());
            }
            else
            {
                Prefs.FrameRate = EditorTools.CleanPopup<int>(Lan.ins.PreferencesFrameRate, Prefs.FrameRate,
                    Prefs.frameRates.ToList());
            }


            Prefs.MagnetSnapping =
                EditorGUILayout.Toggle(new GUIContent(Lan.ins.PreferencesMagnetSnapping, Lan.ins.PreferencesMagnetSnappingTips),
                    Prefs.MagnetSnapping);





            Prefs.autoSaveSeconds = EditorGUILayout.IntSlider(
                new GUIContent(Lan.ins.PreferencesAutoSaveTime, Lan.ins.PreferencesAutoSaveTimeTips), Prefs.autoSaveSeconds, 5,
                120);





        }


    }
}