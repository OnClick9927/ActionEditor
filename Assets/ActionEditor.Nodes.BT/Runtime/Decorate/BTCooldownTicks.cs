using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("子节点结束后按行为树 Update 次数进入冷却；冷却状态不读取真实时间，并完整写入状态快照。")]
    [Name("Tick 冷却"), Attachable(typeof(BTTree)),
     Node(BTNodeTypes.Decorate), Icon("CooldownTicks")]
    public sealed class BTCooldownTicks : BTDecorateSingle
    {
        public enum CooldownResult
        {
            Failure,
            Success
        }

        [Name("冷却 Tick 数", "子节点每次成功或失败后禁止再次进入的逻辑 Tick 数，不能为负数。")]
        public int tickCount = 1;

        [Name("冷却期结果", "处于冷却期时直接返回的固定结果，不会更新子节点。")]
        public CooldownResult cooldownResult;

        private int GetRemainingTicks(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetRemainingTicks(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => tickCount;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (tickCount < 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(tickCount)} cannot be negative");
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            int remainingTicks = GetRemainingTicks(blackboard);
            if (remainingTicks > 0)
            {
                SetRemainingTicks(blackboard, remainingTicks - 1);
                return cooldownResult == CooldownResult.Success
                    ? State.Success
                    : State.Failure;
            }

            State result = child.Update(blackboard);
            if (result != State.Running)
                SetRemainingTicks(blackboard, tickCount);
            return result;
        }

        protected override State Decorate(Blackboard blackboard, State state) =>
            state;

    }
}
