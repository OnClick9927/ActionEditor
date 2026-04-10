using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Experimental.GraphView.Port;

namespace ActionEditor.Nodes
{
    class GraphNodeDefault : GraphNode
    {
        public sealed override NodeData Data => data;
        public NodeData data;
        public override string GUID => data.guid;
        public sealed override string NodeName => EditorEX.GetTypeName(data);
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            GeneratePorts(data.GetType());


        }

    }

    public abstract class GraphNode<T> : GraphNode where T : NodeData, new()
    {
        public sealed override NodeData Data => data;
        public T data;
        public override string GUID => data.guid;
        public sealed override string NodeName => EditorEX.GetTypeName(typeof(T));
    }
    public abstract class GraphNode : Node
    {
        static Dictionary<Type, FieldInfo[]> fields = new();

        protected void GeneratePorts(Type type)
        {
            if (!GraphNode.fields.TryGetValue(type, out var result))
            {
                result = type
                        .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(x => x.IsDefined(typeof(NodePortAttribute))).ToArray();
                GraphNode.fields.Add(type, result);
            }
            foreach (var item in result)
            {
                var attr = item.GetCustomAttribute<NodePortAttribute>();
                GeneratePort((Direction)attr.direction, attr.type == null ? item.FieldType : attr.type, attr.single ? Port.Capacity.Single : Port.Capacity.Multi, item.Name);
            }
        }

        protected Port GeneratePort(Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single, string name = "")
        {
            var port = GraphPort.Create(Orientation.Horizontal, portDir, capacity, type);
            if (string.IsNullOrEmpty(name))
                name = type.Name;

            port.portName = name;
            port.portColor = Prefs.GetColor(type);
            if (portDir == Direction.Input)
                this.inputContainer.Add(port);
            else
                this.outputContainer.Add(port);
            return port;
        }



        public List<GraphConnection> connections => view.connections
                    .FindAll(x => x.output.node == this || x.input.node == this);
        public Action<GraphNode> onSelected;
        public abstract string GUID { get; }
        public abstract string NodeName { get; }
        public NodeGraphView view { get; private set; }
        public List<GraphPort> ports { get { return this.view.ports.FindAll(x => x.node == this); } }
        public abstract NodeData Data { get; }

        private Image dot;
        bool noIcon;

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

            dot = new Image();
            dot.style.position = Position.Absolute;
            dot.style.top = 5;
            dot.style.right = 5;
            dot.style.width = dot.style.height = 22;
            dot.style.unityBackgroundImageTintColor = Color.white;
            style.minWidth = Mathf.Max(150, NodeName.Sum(c => c >= '\u4e00' && c <= '\u9fff' ? 1.6f : 1) * 30);
            var find = this.Data.GetIcon();
            noIcon = find == null;
            dot.style.backgroundImage = find;
            if (find == null)
            {
                dot.style.backgroundImage = EditorGUIUtility.IconContent("d_editicon.sml").image as Texture2D;
            }
            this.titleContainer.Add(dot);
            inspector = new IMGUIContainer(this._inspector);
            inspector.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1);
            this.Add(inspector);
        }
        private IMGUIContainer inspector;
        private void _inspector()
        {
            if (!App.window.showInspector)
            {
                inspector.style.minWidth = 0;
                return;
            }
            inspector.style.minWidth = GraphWindow.sp_width - 80;

            EditorGUIUtility.labelWidth -= 80;
            this.OnInspectorGUI();
            EditorGUIUtility.labelWidth += 80;

        }


        void SetTitleColor()
        {
            this.titleContainer.style.backgroundColor = new StyleColor(this.Data.GetColor());

        }


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
            evt.menu.AppendAction("Pretty Layout OutPuts", (e) =>
            {

                LayoutTree(this);
            });


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
        public virtual void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }
        private Vector2 scroll;
        protected void DrawDefaultInspector()
        {
            scroll = GUILayout.BeginScrollView(scroll);

            ActionEditor.EditorEX.CreateEditor(Data).OnInspectorGUI();
            GUILayout.EndScrollView();
        }

        public virtual void OnUpdate()
        {
            //if (!selected)
            //    dot.style.unityBackgroundImageTintColor = noIcon ? Color.clear : Color.white;
            //if (selected && this.view.selection.FirstOrDefault() == this)
            //{
            //    var value = EditorApplication.timeSinceStartup - Mathf.FloorToInt((float)EditorApplication.timeSinceStartup);
            //    dot.style.unityBackgroundImageTintColor = Color.white.WithAlpha((float)value);

            //}
        }





        //private static float NODE_WIDTH = 150f;


        void LayoutTree(GraphNode root)
        {
            float HorizontalSpacing = Prefs.NodePrettySpacing.x; ;
            float VerticalSpacing = Prefs.NodePrettySpacing.y; ;

            Dictionary<string, List<GraphNode>> child_map = new Dictionary<string, List<GraphNode>>();
            List<GraphNode> GetDirectChildren(GraphNode node)
            {
                if (!child_map.TryGetValue(node.GUID, out var result))
                {
                    result = node.ports.Where(x => x.direction == Direction.Output)
                        .SelectMany(x => x.connections).Select(x => x.input.node as GraphNode).ToList();

                    child_map[node.GUID] = result;
                }
                return result;
            }
            Dictionary<string, Vector2> pos_map = new Dictionary<string, Vector2>();

            float LayoutTree(GraphNode node, float startX, float startY)
            {

                var NodeHeight = node.style.width.value.value;
                var NodeWidth = node.style.height.value.value;
                var children = GetDirectChildren(node);
                if (children.Count == 0)
                {
                    pos_map[node.GUID] = new Vector2(startX, startY);
                    //SetNodePosition(node, startX, startY);
                    return startY + NodeHeight + VerticalSpacing;
                }

                float currentY = startY;
                // 遍历子节点，递归布局，每个子树占用自己的高度，不重叠
                foreach (var child in children)
                {
                    // 子节点放在父节点右侧
                    float childX = startX + NodeWidth + HorizontalSpacing;

                    // 递归布局子树，并返回下一个子节点应该开始的Y坐标
                    currentY = LayoutTree(child, childX, currentY);
                }

                // 父节点垂直居中在所有子树中间
                float totalChildrenHeight = currentY - startY;
                float centerY = startY + totalChildrenHeight / 2 - NodeHeight;

                var min = pos_map[children.First().GUID].y;
                var max = pos_map[children.Last().GUID].y;


                pos_map[node.GUID] = new Vector2(startX, (min + max) / 2);

                //SetNodePosition(node, startX, centerY);

                // 返回整棵子树占用的总高度（给父节点用）
                return startY + totalChildrenHeight;
            }


            var pos = root.GetPosition();
            //float rootY = root.GetPosition();
            LayoutTree(this, pos.x, pos.y);
            var y = pos_map[root.GUID].y - pos.y;
            UpdateChildY(root);

            void UpdateChildY(GraphNode node)
            {
                var pos = node.GetPosition();
                pos.x = pos_map[node.GUID].x;
                pos.y = pos_map[node.GUID].y - y;

                node.SetPosition(pos);
                var children = GetDirectChildren(node);
                if (children.Count != 0)
                {
                    for (int i = 0; i < children.Count; i++)
                    {
                        UpdateChildY(children[i]);
                    }
                }
            }
        }


    }
}
