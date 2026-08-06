using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("直到失败", "反复重新进入唯一子节点：成功时当前节点保持运行中并再次执行，只有子节点失败时才返回失败。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("UtilFailure")]
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
