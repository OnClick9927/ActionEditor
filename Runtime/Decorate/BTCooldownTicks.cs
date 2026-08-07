using System;
using System.Collections.Generic;
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

        [NonSerialized] private int remainingTicks;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (tickCount < 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(tickCount)} cannot be negative");
            remainingTicks = 0;
        }

        protected override State OnUpdate()
        {
            if (remainingTicks > 0)
            {
                remainingTicks--;
                return cooldownResult == CooldownResult.Success
                    ? State.Success
                    : State.Failure;
            }

            State result = child.Update();
            if (result != State.Running) remainingTicks = tickCount;
            return result;
        }

        protected override State Decorate(State state) => state;

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(remainingTicks);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > tickCount ||
                (state == State.Running && value != 0))
                throw new ArgumentException("Invalid cooldown runtime status",
                    nameof(values));
            remainingTicks = value;
        }
    }
}
