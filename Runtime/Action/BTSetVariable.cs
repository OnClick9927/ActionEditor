using ActionUnity;
using System;

namespace ActionEditor.Nodes.BT
{
    [Name("设置参数", "修改黑板中指定参数的值。"), Attachable(typeof(BT.BTTree)), Node(BTNodeTypes.Action), Icon("Action")]

    public class BTSetVariable : BTAction
    {
        public string fieldName;
        public bool boolValue { get { return floatValue == 1; } set { floatValue = value ? 1 : 0; } }
        //public int intValue;
        public float floatValue;
        public BTVariableCondition.VariableType variableType;
        public SetVariableType setType;
        public enum SetVariableType
        {
            Set, Add, Minus, Multiply, Divide, Not, Remainder, Power, Abs, Round, Floor, Ceil, Max, Min
        }

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            Type fieldType = blackboard.GetValueType(fieldName);
            if (!BTVariableCondition.IsVariableType(fieldType, variableType))
                throw new InvalidOperationException(
                    $"{GetType()} cannot use Blackboard field '{fieldName}' as {variableType}");
        }

        private float CalcFloat(float value)
        {
            switch (setType)
            {
                case SetVariableType.Set:
                    return floatValue;
                case SetVariableType.Add:
                    return value + floatValue;
                case SetVariableType.Minus:
                    return value - floatValue;
                case SetVariableType.Multiply:
                    return value * floatValue;
                case SetVariableType.Divide:
                    return value / floatValue;
                case SetVariableType.Remainder:
                    return value % floatValue;
                case SetVariableType.Power:
                    return (float)Math.Pow(value, floatValue);
                case SetVariableType.Round:
                    return (float)Math.Round(value);
                case SetVariableType.Ceil:
                    return (float)Math.Ceiling(value);
                case SetVariableType.Floor:
                    return (float)Math.Floor(value);
                case SetVariableType.Abs:
                    return (float)Math.Abs(value);
                case SetVariableType.Max:
                    return (float)Math.Max(value, floatValue);
                case SetVariableType.Min:
                    return (float)Math.Min(value, floatValue);
                default:
                    return value;
            }

        }
        protected override State OnUpdate()
        {
            switch (variableType)
            {
                case BTVariableCondition.VariableType.Bool:
                    {
                        if (setType == SetVariableType.Set)
                            blackboard.SetValue(fieldName, boolValue);
                        else if (setType == SetVariableType.Not)
                            blackboard.SetValue(fieldName,
                                !(bool)blackboard.GetValue(fieldName));
                    }
                    break;
                case BTVariableCondition.VariableType.Int:
                    {
                        var value = setType == SetVariableType.Set
                            ? floatValue
                            : CalcFloat((int)blackboard.GetValue(fieldName));
                        blackboard.SetValue(fieldName, (int)value);
                    }
                    break;
                case BTVariableCondition.VariableType.FLoat:
                    {
                        var value = setType == SetVariableType.Set
                            ? floatValue
                            : CalcFloat((float)blackboard.GetValue(fieldName));
                        blackboard.SetValue(fieldName, value);
                    }
                    break;
                case BTVariableCondition.VariableType.Enum:
                    {
                        if (setType == SetVariableType.Set)
                            blackboard.SetValue(fieldName, (int)floatValue);
                    }
                    break;
            }
            return State.Success;
        }
    }
}
