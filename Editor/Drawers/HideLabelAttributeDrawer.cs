using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(HideLabelAttribute))]
    internal sealed class HideLabelAttributeDrawer : ActionPropertyDrawer { }
}
