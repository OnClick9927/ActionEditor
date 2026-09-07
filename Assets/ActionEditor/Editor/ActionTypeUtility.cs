using System;
using System.Collections.Generic;
using System.Reflection;
namespace ActionEditor
{
    public static class ActionTypeUtility
    {
        private static IReadOnlyList<Type> allTypes;

        static ActionTypeUtility()
        {
            AppDomain.CurrentDomain.AssemblyLoad += (_, __) => allTypes = null;
        }

        public static IReadOnlyList<Type> GetSubTypes(Type baseType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            IReadOnlyList<Type> types = GetAllTypes();
            var result = new List<Type>();
            for (int i = 0; i < types.Count; i++)
            {
                Type type = types[i];
                if (type != baseType && baseType.IsAssignableFrom(type))
                    result.Add(type);
            }
            return result;
        }

        public static Type GetTypeByFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return null;
            Type direct = Type.GetType(fullName, false);
            if (direct != null) return direct;
            IReadOnlyList<Type> types = GetAllTypes();
            for (int i = 0; i < types.Count; i++)
                if (string.Equals(types[i].FullName, fullName,
                        StringComparison.Ordinal))
                    return types[i];
            return null;
        }

        private static IReadOnlyList<Type> GetAllTypes()
        {
            if (allTypes != null) return allTypes;
            var result = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                try
                {
                    result.AddRange(assemblies[i].GetTypes());
                }
                catch (ReflectionTypeLoadException exception)
                {
                    Type[] loaded = exception.Types;
                    for (int j = 0; j < loaded.Length; j++)
                        if (loaded[j] != null) result.Add(loaded[j]);
                }
            }
            allTypes = result;
            return allTypes;
        }
    }

}
