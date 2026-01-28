using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{




    public class BTTreeView<T> : Nodes.NodeGraphView<T> where T : BTTree
    {

        private static float _height = -1;
        internal static float height
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
        public static void DrawBlackBord(Blackboard blackboard, float maxheight)
        {
            GUI.color = Color.black;
            GUILayout.Box("", GUILayout.Height(30), GUILayout.ExpandWidth(true));
            GUI.color = Color.white;
            var rect = GUILayoutUtility.GetLastRect();
            EditorGUI.LabelField(rect, "BlackBord", new GUIStyle(EditorStyles.largeLabel)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold
            });
            Event e = Event.current;
            if (e.type == EventType.MouseDrag && rect.Contains(e.mousePosition))
            {
                height += e.delta.y;
                height = Mathf.Clamp(height, 100, maxheight - 300);
                e.Use();
            }
            GUILayout.BeginVertical(EditorStyles.helpBox);
            EditorEX.DrawPingScript(blackboard.GetType());
            EditorEX.CreateEditor(blackboard).OnInspectorGUI();
            GUILayout.EndVertical();
        }
        protected override void OnInspectorGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(height));
            base.OnInspectorGUI();
            GUILayout.EndVertical();
            DrawBlackBord(graph.blackBoard, position.height);
            GUILayout.Space(2);
        }

        public override void Load(GraphAsset data)
        {
            base.Load(data);

        }
        public override void OnSelectNode(GraphNode obj)
        {
        }

        protected override void AfterCreateNode(GraphElement element)
        {
            if (port == null) return;
            if (element.GetType() == port.node.GetType())
            {
                if (port.direction == Direction.Input)
                {
                    App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Output));
                }
                else
                {
                    App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Input));
                }
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
                src.RemoveAll(x => port.node.GetType() != x);
            }
            return src;
        }

        protected override bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end)
        {
            return start.portType == end.portType;
        }
    }


    public class BTNodeView<T> : ActionEditor.Nodes.GraphNode<T> where T : BTNode, new()
    {
        public override void OnInspectorGUI()
        {
            GUILayout.BeginVertical(GUILayout.Height(BTTreeView<BTTree>.height));
            base.OnInspectorGUI();
            GUILayout.EndVertical();
            BTTreeView<BTTree>.DrawBlackBord((App.asset as BTTree).blackBoard, App.view.position.height);

        }
        ProgressBar progress;
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);

            progress = new ProgressBar();
            progress.style.position = Position.Absolute;
            progress.style.height = 6;
            progress.style.bottom = progress.style.left = progress.style.right = 0;
            progress.style.bottom = -3;
            progress.lowValue = 0;
            progress.highValue = 1;
            this.titleContainer.Add(progress);
        }
        public override void OnUpdate()
        {
            base.OnUpdate();
            progress.visible = false;
            if (BTTree.instance != null)
            {
                var node = BTTree.instance.FindNode(this.GUID);
                if (node != null && node.state == BTNode.State.Running)
                {
                    var time = EditorApplication.timeSinceStartup;
                    time %= 1f;
                    progress.value = (float)time;
                    progress.visible = true;
                }
            }

        }

    }
    public class RootView : BTNodeView<BTRoot>
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Output, typeof(BTNode));
        }
    }
    public class DecorateView<T> : BTNodeView<T> where T : BTDecorate, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode));
        }
    }
    public class CompositeView<T> : BTNodeView<T> where T : BTComposite, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            IMGUIContainer abort = new IMGUIContainer(DrawAbort);
            abort.style.position = Position.Absolute;
            abort.style.width = abort.style.height = 20;
            abort.style.left = abort.style.top = 10;
            this.Add(abort);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode), Port.Capacity.Multi);
        }

        private void DrawAbort()
        {
            string icon = string.Empty;
            switch (this.data.abortType)
            {
                case BTComposite.AbortType.None:
                    break;
                case BTComposite.AbortType.Self:
                    icon = "ConditionalAbortLowerPriorityIcon";
                    break;
                case BTComposite.AbortType.LowerPriority:
                    icon = "ConditionalAbortSelfIcon";
                    break;
                case BTComposite.AbortType.Both:
                    icon = "ConditionalAbortBothIcon";
                    break;
                default:
                    break;
            }
            if (!string.IsNullOrEmpty(icon))
                GUILayout.Box(Resources.Load<Texture2D>(icon), EditorStyles.iconButton);

        }
    }
    public class ConditionView<T> : BTNodeView<T> where T : BTCondition, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
        }
    }
    public class ActionView<T> : BTNodeView<T> where T : BTAction, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
        }
    }

    class SequenceView : CompositeView<BTSeuquence> { }
    class SelectorView : CompositeView<BTSelector> { }
    class ParallelView : CompositeView<BTParallel> { }

    class BTFailureView : DecorateView<BTFailure> { }
    class BTSuccessView : DecorateView<BTSuccess> { }
    class BTInverterView : DecorateView<BTInverter> { }
    class BTRepeatView : DecorateView<BTRepeat> { }

}
