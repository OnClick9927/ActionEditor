using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    internal sealed class HideIfAttributeDrawer : ActionPropertyDrawer { }
}
