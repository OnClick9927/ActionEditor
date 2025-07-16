using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public abstract class ClipInspector<T> : ClipInspector where T : Clip
    {
        protected T action => (T)target;
    }

    [CustomInspectorAttribute(typeof(Clip))]
    public class ClipInspector : InspectorsBase
    {
        private Clip action => (Clip)target;

        public override void OnInspectorGUI()
        {
            ShowCommonInspector();
        }

        protected void ShowCommonInspector()
        {
            using (new EditorGUI.DisabledScope(action.IsLocked))
            {
                ShowErrors();
                ShowInOutControls();
                ShowBlendingControls();
            }

            base.OnInspectorGUI();

        }

        void ShowErrors()
        {
            if (action.IsValid) return;
            EditorGUILayout.HelpBox(Lan.ins.ClipInvalid,
                MessageType.Error);
            GUILayout.Space(5);
        }

        void ShowInOutControls()
        {
            var previousClip = action.GetPreviousSibling();
            var previousTime = previousClip != null ? previousClip.EndTime : action.Parent.StartTime;
            if (previousClip.CanCrossBlend(action))
            {
                previousTime -= Mathf.Min(action.Length / 2, (previousClip.EndTime - previousClip.StartTime) / 2);
            }

            var nextClip = action.GetNextSibling();
            var nextTime = nextClip != null ? nextClip.StartTime : action.Parent.EndTime;
            if (action.CanCrossBlend(nextClip))
            {
                nextTime += Mathf.Min(action.Length / 2, (nextClip.EndTime - nextClip.StartTime) / 2);
            }

            var canScale = action.CanScale();
            var doFrames = Prefs.timeStepMode == Prefs.TimeStepMode.Frames;

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();

            var _in = action.StartTime;
            var _length = action.Length;
            var _out = action.EndTime;

            if (canScale)
            {
                GUILayout.Label("IN", GUILayout.Width(30));
                if (doFrames)
                {
                    _in *= Prefs.FrameRate;
                    _in = EditorGUILayout.DelayedIntField((int)_in, GUILayout.Width(80));
                    _in *= (1f / Prefs.FrameRate);
                }
                else
                {
                    _in = EditorGUILayout.DelayedFloatField(_in, GUILayout.Width(80));
                }

                GUILayout.FlexibleSpace();
                GUILayout.Label("◄");
                if (doFrames)
                {
                    _length *= Prefs.FrameRate;
                    _length = EditorGUILayout.DelayedIntField((int)_length, GUILayout.Width(80));
                    _length *= (1f / Prefs.FrameRate);
                }
                else
                {
                    _length = EditorGUILayout.DelayedFloatField(_length, GUILayout.Width(80));
                }

                GUILayout.Label("►");
                GUILayout.FlexibleSpace();

                GUILayout.Label("OUT", GUILayout.Width(30));
                if (doFrames)
                {
                    _out *= Prefs.FrameRate;
                    _out = EditorGUILayout.DelayedIntField((int)_out, GUILayout.Width(80));
                    _out *= (1f / Prefs.FrameRate);
                }
                else
                {
                    _out = EditorGUILayout.DelayedFloatField(_out, GUILayout.Width(80));
                }
            }

            GUILayout.EndHorizontal();

            if (canScale)
            {
                if (_in >= action.Parent.StartTime && _out <= action.Parent.EndTime)
                {
                    if (_out > _in)
                    {
                        EditorGUILayout.MinMaxSlider(ref _in, ref _out, previousTime, nextTime);
                    }
                    else
                    {
                        _in = EditorGUILayout.Slider(_in, previousTime, nextTime);
                        _out = _in;
                    }
                }
            }
            else
            {
                GUILayout.Label("IN", GUILayout.Width(30));
                _in = EditorGUILayout.Slider(_in, 0, action.Parent.EndTime);
                _out = _in;
            }


            if (GUI.changed)
            {
                if (_length != action.Length)
                {
                    _out = _in + _length;
                }

                _in = Mathf.Round(_in / Prefs.SnapInterval) * Prefs.SnapInterval;
                _out = Mathf.Round(_out / Prefs.SnapInterval) * Prefs.SnapInterval;

                _in = Mathf.Clamp(_in, previousTime, _out);
                _out = Mathf.Clamp(_out, _in, nextClip != null ? nextTime : float.PositiveInfinity);

                action.StartTime = _in;
                action.EndTime = _out;
                AppInternal.Repaint();
            }

            if (_in > action.Parent.EndTime)
            {
                EditorGUILayout.HelpBox(Lan.ins.OverflowInvalid, MessageType.Warning);
            }
            else
            {
                if (_out > action.Parent.EndTime)
                {
                    EditorGUILayout.HelpBox(Lan.ins.EndTimeOverflowInvalid, MessageType.Warning);
                }
            }

            if (_out < action.Parent.StartTime)
            {
                EditorGUILayout.HelpBox(Lan.ins.OverflowInvalid, MessageType.Warning);
            }
            else
            {
                if (_in < action.Parent.StartTime)
                {
                    EditorGUILayout.HelpBox(Lan.ins.StartTimeOverflowInvalid, MessageType.Warning);
                }
            }

            GUILayout.EndVertical();
        }

        /// <summary>
        /// 显示混合输入/输出控件
        /// </summary>
        void ShowBlendingControls()
        {
            var blend = action.AsBlendAble();
            //var canBlendIn = action.CanBlend() != null;
            //var canBlendOut = action.CanBlend() != null;
            if (blend != null && action.Length > 0)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);

                var left = blend.BlendIn;
                var right = blend.Length - blend.BlendOut;
                GUILayout.Label("Blend", EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(true))
                {

                    EditorGUILayout.MinMaxSlider(ref left, ref right, 0, blend.Length);
                    blend.SetBlendIn(left);
                    blend.SetBlendOut(blend.Length - right);
                    EditorGUILayout.FloatField(nameof(IBlendAble.BlendIn), blend.BlendIn);
                    EditorGUILayout.FloatField(nameof(IBlendAble.BlendOut), blend.BlendOut);
                }


                GUILayout.EndVertical();
            }
        }
    }
}