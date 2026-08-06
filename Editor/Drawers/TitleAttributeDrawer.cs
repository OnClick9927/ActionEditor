using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(TitleAttribute))]
    internal sealed class TitleAttributeDrawer : ActionPropertyDrawer { }
}
