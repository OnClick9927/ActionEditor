using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(RequiredListLengthAttribute))]
    internal sealed class RequiredListLengthAttributeDrawer : ActionPropertyDrawer { }
}
