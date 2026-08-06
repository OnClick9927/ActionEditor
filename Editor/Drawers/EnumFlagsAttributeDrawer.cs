using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(EnumFlagsAttribute))]
    internal sealed class EnumFlagsAttributeDrawer : ActionPropertyDrawer { }
}
