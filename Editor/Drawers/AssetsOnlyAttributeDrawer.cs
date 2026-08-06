using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(AssetsOnlyAttribute))]
    internal sealed class AssetsOnlyAttributeDrawer : ActionPropertyDrawer { }
}
