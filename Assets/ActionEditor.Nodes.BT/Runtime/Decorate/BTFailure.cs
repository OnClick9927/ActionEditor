using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("失败", "完整执行唯一子节点；子节点运行中时继续等待，子节点以成功或失败结束后都统一返回失败。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Failure")]

    public class BTFailure : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Running)
                return State.Running;
            return State.Failure;
        }
    }
}
