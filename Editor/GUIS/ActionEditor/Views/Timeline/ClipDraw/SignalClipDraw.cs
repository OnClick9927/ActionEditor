using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public class SignalClipDraw : BasicClipDraw
    {
        protected override void OnDraw()
        {
            //select
            //GUI.color = Select ? new Color(153 / 255f, 242 / 255f, 1) : Color.white;


            Texture2D tx = EditorGUIUtility.TrIconContent("Animation.EventMarker").image as Texture2D;
            ClipRect.width = tx.width;
            //ClipRect.height = tx.height;
            // var showRect = new Rect(ClipRect);
            ClipRect.x -= (int)(tx.width * 0.5f);
            var matrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(2, 2), ClipRect.center + new Vector2(0,0));
            GUI.Label(ClipRect, tx);
            GUI.matrix = matrix;

            //OnDrawSelect();
        }
    }
}