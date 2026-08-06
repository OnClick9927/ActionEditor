using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(AnimatorParamAttribute))]
    internal sealed class AnimatorParamAttributeDrawer : ActionPropertyDrawer { }
}
