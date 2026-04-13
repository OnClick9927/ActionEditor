using ActionEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.LightingExplorerTableColumn;

namespace ActionEditor.Nodes
{
    public abstract class NodeGraphView<T> : NodeGraphView where T : GraphAsset
    {
        public new T graph { get { return base.graph as T; } }
    }
    partial class NodeGraphView
    {
        private GraphElement context_target;
        internal bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
        {
            if (SearchTreeEntry.userData == null) return false;


            var mousePosition = root.ChangeCoordinatesTo(root.parent,
                context.screenMousePosition - position.position);
            var graphMousePosition = this.contentViewContainer.WorldToLocal(mousePosition);

            GraphElement element;
            Type type = (Type)SearchTreeEntry.userData;
            //if (type == typeof(GroupData))
            //{

            //}
            //else
            //{
            //}
            element = App.CreateNode((Type)SearchTreeEntry.userData, null);
            element.SetPosition(new Rect(graphMousePosition, element.GetPosition().size));
            this.AfterCreateNode(element);
            return true;
        }

        private class Node
        {
            public List<Node> children;
            public int childCount => children == null ? 0 : children.Count;
            private Dictionary<string, Node> map;
            public Type type;
            public int depth = 0;
            private int start_index;
            public string name = "Root";
            public Node parent;

            public string key;
            private Node AddChild(string name, string key)
            {
                children = children ?? new List<Node>();
                map = map ?? new Dictionary<string, Node>();

                if (map.TryGetValue(name, out var result))
                {
                    return result;
                }
                result = new Node()
                {
                    //root = root_node,
                    name = name,
                    parent = this,
                    depth = this.depth + 1,
                    start_index = start_index + name.Length + 1,
                };
                children.Add(result);
                map[name] = result;
                //root_map[result.id] = result;
                return result;
            }
            public void ReadKey(string key, Type type)
            {
                var index = key.IndexOf('/', start_index);

                if (index == -1)
                {
                    var childName = key.Substring(start_index);
                    var child = AddChild(childName, key);
                    child.key = key;
                    child.type = type;
                }
                else
                {
                    var childName = key.Substring(start_index, index - start_index);
                    var child = AddChild(childName, key);
                    child.ReadKey(key, type);

                }



            }


        }

        internal List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 0),
            };
            var nodeTypes = this.FitterNodeTypes(App.GetNodeTypes(), context_target);

            Node temp = new();
            for (int i = 0; i < nodeTypes.Count; i++)
            {
                var dataType = nodeTypes[i];
                var path = App.GetNodePath(dataType);
                temp.ReadKey(path, dataType);

            }
            Add(temp);
            void Add(Node node)
            {
                if (node.children != null)
                {
                    if (node != temp)
                    {
                        var entry = new SearchTreeGroupEntry(new GUIContent(node.name), node.depth);
                        tree.Add(entry);
                    }

                    node.children.Sort((a, b) => -a.childCount.CompareTo(b.childCount));
                    for (int i = 0; i < node.children.Count; i++)
                    {
                        Add(node.children[i]);
                    }

                }
                else
                {
                    var entry = new SearchTreeEntry(new GUIContent(node.name,
                        EditorGUIUtility.TrIconContent("sv_icon_dot0_pix16_gizmo").image))
                    {
                        level = node.depth,
                        userData = node.type
                    };
                    tree.Add(entry);
                }
            }



            //tree.Add(new SearchTreeEntry(new GUIContent("Group"))
            //{
            //    level = 1,
            //    userData = typeof(GroupData),
            //});
            return tree;
        }

        //internal void UpdateGraphColor()
        //{
        //    for (int i = 0; i < nodes.Count; i++)
        //    {
        //        nodes[i].SetTitleColor();
        //    }
        //}
    }




    public abstract partial class NodeGraphView : GraphView
    {
        public enum UpdateType
        {
            Update,
            Inspector
        }
        protected GraphAsset graph;
        protected sealed override bool canCopySelection => false;
        protected sealed override bool canCutSelection => false;
        protected sealed override bool canDuplicateSelection => false;
        protected sealed override bool canPaste => false;

        public VisualElement root => this.parent;
        public Rect position => App.window.position;
        public new List<GraphPort> ports => base.ports.ToList().ConvertAll(x => x as GraphPort);

        public new List<GraphNode> nodes => base.nodes.ToList().ConvertAll(x => x as GraphNode);
        public List<GraphConnection> connections => base.edges.ToList().ConvertAll(x => x as GraphConnection);
        public List<GraphGroup> groups => graphElements.ToList().Where(x => x is GraphGroup).Cast<GraphGroup>().ToList();
        public List<GraphNode> selectedNodes { get { return selection.Where(x => x is GraphNode).Select(x => x as GraphNode).ToList(); } }


        public NodeGraphView()
        {
            var styleSheet = Resources.Load<StyleSheet>("NodeGraphView");
            if (styleSheet != null) styleSheets.Add(styleSheet);
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);//zoom     

            //拖拽背景
            this.AddManipulator(new ContentDragger());
            //拖拽节点
            this.AddManipulator(new SelectionDragger());

            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new FreehandSelector());

            //背景 这个需要在 uss/styleSheet 中定义GridBackground类来描述类型
            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
            nodeCreationRequest = context =>
            {
                context_target = context.target as GraphElement;
                if (context.screenMousePosition == Vector2.zero)
                    SearchWindow.Open(new SearchWindowContext(Event.current.mousePosition + position.position), App.window);
                else
                    SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), App.window);
            };

        }

        public virtual UpdateType updateType { get { return UpdateType.Inspector; } }
        public void OpenSearchPop(VisualElement target, Vector2 position)
        {
            nodeCreationRequest?.Invoke(new NodeCreationContext
            {
                target = target,
                index = 0,
                screenMousePosition = position
            });
        }

        private DropdownMenuAction.Status DeleteSelectionStutas(DropdownMenuAction arg)
        {
            return selection.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled;
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList()
                .Where(x =>
                    x.direction != startPort.direction &&
                    x.node != (startPort as GraphPort).node
                    && OnCheckCouldLink((startPort as GraphPort).node, x.node, startPort as GraphPort, x)
                )
                .ToList()
                .ConvertAll(x => x as Port);
        }

        static GUIStyle _style;
        internal void DrawInspector()
        {
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 25,
                    fontStyle = FontStyle.Bold
                };
            }





            GUILayout.Space(2);
            object target = this.selection.FirstOrDefault();
            if (target is Edge)
                target = null;
            if (target == null)
            {
                EditorEX.DrawPingScript(graph.GetType());
                var title = EditorEX.GetTypeName(graph.GetType());
                EditorGUILayout.LabelField(title, _style, GUILayout.Height(30));
                this.OnInspectorGUI();
                return;
            }

            {

                if (target is GraphNode node)
                {

                    var dataType = node.Data.GetType();
                    EditorEX.DrawPingScript(dataType);
                    var title = EditorEX.GetTypeName(dataType);
                    EditorGUILayout.LabelField(title, _style, GUILayout.Height(30));
                    node.OnInspectorGUI();
                }
                ;
                if (target is GraphGroup group)
                {
                    //EditorEX.DrawPingScript(node.targetType);

                    var title = EditorEX.GetTypeName(group.data.GetType());
                    EditorGUILayout.LabelField(title, _style, GUILayout.Height(30));
                    group.OnInspectorGUI();
                }
                ;
            }



        }





        private MiniMap minimap;
        private void CreateMiniMap()
        {
            minimap = new MiniMap();
            //minimap.anchored = true;
            minimap.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f); // 背景色（半透明）

            minimap.style.position = Position.Absolute;
            minimap.style.height = minimap.style.width = 200;
            minimap.style.top = 0;
            minimap.style.right = 0;
            minimap.contentContainer.Clear();

            this.Add(minimap);
        }
        public virtual void Load(GraphAsset data)
        {
            CreateMiniMap();
            this.graph = data;
            this.viewTransform.position = graph.position;
            this.viewTransform.scale = graph.scale;
            App.CreateElements(new List<GraphElement>(), graph.nodes, graph.groups, graph.connections);
            root.RegisterCallback<MouseMoveEvent>(OnDragging);
            root.RegisterCallback<MouseUpEvent>(OnDragEnd);
            root.RegisterCallback<MouseCaptureOutEvent>(OnDragEnd);
            this.RegisterCallback<KeyDownEvent>(KeyDownCallback);
        }
        protected virtual void KeyDownCallback(KeyDownEvent evt)
        {
            if (evt.commandKey || evt.ctrlKey)
            {
                switch (evt.keyCode)
                {
                    case KeyCode.S:
                        if (evt.shiftKey)
                            App.SaveAs();
                        else
                            App.Save();
                        evt.StopImmediatePropagation();
                        break;
                    case KeyCode.D:
                        App.Duplicate();
                        evt.StopImmediatePropagation();
                        break;
                }
            }

        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target is Edge con)
            {
                evt.menu.AppendAction("Delete", (x) =>
                {
                    this.DeleteElements(new List<GraphElement>() { con });
                }, DropdownMenuAction.AlwaysEnabled);
            }
            if (!(evt.target is NodeGraphView)) return;
            if (nodeCreationRequest != null)
            {
                evt.menu.AppendAction("Create Node", (x) =>
                {
                    OpenSearchPop(null, x.eventInfo.mousePosition + position.position);

                }, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Create Group", (x) =>
                {

                    var mousePosition = root.ChangeCoordinatesTo(root.parent,x.eventInfo.mousePosition);
                    var graphMousePosition = this.contentViewContainer.WorldToLocal(mousePosition);
                    var element = App.CreateGroup(null);
                    element.SetPosition(new Rect(graphMousePosition, element.GetPosition().size));
                    //OpenSearchPop(null, x.eventInfo.mousePosition + position.position);

                }, DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();



            }
            evt.menu.AppendAction("Delete Selection", (x) =>
            {
                this.DeleteSelection();
            }, DeleteSelectionStutas);
        }

        protected virtual void OnDragging(MouseMoveEvent evt) { }

        protected virtual void OnDragEnd(EventBase evt) { }

        protected abstract bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end);
        public abstract void OnSelectNode(GraphNode obj);
        protected abstract void AfterCreateNode(GraphElement element);
        protected abstract List<Type> FitterNodeTypes(List<Type> src, GraphElement element);

        Vector2 scroll;
        protected virtual void OnInspectorGUI()
        {
            scroll = GUILayout.BeginScrollView(scroll);

            ActionEditor.EditorEX.CreateEditor(this.graph).OnInspectorGUI();
            GUILayout.EndScrollView();
        }
        public virtual void Update()
        {
            for (int i = 0; this.connections.Count > i; i++)
            {
                var con = connections[i] as GraphConnection;
                con.UpdateFlow();
            }
        }
        public virtual void OnFootGUI()
        {
        }
        public virtual void OnHeaderGUI()
        {
            App.window.showMiniMap = GUILayout.Toggle(App.window.showMiniMap, "Mini", EditorStyles.toolbarButton);
            minimap.visible = App.window.showMiniMap;
        }
    }
}