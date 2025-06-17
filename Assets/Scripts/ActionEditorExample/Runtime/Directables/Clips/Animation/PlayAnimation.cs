using ActionEditor;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("动画片段")]
    [Attachable(typeof(AnimationTrack))]
    public class PlayAnimation : Clip, ILengthMatchAble,IBlendAble,IResizeAble
    {
        [SerializeField][HideInInspector] private float blendIn = 0.25f;
        [SerializeField][HideInInspector] private float blendOut = 0.25f;

        [Name("播放音频")]
        [ObjectPath(typeof(AnimationClip))]
        public string resPath = "";

        private AnimationClip _animationClip;

        public AnimationClip animationClip
        {
            get
            {
                if (string.IsNullOrEmpty(resPath))
                {
                    _animationClip = null;
                    return null;
                }

                if (_animationClip == null)
                {
#if UNITY_EDITOR
                    _animationClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(resPath);
#endif
                }

                return _animationClip;
            }
        }

        [Range(0.1f, 10f)][Name("播放速度")] public float playbackSpeed = 1;
        [Name("偏移量")] public float clipOffset;


        public override float Length
        {
            get => length;
            set => length = value;
        }

        public float BlendIn
        {
            get => blendIn;
            set => blendIn = value;
        }

        public float BlendOut
        {
            get => blendOut;
            set { blendOut = value; }

        }
        public bool CanCrossBlend => true;

     

        float ILengthMatchAble.MatchAbleLength => animationClip != null ? animationClip.length : 0;


        public override bool IsValid => animationClip != null;

        //public override string Info => IsValid ? animationClip.name : base.Info;

        public AudioTrack Track => (AudioTrack)Parent;
    }
}