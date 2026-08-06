using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PropertyTooltipAttribute))]
    internal sealed class PropertyTooltipAttributeDrawer : ActionPropertyDrawer { }
}
