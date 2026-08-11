using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PathAttribute))]
    internal sealed class PathAttributeDrawer : ActionPropertyDrawer { }
}
