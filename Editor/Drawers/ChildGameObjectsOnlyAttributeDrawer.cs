using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ChildGameObjectsOnlyAttribute))]
    internal sealed class ChildGameObjectsOnlyAttributeDrawer : ActionPropertyDrawer { }
}
