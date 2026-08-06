using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ReorderableListAttribute))]
    internal sealed class ReorderableListAttributeDrawer : ActionPropertyDrawer { }
}
