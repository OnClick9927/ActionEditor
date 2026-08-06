using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ValidateInputAttribute))]
    internal sealed class ValidateInputAttributeDrawer : ActionPropertyDrawer { }
}
