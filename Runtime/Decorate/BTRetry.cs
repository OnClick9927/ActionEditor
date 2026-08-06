using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("失败重试", "子节点失败后重新进入，直到成功或耗尽最大尝试次数；失败计数会写入状态快照以支持精确恢复。"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Repeater")]
    public sealed class BTRetry : BTDecorateSingle
    {
        [Name("最大尝试次数", "包含首次执行在内的总尝试次数，必须大于零；达到上限后的最后一次失败会直接结束节点。")]
        public int maxAttempts = 1;
        [NonSerialized] private int failedAttempts;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (maxAttempts <= 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(maxAttempts)} must be positive");
            failedAttempts = 0;
        }

        protected override void OnStart()
        {
            failedAttempts = 0;
        }

        protected override State OnUpdate()
        {
            State result = child.Update();
            if (result != State.Failure) return result;
            failedAttempts++;
            return failedAttempts < maxAttempts ? State.Running : State.Failure;
        }

        protected override void OnAbort()
        {
            base.OnAbort();
            failedAttempts = 0;
        }

        protected override State Decorate(State state) => state;

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(failedAttempts);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > maxAttempts)
                throw new ArgumentException("Invalid retry runtime status",
                    nameof(values));
            if (state == State.Running && value >= maxAttempts)
                throw new ArgumentException(
                    "A running retry node must have an attempt remaining",
                    nameof(values));
            failedAttempts = value;
        }
    }
}
