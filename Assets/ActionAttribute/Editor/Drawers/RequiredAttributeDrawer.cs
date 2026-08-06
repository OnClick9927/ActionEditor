using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(RequiredAttribute))]
    internal sealed class RequiredAttributeDrawer : ActionPropertyDrawer { }
}
