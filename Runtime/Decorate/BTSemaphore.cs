using ActionAttribute;
using System;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("进入唯一子分支前申请行为树整数信号量，结束或中止时归还，用于确定性限制可同时运行的分支数量。")]
    [Name("信号量"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Semaphore")]
    public class BTSemaphore : BTDecorateSingle
    {
        [Name("等待空位", "信号量达到上限时，开启会保持运行中并在后续 Tick 重试申请；关闭则不进入子节点并立即失败。")]
        public bool wait = true;
        [ReadOnly, Name("信号量", "需要申请的树级信号量稳定索引，由编辑器根据配置列表写入；越界索引会在运行初始化阶段报错。")]
        public int semaphore;
        private bool IsAcquired(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0) != 0;
        private void SetAcquired(Blackboard blackboard, bool value) =>
            SetRuntimeData(blackboard, 0, value ? 1 : 0);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => 1;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (!context.Tree.IsValidSemaphore(semaphore))
                throw new InvalidOperationException(
                    $"{GetType()} has invalid semaphore index {semaphore}");
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            bool acquired = IsAcquired(blackboard);
            if (!acquired)
            {
                acquired = blackboard.WaitSemaphore(semaphore);
                SetAcquired(blackboard, acquired);
            }
            if (!acquired) return wait ? State.Running : State.Failure;
            return base.OnUpdate(blackboard);
        }

        protected override void OnStop(Blackboard blackboard)
        {
            base.OnStop(blackboard);
            ReleaseSemaphore(blackboard);
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            try
            {
                base.OnAbort(blackboard);
            }
            finally
            {
                ReleaseSemaphore(blackboard);
            }
        }

        private void ReleaseSemaphore(Blackboard blackboard)
        {
            if (!IsAcquired(blackboard)) return;
            blackboard.ReleaseSemaphore(semaphore);
            SetAcquired(blackboard, false);
        }

        protected override State Decorate(Blackboard blackboard, State state) =>
            state;

    }
}
