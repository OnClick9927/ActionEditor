using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("在行为树本次运行会话中只执行一次子节点；后续进入直接返回首次结束结果，缓存包含在状态快照中。")]
    [Name("单次执行"), Attachable(typeof(BTTree)),
     Node(BTNodeTypes.Decorate), Icon("Once")]
    public sealed class BTOnce : BTDecorateSingle
    {
        private bool IsCompleted(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0) != 0;
        private void SetCompleted(Blackboard blackboard, bool value) =>
            SetRuntimeData(blackboard, 0, value ? 1 : 0);
        private State GetCompletedState(Blackboard blackboard) =>
            (State)GetRuntimeData(blackboard, 1);
        private void SetCompletedState(Blackboard blackboard, State value) =>
            SetRuntimeData(blackboard, 1, (int)value);

        protected override int RuntimeDataSize => 2;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) =>
            index == 0 ? 1 : (int)State.Running;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            if (IsCompleted(blackboard))
                return GetCompletedState(blackboard);
            State result = child.Update(blackboard);
            if (result == State.Running) return result;
            SetCompleted(blackboard, true);
            SetCompletedState(blackboard, result);
            return result;
        }

        protected override State Decorate(Blackboard blackboard, State state) =>
            state;

    }
}
