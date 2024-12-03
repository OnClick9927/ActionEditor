using UnityEngine;

namespace ActionEditor
{
    public class MoveByClipTask : SkillClipBase
    {
        protected override void Begin()
        {
            Debug.Log("播放 MoveByClipTask");
        }

        protected override void End()
        {
            
        }
    }
}