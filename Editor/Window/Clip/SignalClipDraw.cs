using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public class SignalClipDraw : BasicClipDraw
    {
        protected override void OnDraw()
        {





            var center = ClipRect.center;
            ClipRect.width = 20;
            ClipRect.center = center;
            GUI.color = _clip.GetColor();
            //var matrix = GUI.matrix;

            //GUI.DrawTexture(ClipRect, 
            //    EditorGUIUtility.TrIconContent("AnimationWindowEvent Icon").image,
            //    ScaleMode.StretchToFill,false,1,_clip.GetColor(),100,100);
            //GUIUtility.ScaleAroundPivot(new Vector2(3, 1.5f), ClipRect.center);
            EditorGUI.LabelField(ClipRect, EditorGUIUtility.TrIconContent("AnimationWindowEvent Icon", _clip.GetTypeName()));
            //GUI.matrix = matrix;
            GUI.color = Color.white;

            center = ClipRealRect.center;
            ClipRealRect.width = 20;
            ClipRealRect.center = center;


        }
    }
}