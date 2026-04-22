using UnityEditor.Experimental.GraphView;

namespace ActionEditor.Nodes.BT
{
    class BTFailureView : BTDecorateView<BTFailure> { }
    class BTSuccessView : BTDecorateView<BTSuccess> { }
    class BTInverterView : BTDecorateView<BTInverter> { }
    class BTRepeatView : BTDecorateView<BTRepeat> { }
    public class BTDecorateView<T> : BTNodeView<T> where T : BTDecorate, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode));
        }
    }

}
