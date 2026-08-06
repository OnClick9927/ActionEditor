using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PreviewFieldAttribute))]
    internal sealed class PreviewFieldAttributeDrawer : ActionPropertyDrawer { }
}
