﻿using ActionEditor;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("动画片段")]
    [Color(0.48f, 0.71f, 0.84f)]
    [Attachable(typeof(AnimationTrack))]
    public class PlayAnimation : Clip, ISubClipContainable
    {
        [SerializeField] [HideInInspector] private float blendIn = 0.25f;
        [SerializeField] [HideInInspector] private float blendOut = 0.25f;

        [Name("播放音频")] [ObjectPath(typeof(AnimationClip))]
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

        [Range(0.1f, 10f)] [Name("播放速度")] public float playbackSpeed = 1;
        [Name("偏移量")] public float clipOffset;


        public override float Length
        {
            get => length;
            set => length = value;
        }

        public override float BlendIn
        {
            get => blendIn;
            set => blendIn = value;
        }

        public override float BlendOut
        {
            get => blendOut;
            set => blendOut = value;
        }


        float ISubClipContainable.SubClipOffset
        {
            get => clipOffset;
            set => clipOffset = value;
        }

        float ISubClipContainable.SubClipLength => animationClip != null ? animationClip.length : 0;

        float ISubClipContainable.SubClipSpeed => 1;

        public override bool IsValid => animationClip != null;

        //public override string Info => IsValid ? animationClip.name : base.Info;

        public AudioTrack Track => (AudioTrack)Parent;
    }
}