using ActionAttribute;
using ActionBuffer;
using System;
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
        private static GUIStyle _titleStyle;
        private static List<string> _languageNames;
        private static readonly List<float> SnapIntervalOptions =
            new List<float>(Prefs.snapIntervals);
        private static readonly List<int> FrameRateOptions =
            new List<int>(Prefs.frameRates);
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
        private static List<string> assetNames = new List<string>();

        static Dictionary<Type, List<Prefs.SerializedData.ColorPref>> assetTackMaps = new Dictionary<Type, List<Prefs.SerializedData.ColorPref>>();
        private static List<Prefs.SerializedData.ColorPref> GetAssetTrackNames(Type assetType)
        {
            if (!assetTackMaps.TryGetValue(assetType,out var result))
            {
                assetNames.Clear();
                var temp = assetType;
                while (temp != typeof(object))
                {
                    assetNames.Add(temp.FullName);
                    temp = temp.BaseType;
                }

                //var assetName = App.AssetTypes[App.AssetNames[assetIndex]].FullName;

                result = Prefs.data.tracks
                    .Where(x => x.attach != null)
                    .ToDictionary(x => x)
                    .Where(x =>
                    {
                        var attachs = x.Value.attach.Select(x => TypeHelper.GetTypeByFullName(x));
                        foreach (var item in attachs)
                        {
                            var names = EditorEX.GetTypeMetaDerivedFrom(item).SelectMany(x => x.attachableTypes.Select(x => x.Name));
                            if (names.Intersect(assetNames).Any())
                            {
                                return true;
                            }
                        }
                        return false;

                    })
                    .Select(x => x.Key)
                    .ToList();
                assetTackMaps.Add(assetType, result);
            }
            return result;
        }
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
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(2);

            var lan = EditorEX.CleanPopup<string>(Lan.ins.Language, Lan.Language,
                _languageNames);



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
                    SnapIntervalOptions);
            }
            else
            {
                Prefs.FrameRate = EditorEX.CleanPopup<int>(Lan.ins.FrameRate, Prefs.FrameRate,
                    FrameRateOptions);
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

            if (_assetTypeNames == null ||
                _assetTypeNames.Length != AppInternal.AssetNames.Count)
                _assetTypeNames = AppInternal.AssetNames.ToArray();
            assetIndex = GUILayout.Toolbar(assetIndex, _assetTypeNames);
            var temp = AppInternal.AssetTypes[AppInternal.AssetNames[assetIndex]];



            var tracks = GetAssetTrackNames(temp);
            foreach (var track in tracks)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                track.color = EditorGUILayout.ColorField(EditorEX.GetTypeName(track.GetRealType()), track.color);
                GUI.Label(GUILayoutUtility.GetLastRect(), "", EditorStyles.helpBox);
                for (int i = 0; i < Prefs.data.clips.Count; i++)
                {
                    var clip = Prefs.data.clips[i];
                    if (clip.attach == null || !clip.attach.Contains(track.type)) continue;
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
        private string[] _assetTypeNames;
        private Vector2 scroll;
        private int assetIndex;
    }
}
