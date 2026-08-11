using ActionAttribute;
using System;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("进入后先按行为树 Update 次数等待，延迟结束才开始执行唯一子节点；不读取真实时间，适用于确定性帧同步。")]
    [Name("延迟 Tick"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("DelayTicks")]
    public class BTDelayTicks : BTDecorateSingle
    {
        [Name("延迟 Tick 数", "开始更新子节点前必须经过的逻辑 Tick 数；当前等待计数会写入状态快照，中止时按节点规则重置。")]
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
            if (elapsedTicks < tickCount)
            {
                SetElapsedTicks(blackboard, elapsedTicks + 1);
                return State.Running;
            }
            return base.OnUpdate(blackboard);
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            try
            {
                base.OnAbort(blackboard);
            }
            finally
            {
                SetElapsedTicks(blackboard, 0);
            }
        }

        protected override State Decorate(Blackboard blackboard, State state) =>
            state;

    }
}
