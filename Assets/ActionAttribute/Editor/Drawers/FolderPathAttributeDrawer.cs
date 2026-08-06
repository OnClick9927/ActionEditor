using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(FolderPathAttribute))]
    internal sealed class FolderPathAttributeDrawer : ActionPropertyDrawer { }
}
