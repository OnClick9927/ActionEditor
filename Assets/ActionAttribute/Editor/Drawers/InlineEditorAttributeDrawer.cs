using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(InlineEditorAttribute))]
    internal sealed class InlineEditorAttributeDrawer : ActionPropertyDrawer { }
}
