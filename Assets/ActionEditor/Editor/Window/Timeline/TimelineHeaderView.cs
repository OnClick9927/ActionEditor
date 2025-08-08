using System.IO;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class TimelineHeaderView : ViewBase
    {
        private GUIStyle _customToolbarButtonStyle;

        public override void OnDraw()
        {
            using (new EditorGUI.DisabledScope(AppInternal.AssetData == null))
                DrawPlayControl();

            DrawPlayHeader();

        }

        #region Play control

        private float _buttonWidth = 30;

        private void DrawPlayControl()
        {
            if (_customToolbarButtonStyle == null)
            {
                _customToolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedHeight = Styles.PlayControlHeight
                };
            }

            var rect = new Rect(0, 0, Styles.TimelineLeftWidth, Styles.PlayControlHeight);
            GUILayout.BeginArea(rect);

            _buttonWidth = rect.width / 6;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            //if (DrawButton(Styles.BackIcon, Lan.ins.BackMenuTips))
            //{
            //    App.TextAsset = null;
            //    // GUILayout.EndHorizontal();
            //    // return;
            //}

            if (DrawButton(EditorGUIUtility.TrIconContent("d_Animation.FirstKey")))
            {
                AssetPlayer.Inst.CurrentTime = 0;
            }

            if (DrawButton(EditorGUIUtility.TrIconContent("d_Animation.PrevKey")))
            {
                AppInternal.StepBackward();
            }

            EditorGUI.BeginChangeCheck();

            if (AppInternal.IsPlay)
                GUI.backgroundColor = Color.blue + Color.cyan;
            var isPlaying = DrawToggle(AppInternal.IsPlay, EditorGUIUtility.TrIconContent("d_Animation.Play"));
            GUI.backgroundColor = Color.white;


            if (EditorGUI.EndChangeCheck())
            {
                if (isPlaying)
                {
                    AppInternal.Pause(false);
                    AppInternal.Play();
                }
                else
                {
                    AppInternal.Stop();
                }
            }
            EditorGUI.BeginChangeCheck();
            if (AppInternal.IsPause)
                GUI.backgroundColor = Color.blue + Color.cyan;
            var isPause = DrawToggle(AppInternal.IsPause, EditorGUIUtility.TrIconContent("d_PauseButton"));
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                AppInternal.Pause(isPause);
            }

            if (DrawButton(EditorGUIUtility.TrIconContent("d_Animation.NextKey")))
            {
                AppInternal.StepForward();
            }

            if (DrawButton(EditorGUIUtility.TrIconContent("d_Animation.LastKey")))
            {
                AssetPlayer.Inst.CurrentTime = AssetPlayer.Inst.Length;
            }
            //GUILayout.Button("sdsa");
            //App.IsRange = DrawToggle(App.IsRange, Styles.RangeIcon, Lan.ins.StepBackwardTips);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private bool DrawButton(GUIContent content)
        {
            return GUILayout.Button(content,
                _customToolbarButtonStyle, GUILayout.Width(_buttonWidth));
        }

        private bool DrawToggle(bool value, GUIContent content)
        {
            return GUILayout.Toggle(value, content, _customToolbarButtonStyle,
                GUILayout.Width(_buttonWidth));
        }

        #endregion

        #region Header


        private void DrawPlayHeader()
        {
            var gap = Styles.TimelineLeftWidth + Styles.SplitterWidth;
            var _headerRect = new Rect(Position.x + gap, Position.y,
                   Position.width - gap,
                   Position.height - Styles.HeaderHeight);

            GUILayout.BeginArea(_headerRect, EditorStyles.toolbar);
            OnHearderGUI();
            GUILayout.EndArea();


 
        }

        private void OnHearderGUI()
        {

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, EditorGUIUtility.TrIconContent("Toolbar Plus"), EditorStyles.toolbarButton))
                    CreateAssetWindow.Show(rect);
            }
            {

                var file = Path.GetFileName(AppInternal.assetPath);
                var gName = file;
                gName = gName.Replace($".{Asset.FileEx}", "");
                gName = string.IsNullOrEmpty(gName) ? "None" : gName;
                var size = GUI.skin.label.CalcSize(new GUIContent(gName));
                var width = size.x + 8;
                if (width < 80) width = 80;
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width + 20));
                if (GUI.Button(rect, $"[{gName}]", EditorStyles.toolbarDropDown))
                {
                    AppInternal.AutoSave();

                    AssetPick.ShowObjectPicker(rect, "Assets", "t:TextAsset", Prefs.pickListType, (o) =>
                    {
                        AppInternal.OnObjectPickerConfig(AssetDatabase.GetAssetPath(o));
                        GUIUtility.ExitGUI();
                    }, (x) =>
                    {
                        return x.EndsWith(Asset.FileEx);

                    });
                }
            }
            ActonEditorView.GetEditor(AppInternal.AssetData)?.OnAssetHeaderGUI();

            //var header = EditorCustomFactory.GetEditor(AppInternal.AssetData);
            //header?.OnGUI(AppInternal.AssetData);

            //DrawAssetsHeader();


            GUILayout.FlexibleSpace();






            if (AppInternal.AssetData != null)
            {
                if (GUILayout.Button(new GUIContent(EditorGUIUtility.TrIconContent("SaveActive")), EditorStyles.toolbarButton,
                        GUILayout.Width(26)))
                {
                    AppInternal.AutoSave(); //先保存当前的
                }

            }
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, EditorGUIUtility.TrIconContent("Settings"),
                        EditorStyles.toolbarButton))
                {
                    PreferencesWindow.Show(rect);
                }
            }

            GUILayout.EndHorizontal();
        }

        #endregion
    }
}