using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ObjectsOnlyAttribute))]
    internal sealed class ObjectsOnlyAttributeDrawer : ActionPropertyDrawer { }
}
