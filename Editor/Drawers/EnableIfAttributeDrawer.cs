using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(EnableIfAttribute))]
    internal sealed class EnableIfAttributeDrawer : ActionPropertyDrawer { }
}
