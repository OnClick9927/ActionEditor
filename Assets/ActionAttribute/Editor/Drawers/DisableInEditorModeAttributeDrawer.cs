using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(DisableInEditorModeAttribute))]
    internal sealed class DisableInEditorModeAttributeDrawer : ActionPropertyDrawer { }
}
