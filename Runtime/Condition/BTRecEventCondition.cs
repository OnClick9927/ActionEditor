using ActionAttribute;
using System;
namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("检查所属行为树是否已广播指定命名事件；收到后本次条件成功并消费标记，未收到时失败。")]
    [Name("收到事件？"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Condition), Icon("Event")]
    public class BTRecEventCondition : BTCondition, IBTEventReceiver
    {
        [ReadOnly, Name("事件名称", "监听和消费的精确事件键，由树资源的事件列表统一维护；接收标记会写入运行时状态快照。")]
        public string eventName;
        private bool HasReceivedEvent(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0) != 0;
        private void SetReceivedEvent(Blackboard blackboard, bool value) =>
            SetRuntimeData(blackboard, 0, value ? 1 : 0);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) => 1;

        string IBTEventReceiver.EventName => eventName;
        void IBTEventReceiver.ReceiveEvent(Blackboard blackboard) =>
            SetReceivedEvent(blackboard, true);

        protected override bool Condition(Blackboard blackboard)
        {
            bool rec = HasReceivedEvent(blackboard);
            SetReceivedEvent(blackboard, false);
            return rec;
        }

    }
}
