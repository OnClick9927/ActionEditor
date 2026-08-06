using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ExpandableAttribute))]
    internal sealed class ExpandableAttributeDrawer : ActionPropertyDrawer { }
}
