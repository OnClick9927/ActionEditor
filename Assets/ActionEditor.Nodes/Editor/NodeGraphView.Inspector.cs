using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{
    public abstract partial class NodeGraphView
    {
        private static GUIContent _miniMapToolbarContent;

        internal void DrawInspectorPanel()
        {
            EditorGUI.BeginChangeCheck();
            if (!OnDrawInspector()) DrawInspector();
            if (EditorGUI.EndChangeCheck())
                App.RequestInspectorUndoCommit("Edit Graph Inspector");
        }

        protected virtual bool OnDrawInspector()
        {
            return false;
        }

        protected void RepaintEditorWindow()
        {
            App.window?.Repaint();
        }

        internal void DrawHeaderToolbar()
        {
            if (_miniMapToolbarContent == null)
                _miniMapToolbarContent = EditorGUIUtility.TrIconContent(
                    "d_UnityEditor.SceneView", "Mini Map");

            App.window.showMiniMap = GUILayout.Toggle(App.window.showMiniMap,
                _miniMapToolbarContent, EditorStyles.toolbarButton, GUILayout.Width(25));
            minimap.visible = App.window.showMiniMap;
            OnHeaderToolsGUI();
        }

        protected virtual void OnHeaderToolsGUI()
        {
        }

        protected void UpdateConnectionFlows()
        {
            foreach (var edge in base.edges)
            {
                if (edge is GraphConnection connection)
                    connection.UpdateFlow();
            }
        }

        internal void UpdateNodeViews()
        {
            foreach (var node in base.nodes)
            {
                if (node is GraphNode graphNode)
                    graphNode.OnUpdate();
            }
        }

        internal void InitializeUndoTracking()
        {
            graphViewChanged -= TrackGraphUndo;
            graphViewChanged += TrackGraphUndo;
            elementsAddedToGroup -= TrackGroupElementsAdded;
            elementsAddedToGroup += TrackGroupElementsAdded;
            elementsRemovedFromGroup -= TrackGroupElementsRemoved;
            elementsRemovedFromGroup += TrackGroupElementsRemoved;
        }

        private GraphViewChange TrackGraphUndo(GraphViewChange change)
        {
            App.RequestUndoCommit("Edit Graph");
            return change;
        }

        private static void TrackGroupElementsAdded(UnityEditor.Experimental.GraphView.Group group,
            System.Collections.Generic.IEnumerable<GraphElement> elements)
        {
            App.RequestUndoCommit("Add Nodes To Group");
        }

        private static void TrackGroupElementsRemoved(UnityEditor.Experimental.GraphView.Group group,
            System.Collections.Generic.IEnumerable<GraphElement> elements)
        {
            App.RequestUndoCommit("Remove Nodes From Group");
        }

    }
}
