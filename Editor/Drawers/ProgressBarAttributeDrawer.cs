using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ProgressBarAttribute))]
    internal sealed class ProgressBarAttributeDrawer : ActionPropertyDrawer { }
}
