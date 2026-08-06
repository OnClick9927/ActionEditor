using ActionAttribute;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("进入后先按行为树 Update 次数等待，延迟结束才开始执行唯一子节点；不读取真实时间，适用于确定性帧同步。")]
    [Name("延迟 Tick"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("DelayTicks")]
    public class BTDelayTicks : BTDecorateSingle
    {
        [Name("延迟 Tick 数", "开始更新子节点前必须经过的逻辑 Tick 数；当前等待计数会写入状态快照，中止时按节点规则重置。")]
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
