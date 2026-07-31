using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{
    interface IBTTreeHierarchy
    {
        void RefreshNodeTree();
    }

    public class BTTreeView<T> : Nodes.NodeGraphView<T>, IGraphInspectorOverride,
        IBTTreeHierarchy where T : BTTree
    {
        protected BTTree runningTree { get; private set; }

        private sealed class NodeTreeEntry
        {
            public readonly GraphNode GraphNode;
            public readonly NodeData Data;
            public readonly string SourcePath;

            public NodeTreeEntry(GraphNode graphNode, NodeData data, string sourcePath)
            {
                GraphNode = graphNode;
                Data = data;
                SourcePath = sourcePath;
            }
        }

        private sealed class LoadedSubTree
        {
            public readonly NodeData Root;
            public readonly Dictionary<NodeData, List<NodeData>> Children;

            public LoadedSubTree(NodeData root, Dictionary<NodeData, List<NodeData>> children)
            {
                Root = root;
                Children = children;
            }
        }

        private sealed class TreeNodeRow : VisualElement
        {
            public readonly Image Icon;
            public readonly Label Label;
            public readonly Image SourceIcon;

            public TreeNodeRow()
            {
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
                style.height = 20;

                Icon = new Image();
                Icon.style.width = 16;
                Icon.style.height = 16;
                Icon.style.marginRight = 4;
                Icon.style.flexShrink = 0;
                Add(Icon);

                Label = new Label();
                Label.style.flexGrow = 1;
                Label.style.unityTextAlign = TextAnchor.MiddleLeft;
                Add(Label);

                SourceIcon = new Image();
                SourceIcon.style.width = 14;
                SourceIcon.style.height = 14;
                SourceIcon.style.marginRight = 3;
                SourceIcon.style.flexShrink = 0;
                Add(SourceIcon);
            }
        }

        private static bool _showNodeTree;
        private static Texture _subTreeSourceIcon;
        private readonly Dictionary<GraphNode, List<GraphNode>> _treeChildren =
            new Dictionary<GraphNode, List<GraphNode>>();
        private readonly Dictionary<GraphNode, int> _treeItemIds =
            new Dictionary<GraphNode, int>();
        private readonly HashSet<GraphNode> _reachableNodes = new HashSet<GraphNode>();
        private readonly Dictionary<string, LoadedSubTree> _loadedSubTrees =
            new Dictionary<string, LoadedSubTree>();
        private readonly HashSet<string> _subTreeAssetStack = new HashSet<string>();
        private readonly List<TreeViewItemData<NodeTreeEntry>> _treeRoots =
            new List<TreeViewItemData<NodeTreeEntry>>(1);
        private VisualElement _treePanel;
        private TreeView _nodeTree;
        private NodeData _subTreeInspectorNode;
        private Vector2 _subTreeInspectorScroll;
        private bool _treeDirty = true;
        private bool _treeRefreshScheduled;
        private bool _syncingTreeSelection;
        private int _nextTreeItemId;
        private int _subTreePathHash;

        private static int _Runing_BlackBoard = -1;
        private static float _height = -1;
        private static bool Runing_BlackBoard
        {
            get
            {
                if (_Runing_BlackBoard == -1)
                {
                    _Runing_BlackBoard = EditorPrefs.GetInt($"{typeof(BTTreeView<>).FullName}.{nameof(Runing_BlackBoard)}", 0);
                }
                return _Runing_BlackBoard > 0;
            }
            set
            {
                var target = value ? 1 : 0;

                if (_Runing_BlackBoard == target) return;
                _Runing_BlackBoard = target;
                EditorPrefs.SetInt($"{typeof(BTTreeView<>).FullName}.{nameof(Runing_BlackBoard)}", _Runing_BlackBoard);
            }
        }

        private static float height
        {
            get
            {
                if (_height == -1)
                {
                    _height = EditorPrefs.GetFloat($"{typeof(BTTreeView<>).FullName}.{nameof(height)}", 100f);
                }

                return _height;
            }
            set
            {
                if (_height == value) return;
                _height = value;
                EditorPrefs.SetFloat($"{typeof(BTTreeView<>).FullName}.{nameof(height)}", value);
            }
        }
        internal static void DrawBlackBord(BTTreeView<T> view, float maxheight)
        {
            var run = Runing_BlackBoard && view.runningTree != null;
            var blackboard = run ? view.runningTree.Blackboard : view.graph.Blackboard;

            GUI.color = Color.black;
            GUILayout.Box("", GUILayout.Height(30), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            var rect = GUILayoutUtility.GetLastRect();
            var _rect = new Rect(rect.xMax - 30, rect.y + 5, 20f, 20f);
            if (run)
            {
                var value = EditorApplication.timeSinceStartup - Mathf.FloorToInt((float)EditorApplication.timeSinceStartup);
                value = value / 0.1;
                var index = Mathf.Max(Mathf.FloorToInt((float)value), 0);
                if (GUI.Button(_rect, EditorGUIUtility.IconContent($"WaitSpin0{index}"), EditorStyles.toolbarButton))
                {
                    Runing_BlackBoard = false;
                }
                string temp = ".";
                for (int i = 0; i < index % 3; i++)
                    temp += ".";

                EditorGUI.LabelField(rect, $"BlackBord {temp}", new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                });

            }
            else
            {
                if (GUI.Button(_rect, EditorGUIUtility.IconContent("PlayButton"), EditorStyles.toolbarButton))
                {
                    if (BTTree.instance != null)
                    {
                        if (FindRunningTree(BTTree.instance, App.asset.guid) != null)
                        {
                            Runing_BlackBoard = true;
                        }
                    }
                }
                EditorGUI.LabelField(rect, "BlackBord", new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                });
            }


            Event e = Event.current;
            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
            {
                height += e.delta.y;
                height = Mathf.Clamp(height, 100, maxheight - 300);
                e.Use();
            }
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);
                EditorEX.DrawPingScript(blackboard.GetType());
                using (new UnityEditor.EditorGUI.DisabledScope(run || view.graph.IsSubTree))
                {
                    scroll = GUILayout.BeginScrollView(scroll);

                    EditorEX.CreateEditor(blackboard).OnInspectorGUI();
                    GUILayout.EndScrollView();
                }
                GUILayout.EndVertical();
            }
        }
        private static Vector2 scroll;

        protected override void OnInspectorGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(height));

            scroll = GUILayout.BeginScrollView(scroll);

            var editor = ActionEditor.EditorEX.CreateEditor(this.graph);
            editor.OnInspectorGUI();
            using (new EditorGUI.DisabledScope(this.graph.IsSubTree))
            {
                var p = editor.serializedObject.FindProperty("obj");
                editor.serializedObject.UpdateIfRequiredOrScript();
                EditorGUILayout.PropertyField(p.FindPropertyRelative(nameof(this.graph.events)), true);
                EditorGUILayout.PropertyField(p.FindPropertyRelative(nameof(this.graph.interruptFlags)), true);
                EditorGUILayout.PropertyField(p.FindPropertyRelative(nameof(this.graph.semaphores)),true);
                editor.serializedObject.ApplyModifiedProperties();
             
            }
            GUILayout.EndScrollView();
            DrawBlackBord(this, position.height);
            GUILayout.Space(2);
        }

        public override void OnHeaderGUI()
        {
            base.OnHeaderGUI();
            var content = EditorGUIUtility.TrIconContent("d_UnityEditor.HierarchyWindow", "Tree");
            bool show = GUILayout.Toggle(_showNodeTree, content, EditorStyles.toolbarButton);
            if (show == _showNodeTree) return;

            _showNodeTree = show;
            SetNodeTreeVisible(show);
        }



        public override UpdateType updateType
        {
            get
            {
                if (this.runningTree != null)
                {
                    return UpdateType.Update;
                }
                return base.updateType;
            }
        }


        public override void Update()
        {
            if (_showNodeTree && !_treeDirty && CalculateSubTreePathHash() != _subTreePathHash)
                ScheduleNodeTreeRefresh();
            if (_subTreeInspectorNode != null && selection.Count > 0)
            {
                _subTreeInspectorNode = null;
                GraphWindowBridge.Repaint();
            }
            base.Update();
        }

        public override void Load(GraphAsset data)
        {
            base.Load(data);
            CreateNodeTree();
            graphViewChanged += OnGraphViewChanged;
            EditorApplication.projectChanged += OnProjectChanged;
            RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
            SetNodeTreeVisible(_showNodeTree);
            BTTree_onInstanceChanged(BTTree.instance);
            BTTree.onInstanceChanged -= BTTree_onInstanceChanged;

            BTTree.onInstanceChanged += BTTree_onInstanceChanged;
        }
        private static BTTree FindRunningTree(BTTree tree, string guid)
        {
            if (tree == null)
                return null;
            if (tree.guid == guid)
                return tree;

            var subTrees = tree.subs;
            if (subTrees == null)
                return null;
            for (int i = 0; i < subTrees.Count; i++)
            {
                var result = FindRunningTree(subTrees[i], guid);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void BTTree_onInstanceChanged(BTTree tree)
        {
            tree = FindRunningTree(tree, this.graph.guid);
            this.runningTree = tree;
            OnBTTreeChanged(tree);

            var nodes = this.nodes;
            for (int i = 0; nodes.Count > i; i++)
            {
                if (nodes[i] is IBTNodeView node)
                    node.OnBTTreeChanged(tree);
            }
        }
        protected virtual void OnBTTreeChanged(BTTree tree)
        {

        }
        public override void OnSelectNode(GraphNode obj)
        {
            _subTreeInspectorNode = null;
            if (_nodeTree == null || _syncingTreeSelection) return;

            _syncingTreeSelection = true;
            if (_treeItemIds.TryGetValue(obj, out int id))
                _nodeTree.SetSelectionById(id);
            else
                _nodeTree.ClearSelection();
            _syncingTreeSelection = false;
            GraphWindowBridge.Repaint();
        }

        private void OnProjectChanged()
        {
            ScheduleNodeTreeRefresh();
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            BTTree.onInstanceChanged -= BTTree_onInstanceChanged;
        }

        void IBTTreeHierarchy.RefreshNodeTree()
        {
            ScheduleNodeTreeRefresh();
        }

        private void CreateNodeTree()
        {
            _treePanel = new VisualElement();
            _treePanel.style.flexGrow = 1;
            _treePanel.style.borderRightWidth = 1;
            _treePanel.style.borderRightColor = new Color(0f, 0f, 0f, 0.45f);
            _treePanel.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.15f, 0.15f, 0.15f)
                : new Color(0.76f, 0.76f, 0.76f);

            _nodeTree = new TreeView(20, MakeTreeItem, BindTreeItem)
            {
                selectionType = SelectionType.Single,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                showBorder = false
            };
            _nodeTree.style.flexGrow = 1;
            _nodeTree.selectionChanged += OnTreeSelectionChanged;
            _nodeTree.itemsChosen += OnTreeItemsChosen;
            _treePanel.Add(_nodeTree);
        }

        private static VisualElement MakeTreeItem() => new TreeNodeRow();

        private void BindTreeItem(VisualElement element, int index)
        {
            var row = (TreeNodeRow)element;
            var entry = _nodeTree.GetItemDataForIndex<NodeTreeEntry>(index);
            row.Label.text = entry.GraphNode == null
                ? EditorEX.GetTypeName(entry.Data.GetType())
                : entry.GraphNode.NodeName;
            row.Icon.image = entry.Data.GetIcon();
            bool fromSubTree = !string.IsNullOrEmpty(entry.SourcePath);
            if (fromSubTree && _subTreeSourceIcon == null)
                _subTreeSourceIcon = EditorGUIUtility.TrIconContent("d_TextAsset Icon").image;
            row.SourceIcon.image = fromSubTree ? _subTreeSourceIcon : null;
            row.SourceIcon.style.display = fromSubTree ? DisplayStyle.Flex : DisplayStyle.None;
            row.tooltip = fromSubTree ? entry.SourcePath : entry.Data.guid;
        }

        private void SetNodeTreeVisible(bool visible)
        {
            if (_treePanel == null) return;
            if (visible && _treeDirty) RefreshNodeTree();
            GraphWindowBridge.SetLeftSidebar(visible ? _treePanel : null);
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            ScheduleNodeTreeRefresh();
            return change;
        }

        private void ScheduleNodeTreeRefresh()
        {
            _treeDirty = true;
            if (!_showNodeTree || _treeRefreshScheduled) return;

            _treeRefreshScheduled = true;
            schedule.Execute(() =>
            {
                _treeRefreshScheduled = false;
                RefreshNodeTree();
            });
        }

        private void RefreshNodeTree()
        {
            _treeDirty = false;
            _treeRoots.Clear();
            _treeChildren.Clear();
            _treeItemIds.Clear();
            _reachableNodes.Clear();
            _loadedSubTrees.Clear();
            _subTreeAssetStack.Clear();
            if (!string.IsNullOrEmpty(App.assetPath))
                _subTreeAssetStack.Add(App.assetPath);
            _nextTreeItemId = 1;

            GraphNode root = null;
            var graphNodes = nodes;
            for (int i = 0; i < graphNodes.Count; i++)
            {
                if (graphNodes[i] is BTRootView)
                {
                    root = graphNodes[i];
                    break;
                }
            }

            var graphConnections = connections;
            for (int i = 0; i < graphConnections.Count; i++)
            {
                var connection = graphConnections[i];
                var parent = connection?.output?.node;
                var child = connection?.input?.node;
                if (parent == null || child == null) continue;

                if (!_treeChildren.TryGetValue(parent, out var children))
                {
                    children = new List<GraphNode>();
                    _treeChildren.Add(parent, children);
                }
                if (!children.Contains(child)) children.Add(child);
            }

            foreach (var pair in _treeChildren)
                pair.Value.Sort(CompareTreeNodes);

            if (root != null)
            {
                _reachableNodes.Add(root);
                _treeRoots.Add(CreateGraphTreeItem(root));
            }

            _syncingTreeSelection = true;
            _nodeTree.SetRootItems(_treeRoots);
            _nodeTree.ExpandAll();

            GraphNode selected = null;
            for (int i = 0; i < selection.Count; i++)
            {
                if (selection[i] is GraphNode node)
                {
                    selected = node;
                    break;
                }
            }
            if (selected != null && _treeItemIds.TryGetValue(selected, out int id))
                _nodeTree.SetSelectionById(id);
            _syncingTreeSelection = false;
            _subTreePathHash = CalculateSubTreePathHash();
        }

        private int CalculateSubTreePathHash()
        {
            int hash = 0;
            foreach (var pair in _treeItemIds)
            {
                if (!(pair.Key.Data is BTSubTree subTree)) continue;
                int itemHash = pair.Key.GUID.GetHashCode();
                itemHash = itemHash * 397 ^ (subTree.path == null ? 0 : subTree.path.GetHashCode());
                hash ^= itemHash;
            }
            return hash;
        }

        private TreeViewItemData<NodeTreeEntry> CreateGraphTreeItem(GraphNode node)
        {
            int id = _nextTreeItemId++;
            _treeItemIds.Add(node, id);
            List<TreeViewItemData<NodeTreeEntry>> items = null;
            if (_treeChildren.TryGetValue(node, out var children))
            {
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!_reachableNodes.Add(child)) continue;
                    if (items == null) items = new List<TreeViewItemData<NodeTreeEntry>>();
                    items.Add(CreateGraphTreeItem(child));
                }
            }
            if (node.Data is BTSubTree subTree)
                AddSubTreeRootChildren(subTree.path, ref items);

            var entry = new NodeTreeEntry(node, node.Data, null);
            return new TreeViewItemData<NodeTreeEntry>(id, entry, items);
        }

        private void AddSubTreeRootChildren(string path,
            ref List<TreeViewItemData<NodeTreeEntry>> items)
        {
            if (string.IsNullOrEmpty(path) || !_subTreeAssetStack.Add(path)) return;
            try
            {
                var loaded = LoadSubTree(path);
                if (loaded?.Root == null ||
                    !loaded.Children.TryGetValue(loaded.Root, out var children)) return;

                var reachable = new HashSet<NodeData> { loaded.Root };
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!reachable.Add(child)) continue;
                    if (items == null) items = new List<TreeViewItemData<NodeTreeEntry>>();
                    items.Add(CreateSubTreeItem(child, path, loaded, reachable));
                }
            }
            finally
            {
                _subTreeAssetStack.Remove(path);
            }
        }

        private TreeViewItemData<NodeTreeEntry> CreateSubTreeItem(NodeData node, string path,
            LoadedSubTree loaded, HashSet<NodeData> reachable)
        {
            List<TreeViewItemData<NodeTreeEntry>> items = null;
            if (loaded.Children.TryGetValue(node, out var children))
            {
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!reachable.Add(child)) continue;
                    if (items == null) items = new List<TreeViewItemData<NodeTreeEntry>>();
                    items.Add(CreateSubTreeItem(child, path, loaded, reachable));
                }
            }
            if (node is BTSubTree subTree)
                AddSubTreeRootChildren(subTree.path, ref items);

            int id = _nextTreeItemId++;
            var entry = new NodeTreeEntry(null, node, path);
            return new TreeViewItemData<NodeTreeEntry>(id, entry, items);
        }

        private LoadedSubTree LoadSubTree(string path)
        {
            if (_loadedSubTrees.TryGetValue(path, out var cached)) return cached;

            LoadedSubTree loaded = null;
            try
            {
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                var tree = text == null ? null : BTTree.FromBytes(typeof(BTTree), text.bytes) as BTTree;
                if (tree != null && tree.IsSubTree && tree.GetType() == graph.GetType())
                {
                    var nodesByGuid = new Dictionary<string, NodeData>();
                    NodeData root = null;
                    for (int i = 0; i < tree.nodes.Count; i++)
                    {
                        var node = tree.nodes[i];
                        if (node == null) continue;
                        nodesByGuid[node.guid] = node;
                        if (node is BTRoot) root = node;
                    }

                    var children = new Dictionary<NodeData, List<NodeData>>();
                    for (int i = 0; i < tree.connections.Count; i++)
                    {
                        var connection = tree.connections[i];
                        if (connection == null ||
                            !nodesByGuid.TryGetValue(connection.outNodeGuid, out var parent) ||
                            !nodesByGuid.TryGetValue(connection.InNodeGuid, out var child)) continue;

                        if (!children.TryGetValue(parent, out var list))
                        {
                            list = new List<NodeData>();
                            children.Add(parent, list);
                        }
                        if (!list.Contains(child)) list.Add(child);
                    }
                    loaded = new LoadedSubTree(root, children);
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            _loadedSubTrees.Add(path, loaded);
            return loaded;
        }

        private static int CompareTreeNodes(GraphNode a, GraphNode b)
        {
            int result = a.GetPosition().y.CompareTo(b.GetPosition().y);
            return result != 0 ? result : string.CompareOrdinal(a.GUID, b.GUID);
        }

        private void OnTreeSelectionChanged(IEnumerable<object> items)
        {
            if (_syncingTreeSelection) return;
            foreach (var item in items)
            {
                if (item is NodeTreeEntry entry)
                {
                    SelectTreeNode(entry);
                    return;
                }
            }
        }

        private void OnTreeItemsChosen(IEnumerable<object> items)
        {
            foreach (var item in items)
            {
                if (!(item is NodeTreeEntry entry)) continue;
                SelectTreeNode(entry);
                if (entry.GraphNode != null) FocusGraphNode(entry.GraphNode);
                return;
            }
        }

        private void SelectTreeNode(NodeTreeEntry entry)
        {
            _syncingTreeSelection = true;
            ClearSelection();
            _subTreeInspectorNode = entry.GraphNode == null ? entry.Data : null;
            if (entry.GraphNode != null) AddToSelection(entry.GraphNode);
            _syncingTreeSelection = false;
            GraphWindowBridge.Repaint();
        }

        private void FocusGraphNode(GraphNode node)
        {
            var scale = viewTransform.scale;
            var nodeCenter = node.GetPosition().center;
            var scaledCenter = Vector2.Scale(nodeCenter, new Vector2(scale.x, scale.y));
            var position = contentRect.center - scaledCenter;
            UpdateViewTransform(position, scale);
        }

        public bool DrawInspectorOverride()
        {
            if (_subTreeInspectorNode == null) return false;

            GUILayout.Space(2);
            var type = _subTreeInspectorNode.GetType();
            EditorEX.DrawPingScript(type);
            EditorGUILayout.LabelField(EditorEX.GetTypeName(type), EditorStyles.boldLabel,
                GUILayout.Height(30));
            using (new EditorGUI.DisabledScope(true))
            {
                _subTreeInspectorScroll = GUILayout.BeginScrollView(_subTreeInspectorScroll);
                EditorEX.CreateEditor(_subTreeInspectorNode).OnInspectorGUI();
                GUILayout.EndScrollView();
            }
            return true;
        }

        protected override void AfterCreateNode(GraphElement element)
        {
            if (port == null)
            {
                ScheduleNodeTreeRefresh();
                return;
            }
            try
            {
                if (port.direction == Direction.Input)
                    App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Output));
                else
                    App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Input));

            }
            catch (Exception)
            {
            }
            ScheduleNodeTreeRefresh();

        }
        GraphPort port;
        protected override List<Type> FitterNodeTypes(List<Type> src, GraphElement element)
        {
            src.RemoveAll(x => !EditorEX.CanAttachTo(x, typeof(BTTree))
            && !EditorEX.CanAttachTo(x, typeof(T))
            );
            if (element is GraphPort port)
            {
                this.port = port;
                src.RemoveAll(x => x == typeof(BTRootView) || x == typeof(GraphGroup));
                //src.RemoveAll(x => port.node.GetType() != x);
            }
            return src;
        }

        protected override bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end)
        {
            return start.portType == end.portType;
        }
    }





}
