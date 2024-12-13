using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public class TimelineMiddleView : ViewBase, IPointerDragHandler, IDragBeginHandler, IDragEndHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        public Asset asset => App.AssetData;


        #region Static

        private static readonly List<TimelineTrackItemView> _itemViews = new List<TimelineTrackItemView>();

        public static TimelineTrackItemView GetItem(Vector2 pos)
        {
            if (_itemViews != null)
            {
                foreach (var itemView in _itemViews)
                {
                    if (itemView.Position.Contains(pos))
                    {
                        return itemView;
                    }
                }
            }

            return default;
        }

        public static TimelineTrackItemView GetLastItem()
        {
            if (_itemViews != null && _itemViews.Count > 0)
            {
                return _itemViews.Last();
            }

            return null;
        }

        #endregion

        protected override void OnInit()
        {
            ResetViews();
        }

        public override void OnDraw()
        {
            ClipDrawer.Reset();
            DrawList();
            DrawMultiple();
        }


        #region List

        private void ResetViews()
        {
            void AddView(IDirectable data)
            {
                var view = Window.CreateView<TimelineTrackItemView>();
                view.SetData(data);
                _itemViews.Add(view);
            }

            _itemViews.Clear();
            if (asset == null) return;
            foreach (var group in asset.groups)
            {
                AddView(group);
                if (group.IsCollapsed) continue;
                foreach (var track in group.Tracks)
                {
                    AddView(track);
                }
            }
        }


        private void DrawList()
        {
            if (_itemViews.Count < 1 || App.NeedForceRefresh)
            {
                ResetViews();
            }

            // 左侧列表部分
            GUILayout.BeginVertical();
            Styles.TimelineScrollPos = EditorGUILayout.BeginScrollView(Styles.TimelineScrollPos);

            var maxHeight = _itemViews.Count * Styles.LineHeight + _itemViews.Count * Styles.Space;

            var width = Position.width;
            if (maxHeight > Position.height)
            {
                width -= GUI.skin.verticalScrollbar.fixedWidth;
            }

            for (int i = 0; i < _itemViews.Count; i++)
            {
                var y = Styles.LineHeight * i + i * Styles.Space;
                var itemRect = new Rect(0, y , width, Styles.LineHeight);
                var item = _itemViews[i];
                item.OnGUI(itemRect);
                GUILayout.Space(Styles.Space);
            }
            GUI.color = Color.white;
            GUILayout.Space(_itemViews.Count * Styles.LineHeight);
            emptyRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();
            DrawAddGroupButton(emptyRect);
            GUI.Box(emptyRect, Lan.ins.EmptyRect, new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 20,
                fontStyle = FontStyle.Bold

            });

            GUILayout.EndVertical();
        }
        private Rect emptyRect;

        private void DrawAddGroupButton(Rect rect)
        {
            GUI.enabled = asset != null;
            GUILayout.BeginHorizontal();
            GUILayout.Space(15);
            var width = Styles.TimelineLeftWidth - 30;
            if (GUI.Button(new Rect(15, rect.y, width, 24), Lan.ins.GroupAdd))
            {
                List<EditorEX.TypeMetaInfo> list = new List<EditorEX.TypeMetaInfo>();

                var ts = EditorEX.GetTypeMetaDerivedFrom(typeof(Group));
                foreach (var typeMetaInfo in ts)
                {
                    var info = typeMetaInfo;
                    if (info.type.IsAbstract) continue;
                    if (info.attachableTypes != null)
                    {
                        if (!info.attachableTypes.Contains(asset.GetType()))
                        {
                            continue;
                        }
                    }

                    list.Add(typeMetaInfo);
                }

                if (list.Count > 1)
                {
                    var menu = new GenericMenu();
                    foreach (var typeMetaInfo in list)
                    {
                        var info = typeMetaInfo;
                        menu.AddItem(new GUIContent(info.name), false,
                            () =>
                            {
                                asset.AddGroup(typeMetaInfo.type);
                                App.Refresh();
                            });
                    }

                    menu.ShowAsContext();
                }
                else
                {
                    asset.AddGroup<Group>();
                    App.Refresh();
                }
            }

            GUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        #endregion

        #region Events

        private bool _pointerDown;
        private Vector2 _pointerDownPos;

        public void OnDragBegin(PointerEventData eventData)
        {
            if (emptyRect.Contains(eventData.MousePosition) || !eventData.IsLeft()) return;
            ItemDragger.OnBeginDrag(eventData);
        }

        public void OnPointerDrag(PointerEventData eventData)
        {
            if (emptyRect.Contains(eventData.MousePosition) || !eventData.IsLeft()) return;

            ItemDragger.OnDrag(eventData);
        }

        public void OnDragEnd(PointerEventData eventData)
        {
            if (emptyRect.Contains(eventData.MousePosition) || !eventData.IsLeft()) return;

            ItemDragger.OnEndDrag(eventData);
        }

        public void OnPointerDown(PointerEventData ev)
        {
            _pointerDown = true;
            _pointerDownPos = ev.MousePosition;
            if (emptyRect.Contains(ev.MousePosition))
            {
                App.Select();
            }

        }

        public void OnPointerUp(PointerEventData ev)
        {
            _pointerDown = false;
        }

        #endregion

        #region 多选 multiple select

        private bool _preMultipleResult;
        private Rect _multipleRect;
        public static bool MutiSelecting { get; private set; }
        private void DrawMultiple()
        {
            if (asset == null) return;
            var mousePosition = Event.current.mousePosition;
            var rect = new Rect();
            var bigEnough = false;
            var start = _pointerDownPos;
            if (start.x < Styles.TimelineLeftWidth) return;
            if (Event.current.button == 1 && _pointerDown && App.SelectCount <= 1)
            {
                if ((start - mousePosition).magnitude > 10)
                {
                    bigEnough = true;
                    rect.xMin = Mathf.Max(Mathf.Min(start.x, mousePosition.x), 0);
                    rect.xMax = Mathf.Min(Mathf.Max(start.x, mousePosition.x), Position.width);
                    rect.yMin = Mathf.Min(start.y, mousePosition.y);
                    rect.yMax = Mathf.Max(start.y, mousePosition.y);
                    if (rect.x < Styles.TimelineLeftTotalWidth)
                    {
                        var offsetX = Styles.TimelineLeftTotalWidth - rect.x;
                        rect.x = Styles.TimelineLeftTotalWidth;
                        rect.width -= offsetX;
                    }

                    _multipleRect = rect;
                }
            }

            if (bigEnough)
            {
                GUI.color = Color.white.WithAlpha(0.1f);
                GUI.DrawTexture(rect, Styles.WhiteTexture);
                Vector3 topLeft = new Vector3(rect.xMin, rect.yMin, 0);
                Vector3 topRight = new Vector3(rect.xMax, rect.yMin, 0);
                Vector3 bottomRight = new Vector3(rect.xMax, rect.yMax, 0);
                Vector3 bottomLeft = new Vector3(rect.xMin, rect.yMax, 0);
                Handles.DrawAAPolyLine(2f, topLeft, topRight);
                Handles.DrawAAPolyLine(2f, topRight, bottomRight);
                Handles.DrawAAPolyLine(2f, bottomRight, bottomLeft);
                Handles.DrawAAPolyLine(2f, bottomLeft, topLeft);
                App.Repaint();
            }
            if (Event.current.type == EventType.MouseUp)
                _pointerDown = false;
            //check select clips
            if (!_pointerDown && _preMultipleResult)
            {
                var yOffset = Styles.TimelineScrollPos.y;
                var relaRect = new Rect(_multipleRect.x - Styles.TimelineLeftTotalWidth, _multipleRect.y + yOffset,
                    _multipleRect.width,
                    _multipleRect.height);
                var clips = ClipDrawer.GetClips(relaRect);
                // ClipDrawer.GetClipByTrackPosition();
                App.Select(clips.ToArray());
            }


            MutiSelecting = _preMultipleResult = bigEnough;
        }

        #endregion
    }
}