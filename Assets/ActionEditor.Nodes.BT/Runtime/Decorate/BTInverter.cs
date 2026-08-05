using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("取反", "交换子节点的成功与失败结果。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("Inverter")]

    public class BTInverter : BTDecorateSingle
    {
        protected override State Decorate(State state)
        {
            if (state == State.Success)
                return State.Failure;
            if (state == State.Failure)
                return State.Success;
            return state;
        }
    }
}
