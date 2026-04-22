using UnityEditor.Experimental.GraphView;

namespace ActionEditor.Nodes.BT
{
    public class BTConditionView<T> : BTNodeView<T> where T : BTCondition, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
        }
    }

}
