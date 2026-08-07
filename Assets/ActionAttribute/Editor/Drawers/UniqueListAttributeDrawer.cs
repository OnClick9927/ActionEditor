using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(UniqueListAttribute))]
    internal sealed class UniqueListAttributeDrawer : ActionPropertyDrawer { }
}
