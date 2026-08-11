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
        public override void OnBTTreeChanged(BTTree tree, Blackboard blackboard)
        {
            base.OnBTTreeChanged(tree, blackboard);
            if (runningNode != null && blackboard != null &&
                blackboard.GetState(runningNode) == null)
                runningNode = tree?.root?.child;
        }


    }





}
