using ActionEditor;
using UnityEngine;

namespace ActionEditorExample
{
    /// <summary>
    /// 音频预览
    /// </summary>
    [CustomActionView(typeof(PlayAudio))]
    public class PlayAudioPreview : ClipEditorView<PlayAudio>
    {
        private AudioSource source;

        public override void OnPreviewUpdate(float time, float previousTime)
        {
            if (source != null)
            {
                AudioSampler.Sample(source, clip.audioClip, time - clip.clipOffset, previousTime - clip.clipOffset,
                    clip.volume);
            }
        }

        public override void OnPreviewEnter()
        {
            Do();
        }

        public override void OnPreviewReverseEnter()
        {
            Do();
        }

        public override void OnPreviewExit()
        {
            Undo();
        }

        public override void OnPreviewReverse()
        {
            Undo();
        }

        void Do()
        {
            if (source == null)
            {
                source = AudioSampler.GetSource();
            }

            if (source != null)
            {
                source.clip = clip.audioClip;
            }
        }

        void Undo()
        {
            if (source != null)
            {
                source.clip = null;
                AudioSampler.RetureSource(source);
            }
        }
    }
}