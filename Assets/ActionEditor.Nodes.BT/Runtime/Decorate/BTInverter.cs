using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("取反", "执行唯一子节点并交换其成功与失败结果；运行中状态保持不变，中止时会继续向正在运行的子节点传递。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("Inverter")]

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
