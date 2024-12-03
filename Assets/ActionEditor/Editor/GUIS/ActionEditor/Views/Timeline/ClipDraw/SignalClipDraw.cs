using UnityEngine;

namespace ActionEditor
{
    public class SignalClipDraw : BasicClipDraw
    {
        protected override void OnDraw()
        {
            //select
            //GUI.color = Select ? new Color(153 / 255f, 242 / 255f, 1) : Color.white;
            ClipRect.width = Styles.SignalIcon.width;
            ClipRect.height = Styles.SignalIcon.height;
            // var showRect = new Rect(ClipRect);
            ClipRect.x -= (int)(Styles.SignalIcon.width * 0.5f);
            GUI.DrawTexture(ClipRect, Styles.SignalIcon);
            //OnDrawSelect();
        }
    }
}