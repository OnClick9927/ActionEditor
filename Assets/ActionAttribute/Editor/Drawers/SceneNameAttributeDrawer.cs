using UnityEditor;

namespace ActionAttribute
{
    [CustomPropertyDrawer(typeof(SceneNameAttribute))]
    internal sealed class SceneNameAttributeDrawer : ActionPropertyDrawer { }
}
