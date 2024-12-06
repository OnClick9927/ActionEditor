#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace ActionEditor
{
    [Name("普通粒子片段")]
    [Color(0.0f, 1f, 1f)]
    [Attachable(typeof(EffectTrack))]
    public class PlayParticle : Clip
    {
        [Space(20)]
        [HeaderAttribute("hhhh")]
        [NotNull]
        [Name("特效对象")]
        [ObjectPath(typeof(GameObject))]
        public string resPath = "";

        [Name("是否变形")] public bool scale;

        [TextArea(1,4)]
        public string test;

        private GameObject _effectObject;

        private GameObject audioClip
        {
            get
            {
                if (_effectObject == null)
                {
#if UNITY_EDITOR
                    _effectObject = AssetDatabase.LoadAssetAtPath<GameObject>(resPath);
#endif
                }

                return _effectObject;
            }
        }


        public override float Length
        {
            get => length;
            set => length = value;
        }

        public override bool IsValid => audioClip != null;


    }
}