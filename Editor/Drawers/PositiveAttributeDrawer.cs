using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PositiveAttribute))]
    internal sealed class PositiveAttributeDrawer : ActionPropertyDrawer { }
}
