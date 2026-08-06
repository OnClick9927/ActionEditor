using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(MaxValueAttribute))]
    internal sealed class MaxValueAttributeDrawer : ActionPropertyDrawer { }
}
