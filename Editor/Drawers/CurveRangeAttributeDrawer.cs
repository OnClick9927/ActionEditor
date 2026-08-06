using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(CurveRangeAttribute))]
    internal sealed class CurveRangeAttributeDrawer : ActionPropertyDrawer { }
}
