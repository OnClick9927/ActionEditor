using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PrefixLabelAttribute))]
    internal sealed class PrefixLabelAttributeDrawer : ActionPropertyDrawer { }
}
