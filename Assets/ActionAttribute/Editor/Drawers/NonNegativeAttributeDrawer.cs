using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(NonNegativeAttribute))]
    internal sealed class NonNegativeAttributeDrawer : ActionPropertyDrawer { }
}
