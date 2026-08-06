using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("等待事件", "进入后持续返回运行中，直到所属行为树收到指定命名事件；事件只消费一次，消费后当前执行返回成功。"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("Event")]
    public sealed class BTWaitEvent : BTAction, IBTEventReceiver
    {
        [Name("事件名称", "需要监听的精确事件键；接收标记会写入状态快照，保证恢复后仍保持事件是否已经到达。")]
        public string eventName;
        [NonSerialized] private bool received;

        string IBTEventReceiver.EventName => eventName;
        void IBTEventReceiver.ReceiveEvent() => received = true;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (string.IsNullOrEmpty(eventName))
                throw new InvalidOperationException(
                    $"{GetType()} requires an event name");
            received = false;
            tree.AddSpecialNode(this);
        }

        protected override State OnUpdate()
        {
            if (!received) return State.Running;
            received = false;
            return State.Success;
        }

        protected override void OnAbort()
        {
            received = false;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(received ? 1 : 0);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value != 0 && value != 1)
                throw new ArgumentException("Invalid wait-event runtime status",
                    nameof(values));
            received = value != 0;
        }
    }
}
