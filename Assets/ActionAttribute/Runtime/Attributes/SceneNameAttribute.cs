using System;

namespace ActionAttribute
{
    /// <summary>将字符串字段绘制为 Build Settings 中的场景名称选择器。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class SceneNameAttribute : ActionAttributeBase
    {
        public readonly bool includeDisabled;

        public SceneNameAttribute(bool includeDisabled = false)
        {
            this.includeDisabled = includeDisabled;
        }
    }
}
