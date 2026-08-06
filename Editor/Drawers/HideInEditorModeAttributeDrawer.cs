using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(HideInEditorModeAttribute))]
    internal sealed class HideInEditorModeAttributeDrawer : ActionPropertyDrawer { }
}
