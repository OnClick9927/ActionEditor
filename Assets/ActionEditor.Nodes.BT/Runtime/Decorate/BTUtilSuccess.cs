using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("直到成功", "反复重新进入唯一子节点：失败时当前节点保持运行中并再次执行，只有子节点成功时才返回成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("UtilSuccess")]
    public class BTUtilSuccess : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Success)
                return state;
            return State.Running;
        }
    }
}
