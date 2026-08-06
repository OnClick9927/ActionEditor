using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(WrapAttribute))]
    internal sealed class WrapAttributeDrawer : ActionPropertyDrawer { }
}
