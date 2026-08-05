using ActionUnity;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using IMGUIControls = UnityEditor.IMGUI.Controls;

namespace ActionEditor.Nodes.BT
{
    interface IBTTreeHierarchy
    {
        void RefreshNodeTree();
    }

    public class BTTreeView<T> : Nodes.NodeGraphView<T>, IBTTreeHierarchy where T : BTTree
    {
        protected BTTree runningTree { get; private set; }

        private sealed class NodeTreeEntry
        {
            public readonly GraphNode GraphNode;
            public readonly NodeData Data;
            public readonly string SourcePath;
            public readonly GUIContent Tooltip;
            public readonly string[] RuntimeTreePath;
            public readonly int TreeId;
            public readonly NodeTreeEntry Parent;
            public readonly List<NodeTreeEntry> Children = new List<NodeTreeEntry>();
            public readonly int Depth;
            public readonly int SubTreeDepth;

            public NodeTreeEntry(GraphNode graphNode, NodeData data, string sourcePath,
                string[] runtimeTreePath, int treeId, NodeTreeEntry parent,
                int depth, int subTreeDepth)
            {
                GraphNode = graphNode;
                Data = data;
                SourcePath = sourcePath;
                string tooltip = EditorEX.GetTypeTooltip(data?.GetType());
                if (!string.IsNullOrEmpty(sourcePath))
                    tooltip = string.IsNullOrEmpty(tooltip)
                        ? sourcePath
                        : tooltip + "\n" + sourcePath;
                Tooltip = new GUIContent(string.Empty, tooltip);
                RuntimeTreePath = runtimeTreePath;
                TreeId = treeId;
                Parent = parent;
                Depth = depth;
                SubTreeDepth = subTreeDepth;
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

        private sealed class NodeTreeViewItem : IMGUIControls.TreeViewItem
        {
            public readonly NodeTreeEntry Entry;

            public NodeTreeViewItem(NodeTreeEntry entry, string displayName)
                : base(entry.TreeId, entry.Depth, displayName)
            {
                Entry = entry;
                icon = entry.Data.GetIcon();
            }
        }

        private sealed class NodeTreeIMGUI : IMGUIControls.TreeView
        {
            private const float SubTreeMarkerWidth = 24;
            private static readonly Color RunningColor = new Color(0.2f, 1f, 0.35f);
            private static readonly Color SubTreeMarkerColor =
                new Color(0.2f, 0.82f, 0.78f);
            private static readonly string[] SubTreeDepthLabels =
                CreateSubTreeDepthLabels();
            private static Texture2D _runningIcon;
            private static GUIStyle _nodeLabelStyle;
            private static GUIStyle _selectedNodeLabelStyle;
            private static GUIStyle _subTreeMarkerStyle;
            private readonly BTTreeView<T> _owner;
            private readonly Dictionary<int, int> _visibleRowIndices =
                new Dictionary<int, int>();

            public NodeTreeIMGUI(IMGUIControls.TreeViewState state, BTTreeView<T> owner)
                : base(state)
            {
                _owner = owner;
                rowHeight = 20;
                showAlternatingRowBackgrounds = true;
                showBorder = false;
                extraSpaceBeforeIconAndLabel = 12;
            }

            protected override IMGUIControls.TreeViewItem BuildRoot()
            {
                var root = new IMGUIControls.TreeViewItem(0, -1, "Root");
                for (int i = 0; i < _owner._allTreeItems.Count; i++)
                {
                    var entry = _owner._allTreeItems[i];
                    if (entry.Parent == null) root.AddChild(BuildItem(entry));
                }
                SetupDepthsFromParentsAndChildren(root);
                return root;
            }

            private static NodeTreeViewItem BuildItem(NodeTreeEntry entry)
            {
                string displayName = entry.GraphNode == null
                    ? EditorEX.GetTypeName(entry.Data.GetType())
                    : entry.GraphNode.NodeName;
                var item = new NodeTreeViewItem(entry, displayName);
                for (int i = 0; i < entry.Children.Count; i++)
                    item.AddChild(BuildItem(entry.Children[i]));
                return item;
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = (NodeTreeViewItem)args.item;
                var entry = item.Entry;
                bool running = _owner.IsTreeEntryRunning(entry);
                bool parentRunning = running && entry.Parent != null &&
                    _owner.IsTreeEntryRunning(entry.Parent);
                bool childRunning = running && _owner.HasRunningChild(entry);
                float dotX = args.rowRect.x + GetContentIndent(item) -
                    extraSpaceBeforeIconAndLabel + 6;
                float centerY = args.rowRect.center.y;

                if (parentRunning)
                {
                    float parentX = dotX - depthIndentWidth;
                    EditorGUI.DrawRect(new Rect(parentX - 1, args.rowRect.y, 2,
                        centerY - args.rowRect.y), RunningColor);
                    EditorGUI.DrawRect(new Rect(parentX, centerY - 1,
                        dotX - parentX, 2), RunningColor);
                }
                if (childRunning)
                {
                    EditorGUI.DrawRect(new Rect(dotX - 1, centerY, 2,
                        args.rowRect.yMax - centerY), RunningColor);
                }

                bool fromSubTree = entry.SubTreeDepth > 0;
                var baseArgs = args;
                string displayName = baseArgs.label;
                baseArgs.label = string.Empty;
                if (fromSubTree) baseArgs.rowRect.xMax -= SubTreeMarkerWidth;
                base.RowGUI(baseArgs);

                if (!args.isRenaming)
                {
                    Rect labelRect = GetRenameRect(args.rowRect, args.row, item);
                    labelRect.y = args.rowRect.y;
                    labelRect.height = args.rowRect.height;
                    if (fromSubTree)
                        labelRect.xMax = Mathf.Min(labelRect.xMax,
                            args.rowRect.xMax - SubTreeMarkerWidth);
                    GUI.Label(labelRect, displayName,
                        GetNodeLabelStyle(args.selected && args.focused));
                    GUI.Label(args.rowRect, entry.Tooltip, GUIStyle.none);
                }

                if (running)
                {
                    if (_runningIcon == null)
                        _runningIcon = EditorGUIUtility.TrIconContent("d_greenLight").image
                            as Texture2D;
                    var dotRect = new Rect(dotX - 4, centerY - 4, 8, 8);
                    if (_runningIcon != null)
                        GUI.DrawTexture(dotRect, _runningIcon, ScaleMode.ScaleToFit, true);
                    else
                        EditorGUI.DrawRect(new Rect(dotX - 3, centerY - 3, 6, 6),
                            RunningColor);
                }

                if (!fromSubTree) return;
                var markerRect = new Rect(args.rowRect.xMax - SubTreeMarkerWidth,
                    args.rowRect.y, SubTreeMarkerWidth, args.rowRect.height);
                GUI.Label(markerRect, GetSubTreeDepthLabel(entry.SubTreeDepth),
                    GetSubTreeMarkerStyle());
            }

            private static GUIStyle GetNodeLabelStyle(bool selected)
            {
                if (_nodeLabelStyle == null)
                {
                    _nodeLabelStyle = new GUIStyle(EditorStyles.label)
                    {
                        alignment = TextAnchor.MiddleLeft,
                        contentOffset = new Vector2(0, 1),
                        padding = new RectOffset(),
                        clipping = TextClipping.Clip
                    };
                    _selectedNodeLabelStyle = new GUIStyle(_nodeLabelStyle);
                    _selectedNodeLabelStyle.normal.textColor = Color.white;
                }
                return selected ? _selectedNodeLabelStyle : _nodeLabelStyle;
            }

            private static string[] CreateSubTreeDepthLabels()
            {
                var labels = new string[51];
                for (int depth = 1; depth <= 50; depth++)
                {
                    int codePoint = depth <= 20
                        ? 0x2460 + depth - 1
                        : depth <= 35
                            ? 0x3251 + depth - 21
                            : 0x32B1 + depth - 36;
                    labels[depth] = char.ConvertFromUtf32(codePoint);
                }
                return labels;
            }

            private static string GetSubTreeDepthLabel(int depth)
            {
                return depth > 0 && depth < SubTreeDepthLabels.Length
                    ? SubTreeDepthLabels[depth]
                    : depth.ToString();
            }

            private static GUIStyle GetSubTreeMarkerStyle()
            {
                if (_subTreeMarkerStyle != null) return _subTreeMarkerStyle;
                _subTreeMarkerStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    padding = new RectOffset()
                };
                _subTreeMarkerStyle.normal.textColor = SubTreeMarkerColor;
                return _subTreeMarkerStyle;
            }

            protected override void AfterRowsGUI()
            {
                base.AfterRowsGUI();
                if (Event.current.type != EventType.Repaint) return;

                IList<IMGUIControls.TreeViewItem> rows = GetRows();
                _visibleRowIndices.Clear();
                for (int i = 0; i < rows.Count; i++)
                    _visibleRowIndices[rows[i].id] = i;

                for (int parentIndex = 0; parentIndex < rows.Count; parentIndex++)
                {
                    var parentItem = rows[parentIndex] as NodeTreeViewItem;
                    if (parentItem == null || !_owner.IsTreeEntryRunning(parentItem.Entry))
                        continue;

                    for (int i = 0; i < parentItem.Entry.Children.Count; i++)
                    {
                        NodeTreeEntry child = parentItem.Entry.Children[i];
                        int childIndex;
                        if (!_owner.IsTreeEntryRunning(child) ||
                            !_visibleRowIndices.TryGetValue(child.TreeId, out childIndex) ||
                            childIndex <= parentIndex + 1)
                            continue;

                        Rect parentRect = GetRowRect(parentIndex);
                        Rect childRect = GetRowRect(childIndex);
                        float lineX = parentRect.x + GetContentIndent(parentItem) -
                            extraSpaceBeforeIconAndLabel + 6;
                        EditorGUI.DrawRect(new Rect(lineX - 1, parentRect.center.y, 2,
                            childRect.center.y - parentRect.center.y), RunningColor);
                    }
                }
            }

            protected override void SelectionChanged(IList<int> selectedIds)
            {
                if (_owner._syncingTreeSelection || selectedIds.Count == 0) return;
                var item = FindItem(selectedIds[0], rootItem) as NodeTreeViewItem;
                if (item != null) _owner.SelectTreeNode(item.Entry);
            }

            protected override void DoubleClickedItem(int id)
            {
                var item = FindItem(id, rootItem) as NodeTreeViewItem;
                if (item?.Entry.GraphNode == null) return;
                _owner.SelectTreeNode(item.Entry);
                _owner.FocusGraphNode(item.Entry.GraphNode);
            }
        }

        private const string ShowNodeTreePrefKey =
            "ActionEditor.Nodes.BT.BTTreeView.ShowNodeTree";
        private const string NodeTreeWidthPrefKey =
            "ActionEditor.Nodes.BT.BTTreeView.NodeTreeWidth";
        private const float DefaultNodeTreeWidth = 240;
        private const float MinNodeTreeWidth = 200;
        private static bool _showNodeTree = EditorPrefs.GetBool(ShowNodeTreePrefKey, false);
        private readonly Dictionary<GraphNode, List<GraphNode>> _treeChildren =
            new Dictionary<GraphNode, List<GraphNode>>();
        private readonly Dictionary<GraphNode, int> _treeItemIds =
            new Dictionary<GraphNode, int>();
        private readonly Dictionary<string, int> _treeIdsByKey =
            new Dictionary<string, int>();
        private readonly HashSet<GraphNode> _reachableNodes = new HashSet<GraphNode>();
        private readonly HashSet<int> _runningTreeItemIds = new HashSet<int>();
        private readonly HashSet<int> _nextRunningTreeItemIds = new HashSet<int>();
        private readonly Dictionary<string, LoadedSubTree> _loadedSubTrees =
            new Dictionary<string, LoadedSubTree>();
        private readonly HashSet<string> _subTreeAssetStack = new HashSet<string>();
        private readonly List<NodeTreeEntry> _allTreeItems = new List<NodeTreeEntry>();
        private VisualElement _treePanel;
        private IMGUIContainer _nodeTreeContainer;
        private NodeTreeIMGUI _nodeTree;
        private Label _emptyTreeLabel;
        private VisualElement _treeResizeHandle;
        private VisualElement _treeResizeIndicator;
        private NodeData _subTreeInspectorNode;
        private Vector2 _subTreeInspectorScroll;
        private float _treeWidth;
        private float _treeResizeStartX;
        private float _treeResizeStartWidth;
        private bool _treeResizeHovered;
        private bool _treeResizeActive;
        private bool _treeDirty = true;
        private bool _treeRefreshScheduled;
        private bool _syncingTreeSelection;
        private bool _nodeTreeInitialized;
        private int _nextTreeId = 1;
        private int _subTreePathHash;
        private double _nextSubTreePathCheck;

        private static int _Runing_BlackBoard = -1;
        private static float _height = -1;
        private static GUIStyle _blackboardHeaderStyle;
        private static GUIContent _blackboardPlayContent;
        private static GUIContent _treeToolbarContent;
        private static readonly GUIContent[] BlackboardRunningContents =
        {
            new GUIContent("BlackBord ."),
            new GUIContent("BlackBord .."),
            new GUIContent("BlackBord ...")
        };
        private static readonly GUIContent[] BlackboardWaitContents =
            new GUIContent[10];
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
            if (_blackboardHeaderStyle == null)
            {
                _blackboardHeaderStyle = new GUIStyle(EditorStyles.largeLabel)
                {
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
                _blackboardPlayContent = EditorGUIUtility.IconContent("PlayButton");
            }

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
                index %= BlackboardWaitContents.Length;
                if (BlackboardWaitContents[index] == null)
                    BlackboardWaitContents[index] = EditorGUIUtility.IconContent(
                        $"WaitSpin0{index}");
                if (GUI.Button(_rect, BlackboardWaitContents[index],
                        EditorStyles.toolbarButton))
                {
                    Runing_BlackBoard = false;
                }
                EditorGUI.LabelField(rect, BlackboardRunningContents[index % 3],
                    _blackboardHeaderStyle);

            }
            else
            {
                if (GUI.Button(_rect, _blackboardPlayContent, EditorStyles.toolbarButton))
                {
                    if (BTTree.instance != null)
                    {
                        if (FindRunningTree(BTTree.instance, App.asset.guid) != null)
                        {
                            Runing_BlackBoard = true;
                        }
                    }
                }
                EditorGUI.LabelField(rect, "BlackBord", _blackboardHeaderStyle);
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

            var editor = EditorEX.CreateEditor(this.graph);
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

        protected override void OnHeaderToolsGUI()
        {
            if (_treeToolbarContent == null)
                _treeToolbarContent = EditorGUIUtility.TrIconContent(
                    "d_UnityEditor.HierarchyWindow", "Tree");
            bool show = GUILayout.Toggle(_showNodeTree, _treeToolbarContent,
                EditorStyles.toolbarButton);
            if (show == _showNodeTree) return;

            _showNodeTree = show;
            EditorPrefs.SetBool(ShowNodeTreePrefKey, show);
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
            if (_showNodeTree && !_treeDirty &&
                EditorApplication.timeSinceStartup >= _nextSubTreePathCheck)
            {
                _nextSubTreePathCheck = EditorApplication.timeSinceStartup + 0.25;
                if (CalculateSubTreePathHash() != _subTreePathHash)
                    ScheduleNodeTreeRefresh();
            }
            if (_subTreeInspectorNode != null && selection.Count > 0)
            {
                _subTreeInspectorNode = null;
                RepaintEditorWindow();
            }
            if (_showNodeTree)
            {
                bool runningChanged = RefreshRunningTreeItems();
                ExpandRunningTreeNodes();
                if (runningChanged) RepaintNodeTree();
            }
            UpdateConnectionFlows();
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
            RefreshRunningTreeItems();
            RepaintNodeTree();
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
                _nodeTree.SetSelection(new[] { id },
                    IMGUIControls.TreeViewSelectionOptions.RevealAndFrame);
            else
                _nodeTree.SetSelection(Array.Empty<int>());
            _syncingTreeSelection = false;
            RepaintEditorWindow();
        }

        private void OnProjectChanged()
        {
            ScheduleNodeTreeRefresh();
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent evt)
        {
            SaveTreeWidth();
            EditorApplication.projectChanged -= OnProjectChanged;
            BTTree.onInstanceChanged -= BTTree_onInstanceChanged;
        }

        void IBTTreeHierarchy.RefreshNodeTree()
        {
            ScheduleNodeTreeRefresh();
        }

        private void CreateNodeTree()
        {
            _treeWidth = EditorPrefs.GetFloat(NodeTreeWidthPrefKey, DefaultNodeTreeWidth);
            if (float.IsNaN(_treeWidth) || float.IsInfinity(_treeWidth))
                _treeWidth = DefaultNodeTreeWidth;
            _treeWidth = Mathf.Max(MinNodeTreeWidth, _treeWidth);

            _treePanel = new VisualElement();
            _treePanel.style.position = Position.Absolute;
            _treePanel.style.left = 0;
            _treePanel.style.top = 0;
            _treePanel.style.bottom = 0;
            _treePanel.style.width = _treeWidth;
            _treePanel.style.minWidth = MinNodeTreeWidth;
            _treePanel.style.borderRightWidth = 1;
            _treePanel.style.borderRightColor = new Color(0f, 0f, 0f, 0.45f);
            _treePanel.style.backgroundColor = EditorGUIUtility.isProSkin
                ? new Color(0.22f, 0.22f, 0.22f)
                : new Color(0.76f, 0.76f, 0.76f);

            _emptyTreeLabel = new Label("Not found RootNode");
            _emptyTreeLabel.style.display = DisplayStyle.None;
            _emptyTreeLabel.style.alignSelf = Align.Center;
            _emptyTreeLabel.style.marginTop = 28;
            _emptyTreeLabel.style.fontSize = 17;
            _emptyTreeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _emptyTreeLabel.style.color = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.52f, 0.42f)
                : new Color(0.68f, 0.12f, 0.08f);
            _treePanel.Add(_emptyTreeLabel);

            _nodeTree = new NodeTreeIMGUI(new IMGUIControls.TreeViewState(), this);
            _nodeTreeContainer = new IMGUIContainer(DrawNodeTreeGUI);
            _nodeTreeContainer.style.flexGrow = 1;
            _treePanel.Add(_nodeTreeContainer);

            _treeResizeHandle = new VisualElement();
            _treeResizeHandle.style.position = Position.Absolute;
            _treeResizeHandle.style.left = _treeWidth - 2;
            _treeResizeHandle.style.top = 0;
            _treeResizeHandle.style.bottom = 0;
            _treeResizeHandle.style.width = 5;
            _treeResizeHandle.style.backgroundColor = Color.clear;
            _treeResizeHandle.style.cursor = CreateResizeHorizontalCursor();
            _treeResizeHandle.RegisterCallback<PointerEnterEvent>(OnTreeResizeEnter);
            _treeResizeHandle.RegisterCallback<PointerLeaveEvent>(OnTreeResizeLeave);
            _treeResizeHandle.RegisterCallback<PointerDownEvent>(OnTreeResizeDown);
            _treeResizeHandle.RegisterCallback<PointerMoveEvent>(OnTreeResizeMove);
            _treeResizeHandle.RegisterCallback<PointerUpEvent>(OnTreeResizeUp);
            _treeResizeHandle.RegisterCallback<PointerCaptureOutEvent>(OnTreeResizeCaptureOut);

            _treeResizeIndicator = new VisualElement();
            _treeResizeIndicator.style.position = Position.Absolute;
            _treeResizeIndicator.style.left = 2;
            _treeResizeIndicator.style.top = 0;
            _treeResizeIndicator.style.bottom = 0;
            _treeResizeIndicator.style.width = 1;
            _treeResizeIndicator.pickingMode = PickingMode.Ignore;
            _treeResizeHandle.Add(_treeResizeIndicator);
            SetTreeResizeFeedback(false);

            hierarchy.Add(_treePanel);
            hierarchy.Add(_treeResizeHandle);
        }

        private void DrawNodeTreeGUI()
        {
            var rect = new Rect(0, 0, _nodeTreeContainer.contentRect.width,
                _nodeTreeContainer.contentRect.height);
            _nodeTree.OnGUI(rect);
        }

        private bool HasRunningChild(NodeTreeEntry entry)
        {
            for (int i = 0; i < entry.Children.Count; i++)
            {
                if (IsTreeEntryRunning(entry.Children[i])) return true;
            }
            return false;
        }

        private void ExpandRunningTreeNodes()
        {
            if (_nodeTree == null || runningTree == null) return;

            for (int i = 0; i < _allTreeItems.Count; i++)
            {
                NodeTreeEntry entry = _allTreeItems[i];
                if (!IsTreeEntryRunning(entry)) continue;

                if (entry.Children.Count > 0 && !_nodeTree.IsExpanded(entry.TreeId))
                    _nodeTree.SetExpanded(entry.TreeId, true);

                for (NodeTreeEntry parent = entry.Parent;
                    parent != null;
                    parent = parent.Parent)
                {
                    if (!_nodeTree.IsExpanded(parent.TreeId))
                        _nodeTree.SetExpanded(parent.TreeId, true);
                }
            }
        }

        private void RepaintNodeTree()
        {
            _nodeTreeContainer?.MarkDirtyRepaint();
        }

        private bool IsTreeEntryRunning(NodeTreeEntry entry)
        {
            return _runningTreeItemIds.Contains(entry.TreeId);
        }

        private bool RefreshRunningTreeItems()
        {
            _nextRunningTreeItemIds.Clear();

            if (runningTree != null)
            {
                for (int i = 0; i < _allTreeItems.Count; i++)
                {
                    NodeTreeEntry entry = _allTreeItems[i];
                    if (QueryTreeEntryRunning(entry))
                        _nextRunningTreeItemIds.Add(entry.TreeId);
                }
            }

            if (_runningTreeItemIds.SetEquals(_nextRunningTreeItemIds)) return false;
            _runningTreeItemIds.Clear();
            _runningTreeItemIds.UnionWith(_nextRunningTreeItemIds);
            return true;
        }

        private bool QueryTreeEntryRunning(NodeTreeEntry entry)
        {
            if (entry.GraphNode is IBTNodeView nodeView)
                return nodeView.IsTreeNodeRunning;
            if (runningTree == null || !(entry.Data is BTNode)) return false;

            var tree = ResolveRuntimeTree(entry.RuntimeTreePath);
            var node = tree?.FindNode<BTNode>(entry.Data.guid);
            if (node is BTSubTree subTree && subTree.runtimeNode != null)
                node = subTree.runtimeNode;
            return node != null && node.state == BTNode.State.Running;
        }

        private BTTree ResolveRuntimeTree(string[] runtimeTreePath)
        {
            var tree = runningTree;
            if (runtimeTreePath == null) return tree;
            for (int i = 0; tree != null && i < runtimeTreePath.Length; i++)
                tree = tree.FindNode<BTSubTree>(runtimeTreePath[i])?.tree;
            return tree;
        }

        private void SetNodeTreeVisible(bool visible)
        {
            if (_treePanel == null) return;
            if (visible && _treeDirty) RefreshNodeTree();
            _treePanel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _treeResizeHandle.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) return;

            UpdateTreeResizeHandle(_treeWidth);
            _treePanel.BringToFront();
            _treeResizeHandle.BringToFront();
        }

        private void OnTreeResizeEnter(PointerEnterEvent evt)
        {
            _treeResizeHovered = true;
            SetTreeResizeFeedback(true);
        }

        private void OnTreeResizeLeave(PointerLeaveEvent evt)
        {
            _treeResizeHovered = false;
            if (!_treeResizeActive) SetTreeResizeFeedback(false);
        }

        private void OnTreeResizeDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            _treeResizeActive = true;
            _treeResizeStartX = evt.position.x;
            _treeResizeStartWidth = _treePanel.resolvedStyle.width;
            if (float.IsNaN(_treeResizeStartWidth))
                _treeResizeStartWidth = _treeWidth;
            SetTreeResizeFeedback(true);
            _treeResizeHandle.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnTreeResizeMove(PointerMoveEvent evt)
        {
            if (!_treeResizeActive ||
                !_treeResizeHandle.HasPointerCapture(evt.pointerId)) return;

            float width = _treeResizeStartWidth + evt.position.x - _treeResizeStartX;
            float maxWidth = resolvedStyle.width - MinNodeTreeWidth;
            if (!float.IsNaN(maxWidth) && maxWidth >= MinNodeTreeWidth)
                width = Mathf.Min(width, maxWidth);
            _treeWidth = Mathf.Max(MinNodeTreeWidth, width);
            _treePanel.style.width = _treeWidth;
            UpdateTreeResizeHandle(_treeWidth);
            evt.StopPropagation();
        }

        private void OnTreeResizeUp(PointerUpEvent evt)
        {
            if (evt.button != 0) return;
            _treeResizeActive = false;
            if (_treeResizeHandle.HasPointerCapture(evt.pointerId))
                _treeResizeHandle.ReleasePointer(evt.pointerId);
            SaveTreeWidth();
            SetTreeResizeFeedback(_treeResizeHovered);
            evt.StopPropagation();
        }

        private void OnTreeResizeCaptureOut(PointerCaptureOutEvent evt)
        {
            _treeResizeActive = false;
            SaveTreeWidth();
            SetTreeResizeFeedback(_treeResizeHovered);
        }

        private void SaveTreeWidth()
        {
            if (_treePanel == null || float.IsNaN(_treeWidth) ||
                float.IsInfinity(_treeWidth)) return;
            EditorPrefs.SetFloat(NodeTreeWidthPrefKey, _treeWidth);
        }

        private void UpdateTreeResizeHandle(float treeWidth)
        {
            _treeResizeHandle.style.left = Mathf.Max(0, treeWidth - 2);
        }

        private void SetTreeResizeFeedback(bool highlighted)
        {
            if (_treeResizeIndicator != null)
                _treeResizeIndicator.style.backgroundColor = highlighted
                    ? Color.white
                    : Color.clear;
        }

        private static StyleCursor CreateResizeHorizontalCursor()
        {
            var property = typeof(UnityEngine.UIElements.Cursor).GetProperty(
                "defaultCursorId", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic);
            if (property == null) return new StyleCursor(StyleKeyword.Null);

            object cursor = new UnityEngine.UIElements.Cursor();
            property.SetValue(cursor, (int)MouseCursor.ResizeHorizontal);
            return new StyleCursor((UnityEngine.UIElements.Cursor)cursor);
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
            _allTreeItems.Clear();
            _treeChildren.Clear();
            _treeItemIds.Clear();
            _reachableNodes.Clear();
            _loadedSubTrees.Clear();
            _subTreeAssetStack.Clear();
            if (!string.IsNullOrEmpty(App.assetPath))
                _subTreeAssetStack.Add(App.assetPath);

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
                AddGraphTreeItem(root, null, 0);
            }

            bool hasRoot = root != null;
            _emptyTreeLabel.style.display = hasRoot ? DisplayStyle.None : DisplayStyle.Flex;
            _nodeTreeContainer.style.display = hasRoot ? DisplayStyle.Flex : DisplayStyle.None;
            _syncingTreeSelection = true;
            _nodeTree.Reload();
            if (hasRoot && !_nodeTreeInitialized)
            {
                _nodeTree.ExpandAll();
                _nodeTreeInitialized = true;
            }
            RestoreTreeSelection();
            _syncingTreeSelection = false;
            RefreshRunningTreeItems();
            RepaintNodeTree();
            _subTreePathHash = CalculateSubTreePathHash();
        }

        private void RestoreTreeSelection()
        {
            GraphNode selected = null;
            for (int i = 0; i < selection.Count; i++)
            {
                if (!(selection[i] is GraphNode node)) continue;
                selected = node;
                break;
            }

            if (selected != null && _treeItemIds.TryGetValue(selected, out int id))
                _nodeTree.SetSelection(new[] { id },
                    IMGUIControls.TreeViewSelectionOptions.RevealAndFrame);
            else
                _nodeTree.SetSelection(Array.Empty<int>());
        }

        private int CalculateSubTreePathHash()
        {
            int hash = 0;
            for (int i = 0; i < _allTreeItems.Count; i++)
            {
                var entry = _allTreeItems[i];
                if (entry.GraphNode == null || !(entry.Data is BTSubTree subTree)) continue;
                int itemHash = entry.GraphNode.GUID.GetHashCode();
                itemHash = itemHash * 397 ^ (subTree.path == null ? 0 : subTree.path.GetHashCode());
                hash ^= itemHash;
            }
            return hash;
        }

        private void AddGraphTreeItem(GraphNode node, NodeTreeEntry parent, int depth)
        {
            string key = $"graph:{node.GUID}";
            int treeId = GetTreeItemId(key);
            var entry = new NodeTreeEntry(node, node.Data, null, null,
                treeId, parent, depth, 0);
            parent?.Children.Add(entry);
            _allTreeItems.Add(entry);
            _treeItemIds[node] = treeId;
            if (_treeChildren.TryGetValue(node, out var children))
            {
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!_reachableNodes.Add(child)) continue;
                    AddGraphTreeItem(child, entry, depth + 1);
                }
            }
            if (node.Data is BTSubTree subTree)
                AddSubTreeRootChildren(subTree.path, depth + 1, 1, entry,
                    new[] { node.Data.guid });
        }

        private void AddSubTreeRootChildren(string path, int depth, int subTreeDepth,
            NodeTreeEntry parent, string[] runtimeTreePath)
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
                    AddSubTreeItem(child, path, loaded, reachable, parent, depth,
                        subTreeDepth, runtimeTreePath);
                }
            }
            finally
            {
                _subTreeAssetStack.Remove(path);
            }
        }

        private void AddSubTreeItem(NodeData node, string path, LoadedSubTree loaded,
            HashSet<NodeData> reachable, NodeTreeEntry parent, int depth, int subTreeDepth,
            string[] runtimeTreePath)
        {
            string route = runtimeTreePath == null
                ? string.Empty
                : string.Join("/", runtimeTreePath);
            string key = $"sub:{route}:{node.guid}";
            var entry = new NodeTreeEntry(null, node, path, runtimeTreePath,
                GetTreeItemId(key), parent, depth, subTreeDepth);
            parent?.Children.Add(entry);
            _allTreeItems.Add(entry);
            if (loaded.Children.TryGetValue(node, out var children))
            {
                for (int i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    if (!reachable.Add(child)) continue;
                    AddSubTreeItem(child, path, loaded, reachable, entry, depth + 1,
                        subTreeDepth, runtimeTreePath);
                }
            }
            if (node is BTSubTree subTree)
                AddSubTreeRootChildren(subTree.path, depth + 1, subTreeDepth + 1, entry,
                    AppendRuntimeTreePath(runtimeTreePath, node.guid));
        }

        private static string[] AppendRuntimeTreePath(string[] path, string guid)
        {
            int count = path == null ? 0 : path.Length;
            var result = new string[count + 1];
            if (count > 0) Array.Copy(path, result, count);
            result[count] = guid;
            return result;
        }

        private int GetTreeItemId(string key)
        {
            if (_treeIdsByKey.TryGetValue(key, out int id)) return id;
            id = _nextTreeId++;
            _treeIdsByKey.Add(key, id);
            return id;
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

        private void SelectTreeNode(NodeTreeEntry entry)
        {
            _syncingTreeSelection = true;
            ClearSelection();
            _subTreeInspectorNode = entry.GraphNode == null ? entry.Data : null;
            if (entry.GraphNode != null) AddToSelection(entry.GraphNode);
            _syncingTreeSelection = false;
            RepaintEditorWindow();
        }

        private void FocusGraphNode(GraphNode node)
        {
            var scale = viewTransform.scale;
            var nodeCenter = node.GetPosition().center;
            var scaledCenter = Vector2.Scale(nodeCenter, new Vector2(scale.x, scale.y));
            var position = contentRect.center - scaledCenter;
            UpdateViewTransform(position, scale);
        }

        protected override bool OnDrawInspector()
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
