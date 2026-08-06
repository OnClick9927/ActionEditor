using System;
using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("读取黑板 int 或枚举字段，将其整数值直接映射为子节点索引；选择变化时会中止此前仍在运行的旧分支。")]
    [Name("整数分支"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("SwitchInteger")]
    public sealed class BTSwitchInteger : BTComposite
    {
        public enum InvalidIndexResult
        {
            Failure,
            Success,
            Clamp
        }

        [Name("参数名称", "提供分支索引的黑板公开字段，类型只能是 int 或枚举；初始化时校验字段存在且类型受支持。")]
        public string fieldName;
        [Name("越界处理", "索引小于零或大于最后一个子节点时，可直接失败、直接成功，或钳制到最近的有效子节点。")]
        public InvalidIndexResult invalidIndexResult;
        [NonSerialized] private int runningIndex;

        protected override void OnInitialized()
        {
            Type valueType = blackboard.GetValueType(fieldName);
            if (valueType != typeof(int) && (valueType == null || !valueType.IsEnum))
                throw new InvalidOperationException(
                    $"{GetType()} requires integer or enum Blackboard field " +
                    $"'{fieldName}'");
            runningIndex = -1;
        }

        protected override void OnStart()
        {
            runningIndex = -1;
        }

        protected override State OnUpdate()
        {
            int index = Convert.ToInt32(blackboard.GetValue(fieldName));
            if (index < 0 || index >= ChildCount)
            {
                if (invalidIndexResult == InvalidIndexResult.Clamp && ChildCount > 0)
                    index = Math.Max(0, Math.Min(ChildCount - 1, index));
                else
                {
                    AbortRunningSelection();
                    return invalidIndexResult == InvalidIndexResult.Success
                        ? State.Success
                        : State.Failure;
                }
            }

            if (runningIndex >= 0 && runningIndex != index)
                ChildAt(runningIndex).Abort();
            State result = ChildAt(index).Update();
            runningIndex = result == State.Running ? index : -1;
            return result;
        }

        protected override void OnAbort()
        {
            base.OnAbort();
            runningIndex = -1;
        }

        private void AbortRunningSelection()
        {
            if (runningIndex >= 0 && runningIndex < ChildCount)
                ChildAt(runningIndex).Abort();
            runningIndex = -1;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(runningIndex);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < -1 || value >= ChildCount)
                throw new ArgumentException("Invalid integer-switch runtime status",
                    nameof(values));
            if ((state == State.Running) != (value >= 0))
                throw new ArgumentException(
                    "Integer-switch state and running child do not match",
                    nameof(values));
            runningIndex = value;
        }
    }
}
