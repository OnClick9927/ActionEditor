using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    internal sealed class ReadOnlyAttributeDrawer : ActionPropertyDrawer { }
}
