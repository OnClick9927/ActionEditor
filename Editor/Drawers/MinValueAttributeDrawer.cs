using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(MinValueAttribute))]
    internal sealed class MinValueAttributeDrawer : ActionPropertyDrawer { }
}
