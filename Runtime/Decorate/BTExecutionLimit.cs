using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("限制整个行为树运行会话中唯一子节点可完整结束的累计次数；计数不会因节点重新进入而清零，并包含在状态快照中。")]
    [Name("执行上限"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("ExecutionLimit")]
    public sealed class BTExecutionLimit : BTDecorateSingle
    {
        public enum LimitResult
        {
            Failure,
            Success
        }

        [Name("最大执行次数", "当前运行会话内允许子节点结束的累计上限，必须大于零；达到上限后不再进入子节点。")]
        public int maxExecutions = 1;
        [Name("耗尽结果", "累计完成次数达到上限后，不再执行子节点并直接返回的固定状态，可配置为成功或失败。")]
        public LimitResult exhaustedResult;
        private int GetCompletedExecutions(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetCompletedExecutions(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => maxExecutions;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (maxExecutions <= 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(maxExecutions)} must be positive");
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            int completedExecutions = GetCompletedExecutions(blackboard);
            if (completedExecutions >= maxExecutions)
                return exhaustedResult == LimitResult.Success
                    ? State.Success
                    : State.Failure;

            State result = child.Update(blackboard);
            if (result != State.Running)
                SetCompletedExecutions(blackboard, completedExecutions + 1);
            return result;
        }

        protected override State Decorate(Blackboard blackboard, State state) =>
            state;

    }
}
