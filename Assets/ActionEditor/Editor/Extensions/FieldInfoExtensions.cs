using System.Reflection;

namespace ActionEditor
{
    static class FieldInfoExtensions
    {
        public static string GetShowName(this FieldInfo field)
        {
            var name = System.Threading.Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(field.Name);

            var NameAttribute = field.GetCustomAttribute<NameAttribute>();
            if (NameAttribute != null)
            {
                return NameAttribute.name;
            }

            return name;
        }
    }
}