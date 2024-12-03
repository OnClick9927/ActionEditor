using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public class TimelineHeaderView : ViewBase
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

            if (DrawButton(Styles.FirstFrameIcon, Lan.ins.FirstFrame))
            {
                AssetPlayer.Inst.CurrentTime = 0;
            }

            if (DrawButton(Styles.PrevFrameIcon, Lan.ins.StepBackwardTips))
            {
                App.StepBackward();
            }

            EditorGUI.BeginChangeCheck();

            var isPlaying = DrawToggle(App.IsPlay, App.IsPlay ? Styles.StopIcon : Styles.PlayIcon,
                App.IsPlay ? Lan.ins.StopTips : Lan.ins.PlayTips);
            if (EditorGUI.EndChangeCheck())
            {
                if (isPlaying)
                {
                    App.Play();
                }
                else
                {
                    App.Stop();
                }
            }

            var isPause = DrawToggle(App.IsPause, Styles.PauseIcon, Lan.ins.PauseTips);
            if (EditorGUI.EndChangeCheck())
            {
                App.Pause(isPause);
            }

            if (DrawButton(Styles.NextFrameIcon, Lan.ins.StepForwardTips))
            {
                App.StepForward();
            }

            if (DrawButton(Styles.LastFrameIcon, Lan.ins.PlayForwardTips))
            {
                AssetPlayer.Inst.CurrentTime = AssetPlayer.Inst.Length;
            }
            //GUILayout.Button("sdsa");
            //App.IsRange = DrawToggle(App.IsRange, Styles.RangeIcon, Lan.ins.StepBackwardTips);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private bool DrawButton(Texture image, string tooltip)
        {
            return GUILayout.Button(new GUIContent(image, tooltip),
                _customToolbarButtonStyle, GUILayout.Width(_buttonWidth));
        }

        private bool DrawToggle(bool value, Texture image, string tooltip)
        {
            return GUILayout.Toggle(value, new GUIContent(image, tooltip), _customToolbarButtonStyle,
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
            CustomizedDrawTools.Draw<HeaderGUI>();
            GUILayout.EndArea();


            GUI.color = Color.black.WithAlpha(0.2f);
            GUI.DrawTexture(new Rect(_headerRect.x, _headerRect.y + _headerRect.height, _headerRect.width, 1),
                Styles.WhiteTexture);
        }

        #endregion
    }
}