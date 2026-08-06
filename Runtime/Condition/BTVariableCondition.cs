using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("读取指定黑板字段并与配置值比较。整数、布尔、枚举、字符、字符串和 decimal 使用确定性比较；float 与 double 仅供非帧同步逻辑按需使用。")]
    [Name("参数比较"),
     Attachable(typeof(BTTree)), Node(BTNodeTypes.Condition), Icon("Conditional")]
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
            Enum = 4,
            SByte = 5,
            Byte = 6,
            Short = 7,
            UShort = 8,
            UInt = 9,
            Long = 10,
            ULong = 11,
            Double = 12,
            Decimal = 13,
            Char = 14,
            String = 15
        }

        [Name("参数名称", "选择需要读取的黑板公开字段。初始化行为树时会再次检查字段是否存在、字段真实类型是否与下方记录的参数类型一致，配置失效时立即抛出异常。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string fieldName;
        [Name("参数类型", "由编辑器根据黑板字段自动记录，运行时用它选择无装箱转换歧义的比较路径。float 与 double 可能受平台浮点实现影响，不应放进跨平台帧同步判定。")]
        [ReadOnly]
        public VariableType variableType;
        [Name("比较方式", "相等和不相等适用于全部支持类型；大小比较适用于数值、字符和字符串。字符串固定采用 Ordinal 逐字符比较，不受系统语言与区域设置影响。")]
        [ValueDropdown(nameof(InspectorComparisons))]
        public CompareType compareType;
        [Name("32 位有符号值", "保存 int、sbyte、byte、short、ushort 的比较值，同时兼容旧资源中以 32 位整数保存的枚举值。编辑器会按目标字段的实际范围限制输入。")]
        [ShowIf(nameof(ShowIntInspectorValue))]
        public int intValue;
        [Name("32 位无符号值", "保存 uint 字段的比较值。独立存储可覆盖完整的 0 到 4294967295 范围，避免经过有符号整数时溢出。")]
        [ShowIf(nameof(ShowUIntInspectorValue))]
        public uint uintValue;
        [Name("64 位有符号值", "保存 long 字段的比较值。比较直接使用 64 位整数，不转换为浮点数，因此大整数不会丢失精度。")]
        [ShowIf(nameof(ShowLongInspectorValue))]
        public long longValue;
        [Name("64 位无符号值", "保存 ulong 字段的比较值。比较覆盖完整的无符号 64 位范围，不经过 decimal、double 或有符号中间值。")]
        [ShowIf(nameof(ShowULongInspectorValue))]
        public ulong ulongValue;
        [Name("单精度值", "保存 float 字段的比较值。NaN 按 C# 运算符语义处理：等于始终为 false、不等于为 true、所有大小比较为 false。")]
        [ShowIf(nameof(ShowFloatInspectorValue))]
        public float floatValue;
        [Name("双精度值", "保存 double 字段的比较值。该路径保留双精度范围，但不承诺跨硬件平台的帧同步确定性。")]
        [ShowIf(nameof(ShowDoubleInspectorValue))]
        public double doubleValue;
        [Name("十进制值", "保存 decimal 字段的比较值。比较不经过二进制浮点转换，适合需要固定十进制精度的通用逻辑。")]
        [ShowIf(nameof(ShowDecimalInspectorValue))]
        public decimal decimalValue;
        [Name("字符值", "保存 char 字段的比较值。大小关系按 UTF-16 码元数值判断，不使用本地化排序规则。")]
        [ShowIf(nameof(ShowCharInspectorValue))]
        public char charValue;
        [Name("文本或枚举值", "string 字段直接保存比较文本；枚举保存名称或 Flags 组合名称，运行时按字段的枚举类型解析。字符串大小比较固定使用 Ordinal 规则。")]
        [ShowIf(nameof(ShowStringInspectorValue))]
        public string stringValue;

        [NonSerialized] private Type runtimeFieldType;
        [NonSerialized] private object runtimeEnumValue;
        [NonSerialized] private Type inspectorBlackboardType;

        public bool boolValue
        {
            get => intValue != 0;
            set => intValue = value ? 1 : 0;
        }

        public static VariableType GetVariableType(Type type)
        {
            if (type == typeof(bool)) return VariableType.Bool;
            if (type == typeof(int)) return VariableType.Int;
            if (type == typeof(float)) return VariableType.Float;
            if (type != null && type.IsEnum) return VariableType.Enum;
            if (type == typeof(sbyte)) return VariableType.SByte;
            if (type == typeof(byte)) return VariableType.Byte;
            if (type == typeof(short)) return VariableType.Short;
            if (type == typeof(ushort)) return VariableType.UShort;
            if (type == typeof(uint)) return VariableType.UInt;
            if (type == typeof(long)) return VariableType.Long;
            if (type == typeof(ulong)) return VariableType.ULong;
            if (type == typeof(double)) return VariableType.Double;
            if (type == typeof(decimal)) return VariableType.Decimal;
            if (type == typeof(char)) return VariableType.Char;
            if (type == typeof(string)) return VariableType.String;
            return VariableType.None;
        }

        internal static bool IsVariableType(Type type, VariableType valueType) =>
            GetVariableType(type) == valueType && valueType != VariableType.None;

        public static bool IsSupportedEnum(Type type) => type != null && type.IsEnum;

        public static bool SupportsOrdering(VariableType type) =>
            type != VariableType.None && type != VariableType.Bool &&
            type != VariableType.Enum;

        public static object ResolveEnumValue(Type enumType, string name,
            int legacyValue)
        {
            if (!string.IsNullOrEmpty(name) &&
                Enum.TryParse(enumType, name, false, out object parsed))
                return parsed;
            return Enum.ToObject(enumType, legacyValue);
        }

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            runtimeFieldType = blackboard.GetValueType(fieldName);
            if (!IsVariableType(runtimeFieldType, variableType))
                throw new InvalidOperationException(
                    $"{GetType()} cannot use Blackboard field '{fieldName}' as {variableType}");
            if (!SupportsOrdering(variableType) &&
                compareType != CompareType.Equals &&
                compareType != CompareType.NotEquals)
                throw new InvalidOperationException(
                    $"{GetType()} only supports equality checks for {variableType}");
            runtimeEnumValue = variableType == VariableType.Enum
                ? ResolveEnumValue(runtimeFieldType, stringValue, intValue)
                : null;
        }

        protected override bool Condition()
        {
            object current = blackboard.GetValue(fieldName);
            switch (variableType)
            {
                case VariableType.Bool:
                    return CompareEquality((bool)current, boolValue);
                case VariableType.Enum:
                    return CompareEquality(current, runtimeEnumValue);
                case VariableType.SByte:
                    return CompareOrdered((sbyte)current, unchecked((sbyte)intValue));
                case VariableType.Byte:
                    return CompareOrdered((byte)current, unchecked((byte)intValue));
                case VariableType.Short:
                    return CompareOrdered((short)current, unchecked((short)intValue));
                case VariableType.UShort:
                    return CompareOrdered((ushort)current, unchecked((ushort)intValue));
                case VariableType.Int:
                    return CompareOrdered((int)current, intValue);
                case VariableType.UInt:
                    return CompareOrdered((uint)current, uintValue);
                case VariableType.Long:
                    return CompareOrdered((long)current, longValue);
                case VariableType.ULong:
                    return CompareOrdered((ulong)current, ulongValue);
                case VariableType.Float:
                    return CompareFloat((float)current, floatValue);
                case VariableType.Double:
                    return CompareDouble((double)current, doubleValue);
                case VariableType.Decimal:
                    return CompareOrdered((decimal)current, decimalValue);
                case VariableType.Char:
                    return CompareOrdered((char)current, charValue);
                case VariableType.String:
                    return CompareResult(string.CompareOrdinal(
                        (string)current, stringValue));
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

        private bool CompareDouble(double current, double expected)
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
            VariableType next = GetVariableType(fieldType);
            if (next != VariableType.None) variableType = next;
            if (!SupportsOrdering(variableType) &&
                compareType != CompareType.Equals &&
                compareType != CompareType.NotEquals)
                compareType = CompareType.Equals;
        }

        private ValueDropdownList<string> InspectorFields =>
            BTInspectorVariableUtility.GetFields(inspectorBlackboardType);

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

        private bool ShowIntInspectorValue =>
            variableType == VariableType.Bool ||
            variableType == VariableType.Enum ||
            variableType == VariableType.SByte ||
            variableType == VariableType.Byte ||
            variableType == VariableType.Short ||
            variableType == VariableType.UShort ||
            variableType == VariableType.Int;
        private bool ShowUIntInspectorValue => variableType == VariableType.UInt;
        private bool ShowLongInspectorValue => variableType == VariableType.Long;
        private bool ShowULongInspectorValue => variableType == VariableType.ULong;
        private bool ShowFloatInspectorValue => variableType == VariableType.Float;
        private bool ShowDoubleInspectorValue => variableType == VariableType.Double;
        private bool ShowDecimalInspectorValue =>
            variableType == VariableType.Decimal;
        private bool ShowCharInspectorValue => variableType == VariableType.Char;
        private bool ShowStringInspectorValue =>
            variableType == VariableType.String || variableType == VariableType.Enum;
    }
}
