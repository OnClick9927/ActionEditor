using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(SliderAttribute))]
    internal sealed class SliderAttributeDrawer : ActionPropertyDrawer { }
}
