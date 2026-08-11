using ActionAttribute;
using System;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("限制唯一子节点可持续运行的行为树 Update 次数；超出上限时主动中止子节点并失败，不依赖真实时间。")]
    [Name("超时 Tick"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("TimeoutTicks")]
    public class BTTimeoutTicks : BTDecorateSingle
    {
        [Name("超时 Tick 数", "子节点保持运行中状态时允许消耗的最大逻辑 Tick 数，必须为正数；已消耗计数会写入状态快照。")]
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
            if (tickCount <= 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(tickCount)} must be positive");
        }

        protected override void OnStart(Blackboard blackboard)
        {
            SetElapsedTicks(blackboard, 0);
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            State result = base.OnUpdate(blackboard);
            if (result != State.Running) return result;

            int elapsedTicks = GetElapsedTicks(blackboard) + 1;
            SetElapsedTicks(blackboard, elapsedTicks);
            if (elapsedTicks < tickCount) return State.Running;
            child.Abort(blackboard);
            return State.Failure;
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
