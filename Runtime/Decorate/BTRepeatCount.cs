using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("按固定次数完整执行唯一子节点；已完成次数写入状态快照，达到总次数后返回最后一次结果或成功。")]
    [Name("重复次数"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("RepeatCount")]
    public class BTRepeatCount : BTDecorateSingle
    {
        [Name("重复次数", "一次进入期间要求子节点结束的总次数，必须为正数；运行中断后是否保留由节点的状态恢复流程决定。")]
        public int repeatCount = 1;
        [Name("失败时停止", "开启后任意一轮失败都会立即返回失败；关闭时失败也计为一次完成并继续剩余轮次。")]
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
