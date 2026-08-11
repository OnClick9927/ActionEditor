using ActionAttribute;
using System;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("在当前 Tick 向所属行为树广播一次命名事件，通知所有等待或监听该名称的节点后立即返回成功。")]
    [Name("发送事件"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("EventSend")]
    public class BTPushEvent : BTAction
    {
        [ReadOnly, Name("事件名称", "广播使用的精确事件键；必须与接收节点登记的名称完全一致，由树资源的事件列表统一维护。")]
        public string eventName;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (string.IsNullOrEmpty(eventName))
                throw new InvalidOperationException(
                    $"{GetType()} requires an event name");
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            var succ = blackboard.PushEvent(eventName);
            return succ ? State.Success : State.Failure;
        }
    }
}
