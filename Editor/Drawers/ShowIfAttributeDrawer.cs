using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    internal sealed class ShowIfAttributeDrawer : ActionPropertyDrawer { }
}
