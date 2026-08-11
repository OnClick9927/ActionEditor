using System;
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
        private int GetRunningIndex(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetRunningIndex(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetInitialRuntimeData(int index) => -1;
        protected override int GetMinRuntimeData(int index) => -1;
        protected override int GetMaxRuntimeData(int index) => ChildCount - 1;

        internal override void ValidateBlackboard(Blackboard blackboard)
        {
            base.ValidateBlackboard(blackboard);
            Type valueType = blackboard.GetValueType(fieldName);
            if (valueType != typeof(int) && (valueType == null || !valueType.IsEnum))
                throw new InvalidOperationException(
                    $"{GetType()} requires integer or enum Blackboard field " +
                    $"'{fieldName}'");
        }

        protected override void OnStart(Blackboard blackboard)
        {
            if (GetRunningIndex(blackboard) != -1)
                SetRunningIndex(blackboard, -1);
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            int index = Convert.ToInt32(blackboard.GetValue(fieldName));
            if (index < 0 || index >= ChildCount)
            {
                if (invalidIndexResult == InvalidIndexResult.Clamp && ChildCount > 0)
                    index = Math.Max(0, Math.Min(ChildCount - 1, index));
                else
                {
                    AbortRunningSelection(blackboard);
                    return invalidIndexResult == InvalidIndexResult.Success
                        ? State.Success
                        : State.Failure;
                }
            }

            int runningIndex = GetRunningIndex(blackboard);
            if (runningIndex >= 0 && runningIndex != index)
                ChildAt(runningIndex).Abort(blackboard);
            State result = ChildAt(index).Update(blackboard);
            int nextIndex = result == State.Running ? index : -1;
            if (runningIndex != nextIndex)
                SetRunningIndex(blackboard, nextIndex);
            return result;
        }

        protected override void OnAbort(Blackboard blackboard)
        {
            base.OnAbort(blackboard);
            if (GetRunningIndex(blackboard) != -1)
                SetRunningIndex(blackboard, -1);
        }

        private void AbortRunningSelection(Blackboard blackboard)
        {
            int runningIndex = GetRunningIndex(blackboard);
            if (runningIndex >= 0 && runningIndex < ChildCount)
                ChildAt(runningIndex).Abort(blackboard);
            SetRunningIndex(blackboard, -1);
        }

    }
}
