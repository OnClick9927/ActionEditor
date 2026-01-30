using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{
    interface IBTNodeView
    {
        void OnBTTreeChanged(BTTree tree);
    }
    public class BTNodeView<T> : GraphNode<T>, IBTNodeView where T : BTNode, new()
    {
        private GraphConnection flow;
        ProgressBar progress;
        protected BTNode runningNode { get; private set; }
        protected new Port GeneratePort(Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single, string name = "")
        {
            var port = base.GeneratePort(portDir, type, capacity, name);
            if (portDir == Direction.Input)
            {
                port.portColor = new Color(0.5f, 0.3f, 0.8f) + Color.white / 2;
                port.portName = "In";
            }
            else
            {
                port.portColor = new Color(0.3f, 0.5f, 0.8f) + Color.white / 2;
                port.portName = "Out";

            }
            return port;

        }
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);

            progress = new ProgressBar();
            progress.style.position = Position.Absolute;
            progress.style.height = 10;
            progress.style.bottom = progress.style.left = progress.style.right = 0;
            progress.style.bottom = -6;
            progress.lowValue = 0;
            progress.highValue = 1;
            this.titleContainer.Add(progress);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (flow != null)
                flow.enableFlow = false;
            progress.visible = false;
            if (runningNode != null && runningNode.state == BTNode.State.Running)
            {
                var time = EditorApplication.timeSinceStartup;
                time %= 1f;
                progress.value = (float)time;
                progress.visible = true;
                if (flow != null)
                    flow.enableFlow = true;
            }

        }

        public virtual void OnBTTreeChanged(BTTree tree)
        {
            if (flow == null)
                flow = this.connections.FirstOrDefault(x => x.input.node == this);
            if (tree == null)
            {
                if (flow != null)
                    flow.enableFlow = false;
                runningNode = null;
                progress.visible = false;
            }
            else
            {
                runningNode = tree.FindNode<BTNode>(this.GUID);

            }
        }
    }

}
