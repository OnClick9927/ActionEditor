using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(DropdownAttribute))]
    internal sealed class DropdownAttributeDrawer : ActionPropertyDrawer { }
}
