using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ParentGameObjectsOnlyAttribute))]
    internal sealed class ParentGameObjectsOnlyAttributeDrawer : ActionPropertyDrawer { }
}
