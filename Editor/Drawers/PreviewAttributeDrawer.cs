using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PreviewAttribute))]
    internal sealed class PreviewAttributeDrawer : ActionPropertyDrawer { }
}
