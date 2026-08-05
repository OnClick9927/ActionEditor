using ActionUnity;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
    [Name("等待 Tick", "等待指定逻辑 Tick 数后返回成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("Repeater")]
    public class BTWaitTicks : BTAction
    {
        public int tickCount = 1;
        [NonSerialized] private int elapsedTicks;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (tickCount < 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(tickCount)} cannot be negative");
            elapsedTicks = 0;
        }

        protected override void OnStart()
        {
            elapsedTicks = 0;
        }

        protected override State OnUpdate()
        {
            if (elapsedTicks >= tickCount) return State.Success;
            elapsedTicks++;
            return State.Running;
        }

        protected override void OnAbort()
        {
            elapsedTicks = 0;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(elapsedTicks);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > tickCount)
                throw new ArgumentException("Invalid wait tick runtime status",
                    nameof(values));
            elapsedTicks = value;
        }
    }
}
