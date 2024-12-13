using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    public interface IDragEndHandler { void OnDragEnd(PointerEventData eventData); }
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData ev); }
    public interface IPointerDownHandler { void OnPointerDown(PointerEventData ev); }
    public interface IPointerDragHandler { void OnPointerDrag(PointerEventData eventData); }
    public interface IDragBeginHandler { void OnDragBegin(PointerEventData eventData); }
    public interface IPointerUpHandler { void OnPointerUp(PointerEventData ev); }
    public class PointerEventData
    {
        public Event _event;


        public Vector2 MousePosition => _event != null ? _event.mousePosition : Vector2.zero;

        public PointerEventData()
        {
        }

        public PointerEventData(Event ev)
        {
            SetEvent(ev);
        }

        public void SetEvent(Event ev) => _event = ev;
        public void StopPropagation() => _event.Use();

        public bool IsRight() => _event.button == 1;

        public bool IsMiddle() => _event.button == 2;

        public bool IsLeft() => _event.button == 0;
    }


    public abstract class ViewBase
    {
        protected ActionEditorWindow Window;

        public Rect Position;



        public void Init(ActionEditorWindow window)
        {
            Window = window;
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