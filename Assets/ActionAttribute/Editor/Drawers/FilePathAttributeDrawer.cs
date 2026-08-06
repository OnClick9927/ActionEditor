using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(FilePathAttribute))]
    internal sealed class FilePathAttributeDrawer : ActionPropertyDrawer { }
}
