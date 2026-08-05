using System;
using System.Collections.Generic;
using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("重复次数", "将子节点重复执行指定次数。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Repeater")]
    public class BTRepeatCount : BTDecorateSingle
    {
        public int repeatCount = 1;
        public bool stopOnFailure;
        [NonSerialized] private int completedCount;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (repeatCount < 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(repeatCount)} cannot be negative");
            completedCount = 0;
        }

        protected override void OnStart()
        {
            completedCount = 0;
        }

        protected override State OnUpdate()
        {
            if (repeatCount == 0) return State.Success;

            State result = child.Update();
            if (result == State.Running) return State.Running;
            if (result == State.Failure && stopOnFailure) return State.Failure;

            completedCount++;
            return completedCount < repeatCount ? State.Running : result;
        }

        protected override void OnAbort()
        {
            try
            {
                base.OnAbort();
            }
            finally
            {
                completedCount = 0;
            }
        }

        protected override State Decorate(State state) => state;

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(completedCount);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > repeatCount)
                throw new ArgumentException("Invalid repeat runtime status",
                    nameof(values));
            completedCount = value;
        }
    }
}
