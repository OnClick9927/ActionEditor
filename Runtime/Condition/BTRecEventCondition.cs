using ActionUnity;
using System;
using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [Name("收到事件？", "检查行为树是否收到指定的命名事件。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Condition), Icon("Event")]
    public class BTRecEventCondition : BTCondition
    {
        [ReadOnly] public string eventName;
        [NonSerialized] private bool recEve;

        internal void ReceiveEvent() => recEve = true;

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
