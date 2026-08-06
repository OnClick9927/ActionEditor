using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    internal sealed class DisableIfAttributeDrawer : ActionPropertyDrawer { }
}
