using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(ResizableTextAreaAttribute))]
    internal sealed class ResizableTextAreaAttributeDrawer : ActionPropertyDrawer { }
}
