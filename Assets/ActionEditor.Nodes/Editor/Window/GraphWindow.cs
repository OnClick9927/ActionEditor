using ActionUnity;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{
    class GraphWindow : EditorWindow, ISearchWindowProvider
    {
        private static GUIContent _createAssetContent;
        private static GUIContent _folderContent;
        private static GUIContent _inspectorContent;
        private static GUIContent _undoHistoryContent;
        private static GUIContent _saveAsContent;
        private static GUIContent _saveContent;
        private static GUIContent _settingsContent;

        [OnOpenAsset(1)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            if (path.EndsWith(GraphAsset.FileEx))
            {
                if (!App.OnObjectPickerConfig(path)) return true;
                if (App.asset != null)
                {
                    App.OnWindowDisable();
                    OpenWindow();
                    return true;
                }
            }
            return false;
        }

        [MenuItem("Tools/NodeGraph")]
        private static void OpenWindow() => GetWindow<GraphWindow>();

        private void OnFootGUI()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (view != null)
            {
                string prefix = Lan.ins.HeaderLastSaveTime;
                if (_lastSaveTime != App.LastSaveTime || _lastSaveContent == null ||
                    _lastSavePrefix != prefix)
                {
                    _lastSaveTime = App.LastSaveTime;
                    _lastSavePrefix = prefix;
                    _lastSaveContent = new GUIContent(
                        $"{prefix} {_lastSaveTime:HH:mm:ss.ff}");
                }
                GUILayout.Label(_lastSaveContent);
            }
            GUILayout.FlexibleSpace();
            view?.OnFootGUI();

            GUILayout.EndHorizontal();
        }
        private void OnToolBarGUI()
        {
            EnsureToolbarContent();
            GUILayout.BeginHorizontal();
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, _createAssetContent, EditorStyles.toolbarButton))
                {
                    CreateAssetWindow.Show(rect);
                }
            }
            {
                UpdateAssetToolbarContent();
                var rect = EditorGUILayout.GetControlRect(
                    GUILayout.Width(_assetToolbarWidth));
                if (GUI.Button(rect, _assetToolbarContent, EditorStyles.toolbarDropDown))
                {
                    ActionEditor.AssetPick.ShowObjectPicker(rect, "Assets", "t:TextAsset", Prefs.pickListType, (o) =>
                    {
                        App.OnObjectPickerConfig(AssetDatabase.GetAssetPath(o));
                        GUIUtility.ExitGUI();
                    }, (x) =>
                    {
                        if (view == null)
                            return x.EndsWith(GraphAsset.FileEx);
                        return x.EndsWith(GraphAsset.FileEx) && view.IsFileFitAsset(x);

                    });
                }
                if (App.asset != null)
                {
                    var projectRect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                    if (GUI.Button(projectRect, _folderContent, EditorStyles.toolbarButton))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                            App.assetPath));
                    }
                }
            }





            view?.DrawHeaderToolbar();
            GUILayout.FlexibleSpace();
            if (view != null)
            {
                App.window.showInspector = GUILayout.Toggle(App.window.showInspector,
                    _inspectorContent, EditorStyles.toolbarButton);
                var undoRect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(undoRect, _undoHistoryContent, EditorStyles.toolbarButton))
                {
                    ShowUndoHistory(undoRect);
                }
                if (GUILayout.Button(_saveAsContent, EditorStyles.toolbarButton,
                        GUILayout.Width(25)))
                {
                    App.SaveAs();
                }
                if (GUILayout.Button(_saveContent, EditorStyles.toolbarButton))
                {
                    App.Save();
                }
            }
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, _settingsContent, EditorStyles.toolbarButton))
                {
                    PreferencesWindow.Show(rect);
                }
            }
            GUILayout.EndHorizontal();

        }

        private static void EnsureToolbarContent()
        {
            if (_createAssetContent != null) return;
            _createAssetContent = EditorGUIUtility.TrIconContent("Toolbar Plus", "Create");
            _folderContent = EditorGUIUtility.TrIconContent("d_Project", "Show in Project");
            _inspectorContent = EditorGUIUtility.TrIconContent(
                "d_UnityEditor.InspectorWindow", "Inspector");
            _undoHistoryContent = EditorGUIUtility.TrIconContent(
                "d_UndoHistory", "Undo History");
            _saveAsContent = EditorGUIUtility.TrIconContent("SaveAs", "Save As");
            _saveContent = EditorGUIUtility.TrIconContent("SaveActive", "Save");
            _settingsContent = EditorGUIUtility.TrIconContent("Settings", "Settings");
        }

        private void UpdateAssetToolbarContent()
        {
            string path = App.assetPath ?? string.Empty;
            bool isDirty = App.IsDirty;
            if (_assetToolbarPath == path && _assetToolbarDirty == isDirty &&
                _assetToolbarContent != null) return;

            _assetToolbarPath = path;
            _assetToolbarDirty = isDirty;
            string name = Path.GetFileName(path);
            name = name.Replace($".{GraphAsset.FileEx}", "");
            if (string.IsNullOrEmpty(name)) name = "None";
            _assetToolbarContent = new GUIContent($"[{name}]{(isDirty ? "*" : string.Empty)}");
            _assetToolbarWidth = Mathf.Max(80, GUI.skin.label.CalcSize(
                _assetToolbarContent).x + 8) + 20;
        }

        private void ShowUndoHistory(Rect rect)
        {
            App.FlushUndoHistory();
            UnityEditor.PopupWindow.Show(rect, new UndoHistoryPopup(position.height));
        }

        private sealed class UndoHistoryPopup : PopupWindowContent
        {
            private const float Width = 440;
            private const float RowHeight = 22;
            private const float Padding = 4;
            private const float FooterHeight = 25;
            private readonly float _height;
            private Vector2 _scrollPosition;
            private GUIStyle _rowStyle;
            private GUIStyle _stateStyle;

            internal UndoHistoryPopup(float height)
            {
                _height = height;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(Width, _height);
            }

            public override void OnOpen()
            {
                editorWindow.wantsMouseMove = true;
                int row = App.UndoHistoryCount - App.CurrentUndoIndex - 1;
                float viewportHeight = GetWindowSize().y - Padding * 2 - FooterHeight;
                _scrollPosition.y = Mathf.Max(0,
                    row * RowHeight - (viewportHeight - RowHeight) * 0.5f);
            }

            public override void OnGUI(Rect rect)
            {
                int count = App.UndoHistoryCount;
                if (count == 0)
                {
                    GUI.Label(rect, "No Undo History",
                        EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                EnsureStyles();
                var viewport = new Rect(Padding, Padding,
                    rect.width - Padding * 2,
                    rect.height - Padding * 2 - FooterHeight);
                float contentHeight = count * RowHeight;
                float contentWidth = viewport.width -
                    (contentHeight > viewport.height ? 16 : 1);
                var content = new Rect(0, 0, contentWidth, contentHeight);
                _scrollPosition = GUI.BeginScrollView(viewport,
                    _scrollPosition, content);

                int clickedIndex = -1;
                int currentIndex = App.CurrentUndoIndex;
                Event currentEvent = Event.current;
                for (int row = 0; row < count; row++)
                {
                    int index = count - row - 1;
                    var rowRect = new Rect(0, row * RowHeight,
                        content.width, RowHeight);
                    bool isCurrent = index == currentIndex;
                    bool isHovered = rowRect.Contains(currentEvent.mousePosition);
                    if (isCurrent)
                    {
                        EditorGUI.DrawRect(rowRect,
                            new Color(0.24f, 0.49f, 0.9f, 0.65f));
                    }
                    else if (isHovered)
                    {
                        EditorGUI.DrawRect(rowRect,
                            EditorGUIUtility.isProSkin
                                ? new Color(1, 1, 1, 0.08f)
                                : new Color(0, 0, 0, 0.08f));
                    }

                    string state = index < currentIndex
                        ? "Undo"
                        : isCurrent ? "Current" : "Redo";
                    GUI.Label(new Rect(rowRect.x + 7, rowRect.y,
                            rowRect.width - 128, RowHeight),
                        $"{index}  {App.GetUndoHistoryName(index)}", _rowStyle);
                    GUI.Label(new Rect(rowRect.xMax - 120, rowRect.y,
                            114, RowHeight),
                        $"{App.GetUndoHistoryTime(index)}  {state}", _stateStyle);
                    EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

                    if (currentEvent.type == EventType.MouseDown &&
                        currentEvent.button == 0 && isHovered)
                        clickedIndex = index;
                }

                GUI.EndScrollView();
                var clearRect = new Rect(rect.xMax - Padding - 100,
                    rect.yMax - Padding - 20, 100, 20);
                if (GUI.Button(clearRect, "Clear History", EditorStyles.miniButton))
                {
                    App.ClearUndoHistory();
                    editorWindow.Close();
                    return;
                }
                if (clickedIndex < 0) return;
                currentEvent.Use();
                App.RestoreUndoHistory(clickedIndex);
                editorWindow.Close();
            }

            private void EnsureStyles()
            {
                if (_rowStyle != null) return;
                _rowStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
                _stateStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight
                };
            }
        }

        private NodeGraphView view;
        private GUIContent _assetToolbarContent;
        private GUIContent _lastSaveContent;
        private string _assetToolbarPath;
        private bool _assetToolbarDirty;
        private string _lastSavePrefix;
        private float _assetToolbarWidth;
        private System.DateTime _lastSaveTime;
        private VisualElement graphHost;
        //private Label saveTime;
        private GridView grid;
        private TwoPaneSplitView split;
        IMGUIContainer right;
        public bool showMiniMap;
        public bool showInspector;

        private void OnEnable()
        {
            App.window = this;
            grid = new GridView();
            rootVisualElement.Add(grid);
            grid.StretchToParentSize();

            split = new TwoPaneSplitView();

            // 2. 核心配置（必选）
            split.orientation = TwoPaneSplitViewOrientation.Horizontal;
            split.style.flexGrow = 1; // 让分割视图占满父容器
            graphHost = new VisualElement();
            graphHost.style.flexGrow = 1;
            graphHost.style.minWidth = 0;
            right = new IMGUIContainer(this.DrawInspector);

            // 6. 将两个面板添加到分割视图（必须按 0、1 顺序）
            split.Add(graphHost); // Pane 0
            split.Add(right); // Pane 1
            // 7. 将分割视图添加到窗口根节点
            rootVisualElement.Add(split);
            split.StretchToParentSize();
            split.fixedPaneIndex = 1;
            split.fixedPaneInitialDimension = 300;
            split.style.top = 20;
            split.style.bottom = 20;



            var _toolBar = new Toolbar();
            var header = new IMGUIContainer(OnToolBarGUI);
            header.style.position = new StyleEnum<Position>(Position.Absolute);
            header.style.left = header.style.right = header.style.top = header.style.bottom = 0;
            _toolBar.Add(header);
            rootVisualElement.Add(_toolBar);

            var foot = new IMGUIContainer(OnFootGUI);
            foot.style.position = Position.Absolute;
            foot.style.bottom = foot.style.right = foot.style.left = 0;
            rootVisualElement.Add(foot);

            //saveTime = new Label();
            //saveTime.style.position = new StyleEnum<Position>(Position.Absolute);
            //saveTime.style.left = 20;
            //saveTime.style.fontSize = 20;
            //saveTime.style.color = (Color.blue / 2 + Color.cyan);
            //saveTime.style.bottom = 20;
            //this.rootVisualElement.Add(saveTime); ;
            App.OnWindowEnable();
        }
        internal static float sp_width = 0;
        private void _update()
        {
            split.visible = this.view != null;
            split.fixedPaneInitialDimension = this.view == null ? 0 : split.fixedPaneInitialDimension;
            right.style.minWidth = this.view == null ? 0 : 250;
            sp_width = split.fixedPane.style.width.value.value;
            if (view != null)
            {
                App.Update();
                Repaint();
            }
        }
        private void Update()
        {
            if (App.updateType == NodeGraphView.UpdateType.Update) _update();
        }
        private void OnInspectorUpdate()
        {

            if (App.updateType == NodeGraphView.UpdateType.Inspector) _update();


        }
        private void DrawInspector()
        {
            view?.DrawInspectorPanel();
        }
        private void OnDisable()
        {
            App.OnWindowDisable();
            App.ShutdownUndo();
        }

        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context) => view?.CreateSearchTree(context);

        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context) => view.OnSelectEntry(SearchTreeEntry, context);

        internal void ShowGraph()
        {

            if (view != null)
            {
                view.RemoveFromHierarchy();
            }
            view = null;
            graphHost.Clear();
            view = App.CreateView(graphHost);
            graphHost.visible = view != null;
            grid.visible = !graphHost.visible;
            if (view != null)
                this.titleContent = new GUIContent(EditorEX.GetTypeName(App.asset));
        }

        private class GridView : GraphView
        {
            public GridView()
            {
                this.pickingMode = PickingMode.Ignore;
                var styleSheet = Resources.Load<StyleSheet>("NodeGraphView");
                if (styleSheet != null) styleSheets.Add(styleSheet);
                var grid = new GridBackground();
                Insert(0, grid);
                grid.StretchToParentSize();

            }
        }
    }
}
