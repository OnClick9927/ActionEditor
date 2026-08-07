using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("交换两个同类型确定性黑板字段的当前值；不执行隐式数值转换，也不接受浮点字段。")]
    [Name("交换参数"), Attachable(typeof(BTTree)),
     Node(BTNodeTypes.Action), Icon("SwapVariables")]
    public sealed class BTSwapVariables : BTAction, IBTInspectorContext
    {
        [Name("参数 A", "交换操作的第一个黑板字段。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string firstField;

        [Name("参数 B", "交换操作的第二个黑板字段，类型必须与参数 A 完全一致。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string secondField;

        [NonSerialized] private Type inspectorBlackboardType;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            BTCopyVariable.ValidateFields(blackboard, firstField, secondField);
        }

        protected override State OnUpdate()
        {
            object first = blackboard.GetValue(firstField);
            object second = blackboard.GetValue(secondField);
            blackboard.SetValue(firstField, second);
            blackboard.SetValue(secondField, first);
            return State.Success;
        }

        void IBTInspectorContext.SetInspectorBlackboard(Type blackboardType) =>
            inspectorBlackboardType = blackboardType;

        private ValueDropdownList<string> InspectorFields =>
            BTInspectorVariableUtility.GetDeterministicFields(
                inspectorBlackboardType);
    }
}
