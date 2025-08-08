using System;

namespace ActionEditor
{


    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class CustomActionViewAttribute : System.Attribute
    {
        public Type InspectedType;

        public CustomActionViewAttribute(Type inspectedType)
        {
            InspectedType = inspectedType;
        }
    }


   

}