using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class TimelineHeaderView : ViewBase
    {
        private GUIStyle _customToolbarButtonStyle;

        public override void OnDraw()
        {
            GUI.enabled = App.AssetData != null;
            DrawPlayControl();
            GUI.enabled = true;

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
                App.StepBackward();
            }

            EditorGUI.BeginChangeCheck();

            if (App.IsPlay)
                GUI.backgroundColor = Color.blue + Color.cyan;
            var isPlaying = DrawToggle(App.IsPlay, EditorGUIUtility.TrIconContent("d_Animation.Play"));
            GUI.backgroundColor = Color.white;


            if (EditorGUI.EndChangeCheck())
            {
                if (isPlaying)
                {
                    App.Pause(false);
                    App.Play();
                }
                else
                {
                    App.Stop();
                }
            }
            EditorGUI.BeginChangeCheck();
            if (App.IsPause)
                GUI.backgroundColor = Color.blue + Color.cyan;
            var isPause = DrawToggle(App.IsPause, EditorGUIUtility.TrIconContent("d_PauseButton"));
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                App.Pause(isPause);
            }

            if (DrawButton(EditorGUIUtility.TrIconContent("d_Animation.NextKey")))
            {
                App.StepForward();
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

        private Rect _headerRect;

        private void DrawPlayHeader()
        {
            var gap = Styles.TimelineLeftWidth + Styles.SplitterWidth;
            _headerRect = new Rect(Position.x + gap, Position.y,
                Position.width - gap,
                Position.height - Styles.HeaderHeight);

            GUILayout.BeginArea(_headerRect);
            OnHearderGUI();
            GUILayout.EndArea();


            GUI.color = Color.black.WithAlpha(0.2f);
            GUI.DrawTexture(new Rect(_headerRect.x, _headerRect.y + _headerRect.height, _headerRect.width, 1),
                Styles.WhiteTexture);
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
                var gName = App.TextAsset != null ? App.TextAsset.name : "None";
                var size = GUI.skin.label.CalcSize(new GUIContent(gName));
                var width = size.x + 8;
                if (width < 80) width = 80;
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width + 20));
                if (GUI.Button(rect, $"[{gName}]", EditorStyles.toolbarDropDown))
                {
                    App.AutoSave();

                    AssetPick.ShowObjectPicker(rect, "Assets", "t:TextAsset", (o) =>
                    {
                        App.OnObjectPickerConfig(o);
                        GUIUtility.ExitGUI();
                    }, (x) =>
                    {
                        return x.EndsWith(Asset.FileEx);

                    });
                }
            }


            GUILayout.Space(10);
            GUI.color = Color.cyan + Color.blue;
            if (App.SelectCount != 0)
                if (GUILayout.Button(Lan.ins.ClearSelect, EditorStyles.toolbarButton))
                    App.Select();
            GUILayout.Space(2);
            if (App.CopyAsset != null)
                if (GUILayout.Button(Lan.ins.ClearCopy, EditorStyles.toolbarButton))
                    App.SetCopyAsset(null, false);
            GUI.color = Color.white;

            //DrawAssetsHeader();


            GUILayout.FlexibleSpace();






            if (App.AssetData != null)
            {
                GUILayout.Label(
string.Format(Lan.ins.HeaderLastSaveTime, App.LastSaveTime.ToString("HH:mm:ss")));

                if (GUILayout.Button(new GUIContent(EditorGUIUtility.TrIconContent("SaveActive")), EditorStyles.toolbarButton,
                        GUILayout.Width(26)))
                {
                    App.AutoSave(); //先保存当前的
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