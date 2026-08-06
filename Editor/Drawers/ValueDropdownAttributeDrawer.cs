using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ValueDropdownAttribute))]
    internal sealed class ValueDropdownAttributeDrawer : ActionPropertyDrawer { }
}
