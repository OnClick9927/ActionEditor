using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    class TimelineView : ViewBase
    {
        private SplitterView _splitterView;

        private TimelineHeaderView _headerView;
        private TimelineMiddleView _middleView;
        private TimelinePointerView _pointerView;
        private TimelineBottomView _bottomView;
        private TimelineBottomView_custom _Custom_footer;
        private SplitterView _splitter_inspector;
        private InspectorView _inspector;

        public Asset asset => AppInternal.AssetData;

        private Rect _pointerRect;

        protected override void OnInit()
        {
            _splitterView = Window.CreateView<SplitterView>();

            _headerView = Window.CreateView<TimelineHeaderView>();
            _middleView = Window.CreateView<TimelineMiddleView>();
            _pointerView = Window.CreateView<TimelinePointerView>();
            _splitter_inspector = Window.CreateView<SplitterView>();
            _inspector = Window.CreateView<InspectorView>();
            _bottomView = Window.CreateView<TimelineBottomView>();
            _Custom_footer = Window.CreateView<TimelineBottomView_custom>();
            Prefs.SnapInterval = 0.01f;
        }
        static float _inspector_width = 380;
        static float TimelineRightWidth;
        public override void OnDraw()
        {
            var leftWidth = Styles.TimelineLeftWidth;
            var spit_rect = new Rect(0, Styles.PlayControlHeight, Position.width, Position.height - Styles.PlayControlHeight);

            leftWidth = _splitterView.OnSplit(spit_rect, leftWidth);
            if (!leftWidth.Equals(Styles.TimelineLeftWidth))
                Styles.TimelineLeftWidth = leftWidth;
            spit_rect.y = Position.y;
            spit_rect.height = Position.height;

            _inspector_width = Position.width - _splitter_inspector.OnSplit(spit_rect, Position.width - _inspector_width);

            var leftOffset = Styles.TimelineLeftWidth + Styles.SplitterWidth +
                Styles.RightGapWidth;
            TimelineRightWidth = Mathf.Max(1,
                Position.width - leftOffset - _inspector_width);
            AppInternal.Width = TimelineRightWidth;
            _pointerRect = new Rect(leftOffset, Styles.HeaderHeight,
                TimelineRightWidth, Position.height - 5 - Styles.HeaderHeight -
                Styles.BottomHeight);

            var bottom_rect = new Rect(Position.x,
                Position.yMax - Styles.BottomHeight,
                Position.width - _inspector_width,
                Styles.BottomHeight);
            GUI.Box(bottom_rect, "", EditorStyles.helpBox);


            if (asset != null && Event.current.type == EventType.Layout)
                asset.Validate();


            var headRect = new Rect(0, 0, Position.width - _inspector_width, Styles.PlayControlHeight);
            GUILayout.BeginArea(headRect);
            _headerView.OnGUI(new Rect(0, 0, headRect.width, headRect.height));
            GUILayout.EndArea();


            //_width = Mathf.Min(_width, Position.width - 220);


            //return;
            DoZoomAndPan();
            ItemDragger.OnCheck();

            var middleRect = new Rect(0, Styles.PlayControlHeight, Position.width - _inspector_width,
                Position.height - Styles.PlayControlHeight - Styles.BottomHeight);

            //groups and tracks
            GUILayout.BeginArea(middleRect);
            _middleView.OnGUI(new Rect(middleRect.x, middleRect.y - Styles.PlayControlHeight, middleRect.width,
                middleRect.height));
            GUILayout.EndArea();

            var inspector_rect = new Rect(Position.width - _inspector_width, Position.y, _inspector_width, Position.height);
            //inspector_rect.width -= 10;
            //inspector_rect.height -= 10;
            //inspector_rect.x += 5;
            //inspector_rect.y += 5;


            GUILayout.BeginArea(inspector_rect, EditorStyles.helpBox);
            inspector_rect.position = Vector2.zero;
            _inspector.OnGUI(inspector_rect);
            GUILayout.EndArea();

            var pointerRect = _pointerRect;
            GUILayout.BeginArea(pointerRect);
            _pointerView.OnGUI(new Rect(0, 0, pointerRect.width, pointerRect.height));
            GUILayout.EndArea();


            var bottom_rect_right = new Rect(Position.x + leftOffset,
    Position.yMax - Styles.BottomHeight,
    Position.width - _inspector_width - leftOffset,
    Styles.BottomHeight);
            GUILayout.BeginArea(bottom_rect_right);

            _Custom_footer.OnGUI(new Rect(0, 0, bottom_rect_right.width, bottom_rect_right.height));
            GUILayout.EndArea();

            var bottom_rect_left = new Rect(Position.x, Position.yMax - Styles.BottomHeight,
                   leftOffset, Styles.BottomHeight);
            GUILayout.BeginArea(bottom_rect_left);

            _bottomView.OnGUI(new Rect(0, 0, bottom_rect_left.width, bottom_rect_left.height));
            GUILayout.EndArea();

        }

        #region Zoom & Pan

        private bool _isMouseButton2Down;
        private float _lastZoomX;
        private const float MinViewDuration = 0.25f;
        private const float MaxViewDuration = 240f;

        public void DoZoomAndPan()
        {
            var e = Event.current;
            if (asset == null)
            {
                _isMouseButton2Down = false;
                return;
            }

            if (_isMouseButton2Down && e.type == EventType.MouseLeaveWindow)
            {
                _isMouseButton2Down = false;
                Window.Repaint();
                return;
            }

            if (_isMouseButton2Down && e.button == 2 &&
                e.rawType == EventType.MouseUp)
            {
                _isMouseButton2Down = false;
                Window.Repaint();
                e.Use();
                return;
            }

            bool containsPointer = _pointerRect.Contains(e.mousePosition);
            if (!_isMouseButton2Down && !containsPointer) return;

            // var ev = Event.current;
            // if (ev.button == 2)
            // {
            //     Debug.LogError("修改拖动光标===22=");
            //     EditorGUIUtility.AddCursorRect(new Rect(_pointerRect), MouseCursor.Zoom);
            //     // ev.Use();
            // }

            if (e.button == 2 && e.type == EventType.MouseDown)
            {
                _isMouseButton2Down = true;
                _lastZoomX = e.mousePosition.x;
                Window.Repaint();
                e.Use();
                return;
            }

            if (containsPointer && e.type == EventType.ScrollWheel)
            {
                float viewDuration = asset.ViewTime();
                float zoom = Mathf.Exp(e.delta.y * 0.04f);
                float nextDuration = Mathf.Clamp(viewDuration * zoom,
                    MinViewDuration, MaxViewDuration);
                float pointerRatio = Mathf.Clamp01(
                    (e.mousePosition.x - _pointerRect.x) /
                    Mathf.Max(1, _pointerRect.width));
                float pointerTime = Mathf.Lerp(asset.ViewTimeMin,
                    asset.ViewTimeMax, pointerRatio);
                float nextMin = pointerTime - nextDuration * pointerRatio;
                float nextMax = nextMin + nextDuration;
                if (nextMin < 0)
                {
                    nextMax -= nextMin;
                    nextMin = 0;
                }
                SetViewRange(nextMin, nextMax);

                Window.Repaint();
                e.Use();
                return;
            }

            if (_isMouseButton2Down)
            {
                var rect = new Rect(_pointerRect);
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);

                if (e.button == 2 && e.type == EventType.MouseDrag)
                {
                    float offset = e.mousePosition.x - _lastZoomX;
                    float duration = asset.ViewTime();
                    float timeOffset = -offset /
                        Mathf.Max(1, _pointerRect.width) * duration;
                    float nextMin = Mathf.Max(0,
                        asset.ViewTimeMin + timeOffset);
                    SetViewRange(nextMin, nextMin + duration);
                    _lastZoomX = e.mousePosition.x;

                    e.Use();
                    Window.Repaint();
                }
            }
        }

        private void SetViewRange(float min, float max)
        {
            if (min <= asset.ViewTimeMin)
            {
                asset.ViewTimeMin = min;
                asset.ViewTimeMax = max;
            }
            else
            {
                asset.ViewTimeMax = max;
                asset.ViewTimeMin = min;
            }
        }

        #endregion
    }
}
