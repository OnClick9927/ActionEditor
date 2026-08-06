using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(MultilineTextAttribute))]
    internal sealed class MultilineTextAttributeDrawer : ActionPropertyDrawer { }
}
