using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(AssetPathAttribute))]
    internal sealed class AssetPathAttributeDrawer : ActionPropertyDrawer { }
}
