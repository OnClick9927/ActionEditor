using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(MaxLengthAttribute))]
    internal sealed class MaxLengthAttributeDrawer : ActionPropertyDrawer { }
}
