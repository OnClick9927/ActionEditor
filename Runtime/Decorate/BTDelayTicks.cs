using ActionUnity;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
    [Name("延迟 Tick", "等待指定逻辑 Tick 数后开始执行子节点。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Repeater")]
    public class BTDelayTicks : BTDecorateSingle
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
            if (elapsedTicks < tickCount)
            {
                elapsedTicks++;
                return State.Running;
            }
            return base.OnUpdate();
        }

        protected override void OnAbort()
        {
            try
            {
                base.OnAbort();
            }
            finally
            {
                elapsedTicks = 0;
            }
        }

        protected override State Decorate(State state) => state;

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(elapsedTicks);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > tickCount)
                throw new ArgumentException("Invalid delay tick runtime status",
                    nameof(values));
            elapsedTicks = value;
        }
    }
}
