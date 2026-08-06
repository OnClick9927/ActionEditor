using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(InlineButtonAttribute))]
    internal sealed class InlineButtonAttributeDrawer : ActionPropertyDrawer { }
}
