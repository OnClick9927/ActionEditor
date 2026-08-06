using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(DisableInPlayModeAttribute))]
    internal sealed class DisableInPlayModeAttributeDrawer : ActionPropertyDrawer { }
}
