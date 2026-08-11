using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("进入后持续返回运行中，直到所属行为树收到指定命名事件；事件只消费一次，消费后当前执行返回成功。")]
    [Name("等待事件"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("WaitEvent")]
    public sealed class BTWaitEvent : BTAction, IBTEventReceiver
    {
        [Name("事件名称", "需要监听的精确事件键；接收标记会写入状态快照，保证恢复后仍保持事件是否已经到达。")]
        public string eventName;
        private bool IsReceived(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0) != 0;
        private void SetReceived(Blackboard blackboard, bool value) =>
            SetRuntimeData(blackboard, 0, value ? 1 : 0);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => 1;

        string IBTEventReceiver.EventName => eventName;
        void IBTEventReceiver.ReceiveEvent(Blackboard blackboard) =>
            SetReceived(blackboard, true);

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (string.IsNullOrEmpty(eventName))
                throw new InvalidOperationException(
                    $"{GetType()} requires an event name");
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            if (!IsReceived(blackboard)) return State.Running;
            SetReceived(blackboard, false);
            return State.Success;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            SetReceived(blackboard, false);
        }

    }
}
