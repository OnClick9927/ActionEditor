using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(EnumSearchAttribute))]
    internal sealed class EnumSearchAttributeDrawer : ActionPropertyDrawer { }
}
