using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{
    public class GraphGroup : UnityEditor.Experimental.GraphView.Group
    {
        public GroupData data = new GroupData();
        public NodeGraphView view { get; private set; }

        public GraphGroup(NodeGraphView view) : base()
        {
            this.view = view;

            ColorField c = new ColorField();
            c.style.width = 100;
            c.style.alignSelf = Align.FlexEnd;
            this.Insert(0, c);
            SetData(data);
            c.RegisterCallback<ChangeEvent<Color>>((evt) =>
            {
                SetColor(evt.newValue);
            });

            this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));

        }

        public List<GraphNode> containedNodes => this.containedElements.Where(x => x is GraphNode)
              .Cast<GraphNode>()
              .ToList();

        public void SetData(GroupData data)
        {
            this.data = data;
            this.title = data.description;
            this.SetPosition(data.position);
            SetColor(data.color);
        }

        private void SetColor(Color color)
        {
            this.Q<ColorField>().value = color;
            data.color = color;
            this.style.borderLeftWidth = 5;
            this.style.borderLeftColor = color;
        }
        protected override void OnGroupRenamed(string oldName, string newName)
        {
            base.OnGroupRenamed(oldName, newName);
            data.description = newName;
        }

        public void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (!(evt.target is GraphGroup)) return;

            evt.menu.AppendAction("DeleteSelf", DeleteSelf, DeleteStatus);
            evt.menu.AppendAction("Delete", Delete, DeleteStatus);

            evt.menu.AppendAction("Remove Nodes", RemoveNodes, RemoveNodesStatus);
            evt.menu.AppendAction("Remove Selected Nodes", RemoveSelectedNodes, RemoveSelectedNodesStatus);
            evt.menu.AppendAction("Add Selected Nodes", AddSelectedNodes, AddSelectedNodesStatus);

        }

        private void Delete(DropdownMenuAction obj)
        {
            var cons = containedNodes.SelectMany(x => x.connections);


            view.DeleteElements(new List<GraphElement> { this, }.Concat(this.containedNodes).Concat(cons));

        }

        private void DeleteSelf(DropdownMenuAction obj)
        {
            view.DeleteElements(new List<GraphElement> { this });
        }

        private DropdownMenuAction.Status DeleteStatus(DropdownMenuAction arg)
        {
            return DropdownMenuAction.Status.Normal;
        }

        private DropdownMenuAction.Status AddSelectedNodesStatus(DropdownMenuAction arg)
        {
            return view.selectedNodes.Any(x => !containedElements.Contains(x)) ?
                     DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.None;
        }

        private void AddSelectedNodes(DropdownMenuAction obj)
        {
            this.AddElements(view.selectedNodes.Where(x => !containedElements.Contains(x)));
        }

        private DropdownMenuAction.Status RemoveSelectedNodesStatus(DropdownMenuAction arg)
        {
            return this.containedNodes.Any(x => x.selected) ?
                DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.None;
        }

        private void RemoveSelectedNodes(DropdownMenuAction obj)
        {
            this.RemoveElements(this.containedNodes.Where(x => x.selected));
        }

        private DropdownMenuAction.Status RemoveNodesStatus(DropdownMenuAction arg)
        {
            return this.containedNodes.Count > 0 ?
                DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.None;
        }

        private void RemoveNodes(DropdownMenuAction obj)
        {
            this.RemoveElements(containedNodes);
        }

        public void OnInspectorGUI()
        {
            ActionEditor.EditorEX.CreateEditor(data).OnInspectorGUI();

        }
    }
}
