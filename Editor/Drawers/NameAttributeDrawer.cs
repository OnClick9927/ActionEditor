using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(NameAttribute))]
    internal sealed class NameAttributeDrawer : ActionPropertyDrawer { }
}
