using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("读取指定黑板字段并与配置值比较。支持 bool、int、float、string 和枚举。")]
    [Name("参数比较"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Condition),
     Icon("VariableCondition")]
    public class BTVariableCondition : BTCondition, IBTInspectorContext
    {
        public enum CompareType
        {
            Equals,
            NotEquals,
            LessThan,
            LessOrEquals,
            GreaterThan,
            GreaterOrEquals
        }

        public enum VariableType
        {
            None = 0,
            Bool = 1,
            Int = 2,
            Float = 3,
            String = 15,
            Enum = 16
        }

        [Name("参数名称", "选择需要读取的黑板公开字段。只显示 bool、int、float、string 和枚举字段。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string fieldName;

        [Name("参数类型", "由编辑器根据当前黑板字段自动同步。")]
        [ReadOnly]
        public VariableType variableType;

        [Name("比较方式", "bool 仅支持相等和不相等；string 使用 Ordinal 顺序比较。")]
        [ValueDropdown(nameof(InspectorComparisons))]
        [Condition(ConditionMode.Show, nameof(HasSupportedVariable))]
        public CompareType compareType;

        [Name("布尔值")]
        [Condition(ConditionMode.Show, nameof(ShowBoolInspectorValue))]
        public bool boolValue;

        [Name("整数值")]
        [Condition(ConditionMode.Show, nameof(ShowIntInspectorValue))]
        public int intValue;

        [Name("浮点值", "float 使用 C# 运算符语义进行比较。")]
        [Condition(ConditionMode.Show, nameof(ShowFloatInspectorValue))]
        public float floatValue;

        [Name("字符串值", "大小比较固定使用 Ordinal 规则，不受区域设置影响。")]
        [Condition(ConditionMode.Show, nameof(ShowStringInspectorValue))]
        public string stringValue;

        [Name("枚举值")]
        [ValueDropdown(nameof(InspectorEnumValues))]
        [Condition(ConditionMode.Show, nameof(ShowEnumInspectorValue))]
        public string enumValue;

        [ActionBuffer.Buffer] private bool boolValueInitialized = true;
        [NonSerialized] private Type inspectorBlackboardType;

        public static VariableType GetVariableType(Type type)
        {
            if (type == typeof(bool)) return VariableType.Bool;
            if (type == typeof(int)) return VariableType.Int;
            if (type == typeof(float)) return VariableType.Float;
            if (type == typeof(string)) return VariableType.String;
            if (type != null && type.IsEnum) return VariableType.Enum;
            return VariableType.None;
        }

        internal static bool IsVariableType(Type type, VariableType valueType) =>
            GetVariableType(type) == valueType && valueType != VariableType.None;

        public static bool SupportsOrdering(VariableType type) =>
            type == VariableType.Int || type == VariableType.Float ||
            type == VariableType.String;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            MigrateLegacyBoolValue();
            if (!SupportsOrdering(variableType) &&
                compareType != CompareType.Equals &&
                compareType != CompareType.NotEquals)
                throw new InvalidOperationException(
                    $"{GetType()} only supports equality checks for {variableType}");
        }

        internal override void ValidateBlackboard(Blackboard blackboard)
        {
            base.ValidateBlackboard(blackboard);
            Type fieldType = blackboard.GetValueType(fieldName);
            if (!IsVariableType(fieldType, variableType))
                throw new InvalidOperationException(
                    $"{GetType()} cannot use Blackboard field '{fieldName}' as {variableType}");
            if (variableType == VariableType.Enum &&
                !BTEnumValueUtility.TryGetValue(fieldType, enumValue, out _))
                throw new InvalidOperationException(
                    $"{GetType()} cannot use enum value '{enumValue}' for " +
                    $"Blackboard field '{fieldName}'");
        }

        protected override bool Condition(Blackboard blackboard)
        {
            object current = blackboard.GetValue(fieldName);
            switch (variableType)
            {
                case VariableType.Bool:
                    return CompareEquality((bool)current, boolValue);
                case VariableType.Int:
                    return CompareOrdered((int)current, intValue);
                case VariableType.Float:
                    return CompareFloat((float)current, floatValue);
                case VariableType.String:
                    return CompareResult(string.CompareOrdinal(
                        (string)current, stringValue));
                case VariableType.Enum:
                    if (!BTEnumValueUtility.TryGetValue(current?.GetType(),
                            enumValue, out object expected))
                        throw new InvalidOperationException(
                            $"{GetType()} has an invalid enum value '{enumValue}'");
                    return CompareEquality(current, expected);
                default:
                    throw new InvalidOperationException(
                        $"{GetType()} has an unsupported variable type {variableType}");
            }
        }

        private bool CompareEquality(object current, object expected)
        {
            bool equals = Equals(current, expected);
            return compareType == CompareType.Equals ? equals : !equals;
        }

        private bool CompareOrdered<T>(T current, T expected)
            where T : IComparable<T> => CompareResult(current.CompareTo(expected));

        private bool CompareResult(int result)
        {
            switch (compareType)
            {
                case CompareType.Equals: return result == 0;
                case CompareType.NotEquals: return result != 0;
                case CompareType.LessThan: return result < 0;
                case CompareType.LessOrEquals: return result <= 0;
                case CompareType.GreaterThan: return result > 0;
                case CompareType.GreaterOrEquals: return result >= 0;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        private bool CompareFloat(float current, float expected)
        {
            switch (compareType)
            {
                case CompareType.Equals: return current == expected;
                case CompareType.NotEquals: return current != expected;
                case CompareType.LessThan: return current < expected;
                case CompareType.LessOrEquals: return current <= expected;
                case CompareType.GreaterThan: return current > expected;
                case CompareType.GreaterOrEquals: return current >= expected;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        void IBTInspectorContext.SetInspectorBlackboard(Type blackboardType)
        {
            inspectorBlackboardType = blackboardType;
            Type fieldType = BTInspectorVariableUtility.GetFieldType(
                blackboardType, fieldName);
            variableType = GetVariableType(fieldType);
            SynchronizeEnumValue(fieldType);
            MigrateLegacyBoolValue();
            if (!SupportsOrdering(variableType) &&
                compareType != CompareType.Equals &&
                compareType != CompareType.NotEquals)
                compareType = CompareType.Equals;
        }

        private void MigrateLegacyBoolValue()
        {
            if (boolValueInitialized || variableType == VariableType.None) return;
            if (variableType == VariableType.Bool) boolValue = intValue != 0;
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

        private ValueDropdownList<CompareType> InspectorComparisons
        {
            get
            {
                var result = new ValueDropdownList<CompareType>();
                foreach (CompareType comparison in Enum.GetValues(
                    typeof(CompareType)))
                {
                    if (!SupportsOrdering(variableType) &&
                        comparison != CompareType.Equals &&
                        comparison != CompareType.NotEquals) continue;
                    result.Add(comparison.ToString(), comparison);
                }
                return result;
            }
        }

        private bool HasSupportedVariable => variableType != VariableType.None;
        private bool ShowBoolInspectorValue => variableType == VariableType.Bool;
        private bool ShowIntInspectorValue => variableType == VariableType.Int;
        private bool ShowFloatInspectorValue => variableType == VariableType.Float;
        private bool ShowStringInspectorValue => variableType == VariableType.String;
        private bool ShowEnumInspectorValue => variableType == VariableType.Enum;
    }
}
