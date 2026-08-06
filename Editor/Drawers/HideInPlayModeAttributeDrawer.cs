using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(HideInPlayModeAttribute))]
    internal sealed class HideInPlayModeAttributeDrawer : ActionPropertyDrawer { }
}
