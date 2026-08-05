using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("直到成功", "持续执行子节点，直到子节点返回成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("UtilSuccess")]
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
