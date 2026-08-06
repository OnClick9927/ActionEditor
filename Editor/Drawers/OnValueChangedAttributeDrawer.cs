using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(OnValueChangedAttribute))]
    internal sealed class OnValueChangedAttributeDrawer : ActionPropertyDrawer { }
}
