using ActionEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("特效轨道")]
    [TrackIcon(typeof(ParticleSystem))]
    [Color(0f, 1f, 1f)]
    public class EffectTrack : Track
    {
        [Name("轨道层")] 
        public int Layer;
    }
}