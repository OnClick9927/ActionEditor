using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ConditionAttribute), true)]
    internal sealed class ConditionAttributeDrawer : ActionPropertyDrawer { }
}
