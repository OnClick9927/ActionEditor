using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ActionAttributeBase), true)]
    internal sealed class ActionAttributeDrawer : ActionPropertyDrawer { }
}
