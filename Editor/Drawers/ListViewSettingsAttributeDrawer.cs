using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ListViewSettingsAttribute))]
    internal sealed class ListViewSettingsAttributeDrawer : ActionPropertyDrawer { }
}
