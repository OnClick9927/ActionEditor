using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{
    public abstract class GraphNode<T> : GraphNode where T : NodeData, new()
    {
        public T data;
        public override string GUID => data.guid;
        public sealed override string NodeName => EditorEX.GetTypeName(typeof(T));


        protected Port GeneratePort(Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single, string name = "")
        {
            var port = GraphPort.Create(Orientation.Horizontal, portDir, capacity, type);
            if (string.IsNullOrEmpty(name))
                name = type.Name;

            port.portName = name;
            if (portDir == Direction.Input)
                this.inputContainer.Add(port);
            else
                this.outputContainer.Add(port);
            return port;
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
        protected void DrawDefaultInspector()
        {
            ActionEditor.EditorEX.CreateEditor(data).OnInspectorGUI();

        }
        private Image dot;

        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);

            dot = new Image();
            dot.style.position = Position.Absolute;
            dot.style.top = 5;
            dot.style.right = 5;
            dot.style.width = dot.style.height = 22;
            dot.style.unityBackgroundImageTintColor = Color.white;
            style.minWidth = Mathf.Max(150, NodeName.Sum(c => c >= '\u4e00' && c <= '\u9fff' ? 1.6f : 1) * 30);
            var find = this.data.GetIcon();
            noIcon = find == null;
            dot.style.backgroundImage = find;
            if (find == null)
            {
                dot.style.backgroundImage = EditorGUIUtility.IconContent("d_editicon.sml").image as Texture2D;
            }
            this.titleContainer.Add(dot);

        }
        bool noIcon;
        internal sealed override void SetTitleColor()
        {
            this.titleContainer.style.backgroundColor = new StyleColor(this.data.GetColor());
        }
        public override void OnUpdate()
        {
            if (!selected)
                dot.style.unityBackgroundImageTintColor = noIcon ? Color.clear : Color.white;
            if (selected && this.view.selection.FirstOrDefault() == this)
            {
                var value = EditorApplication.timeSinceStartup - Mathf.FloorToInt((float)EditorApplication.timeSinceStartup);
                dot.style.unityBackgroundImageTintColor = Color.white.WithAlpha((float)value);

            }
        }

    }
    public abstract class GraphNode : Node
    {
        public List<GraphConnection> connections => view.connections
                    .FindAll(x => x.output.node == this || x.input.node == this);
        public Action<GraphNode> onSelected;
        public abstract string GUID { get; }
        public abstract string NodeName { get; }
        public NodeGraphView view { get; private set; }
        public List<GraphPort> ports { get { return this.view.ports.FindAll(x => x.node == this); } }




        public virtual void OnCreated(NodeGraphView view)
        {
            //this.title = NodeName;
            this.titleContainer.Clear();
            Label label = new Label() { text = NodeName };

            label.style.fontSize = 25;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            label.style.color = Color.white;
            label.style.backgroundColor = new UnityEngine.Color(0, 0, 0, 0.2f);
            label.StretchToParentSize();
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            this.titleContainer.Add(label);

            SetTitleColor();
            this.view = view;
        }
        internal abstract void SetTitleColor();


        public sealed override void OnSelected()
        {
            base.OnSelected();

            onSelected?.Invoke(this);

        }



        private void AddConnectionsToDeleteSet(VisualElement container, ref HashSet<GraphElement> toDelete)
        {
            List<GraphElement> toDeleteList = new List<GraphElement>();
            container.Query<Port>().ForEach(delegate (Port elem)
            {
                if (elem.connected)
                {
                    foreach (Edge connection in elem.connections)
                    {
                        if ((connection.capabilities & Capabilities.Deletable) != 0)
                        {
                            toDeleteList.Add(connection);
                        }
                    }
                }
            });
            toDelete.UnionWith(toDeleteList);
        }
        private void DisconnectAll(DropdownMenuAction a)
        {
            HashSet<GraphElement> toDelete = new HashSet<GraphElement>();
            AddConnectionsToDeleteSet(inputContainer, ref toDelete);
            AddConnectionsToDeleteSet(outputContainer, ref toDelete);
            toDelete.Remove(null);
            if (view != null)
                view.DeleteElements(toDelete);
        }
        private DropdownMenuAction.Status DisconnectAllStatus(DropdownMenuAction a)
        {

            VisualElement[] array = new VisualElement[2] { inputContainer, outputContainer };
            VisualElement[] array2 = array;
            foreach (VisualElement e in array2)
            {
                List<Port> list = e.Query<Port>().ToList();
                foreach (Port item in list)
                {
                    if (item.connected)
                    {
                        return DropdownMenuAction.Status.Normal;
                    }
                }
            }

            return DropdownMenuAction.Status.Disabled;
        }
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (!(evt.target is Node)) return;
            evt.menu.AppendAction("Delete", Delete, DeleteStatus);

            evt.menu.AppendAction("Disconnect all", DisconnectAll, DisconnectAllStatus);
            evt.menu.AppendAction("Remove From Group", RemoveFromGroup, RemoveFromGroupStatus);
            evt.menu.AppendSeparator();
        }

        private void Delete(DropdownMenuAction obj)
        {
            DisconnectAll(obj);
            view.DeleteElements(new List<GraphElement>() { this });
        }

        private DropdownMenuAction.Status DeleteStatus(DropdownMenuAction arg)
        {
            return (capabilities & Capabilities.Deletable) == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal;
        }

        private DropdownMenuAction.Status RemoveFromGroupStatus(DropdownMenuAction arg)
        {
            return view.groups.Find(x => x.containedNodes.Contains(this)) != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }

        private void RemoveFromGroup(DropdownMenuAction obj)
        {
            view.groups.Find(x => x.containedNodes.Contains(this)).RemoveElement(this);
        }

        public abstract void OnInspectorGUI();

        public abstract void OnUpdate();
    }
}
