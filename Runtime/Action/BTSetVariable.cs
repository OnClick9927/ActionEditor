using System;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("设置或修改指定黑板字段。整数运算统一采用 unchecked 溢出规则，字符串使用 Ordinal 语义，decimal 不经过浮点转换；只有显式选择 float 或 double 字段时才进入非帧同步浮点路径。")]
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

        [Name("参数名称", "选择需要写入的黑板公开字段。行为树初始化时会检查字段仍然存在、记录类型与真实字段类型完全一致，并拒绝不适用于该类型的运算。")]
        [ValueDropdown(nameof(InspectorFields))]
        public string fieldName;
        [Name("参数类型", "由编辑器依据黑板字段自动记录。布尔支持设置和取反，枚举与字符支持设置，字符串支持设置和追加，各数值类型支持其安全且有明确语义的运算。")]
        [ReadOnly]
        public BTVariableCondition.VariableType variableType;
        [Name("运算方式", "Set 直接覆盖；其余操作先读取字段当前值再计算。整数加减乘、幂和取负使用 unchecked 补码回绕，保证相同输入得到相同位结果。")]
        [ValueDropdown(nameof(InspectorOperations))]
        public SetVariableType setType;
        [Name("32 位有符号操作数", "供 int、sbyte、byte、short、ushort 使用，同时兼容旧资源中的布尔值和 32 位枚举值。编辑器会按目标字段范围限制输入。")]
        [ShowIf(nameof(ShowIntInspectorValue))]
        public int intValue;
        [Name("32 位无符号操作数", "供 uint 字段使用，覆盖完整无符号 32 位范围。所有 uint 运算直接在无符号整数域中执行，不经过浮点数。")]
        [ShowIf(nameof(ShowUIntInspectorValue))]
        public uint uintValue;
        [Name("64 位有符号操作数", "供 long 字段使用。加减乘和整数幂在溢出时按 unchecked 规则回绕，long.MinValue 除以 -1 也固定回绕为 long.MinValue。")]
        [ShowIf(nameof(ShowLongInspectorValue))]
        public long longValue;
        [Name("64 位无符号操作数", "供 ulong 字段使用，保留完整 64 位无符号精度。运算不经过 decimal、double 或有符号中间结果。")]
        [ShowIf(nameof(ShowULongInspectorValue))]
        public ulong ulongValue;
        [Name("单精度操作数", "供 float 字段使用。允许常规浮点四则、余数、幂和最值运算，但不承诺跨 CPU 或跨运行时的帧同步一致性。")]
        [ShowIf(nameof(ShowFloatInspectorValue))]
        public float floatValue;
        [Name("双精度操作数", "供 double 字段使用。保留双精度范围和 C# 浮点特殊值语义，仅应在不要求跨平台帧同步的行为树中使用。")]
        [ShowIf(nameof(ShowDoubleInspectorValue))]
        public double doubleValue;
        [Name("十进制操作数", "供 decimal 字段使用。支持设置、四则、余数、取负、绝对值和最值；decimal 溢出会抛出异常，不做静默回绕。")]
        [ShowIf(nameof(ShowDecimalInspectorValue))]
        public decimal decimalValue;
        [Name("字符操作数", "供 char 字段的 Set 操作使用，按一个 UTF-16 码元保存；该节点不执行字符算术或本地化转换。")]
        [ShowIf(nameof(ShowCharInspectorValue))]
        public char charValue;
        [Name("文本或枚举操作数", "string 字段使用此值执行设置或追加；枚举保存名称或 Flags 组合名称并按目标枚举类型解析，避免大底层值被截断。")]
        [ShowIf(nameof(ShowStringInspectorValue))]
        public string stringValue;

        [NonSerialized] private Type runtimeFieldType;
        [NonSerialized] private object runtimeEnumValue;
        [NonSerialized] private Type inspectorBlackboardType;

        public bool boolValue
        {
            get => GetLegacyCompatibleInt() != 0;
            set
            {
                intValue = value ? 1 : 0;
                floatValue = intValue;
            }
        }

        internal override void Init(Blackboard blackboard, BTNode parent,
            BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            runtimeFieldType = blackboard.GetValueType(fieldName);
            if (!BTVariableCondition.IsVariableType(runtimeFieldType, variableType))
                throw new InvalidOperationException(
                    $"{GetType()} cannot use Blackboard field '{fieldName}' as {variableType}");
            if (!SupportsOperation(variableType, setType))
                throw new InvalidOperationException(
                    $"{GetType()} does not support {setType} for {variableType}");
            if ((setType == SetVariableType.Divide ||
                 setType == SetVariableType.Remainder) && IsOperandZero())
                throw new InvalidOperationException($"{GetType()} cannot divide by zero");
            if (setType == SetVariableType.Power &&
                IsSignedInteger(variableType) && GetSignedOperand() < 0)
                throw new InvalidOperationException(
                    $"{GetType()} requires a non-negative integer exponent");
            runtimeEnumValue = variableType == BTVariableCondition.VariableType.Enum
                ? BTVariableCondition.ResolveEnumValue(runtimeFieldType,
                    stringValue, GetLegacyCompatibleInt())
                : null;
        }

        protected override State OnUpdate()
        {
            object current = setType == SetVariableType.Set
                ? null
                : blackboard.GetValue(fieldName);
            object result;
            unchecked
            {
                switch (variableType)
                {
                    case BTVariableCondition.VariableType.Bool:
                        result = setType == SetVariableType.Not
                            ? !(bool)current
                            : boolValue;
                        break;
                    case BTVariableCondition.VariableType.Enum:
                        result = runtimeEnumValue;
                        break;
                    case BTVariableCondition.VariableType.String:
                        result = setType == SetVariableType.Add
                            ? string.Concat((string)current, stringValue)
                            : stringValue;
                        break;
                    case BTVariableCondition.VariableType.Char:
                        result = charValue;
                        break;
                    case BTVariableCondition.VariableType.SByte:
                        result = (sbyte)CalculateSigned((sbyte)current,
                            (sbyte)intValue);
                        break;
                    case BTVariableCondition.VariableType.Byte:
                        result = (byte)CalculateUnsigned((byte)current,
                            (byte)intValue);
                        break;
                    case BTVariableCondition.VariableType.Short:
                        result = (short)CalculateSigned((short)current,
                            (short)intValue);
                        break;
                    case BTVariableCondition.VariableType.UShort:
                        result = (ushort)CalculateUnsigned((ushort)current,
                            (ushort)intValue);
                        break;
                    case BTVariableCondition.VariableType.Int:
                        result = (int)CalculateSigned((int)current,
                            GetLegacyCompatibleInt());
                        break;
                    case BTVariableCondition.VariableType.UInt:
                        result = (uint)CalculateUnsigned((uint)current, uintValue);
                        break;
                    case BTVariableCondition.VariableType.Long:
                        result = CalculateSigned((long)current, longValue);
                        break;
                    case BTVariableCondition.VariableType.ULong:
                        result = CalculateUnsigned((ulong)current, ulongValue);
                        break;
                    case BTVariableCondition.VariableType.Float:
                        result = CalculateFloat((float)current);
                        break;
                    case BTVariableCondition.VariableType.Double:
                        result = CalculateDouble((double)current);
                        break;
                    case BTVariableCondition.VariableType.Decimal:
                        result = CalculateDecimal((decimal)current);
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"{GetType()} has an unsupported variable type {variableType}");
                }
            }
            blackboard.SetValue(fieldName, result);
            return State.Success;
        }

        public static bool SupportsOperation(BTVariableCondition.VariableType type,
            SetVariableType operation)
        {
            bool numeric = IsSignedInteger(type) || IsUnsignedInteger(type) ||
                type == BTVariableCondition.VariableType.Float ||
                type == BTVariableCondition.VariableType.Double ||
                type == BTVariableCondition.VariableType.Decimal;
            switch (operation)
            {
                case SetVariableType.Set:
                    return type != BTVariableCondition.VariableType.None;
                case SetVariableType.Add:
                    return numeric || type == BTVariableCondition.VariableType.String;
                case SetVariableType.Subtract:
                case SetVariableType.Multiply:
                case SetVariableType.Divide:
                case SetVariableType.Remainder:
                case SetVariableType.Min:
                case SetVariableType.Max:
                    return numeric;
                case SetVariableType.Power:
                    return type != BTVariableCondition.VariableType.Decimal && numeric;
                case SetVariableType.Negate:
                case SetVariableType.Absolute:
                    return !IsUnsignedInteger(type) && numeric;
                case SetVariableType.Not:
                    return type == BTVariableCondition.VariableType.Bool;
                default:
                    return false;
            }
        }

        public static bool IsSignedInteger(BTVariableCondition.VariableType type) =>
            type == BTVariableCondition.VariableType.SByte ||
            type == BTVariableCondition.VariableType.Short ||
            type == BTVariableCondition.VariableType.Int ||
            type == BTVariableCondition.VariableType.Long;

        public static bool IsUnsignedInteger(BTVariableCondition.VariableType type) =>
            type == BTVariableCondition.VariableType.Byte ||
            type == BTVariableCondition.VariableType.UShort ||
            type == BTVariableCondition.VariableType.UInt ||
            type == BTVariableCondition.VariableType.ULong;

        private bool IsOperandZero()
        {
            switch (variableType)
            {
                case BTVariableCondition.VariableType.SByte:
                case BTVariableCondition.VariableType.Byte:
                case BTVariableCondition.VariableType.Short:
                case BTVariableCondition.VariableType.UShort:
                    return intValue == 0;
                case BTVariableCondition.VariableType.Int:
                    return GetLegacyCompatibleInt() == 0;
                case BTVariableCondition.VariableType.UInt: return uintValue == 0;
                case BTVariableCondition.VariableType.Long: return longValue == 0;
                case BTVariableCondition.VariableType.ULong: return ulongValue == 0;
                case BTVariableCondition.VariableType.Float: return floatValue == 0f;
                case BTVariableCondition.VariableType.Double: return doubleValue == 0d;
                case BTVariableCondition.VariableType.Decimal: return decimalValue == 0m;
                default: return false;
            }
        }

        private long GetSignedOperand() =>
            variableType == BTVariableCondition.VariableType.Long
                ? longValue
                : variableType == BTVariableCondition.VariableType.Int
                    ? GetLegacyCompatibleInt()
                    : intValue;

        private int GetLegacyCompatibleInt() =>
            intValue != 0 || floatValue == 0f
                ? intValue
                : unchecked((int)floatValue);

        private long CalculateSigned(long current, long operand)
        {
            unchecked
            {
                switch (setType)
                {
                    case SetVariableType.Set: return operand;
                    case SetVariableType.Add: return current + operand;
                    case SetVariableType.Subtract: return current - operand;
                    case SetVariableType.Multiply: return current * operand;
                    case SetVariableType.Divide:
                        return current == long.MinValue && operand == -1
                            ? long.MinValue
                            : current / operand;
                    case SetVariableType.Remainder:
                        return current == long.MinValue && operand == -1
                            ? 0
                            : current % operand;
                    case SetVariableType.Power:
                        return SignedPower(current, (ulong)operand);
                    case SetVariableType.Negate: return -current;
                    case SetVariableType.Absolute: return current < 0 ? -current : current;
                    case SetVariableType.Min: return Math.Min(current, operand);
                    case SetVariableType.Max: return Math.Max(current, operand);
                    default: throw new InvalidOperationException(
                        $"Unsupported signed integer operation {setType}");
                }
            }
        }

        private ulong CalculateUnsigned(ulong current, ulong operand)
        {
            unchecked
            {
                switch (setType)
                {
                    case SetVariableType.Set: return operand;
                    case SetVariableType.Add: return current + operand;
                    case SetVariableType.Subtract: return current - operand;
                    case SetVariableType.Multiply: return current * operand;
                    case SetVariableType.Divide: return current / operand;
                    case SetVariableType.Remainder: return current % operand;
                    case SetVariableType.Power: return UnsignedPower(current, operand);
                    case SetVariableType.Min: return Math.Min(current, operand);
                    case SetVariableType.Max: return Math.Max(current, operand);
                    default: throw new InvalidOperationException(
                        $"Unsupported unsigned integer operation {setType}");
                }
            }
        }

        private float CalculateFloat(float current)
        {
            switch (setType)
            {
                case SetVariableType.Set: return floatValue;
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

        private double CalculateDouble(double current)
        {
            switch (setType)
            {
                case SetVariableType.Set: return doubleValue;
                case SetVariableType.Add: return current + doubleValue;
                case SetVariableType.Subtract: return current - doubleValue;
                case SetVariableType.Multiply: return current * doubleValue;
                case SetVariableType.Divide: return current / doubleValue;
                case SetVariableType.Remainder: return current % doubleValue;
                case SetVariableType.Power: return Math.Pow(current, doubleValue);
                case SetVariableType.Negate: return -current;
                case SetVariableType.Absolute: return Math.Abs(current);
                case SetVariableType.Min: return Math.Min(current, doubleValue);
                case SetVariableType.Max: return Math.Max(current, doubleValue);
                default: throw new InvalidOperationException(
                    $"Unsupported double operation {setType}");
            }
        }

        private decimal CalculateDecimal(decimal current)
        {
            switch (setType)
            {
                case SetVariableType.Set: return decimalValue;
                case SetVariableType.Add: return current + decimalValue;
                case SetVariableType.Subtract: return current - decimalValue;
                case SetVariableType.Multiply: return current * decimalValue;
                case SetVariableType.Divide: return current / decimalValue;
                case SetVariableType.Remainder: return current % decimalValue;
                case SetVariableType.Negate: return -current;
                case SetVariableType.Absolute: return Math.Abs(current);
                case SetVariableType.Min: return Math.Min(current, decimalValue);
                case SetVariableType.Max: return Math.Max(current, decimalValue);
                default: throw new InvalidOperationException(
                    $"Unsupported decimal operation {setType}");
            }
        }

        private static long SignedPower(long value, ulong exponent)
        {
            long result = 1;
            long factor = value;
            unchecked
            {
                while (exponent > 0)
                {
                    if ((exponent & 1UL) != 0) result *= factor;
                    exponent >>= 1;
                    if (exponent != 0) factor *= factor;
                }
            }
            return result;
        }

        private static ulong UnsignedPower(ulong value, ulong exponent)
        {
            ulong result = 1;
            ulong factor = value;
            unchecked
            {
                while (exponent > 0)
                {
                    if ((exponent & 1UL) != 0) result *= factor;
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
            BTVariableCondition.VariableType next =
                BTVariableCondition.GetVariableType(fieldType);
            if (next != BTVariableCondition.VariableType.None)
                variableType = next;
            if (!SupportsOperation(variableType, setType))
            {
                foreach (SetVariableType operation in
                    Enum.GetValues(typeof(SetVariableType)))
                {
                    if (!SupportsOperation(variableType, operation)) continue;
                    setType = operation;
                    break;
                }
            }
        }

        private ValueDropdownList<string> InspectorFields =>
            BTInspectorVariableUtility.GetFields(inspectorBlackboardType);

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

        private bool InspectorNeedsOperand =>
            setType != SetVariableType.Not &&
            setType != SetVariableType.Negate &&
            setType != SetVariableType.Absolute;
        private bool ShowIntInspectorValue => InspectorNeedsOperand &&
            (variableType == BTVariableCondition.VariableType.Bool ||
             variableType == BTVariableCondition.VariableType.Enum ||
             variableType == BTVariableCondition.VariableType.SByte ||
             variableType == BTVariableCondition.VariableType.Byte ||
             variableType == BTVariableCondition.VariableType.Short ||
             variableType == BTVariableCondition.VariableType.UShort ||
             variableType == BTVariableCondition.VariableType.Int);
        private bool ShowUIntInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.UInt;
        private bool ShowLongInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Long;
        private bool ShowULongInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.ULong;
        private bool ShowFloatInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Float;
        private bool ShowDoubleInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Double;
        private bool ShowDecimalInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Decimal;
        private bool ShowCharInspectorValue => InspectorNeedsOperand &&
            variableType == BTVariableCondition.VariableType.Char;
        private bool ShowStringInspectorValue => InspectorNeedsOperand &&
            (variableType == BTVariableCondition.VariableType.String ||
             variableType == BTVariableCondition.VariableType.Enum);
    }
}
