using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("比较两个同类型确定性黑板字段是否相等；字符串使用区分大小写的 Ordinal 语义。")]
    [Name("参数间比较"), Attachable(typeof(BTTree)),
     Node(BTNodeTypes.Condition), Icon("CompareVariables")]
    public sealed class BTCompareVariables : BTCondition, IBTInspectorContext
    {
        public enum Comparison
        {
            Equal,
            NotEqual
        }

        [Name("参数 A", "参与相等性比较的第一个黑板字段。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string firstField;

        [Name("参数 B", "参与相等性比较的第二个黑板字段，类型必须与参数 A 完全一致。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string secondField;

        [Name("比较方式", "Equal 要求值相等；NotEqual 对相等结果取反。")]
        public Comparison comparison;

        [NonSerialized] private Type inspectorBlackboardType;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            BTCopyVariable.ValidateFields(blackboard, firstField, secondField);
        }

        protected override bool Condition()
        {
            bool equal = Equals(blackboard.GetValue(firstField),
                blackboard.GetValue(secondField));
            return comparison == Comparison.Equal ? equal : !equal;
        }

        void IBTInspectorContext.SetInspectorBlackboard(Type blackboardType) =>
            inspectorBlackboardType = blackboardType;

        private ValueDropdownList<string> InspectorFields =>
            BTInspectorVariableUtility.GetDeterministicFields(
                inspectorBlackboardType);
    }
}
