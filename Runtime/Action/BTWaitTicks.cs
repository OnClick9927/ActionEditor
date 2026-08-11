using ActionAttribute;
using System;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("完全基于行为树更新次数计时：进入后持续返回运行中，累计到指定逻辑 Tick 数时返回成功，不依赖真实时间或浮点数。")]
    [Name("等待 Tick"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("WaitTicks")]
    public class BTWaitTicks : BTAction
    {
        [Name("等待 Tick 数", "从节点进入开始需要经过的行为树 Update 次数；计数器会写入运行时状态快照，中止或重新进入时按节点规则重置。")]
        public int tickCount = 1;
        private int GetElapsedTicks(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetElapsedTicks(Blackboard blackboard, int value) =>
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

        protected override void OnStart(Blackboard blackboard)
        {
            SetElapsedTicks(blackboard, 0);
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            int elapsedTicks = GetElapsedTicks(blackboard);
            if (elapsedTicks >= tickCount) return State.Success;
            SetElapsedTicks(blackboard, elapsedTicks + 1);
            return State.Running;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            SetElapsedTicks(blackboard, 0);
        }

    }
}
