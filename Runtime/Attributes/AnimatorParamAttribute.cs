using System;

namespace ActionAttribute
{
    /// <summary>将字段绘制为指定 Animator 成员中的参数选择器。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AnimatorParamAttribute : ActionAttributeBase
    {
        public readonly string animatorMember;

        public AnimatorParamAttribute(string animatorMember)
        {
            this.animatorMember = animatorMember;
        }
    }
}
