using UnityEditor.Experimental.GraphView;

namespace ActionEditor.Nodes.BT
{
    class BTRootView : BTNodeView<BTRoot>
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Output, typeof(BTNode));
        }
    }





}
