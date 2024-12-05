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


            Texture2D tx = EditorGUIUtility.TrIconContent("AnimationWindowEvent Icon").image as Texture2D;
            var center = ClipRect.center;
            ClipRect.width = 20;
            ClipRect.center = center;
            //ClipRect.x -= (int)(tx.width * 0.5f);
            var matrix = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(3, 1.5f), ClipRect.center);
            GUI.Label(ClipRect, tx);
            GUI.matrix = matrix;

            //OnDrawSelect();
        }
    }
}