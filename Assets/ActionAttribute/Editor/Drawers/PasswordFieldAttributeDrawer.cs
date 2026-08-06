using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(PasswordFieldAttribute))]
    internal sealed class PasswordFieldAttributeDrawer : ActionPropertyDrawer { }
}
