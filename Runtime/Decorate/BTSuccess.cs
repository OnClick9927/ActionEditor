using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("成功", "子节点结束后始终返回成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("Success")]

    public class BTSuccess : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Running)
                return State.Running;
            return State.Success;
        }
    }
}
