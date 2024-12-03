using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public class PreferencesWindow : PopupWindowContent
    {
        private static Rect _myRect;
        private bool firstPass = true;

        public static void Show(Rect rect)
        {
            _myRect = rect;
            PopupWindow.Show(new Rect(rect.x, rect.y, 0, 0), new PreferencesWindow());
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(_myRect.width, _myRect.height);
        }

        public override void OnGUI(Rect rect)
        {
            GUILayout.BeginVertical("box");
            GUI.color = new Color(0, 0, 0, 0.3f);

            GUILayout.BeginHorizontal();
            GUI.color = Color.white;
            GUILayout.Label($"<size=22><b>{Lan.ins.PreferencesTitle}</b></size>");
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            GUILayout.BeginVertical("box");
            var lan = EditorTools.CleanPopup<string>("Language", Lan.Language,
                Lan.AllLanguages.Keys.ToList());
            if (lan != Lan.Language)
            {
                Lan.SetLanguage(lan);
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical("box");
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

            GUILayout.EndVertical();

            //GUILayout.BeginVertical("box");
            Prefs.MagnetSnapping =
                EditorGUILayout.Toggle(new GUIContent(Lan.ins.PreferencesMagnetSnapping, Lan.ins.PreferencesMagnetSnappingTips),
                    Prefs.MagnetSnapping);
            //Prefs.scrollWheelZooms =
            //    EditorGUILayout.Toggle(
            //        new GUIContent(Lan.ins.PreferencesScrollWheelZooms, Lan.ins.PreferencesScrollWheelZoomsTips),
            //        Prefs.scrollWheelZooms);
            //GUILayout.EndVertical();

            GUILayout.BeginVertical("box");

            //Prefs.savePath = EditorTools.GUILayoutGetFolderPath(Lan.ins.PreferencesSavePath, Lan.ins.PreferencesSavePathTips,
            //    Prefs.savePath, true);

            Prefs.autoSaveSeconds = EditorGUILayout.IntSlider(
                new GUIContent(Lan.ins.PreferencesAutoSaveTime, Lan.ins.PreferencesAutoSaveTimeTips), Prefs.autoSaveSeconds, 5,
                120);
            GUILayout.EndVertical();


            GUILayout.EndVertical();

            //DrawHelpButton();

            if (firstPass || Event.current.type == EventType.Repaint)
            {
                firstPass = false;
                _myRect.height = GUILayoutUtility.GetLastRect().yMax + 5;
            }
        }

        //protected virtual void DrawHelpButton()
        //{
        //    if (GUILayout.Button(Lan.ins.PreferencesHelpDoc))
        //    {
        //        Application.OpenURL("https://nobug.cn/book/414447506088261");
        //    }
        //}
    }
}