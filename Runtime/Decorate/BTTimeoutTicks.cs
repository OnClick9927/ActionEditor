using ActionUnity;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
    [Name("超时 Tick", "子节点运行超过指定逻辑 Tick 数时中止并返回失败。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Failure")]
    public class BTTimeoutTicks : BTDecorateSingle
    {
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
