using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(AssetGuidAttribute))]
    internal sealed class AssetGuidAttributeDrawer : ActionPropertyDrawer { }
}
