using System;

namespace ActionAttribute
{
    /// <summary>SceneNameAttribute 的别名，将字符串字段绘制为构建场景选择器。</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SceneAttribute : SceneNameAttribute
    {
        public SceneAttribute(bool includeDisabled = false)
            : base(includeDisabled) { }
    }
}
