using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(SceneObjectsOnlyAttribute))]
    internal sealed class SceneObjectsOnlyAttributeDrawer : ActionPropertyDrawer { }
}
