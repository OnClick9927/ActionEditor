using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(SearchableAttribute))]
    internal sealed class SearchableAttributeDrawer : ActionPropertyDrawer { }
}
