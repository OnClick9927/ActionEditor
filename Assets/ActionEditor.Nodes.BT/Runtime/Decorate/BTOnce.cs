using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("在行为树本次运行会话中只执行一次子节点；后续进入直接返回首次结束结果，缓存包含在状态快照中。")]
    [Name("单次执行"), Attachable(typeof(BTTree)),
     Node(BTNodeTypes.Decorate), Icon("Once")]
    public sealed class BTOnce : BTDecorateSingle
    {
        [NonSerialized] private bool completed;
        [NonSerialized] private State completedState;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            completed = false;
            completedState = State.Inactive;
        }

        protected override State OnUpdate()
        {
            if (completed) return completedState;
            State result = child.Update();
            if (result == State.Running) return result;
            completed = true;
            completedState = result;
            return result;
        }

        protected override State Decorate(State state) => state;

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(completed ? 1 : 0);
            values.Add((int)completedState);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int completedValue = ReadStatusValue(values, ref index);
            int resultValue = ReadStatusValue(values, ref index);
            if ((completedValue != 0 && completedValue != 1) ||
                resultValue < (int)State.Inactive ||
                resultValue > (int)State.Running ||
                (completedValue == 0 && resultValue != (int)State.Inactive) ||
                (completedValue != 0 && resultValue != (int)State.Success &&
                 resultValue != (int)State.Failure))
                throw new ArgumentException("Invalid once runtime status",
                    nameof(values));
            completed = completedValue != 0;
            completedState = (State)resultValue;
        }
    }
}
