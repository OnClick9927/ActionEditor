using UnityEditor.Experimental.GraphView;

namespace ActionEditor.Nodes.BT
{
    public class BTActionView<T> : BTNodeView<T> where T : BTAction, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
        }
    }

}
