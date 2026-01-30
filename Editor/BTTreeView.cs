using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace ActionEditor.Nodes.BT
{


    public class BTTreeView<T> : Nodes.NodeGraphView<T> where T : BTTree
    {
        protected BTTree runningTree { get; private set; }

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
            var blackboard = run ? view.runningTree.blackBoard : view.graph.blackBoard;

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
                    if (BTTree.instance != null && App.asset.guid == BTTree.instance.guid)
                        Runing_BlackBoard = true;
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
                using (new UnityEditor.EditorGUI.DisabledScope(run))
                    EditorEX.CreateEditor(blackboard).OnInspectorGUI();
                GUILayout.EndVertical();
            }
        }
        protected override void OnInspectorGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(height));
            base.OnInspectorGUI();
            GUILayout.EndVertical();
            DrawBlackBord(this, position.height);
            GUILayout.Space(2);
        }






        public override void Load(GraphAsset data)
        {
            base.Load(data);
            BTTree_onInstanceChanged(BTTree.instance);
            BTTree.onInstanceChanged -= BTTree_onInstanceChanged;

            BTTree.onInstanceChanged += BTTree_onInstanceChanged;
        }
        private void BTTree_onInstanceChanged(BTTree tree)
        {
            if (tree != null && tree.guid != this.graph.guid)
                tree = null;
            this.runningTree = tree;
            OnBTTreeChanged(tree);

            for (int i = 0; this.nodes.Count > i; i++)
            {
                var node = this.nodes[i] as IBTNodeView;
                node.OnBTTreeChanged(tree);
            }
        }
        protected virtual void OnBTTreeChanged(BTTree tree)
        {

        }
        public override void OnSelectNode(GraphNode obj)
        {
        }

        protected override void AfterCreateNode(GraphElement element)
        {
            if (port == null) return;
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

        }
        GraphPort port;
        protected override List<Type> FitterNodeTypes(List<Type> src, GraphElement element)
        {
            src.RemoveAll(x => !EditorEX.CanAttachTo(App.GetNodeDataType(x), typeof(BTTree))
            && !EditorEX.CanAttachTo(App.GetNodeDataType(x), typeof(T))
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
