using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ShowAssetPreviewAttribute))]
    internal sealed class ShowAssetPreviewAttributeDrawer : ActionPropertyDrawer { }
}
