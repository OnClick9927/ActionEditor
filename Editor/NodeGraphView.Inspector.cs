namespace ActionEditor.Nodes
{
    public abstract partial class NodeGraphView
    {
        internal void DrawInspectorPanel()
        {
            if (!OnDrawInspector()) DrawInspector();
        }

        protected virtual bool OnDrawInspector()
        {
            return false;
        }

        protected void RepaintEditorWindow()
        {
            App.window?.Repaint();
        }
    }
}
