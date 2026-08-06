using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PlaceholderAttribute))]
    internal sealed class PlaceholderAttributeDrawer : ActionPropertyDrawer { }
}
