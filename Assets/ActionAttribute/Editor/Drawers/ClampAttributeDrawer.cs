using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ClampAttribute))]
    internal sealed class ClampAttributeDrawer : ActionPropertyDrawer { }
}
