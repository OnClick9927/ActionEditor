namespace ActionEditor.Nodes.BT
{
    [Name("参数比较"), Attachable(typeof(BT.BTTree)), Node(BTNodeTypes.Condition)]
    public class BTVariableCondition : BTCondition
    {
        public enum CompareType
        {
            Equals,
            NotEquals,
            LessThen, LessOrEquals, LargeThen, LargeOrEquals,
        }
        public enum VariableType
        {
            None, Bool, Int, FLoat, Enum,
        }
        public VariableType variableType;
        public CompareType compareType;

        public string fieldName;
        public bool boolValue { get { return intValue == 1; } set { intValue = value ? 1 : 0; } }
        public int intValue;
        public float floatValue;

        private bool CompareInt(int value)
        {
            switch (compareType)
            {
                case CompareType.Equals: return intValue == value;
                case CompareType.NotEquals: return intValue != value;
                case CompareType.LessThen: return intValue > value;
                case CompareType.LessOrEquals: return intValue >= value;
                case CompareType.LargeThen: return intValue < value;
                case CompareType.LargeOrEquals: return intValue <= value;
            }
            return true;
        }
        private bool CompareFloat(float value)
        {
            switch (compareType)
            {
                case CompareType.Equals: return floatValue == value;
                case CompareType.NotEquals: return floatValue != value;
                case CompareType.LessThen: return floatValue > value;
                case CompareType.LessOrEquals: return floatValue >= value;
                case CompareType.LargeThen: return floatValue < value;
                case CompareType.LargeOrEquals: return floatValue <= value;
            }
            return true;
        }

        protected override bool Condition()
        {
            var _value = blackboard.GetValue(fieldName);
            switch (variableType)
            {
                case VariableType.Bool:
                    var value = (bool)_value;
                    return CompareInt(value ? 1 : 0);
                case VariableType.Enum:
                case VariableType.Int:
                    return CompareInt((int)_value);
                case VariableType.FLoat:
                    return CompareFloat((float)_value);
            }
            return true;
        }


    }
}
