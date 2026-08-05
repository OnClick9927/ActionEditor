using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("直到失败", "持续执行子节点，直到子节点返回失败。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("UtilFailure")]
    public class BTUtilFailure : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Failure)
                return state;
            return State.Running;
        }
    }
}
