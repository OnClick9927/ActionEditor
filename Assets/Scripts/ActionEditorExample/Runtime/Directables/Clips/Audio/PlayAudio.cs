using ActionEditor;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ActionEditor
{
    [Name("声音片段")]
    [Attachable(typeof(AudioTrack))]
    public class PlayAudio : Clip, ILengthMatchAble, IBlendAble
    {
        [SerializeField][HideInInspector] private float blendIn = 0.25f;
        [SerializeField][HideInInspector] private float blendOut = 0.25f;

        [Name("播放音频")]
        [ObjectPath(typeof(AudioClip))]
        public string resPath = "";

        private AudioClip _audioClip;

        public AudioClip audioClip
        {
            get
            {
                if (string.IsNullOrEmpty(resPath))
                {
                    _audioClip = null;
                    return null;
                }

                if (_audioClip == null)
                {
#if UNITY_EDITOR
                    _audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(resPath);
#endif
                }

                return _audioClip;
            }
        }

        [Range(0f, 1f)][Name("音量")] public float volume = 1;
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
            set => blendOut = value;
        }


 

        float ILengthMatchAble.MatchAbleLength => audioClip != null ? audioClip.length : 0;


        public override bool IsValid => audioClip != null;

        //public override string Info => IsValid ? audioClip.name : base.Info;

        public AudioTrack Track => (AudioTrack)Parent;

        public bool CanCrossBlend => true;

        // #if UNITY_EDITOR
        //         protected override void OnClipGUI(Rect rect)
        //         {
        //             DrawTools.DrawLoopedAudioTexture(rect, audioClip, Length, clipOffset);
        //         }
        // #endif
    }
}