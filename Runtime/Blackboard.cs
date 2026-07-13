using ActionBuffer;

namespace ActionEditor.Nodes.BT
{
    [System.Serializable]
    public abstract class Blackboard
    {
        public virtual object GetValue(string fieldName)
        {
            var field = TypeHelper.GetTypeFields(GetType()).FindField(fieldName);
            if (field == null) return default;
            return field.GetValue(this);

        }

        public virtual void SetValue(string fieldName, object value)
        {
            var field = TypeHelper.GetTypeFields(GetType()).FindField(fieldName);
            if (field == null) return;
            field.SetValue(this, value);
        }
    }



}
