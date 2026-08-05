using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("失败", "子节点结束后始终返回失败。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Failure")]

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
