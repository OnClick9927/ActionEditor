using ActionAttribute;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
    [Name("超时 Tick", "限制唯一子节点可持续运行的行为树 Update 次数；超出上限时主动中止子节点并失败，不依赖真实时间。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Failure")]
    public class BTTimeoutTicks : BTDecorateSingle
    {
        [Name("超时 Tick 数", "子节点保持运行中状态时允许消耗的最大逻辑 Tick 数，必须为正数；已消耗计数会写入状态快照。")]
        public int tickCount = 1;
        [NonSerialized] private int elapsedTicks;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (tickCount <= 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(tickCount)} must be positive");
            elapsedTicks = 0;
        }

        protected override void OnStart()
        {
            elapsedTicks = 0;
        }

        protected override State OnUpdate()
        {
            State result = base.OnUpdate();
            if (result != State.Running) return result;

            elapsedTicks++;
            if (elapsedTicks < tickCount) return State.Running;
            child.Abort();
            return State.Failure;
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
                throw new ArgumentException("Invalid timeout tick runtime status",
                    nameof(values));
            elapsedTicks = value;
        }
    }
}
