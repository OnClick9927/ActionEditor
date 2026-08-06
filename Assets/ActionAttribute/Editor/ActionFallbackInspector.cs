using UnityEditor;

namespace ActionAttribute
{
    [CustomEditor(typeof(UnityEngine.Object), true, isFallback = true)]
    internal sealed class ActionFallbackInspector : Editor
    {
        private readonly ActionInspectorRenderer renderer =
            new ActionInspectorRenderer();

        private void OnEnable() => renderer.SetTarget(target);
        private void OnDisable() => renderer.Dispose();

        public override void OnInspectorGUI()
        {
            renderer.DrawRoot(serializedObject, target);
        }
    }
}
