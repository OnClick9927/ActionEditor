using ActionEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

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
            if (type == typeof(GroupData))
            {
                element = App.CreateGroup(null);
                element.SetPosition(new Rect(graphMousePosition, element.GetPosition().size));
            }
            else
            {
                element = App.CreateNode((Type)SearchTreeEntry.userData, null);
                element.SetPosition(new Rect(graphMousePosition, element.GetPosition().size));
            }
            this.AfterCreateNode(element);
            return true;
        }
        internal List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree = new List<SearchTreeEntry>
            {
                new SearchTreeGroupEntry(new GUIContent("Nodes"), 0),
            };
            var nodeTypes = this.FitterNodeTypes(App.GetNodeEditorTypes(), context_target);

            for (int i = 0; i < nodeTypes.Count; i++)
            {
                var type = nodeTypes[i];

                NodeAttribute attr = App.GetNodeDataType(type).GetCustomAttribute(typeof(NodeAttribute)) as NodeAttribute;
                if (attr == null)
                {
                    var entry = new SearchTreeEntry(new GUIContent(type.FullName))
                    {
                        level = 1,
                        userData = type
                    };
                    tree.Add(entry);
                }
                else
                {
                    var path = attr.path;
                    var sp = path.Split('/');

                    for (int j = 0; j < sp.Length; j++)
                    {
                        if (sp.Length - 1 == j)
                        {
                            if (tree.Find(x => x.name == sp[j] && x.level == j + 1) == null)
                            {
                                var entry = new SearchTreeEntry(new GUIContent(sp[j]))
                                {
                                    level = j + 1,
                                    userData = type
                                };
                                tree.Add(entry);
                            }
                            else
                            {
                                throw new Exception($"Same Node path : {path}");
                            }
                        }
                        else
                        {
                            if (tree.Find(x => x.name == sp[j] && x.level == j + 1) == null)
                            {
                                var entry = new SearchTreeGroupEntry(new GUIContent(sp[j]), j + 1);
                                tree.Add(entry);
                            }
                        }
                    }

                }
            }

            tree.Add(new SearchTreeEntry(new GUIContent("Group"))
            {
                level = 1,
                userData = typeof(GroupData)
            });
            return tree;
        }


    }


    partial class NodeGraphView
    {
      
    }

    public abstract partial class NodeGraphView : GraphView
    {
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
            if (target is GraphConnection)
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

                    var dataType = App.GetNodeDataType(node.GetType());
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
            if (evt.target is GraphConnection)
            {
                GraphConnection con = (GraphConnection)evt.target;
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

        protected virtual void OnInspectorGUI()
        {
            ActionEditor.EditorEX.CreateEditor(this.graph).OnInspectorGUI();

        }
        public virtual void Update()
        {
        }
        public virtual void OnHeaderGUI()
        {
            App.window.showMiniMap = GUILayout.Toggle(App.window.showMiniMap, "Mini", EditorStyles.toolbarButton);
            minimap.visible = App.window.showMiniMap;
        }
    }
}