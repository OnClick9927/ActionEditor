using ActionEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("特效轨道")]
    [TrackIcon(typeof(ParticleSystem))]
    [Attachable(typeof(TestGroup))]

    public class EffectTrack : Track
    {
        [Name("轨道层")] 
        public int Layer;
    }
}