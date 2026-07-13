using System;
namespace ActionEditor.Nodes.BT
{
    [Name("收到事件？"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Condition), Icon("Event")]
    public class BTRecEventCondition : BTCondition
    {
        [ReadOnly] public string eventName;
        [NonSerialized] internal bool recEve;
        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            tree.AddSpecialNode(this);
        }
        protected override bool Condition()
        {
            var rec = recEve;
            recEve = false;
            return rec;
        }

    }
}
