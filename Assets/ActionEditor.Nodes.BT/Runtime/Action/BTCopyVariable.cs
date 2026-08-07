using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("将一个确定性黑板字段复制到另一个同类型字段；初始化时拒绝缺失字段、类型转换和浮点字段。")]
    [Name("复制参数"), Attachable(typeof(BTTree)),
     Node(BTNodeTypes.Action), Icon("CopyVariable")]
    public sealed class BTCopyVariable : BTAction, IBTInspectorContext
    {
        [Name("来源参数", "提供当前值的黑板字段，必须是整数、布尔、字符、字符串、枚举或 decimal。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string sourceField;

        [Name("目标参数", "接收复制值的黑板字段，字段类型必须与来源参数完全一致。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string destinationField;

        [NonSerialized] private Type inspectorBlackboardType;

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            ValidateFields(blackboard, sourceField, destinationField);
        }

        protected override State OnUpdate()
        {
            blackboard.SetValue(destinationField,
                blackboard.GetValue(sourceField));
            return State.Success;
        }

        internal static void ValidateFields(Blackboard blackboard,
            string first, string second)
        {
            Type firstType = blackboard.GetValueType(first);
            Type secondType = blackboard.GetValueType(second);
            if (firstType == null || secondType == null)
                throw new InvalidOperationException(
                    "Blackboard variable does not exist.");
            if (firstType != secondType)
                throw new InvalidOperationException(
                    "Blackboard variables must have the same type.");
            if (!BTInspectorVariableUtility.IsDeterministicType(firstType))
                throw new InvalidOperationException(
                    "Blackboard variable type is not deterministic.");
        }

        void IBTInspectorContext.SetInspectorBlackboard(Type blackboardType) =>
            inspectorBlackboardType = blackboardType;

        private ValueDropdownList<string> InspectorFields =>
            BTInspectorVariableUtility.GetDeterministicFields(
                inspectorBlackboardType);
    }
}
