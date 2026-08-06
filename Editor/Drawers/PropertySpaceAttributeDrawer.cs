using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PropertySpaceAttribute))]
    internal sealed class PropertySpaceAttributeDrawer : ActionPropertyDrawer { }
}
