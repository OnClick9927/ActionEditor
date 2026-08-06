using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [Name("执行上限", "限制整个行为树运行会话中唯一子节点可完整结束的累计次数；计数不会因节点重新进入而清零，并包含在状态快照中。"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Repeater")]
    public sealed class BTExecutionLimit : BTDecorateSingle
    {
        public enum LimitResult
        {
            Failure,
            Success
        }

        [Name("最大执行次数", "当前运行会话内允许子节点结束的累计上限，必须大于零；达到上限后不再进入子节点。")]
        public int maxExecutions = 1;
        [Name("耗尽结果", "累计完成次数达到上限后，不再执行子节点并直接返回的固定状态，可配置为成功或失败。")]
        public LimitResult exhaustedResult;
        [NonSerialized] private int completedExecutions;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (maxExecutions <= 0)
                throw new InvalidOperationException(
                    $"{GetType()} {nameof(maxExecutions)} must be positive");
            completedExecutions = 0;
        }

        protected override State OnUpdate()
        {
            if (completedExecutions >= maxExecutions)
                return exhaustedResult == LimitResult.Success
                    ? State.Success
                    : State.Failure;

            State result = child.Update();
            if (result != State.Running) completedExecutions++;
            return result;
        }

        protected override State Decorate(State state) => state;

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(completedExecutions);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > maxExecutions)
                throw new ArgumentException(
                    "Invalid execution-limit runtime status", nameof(values));
            if (state == State.Running && value >= maxExecutions)
                throw new ArgumentException(
                    "A running execution-limit node must have capacity",
                    nameof(values));
            completedExecutions = value;
        }
    }
}
