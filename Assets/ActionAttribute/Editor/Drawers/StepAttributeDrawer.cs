using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(StepAttribute))]
    internal sealed class StepAttributeDrawer : ActionPropertyDrawer { }
}
