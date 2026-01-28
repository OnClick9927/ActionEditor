using ActionEditor;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Graphs;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{

    class GraphWindow : EditorWindow, ISearchWindowProvider
    {
        [OnOpenAsset(1)]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            var path = AssetDatabase.GetAssetPath(instanceID);
            if (path.EndsWith(GraphAsset.FileEx))
            {
                App.OnObjectPickerConfig(path);
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
                GUILayout.Label($"{Lan.ins.HeaderLastSaveTime} {App.LastSaveTime.ToString("HH:mm:ss.ff")}");
            }
            GUILayout.FlexibleSpace();
            view?.OnFootGUI();

            GUILayout.EndHorizontal();
        }
        private void OnToolBarGUI()
        {
            GUILayout.BeginHorizontal();
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, EditorGUIUtility.TrIconContent("Toolbar Plus"), EditorStyles.toolbarButton))
                {
                    CreateAssetWindow.Show(rect);
                }
            }


            {

                var file = Path.GetFileName(App.assetPath);
                var gName = file;
                gName = gName.Replace($".{GraphAsset.FileEx}", "");
                gName = string.IsNullOrEmpty(gName) ? "None" : gName;
                var size = GUI.skin.label.CalcSize(new GUIContent(gName));
                var width = size.x + 8;
                if (width < 80) width = 80;
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(width + 20));
                if (GUI.Button(rect, $"[{gName}]", EditorStyles.toolbarDropDown))
                {
                    App.Save();

                    ActionEditor.AssetPick.ShowObjectPicker(rect, "Assets", "t:TextAsset", Prefs.pickListType, (o) =>
                    {
                        App.OnObjectPickerConfig(AssetDatabase.GetAssetPath(o));
                        GUIUtility.ExitGUI();
                    }, (x) =>
                    {
                        return x.EndsWith(GraphAsset.FileEx);

                    });
                }
                if (App.asset != null)
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);
                    if (GUILayout.Button(EditorGUIUtility.IconContent("d_Folder Icon"), EditorStyles.iconButton))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(App.assetPath));
                    }
                    GUILayout.EndVertical();
                }

            }





            view?.OnHeaderGUI();
            GUILayout.FlexibleSpace();
            if (view != null)
            {
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("SaveAs")))
                {
                    App.SaveAs();
                }
                if (GUILayout.Button(EditorGUIUtility.TrIconContent("SaveActive")))
                {
                    App.Save();
                }
            }
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, EditorGUIUtility.TrIconContent("Settings"),
                        EditorStyles.toolbarButton))
                {
                    PreferencesWindow.Show(rect);
                }
            }
            GUILayout.EndHorizontal();

        }

        private NodeGraphView view;
        private VisualElement left;
        //private Label saveTime;
        private GridView grid;
        private TwoPaneSplitView split;
        IMGUIContainer right;
        public bool showMiniMap;

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
            left = new VisualElement();
            right = new IMGUIContainer(this.DrawInspector);

            // 6. 将两个面板添加到分割视图（必须按 0、1 顺序）
            split.Add(left); // Pane 0
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

        private void Update()
        {
            split.visible = this.view != null;
            split.fixedPaneInitialDimension = this.view == null ? 0 : split.fixedPaneInitialDimension;
            right.style.minWidth = this.view == null ? 0 : 250;
            if (view == null)
            {

            }
            else
            {
                App.Update();
            }
        }
        private void DrawInspector() => view?.DrawInspector();
        private void OnDisable() => App.OnWindowDisable();

        List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context) => view?.CreateSearchTree(context);

        bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context) => view.OnSelectEntry(SearchTreeEntry, context);

        internal void ShowGraph()
        {

            if (view != null)
            {
                view.parent.Remove(view);
            }
            view = null;
            left.Clear();
            view = App.CreateView(left);
            left.visible = view != null;
            grid.visible = !left.visible;
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