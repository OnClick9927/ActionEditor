using System.Xml.Linq;
using UnityEditor.Experimental.GraphView;

namespace ActionEditor.Nodes.BT
{
    class BTUtilSuccessView : BTDecorateSingleView<BTUtilSuccess> { }
    class BTUtilFailureView : BTDecorateSingleView<BTUtilFailure> { }

    class BTFailureView : BTDecorateSingleView<BTFailure> { }
    class BTSuccessView : BTDecorateSingleView<BTSuccess> { }
    class BTInverterView : BTDecorateSingleView<BTInverter> { }
    class BTRepeatView : BTDecorateSingleView<BTRepeat> { }
    class BTORView : BTDecorateMutiView<BTOR> { }
    class BTAndView : BTDecorateMutiView<BTAnd> { }

    public class BTDecorateSingleView<T> : BTNodeView<T> where T : BTDecorateSingle, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode));
        }
    }
    public class BTDecorateMutiView<T> : BTNodeView<T> where T : BTDecorateMuti, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode), Port.Capacity.Multi);
        }
    }
}
