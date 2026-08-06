using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(EulerAnglesAttribute))]
    internal sealed class EulerAnglesAttributeDrawer : ActionPropertyDrawer { }
}
