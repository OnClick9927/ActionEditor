using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("按固定次数完整执行唯一子节点；已完成次数写入状态快照，达到总次数后返回最后一次结果或成功。")]
    [Name("重复次数"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("RepeatCount")]
    public class BTRepeatCount : BTDecorateSingle
    {
        [Name("重复次数", "一次进入期间要求子节点结束的总次数，必须为正数；运行中断后是否保留由节点的状态恢复流程决定。")]
        public int repeatCount = 1;
        [Name("失败时停止", "开启后任意一轮失败都会立即返回失败；关闭时失败也计为一次完成并继续剩余轮次。")]
        public bool stopOnFailure;
        private int GetCompletedCount(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetCompletedCount(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => repeatCount;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (repeatCount < 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(repeatCount)} cannot be negative");
        }

        protected override void OnStart(Blackboard blackboard)
        {
            SetCompletedCount(blackboard, 0);
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            if (repeatCount == 0) return State.Success;

            State result = child.Update(blackboard);
            if (result == State.Running) return State.Running;
            if (result == State.Failure && stopOnFailure) return State.Failure;

            int completedCount = GetCompletedCount(blackboard) + 1;
            SetCompletedCount(blackboard, completedCount);
            return completedCount < repeatCount ? State.Running : result;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            try
            {
                base.OnAbort(blackboard);
            }
            finally
            {
                SetCompletedCount(blackboard, 0);
            }
        }

        protected override State Decorate(Blackboard blackboard, State state) =>
            state;

    }
}
