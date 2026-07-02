using System;

namespace ActionEditor.Nodes.BT
{
    [Name("…Ë÷√≤Œ ˝"), Attachable(typeof(BT.BTTree)), Node(BTNodeTypes.Action)]

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
                    return (float)Math.Ceiling(value);
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
            var _value = blackboard.GetValue(fieldName);
            switch (variableType)
            {
                case BTVariableCondition.VariableType.Bool:
                    {
                        if (setType == SetVariableType.Set)
                            blackboard.SetValue(fieldName, boolValue);
                        else if (setType == SetVariableType.Not)
                            blackboard.SetValue(fieldName, !((bool)_value));
                    }
                    break;
                case BTVariableCondition.VariableType.Int:
                    {
                        var value = CalcFloat((float)_value);
                        blackboard.SetValue(fieldName, (int)value);
                    }
                    break;
                case BTVariableCondition.VariableType.FLoat:
                    {
                        var value = CalcFloat((float)_value);
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
