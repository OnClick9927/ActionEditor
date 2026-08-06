using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    internal sealed class MinMaxSliderAttributeDrawer : ActionPropertyDrawer { }
}
