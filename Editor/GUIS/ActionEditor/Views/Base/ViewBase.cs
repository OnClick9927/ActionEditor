using System;
using ActionEditor.Events;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public abstract class ViewBase
    {
        protected EditorWindow Window;

        public Rect Position;

        private bool _visible;

        public bool Visible
        {
            get => _visible;
            set { _visible = value; }
        }

        public void Init(EditorWindow window)
        {
            Window = window;
            _visible = true;
            OnInit();
        }

        public void Update()
        {
            if (Position.width <= 0 && Position.height <= 0) return;
            OnUpdate();
        }


        protected virtual void OnInit()
        {
        }


        public virtual void OnGUI(Rect rect)
        {
            Position = rect;
            OnDraw();
            CheckPointerEvent();
        }

        public abstract void OnDraw();

        protected virtual void OnUpdate()
        {
        }

        #region Event Handler

        private bool _havePointerDown;
        private bool _isDragging;
        private bool _isPointerOver;
        private static PointerEventData _eventData = new PointerEventData();
        private const float DragThreshold = 1f;
        private Vector2 _dragStartPos;

        protected void CheckPointerEvent()
        {
            var ev = Event.current;
            _eventData.SetEvent(ev);
            // Debug.Log("CheckPointerEvent");
            var hasRect = Position.Contains(_eventData._event.mousePosition);

            if (_eventData._event.type == EventType.MouseDown)
            {
                if (hasRect)
                {
                    _havePointerDown = true;
                    if (this is IPointerDownHandler pointerDownHandler)
                    {

                        pointerDownHandler.OnPointerDown(_eventData);
                    }
                }
                else
                {
                    _havePointerDown = false;
                }
            }
            else if (_eventData._event.type == EventType.MouseUp)
            {
                if (this is IPointerUpHandler pointerUpHandler)
                {

                    pointerUpHandler.OnPointerUp(_eventData);
                }

                if (_isDragging && this is IDragEndHandler dragEndHandler)
                {
                    _isDragging = false;
                    dragEndHandler.OnDragEnd(_eventData);
                }

                if (hasRect)
                {
                    if (_havePointerDown && this is IPointerClickHandler pointerClickHandler)
                    {

                        pointerClickHandler.OnPointerClick(_eventData);

                    }
                }
                else
                {
                    _havePointerDown = false;
                }
            }
            else if (_eventData._event.type == EventType.MouseDrag)
            {
                if (hasRect)
                {
                    if (!_isDragging && Vector2.Distance(_dragStartPos, _eventData._event.mousePosition) > DragThreshold)
                    {
                        _isDragging = true; // 达到阈值，开始拖动 When the threshold is reached, drag begins
                        if (this is IDragBeginHandler dragBeginHandler)
                        {
                            dragBeginHandler.OnDragBegin(_eventData);
                        }
                    }

                    if (_isDragging && this is IPointerDragHandler dragHandler)
                    {
                        dragHandler.OnPointerDrag(_eventData);
                    }
                }
            }

            // if (ev.type == EventType.MouseMove)
            // {
            //     // if (_havePointerDown)
            //     {
            //         if (this is IPointerMoveHandler pointerMoveHandler)
            //         {
            //             pointerMoveHandler.OnPointerMove(_eventData);
            //         }
            //     }
            // }




        }

        #endregion
    }
}