using ActionAttribute;
using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("检查所属行为树是否已广播指定命名事件；收到后本次条件成功并消费标记，未收到时失败。")]
    [Name("收到事件？"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Condition), Icon("Event")]
    public class BTRecEventCondition : BTCondition, IBTEventReceiver
    {
        [ReadOnly, Name("事件名称", "监听和消费的精确事件键，由树资源的事件列表统一维护；接收标记会写入运行时状态快照。")]
        public string eventName;
        [NonSerialized] private bool recEve;

        string IBTEventReceiver.EventName => eventName;
        void IBTEventReceiver.ReceiveEvent() => recEve = true;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            recEve = false;
            tree.AddSpecialNode(this);
        }
        protected override bool Condition()
        {
            var rec = recEve;
            recEve = false;
            return rec;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(recEve ? 1 : 0);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value != 0 && value != 1)
                throw new ArgumentException("Invalid event runtime status",
                    nameof(values));
            recEve = value != 0;
        }
    }
}
