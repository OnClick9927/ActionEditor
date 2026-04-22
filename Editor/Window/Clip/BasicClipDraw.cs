using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public abstract class ClipDrawBase
    {
        public Clip PreviousClip;
        public Clip NextClip;

        public Rect ClipRect;
        public Rect ClipRealRect;
        public Rect TrackRect;
        protected float StartPosX;
        protected float EndPosX;
        protected bool Select;
        protected bool Copy;
        protected GUIStyle NameStyle;
        protected EditorWindow Window;
        protected Clip _clip;
        private float _overlapIn;
        private float _overlapOut;

        public void Draw(EditorWindow window, Rect trackRect, Rect trackRightRect, Clip clip, bool select, bool copy)
        {
            if (NameStyle == null)
            {
                NameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold
                };
            }

            Window = window;
            _clip = clip;
            Select = select;
            Copy = copy;
            TrackRect = trackRect;
            StartPosX = clip.Root.TimeToPos(clip.StartTime, AppInternal.Width) + trackRect.x;
            EndPosX = clip.Root.TimeToPos(clip.EndTime, AppInternal.Width) + trackRect.x;

            ClipRect = new Rect(StartPosX, TrackRect.y, EndPosX - StartPosX, Styles.LineHeight);
            ClipRealRect = new Rect(StartPosX, trackRightRect.y, EndPosX - StartPosX, Styles.LineHeight);

            _overlapIn = PreviousClip != null ? Mathf.Max(PreviousClip.EndTime - _clip.StartTime, 0) : 0;
            _overlapOut = NextClip != null ? Mathf.Max(_clip.EndTime - NextClip.StartTime, 0) : 0;


            OnDraw();
            if (Select)
                OnDrawSelect();
            if (Copy)
                OnDrawCopy();



            DrawDragCursor();
            DrawValid();
            CheckBlendInAndOut();
        }
        private void OnDrawCopy()
        {
            if ((EditorApplication.timeSinceStartup) % 0.5 > 0.25)
            {
                GUI.Box(ClipRect, "");
            }
        }
        private void OnDrawSelect()
        {
            var lineHeight = 2;
            GUI.color = Color.cyan;
            var topRect = new Rect(ClipRect.x, ClipRect.y, ClipRect.width, lineHeight);
            var bottomRect = new Rect(ClipRect.x, ClipRect.y + ClipRect.height - lineHeight, ClipRect.width,
                lineHeight);
            var leftRect = new Rect(ClipRect.x, ClipRect.y, lineHeight, ClipRect.height);
            var rightRect = new Rect(ClipRect.x + ClipRect.width - lineHeight, ClipRect.y, lineHeight, ClipRect.height);
            GUI.DrawTexture(topRect, Styles.WhiteTexture);
            GUI.DrawTexture(bottomRect, Styles.WhiteTexture);
            GUI.DrawTexture(leftRect, Styles.WhiteTexture);
            GUI.DrawTexture(rightRect, Styles.WhiteTexture);
            GUI.color = Color.white;
        }
        protected virtual void OnDraw()
        {
        }


        #region DrawDrag

        protected virtual void DrawDragCursor()
        {
            if (!_clip.CanScale()) return;
            var controlRectIn = new Rect(ClipRect.x, ClipRect.y, Styles.ClipScaleRectWidth, ClipRect.height);
            var controlRectOut = new Rect(ClipRect.x + ClipRect.width - Styles.ClipScaleRectWidth, ClipRect.y,
                Styles.ClipScaleRectWidth,
                ClipRect.height);
            EditorGUIUtility.AddCursorRect(controlRectIn, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(controlRectOut, MouseCursor.ResizeHorizontal);
        }

        #endregion

        #region Draw Fun

        protected virtual void DrawBackground()
        {
            GUI.color = Styles.ClipBackColor;
            GUI.DrawTexture(ClipRect, Styles.WhiteTexture);
        }

        protected virtual void DrawBlend()
        {
            var _clip_blend = _clip.AsBlendAble();
            if (_clip_blend == null) return;

            var blendInPosX = (_clip_blend.BlendIn / _clip.Length) * ClipRect.width;
            var blendOutPosX = ((_clip.Length - _clip_blend.BlendOut) / _clip.Length) * ClipRect.width;

            if (_clip_blend.BlendIn > 0)
            {
                Handles.color = Color.black.WithAlpha(0.5f);
                Handles.DrawAAPolyLine(2, new Vector2(ClipRect.x, ClipRect.y + ClipRect.height),
                    new Vector2(ClipRect.x + blendInPosX, ClipRect.y));
                Handles.color = Styles.ClipBlendColor;
                Handles.DrawAAConvexPolygon(new Vector3(ClipRect.x, ClipRect.y),
                    new Vector3(ClipRect.x, ClipRect.y + ClipRect.height),
                    new Vector3(ClipRect.x + blendInPosX, ClipRect.y));
            }

            if (_clip_blend.BlendOut > 0 && _overlapOut == 0)
            {
                Handles.color = Color.black.WithAlpha(0.5f);
                Handles.DrawAAPolyLine(2, new Vector2(ClipRect.x + blendOutPosX, ClipRect.y),
                    new Vector2(ClipRect.x + ClipRect.width, ClipRect.y + ClipRect.height));
                Handles.color = Styles.ClipBlendColor;
                Handles.DrawAAConvexPolygon(new Vector3(ClipRect.x + ClipRect.width, ClipRect.y),
                    new Vector2(ClipRect.x + blendOutPosX, ClipRect.y),
                    new Vector2(ClipRect.x + ClipRect.width, ClipRect.y + ClipRect.height));
            }

            if (_overlapIn > 0)
            {
                Handles.color = Color.black.WithAlpha(0.4f);
                Handles.DrawAAPolyLine(2, new Vector2(ClipRect.x + blendInPosX, ClipRect.y),
                    new Vector2(ClipRect.x + blendInPosX, ClipRect.y + ClipRect.height));
            }

            Handles.color = Color.white;
        }

        protected virtual void DrawBottomColor()
        {
            GUI.color = _clip.GetColor();
            GUI.DrawTexture(
                new Rect(StartPosX, TrackRect.y + (Styles.LineHeight - Styles.ClipBottomRectHeight), ClipRect.width,
                    Styles.ClipBottomRectHeight),
                Styles.WhiteTexture);
        }

        protected virtual void DrawName()
        {
            GUI.color = Color.white;
            var text = _clip.GetTypeName();
            var size = GUI.skin.label.CalcSize(new GUIContent(text));
            if (ClipRect.width > size.x)
            {
                var showY = TrackRect.y + (ClipRect.height - Styles.ClipBottomRectHeight) * 0.5f - size.y * 0.5f;
                var showX = StartPosX + ClipRect.width * 0.5f - size.x * 0.5f;
                var stampRect = new Rect(showX, showY, size.x, size.y);
                GUI.Box(stampRect, text, NameStyle);
            }
        }

        protected virtual void DrawValid()
        {
            if (_clip.IsValid) return;
            //EditorGUIUtility.TrIconContent("console.erroricon");
            //GUI.color = Color.red.WithAlpha(0.2f);
            var rect = new Rect(ClipRect.position, new Vector2(ClipRect.height, ClipRect.height));
            GUI.Label(rect, EditorGUIUtility.TrIconContent("console.erroricon", Lan.ins.ClipInvalid));
            //GUI.DrawTexture(, EditorGUIUtility.TrIconContent("console.erroricon").image);
        }

        #endregion

        #region Nmsl

        private void CheckBlendInAndOut()
        {
            _clip.SetBlendIn(0);
            _clip.SetBlendOut(0);


            var overlap = PreviousClip != null ? Mathf.Max(PreviousClip.EndTime - _clip.StartTime, 0) : 0;
            if (overlap > 0)
            {
                _clip.SetBlendIn(overlap);
            }
            var overlap_next = NextClip != null ? Mathf.Max(_clip.EndTime - NextClip.StartTime, 0) : 0;
            if (overlap_next > 0)
            {
                _clip.SetBlendOut(overlap_next);
            }
        }

        #endregion
    }

    public class BasicClipDraw : ClipDrawBase
    {
        protected override void OnDraw()
        {
            DrawBackground();
            DrawBottomColor();
            DrawBlend();
            DrawName();


        }


    }
}