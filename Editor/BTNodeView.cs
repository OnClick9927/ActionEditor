using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{
    public class BTNodeView<T> : GraphNode<T> where T : BTNode, new()
    {
        protected new Port GeneratePort(Direction portDir, Type type, Port.Capacity capacity = Port.Capacity.Single, string name = "")
        {
            var port = base.GeneratePort(portDir, type, capacity,name);
            if(portDir== Direction.Input)
            {
                port.portColor = new Color(0.5f,0.3f,0.8f)+Color.white/2;
                port.portName = "In";
            }
            else
            {
                port.portColor = new Color(0.3f, 0.5f, 0.8f) + Color.white / 2;
                port.portName = "Out";

            }
            return port;

        }
        ProgressBar progress;
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

        private GraphConnection flow;
        public override void OnUpdate()
        {
            base.OnUpdate();
            if (flow != null)
                flow.enableFlow = false;
            progress.visible = false;
            if (BTTree.instance != null && BTTree.instance.guid == App.asset.guid)
            {
                var node = BTTree.instance.FindNode<BTNode>(this.GUID);
                if (node != null && node.state == BTNode.State.Running)
                {
                    var time = EditorApplication.timeSinceStartup;
                    time %= 1f;
                    progress.value = (float)time;
                    progress.visible = true;
                    flow = this.connections.FirstOrDefault(x => x.input.node == this);
                    if (flow != null)
                        flow.enableFlow = true;
                }
            }

        }

    }

}
