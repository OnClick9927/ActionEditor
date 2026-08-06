using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("重复", "根据配置决定子节点成功或失败后是否立即重新进入；只要需要重启就保持运行中，否则透传子节点最终结果。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate),Icon("Repeater")]

    public class BTRepeat : BTDecorateSingle
    {
        [Name("成功后重启", "开启时，子节点成功不会结束当前节点，而会在后续更新中重新从子节点入口执行。")]
        public bool restartOnSuccess = true;
        [Name("失败后重启", "开启时，子节点失败不会结束当前节点，而会在后续更新中重新从子节点入口执行。")]
        public bool restartOnFailure = true;
        protected override State Decorate(State state)
        {
            if (state == State.Running)
                return State.Running;
            if (state == State.Failure)
                if (restartOnFailure)
                    return State.Running;
                else
                    return State.Failure;
            if (state == State.Success)
                if (restartOnSuccess)
                    return State.Running;
                else
                    return State.Success;
            return State.Success;
        }
    }
}
