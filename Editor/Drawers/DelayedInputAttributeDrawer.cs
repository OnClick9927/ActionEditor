using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(DelayedInputAttribute))]
    internal sealed class DelayedInputAttributeDrawer : ActionPropertyDrawer { }
}
