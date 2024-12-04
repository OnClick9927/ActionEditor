using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionEditor
{

    public static class ReflectionTools
    {

        public static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());

        }


        /// <summary>
        /// 获取类型所有子类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Type[] GetImplementationsOf(Type type)
        {
            if (type.IsInterface)
                return GetAllTypes().Where(x => x.GetInterfaces().Count(y => y == type) != 0).Where(x=>!x.IsAbstract).ToArray();
            return GetAllTypes().Where(x => type.IsAssignableFrom(x)).Where(x => !x.IsAbstract).ToArray();

        }


        public static T RTGetAttribute<T>(this Type type, bool inherited) where T : Attribute
        {
#if NETFX_CORE
			return (T)type.GetTypeInfo().GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
#else
            return (T)type.GetCustomAttributes(typeof(T), inherited).FirstOrDefault();
#endif
        }
    }
}