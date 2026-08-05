using ActionBuffer;
using System;

namespace ActionEditor.Nodes.BT
{
    [System.Serializable]
    public abstract class Blackboard
    {
        [NonSerialized] private TypeHelper.TypeFields typeFields;

        private TypeHelper.TypeFields Fields =>
            typeFields ?? (typeFields = TypeHelper.GetTypeFields(GetType()));

        internal Type GetValueType(string fieldName) =>
            Fields.FindField(fieldName)?.FieldType;

        public virtual object GetValue(string fieldName)
        {
            var field = Fields.FindField(fieldName);
            if (field == null) return default;
            return field.GetValue(this);
        }

        public virtual void SetValue(string fieldName, object value)
        {
            var field = Fields.FindField(fieldName);
            if (field == null) return;
            if (value != null && field.FieldType.IsEnum &&
                value.GetType() != field.FieldType)
                value = Enum.ToObject(field.FieldType, value);
            field.SetValue(this, value);
        }
    }
}
