using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("设置或修改指定黑板字段。支持 bool、int、float、string 和枚举。")]
    [Name("设置参数"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("SetVariable")]
    public class BTSetVariable : BTAction, IBTInspectorContext
    {
        public enum SetVariableType
        {
            Set = 0,
            Add = 1,
            Subtract = 2,
            Multiply = 3,
            Divide = 4,
            Not = 5,
            Remainder = 6,
            Power = 7,
            Absolute = 8,
            Max = 12,
            Min = 13,
            Negate = 14
        }

        [Name("参数名称", "选择需要写入的黑板公开字段。只显示 bool、int、float、string 和枚举字段。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string fieldName;

        [Name("参数类型", "由编辑器根据当前黑板字段自动同步。")]
        [ReadOnly]
        public BTVariableCondition.VariableType variableType;

        [Name("运算方式", "可选项会根据字段类型自动过滤。")]
        [ValueDropdown(nameof(InspectorOperations))]
        [Condition(ConditionMode.Show, nameof(HasSupportedVariable))]
        public SetVariableType setType;

        [Name("布尔值")]
        [Condition(ConditionMode.Show, nameof(ShowBoolInspectorValue))]
        public bool boolValue;

        [Name("整数操作数", "整数运算使用 unchecked 溢出规则。")]
        [Condition(ConditionMode.Show, nameof(ShowIntInspectorValue))]
        public int intValue;

        [Name("浮点操作数")]
        [Condition(ConditionMode.Show, nameof(ShowFloatInspectorValue))]
        public float floatValue;

        [Name("字符串操作数")]
        [Condition(ConditionMode.Show, nameof(ShowStringInspectorValue))]
        public string stringValue;

        [Name("枚举值")]
        [ValueDropdown(nameof(InspectorEnumValues))]
        [Condition(ConditionMode.Show, nameof(ShowEnumInspectorValue))]
        public string enumValue;

        [ActionBuffer.Buffer] private bool boolValueInitialized = true;
        [NonSerialized] private Type inspectorBlackboardType;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            MigrateLegacyBoolValue();
            if (!SupportsOperation(variableType, setType))
                throw new InvalidOperationException(
                    $"{GetType()} does not support {setType} for {variableType}");
            if ((setType == SetVariableType.Divide ||
                 setType == SetVariableType.Remainder) && IsOperandZero())
                throw new InvalidOperationException($"{GetType()} cannot divide by zero");
            if (setType == SetVariableType.Power &&
                variableType == BTVariableCondition.VariableType.Int &&
                intValue < 0)
                throw new InvalidOperationException(
                    $"{GetType()} requires a non-negative integer exponent");
        }

        internal override void ValidateBlackboard(Blackboard blackboard)
        {
            base.ValidateBlackboard(blackboard);
            Type fieldType = blackboard.GetValueType(fieldName);
            if (!BTVariableCondition.IsVariableType(fieldType, variableType))
                throw new InvalidOperationException(
                    $"{GetType()} cannot use Blackboard field '{fieldName}' as {variableType}");
            if (variableType == BTVariableCondition.VariableType.Enum &&
                !BTEnumValueUtility.TryGetValue(fieldType, enumValue, out _))
                throw new InvalidOperationException(
                    $"{GetType()} cannot use enum value '{enumValue}' for " +
                    $"Blackboard field '{fieldName}'");
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            object current = setType == SetVariableType.Set
                ? null
                : blackboard.GetValue(fieldName);
            object result;
            switch (variableType)
            {
                case BTVariableCondition.VariableType.Bool:
                    result = setType == SetVariableType.Not
                        ? !(bool)current
                        : boolValue;
                    break;
                case BTVariableCondition.VariableType.Int:
                    result = setType == SetVariableType.Set
                        ? intValue
                        : CalculateInt((int)current);
                    break;
                case BTVariableCondition.VariableType.Float:
                    result = setType == SetVariableType.Set
                        ? floatValue
                        : CalculateFloat((float)current);
                    break;
                case BTVariableCondition.VariableType.String:
                    result = setType == SetVariableType.Add
                        ? string.Concat((string)current, stringValue)
                        : stringValue;
                    break;
                case BTVariableCondition.VariableType.Enum:
                    result = enumValue;
                    break;
                default:
                    throw new InvalidOperationException(
                        $"{GetType()} has an unsupported variable type {variableType}");
            }
            blackboard.SetValue(fieldName, result);
            return State.Success;
        }

        public static bool SupportsOperation(BTVariableCondition.VariableType type,
            SetVariableType operation)
        {
            bool numeric = type == BTVariableCondition.VariableType.Int ||
                type == BTVariableCondition.VariableType.Float;
            switch (operation)
            {
                case SetVariableType.Set:
                    return numeric || type == BTVariableCondition.VariableType.Bool ||
                        type == BTVariableCondition.VariableType.String ||
                        type == BTVariableCondition.VariableType.Enum;
                case SetVariableType.Add:
                    return numeric || type == BTVariableCondition.VariableType.String;
                case SetVariableType.Subtract:
                case SetVariableType.Multiply:
                case SetVariableType.Divide:
                case SetVariableType.Remainder:
                case SetVariableType.Power:
                case SetVariableType.Negate:
                case SetVariableType.Absolute:
                case SetVariableType.Min:
                case SetVariableType.Max:
                    return numeric;
                case SetVariableType.Not:
                    return type == BTVariableCondition.VariableType.Bool;
                default:
                    return false;
            }
        }

        private bool IsOperandZero()
        {
            switch (variableType)
            {
                case BTVariableCondition.VariableType.Int: return intValue == 0;
                case BTVariableCondition.VariableType.Float: return floatValue == 0f;
                default: return false;
            }
        }

        private int CalculateInt(int current)
        {
            unchecked
            {
                switch (setType)
                {
                    case SetVariableType.Add: return current + intValue;
                    case SetVariableType.Subtract: return current - intValue;
                    case SetVariableType.Multiply: return current * intValue;
                    case SetVariableType.Divide:
                        return current == int.MinValue && intValue == -1
                            ? int.MinValue
                            : current / intValue;
                    case SetVariableType.Remainder:
                        return current == int.MinValue && intValue == -1
                            ? 0
                            : current % intValue;
                    case SetVariableType.Power:
                        return IntPower(current, (uint)intValue);
                    case SetVariableType.Negate: return -current;
                    case SetVariableType.Absolute: return current < 0 ? -current : current;
                    case SetVariableType.Min: return Math.Min(current, intValue);
                    case SetVariableType.Max: return Math.Max(current, intValue);
                    default: throw new InvalidOperationException(
                        $"Unsupported integer operation {setType}");
                }
            }
        }

        private float CalculateFloat(float current)
        {
            switch (setType)
            {
                case SetVariableType.Add: return current + floatValue;
                case SetVariableType.Subtract: return current - floatValue;
                case SetVariableType.Multiply: return current * floatValue;
                case SetVariableType.Divide: return current / floatValue;
                case SetVariableType.Remainder: return current % floatValue;
                case SetVariableType.Power: return (float)Math.Pow(current, floatValue);
                case SetVariableType.Negate: return -current;
                case SetVariableType.Absolute: return Math.Abs(current);
                case SetVariableType.Min: return Math.Min(current, floatValue);
                case SetVariableType.Max: return Math.Max(current, floatValue);
                default: throw new InvalidOperationException(
                    $"Unsupported float operation {setType}");
            }
        }

        private static int IntPower(int value, uint exponent)
        {
            int result = 1;
            int factor = value;
            unchecked
            {
                while (exponent > 0)
                {
                    if ((exponent & 1U) != 0) result *= factor;
                    exponent >>= 1;
                    if (exponent != 0) factor *= factor;
                }
            }
            return result;
        }

        void IBTInspectorContext.SetInspectorBlackboard(Type blackboardType)
        {
            inspectorBlackboardType = blackboardType;
            Type fieldType = BTInspectorVariableUtility.GetFieldType(
                blackboardType, fieldName);
            variableType = BTVariableCondition.GetVariableType(fieldType);
            SynchronizeEnumValue(fieldType);
            MigrateLegacyBoolValue();
            if (!SupportsOperation(variableType, setType))
                setType = SetVariableType.Set;
        }

        private void MigrateLegacyBoolValue()
        {
            if (boolValueInitialized ||
                variableType == BTVariableCondition.VariableType.None) return;
            if (variableType == BTVariableCondition.VariableType.Bool)
                boolValue = intValue != 0 || floatValue != 0f;
            boolValueInitialized = true;
        }

        private ValueDropdownList<string> InspectorFields =>
            BTInspectorVariableUtility.GetFields(inspectorBlackboardType);

        private ValueDropdownList<string> InspectorEnumValues =>
            BTInspectorVariableUtility.GetEnumValues(inspectorBlackboardType,
                fieldName);

        private void SynchronizeEnumValue(Type fieldType)
        {
            if (fieldType == null || !fieldType.IsEnum) return;
            if (BTEnumValueUtility.TryGetValue(fieldType, enumValue, out _))
                return;
            string[] names = Enum.GetNames(fieldType);
            enumValue = names.Length == 0 ? null : names[0];
        }

        private ValueDropdownList<SetVariableType> InspectorOperations
        {
            get
            {
                var result = new ValueDropdownList<SetVariableType>();
                foreach (SetVariableType operation in
                    Enum.GetValues(typeof(SetVariableType)))
                    if (SupportsOperation(variableType, operation))
                        result.Add(operation.ToString(), operation);
                return result;
            }
        }

        private bool HasSupportedVariable =>
            variableType != BTVariableCondition.VariableType.None;
        private bool InspectorNeedsOperand =>
            setType != SetVariableType.Not &&
            setType != SetVariableType.Negate &&
            setType != SetVariableType.Absolute;
        private bool ShowBoolInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Bool;
        private bool ShowIntInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Int;
        private bool ShowFloatInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Float;
        private bool ShowStringInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.String;
        private bool ShowEnumInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Enum;
    }
}
