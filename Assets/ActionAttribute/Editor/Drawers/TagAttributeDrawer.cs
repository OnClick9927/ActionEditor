using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    internal sealed class TagAttributeDrawer : ActionPropertyDrawer { }
}
