using System;
using System.Collections.Generic;
using System.Reflection;


namespace ActionBuffer
{
    public static class TypeHelper
    {
        public class TypeFields
        {
            private Type type;
            public class Field
            {
                public readonly string name;
                public Type FieldType;
                private FieldInfo field;
                public Type DeclaringType;
                public object GetValue(object target) => field.GetValue(target);
                public void SetValue(object target, object value) => field.SetValue(target, value);
                public Field(FieldInfo field, string name)
                {
                    DeclaringType = field.DeclaringType;
                    FieldType = field.FieldType;
                    this.field = field;
                    this.name = name;
                }
            }
            public TypeFields(Type type)
            {
                this.type = type;
            }
            private List<Field> fields = new();
            private Dictionary<string, Field> map = new();
            public List<Field> GetFields() => fields;
            public Field FindField(string name)
            {
                if (map.TryGetValue(name, out var field)) return field;
                return null;
            }
            public void AddField(Field field)
            {
                map[field.name] = field;
                fields.Add(field);
            }
            public void AddField(FieldInfo field, bool force = false)
            {
                if (field.IsDefined(typeof(System.NonSerializedAttribute))) return;
                var attr = field.GetCustomAttribute<BufferAttribute>();
                if (!force)
                    if (!field.IsPublic && attr == null) return;
                var name = attr?.bufferName ?? field.Name;
                if (map.TryGetValue(name, out var info))
                    throw new Exception($"{type}Exist Same Name Field {name}=> {info.DeclaringType}:{field.DeclaringType}");
                var _f = new Field(field, name);
                AddField(_f);
            }
        }
        private static Dictionary<Type, TypeFields> map = new Dictionary<Type, TypeFields>();
        public static TypeFields GetTypeFields(Type type)
        {
            if (map.TryGetValue(type, out var typefield)) return typefield;
            typefield = new TypeFields(type);

            var baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object))
            {
                var baseFields = GetTypeFields(baseType).GetFields();
                for (int i = 0; i < baseFields.Count; i++)
                    typefield.AddField(baseFields[i]);
            }

            var declaredFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                                                BindingFlags.Instance | BindingFlags.DeclaredOnly);
            for (int i = 0; i < declaredFields.Length; i++)
                typefield.AddField(declaredFields[i]);

            map[type] = typefield;
            return typefield;
        }

        public static bool IsSubclassOfGeneric(Type self, Type genericType)
        {
#if NETFX_CORE
                if (!genericTypeDefinition.GetTypeInfo().IsGenericTypeDefinition)
#else
            if (!genericType.IsGenericTypeDefinition)
#endif
                return false;

#if NETFX_CORE
                if (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition().Equals(genericTypeDefinition))
#else
            if (self.IsGenericType && self.GetGenericTypeDefinition().Equals(genericType))
#endif
                return true;

#if NETFX_CORE
                Type baseType = type.GetTypeInfo().BaseType;
#else
            Type baseType = self.BaseType;
#endif
            if (baseType != null && baseType != typeof(object))
            {
                if (IsSubclassOfGeneric(baseType, genericType))
                    return true;
            }

            foreach (Type t in self.GetInterfaces())
            {
                if (IsSubclassOfGeneric(t, genericType))
                    return true;
            }

            return false;
        }

        private static List<Type> types;
        public static List<Type> Types => GetConcreteTypes();
        private static Dictionary<Type, List<Type>> subMap = new();

        private static List<Type> GetConcreteTypes()
        {
            if (types != null) return types;

            var result = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                Type[] assemblyTypes;
                try
                {
                    assemblyTypes = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    assemblyTypes = exception.Types;
                }
                result.AddRange(assemblyTypes);
            }

            types = result;
            return types;
        }

        public static List<Type> GetSubTypes(Type type)
        {
            if (!subMap.TryGetValue(type, out var result))
            {
                var source = GetConcreteTypes();
                var matches = new List<Type>();
                for (int i = 0; i < source.Count; i++)
                {

                    var candidate = source[i];
                    if (candidate == type) continue;
                    if (candidate.IsAbstract) continue;

                    var isMatch = type.IsInterface
                        ? type.IsAssignableFrom(candidate)
                        : type.IsGenericType
                            ? IsSubclassOfGeneric(candidate, type)
                            : candidate.IsSubclassOf(type);
                    if (isMatch)
                        matches.Add(candidate);
                }

                subMap.Add(type, matches);
                result = matches;
            }
            return result;

        }

        private static Dictionary<string, Type> _typeMap = new Dictionary<string, Type>();
        public static Type GetTypeByFullName(string typeFullName, string assemblyName = null)
        {
            if (string.IsNullOrEmpty(typeFullName))
                return null;
            if (_typeMap.TryGetValue(typeFullName, out var type)) return type;
            // 如果指定了程序集名称，拼接完整的类型标识
            string fullTypeName = string.IsNullOrEmpty(assemblyName)
                ? typeFullName
                : $"{typeFullName}, {assemblyName}";

            // 尝试直接获取类型
            type = Type.GetType(fullTypeName);

            // 如果获取失败，遍历当前加载的所有程序集查找
            if (type == null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeFullName);
                    if (type != null)
                        break;
                }
            }
            //if (type != null)
            _typeMap[typeFullName] = type;
            return type;
        }

        public static T DeepCopyByBuffer<T>(this T value) => BuffConverter.ToObject<T>(BuffConverter.ToBytes(value));
        private static readonly Dictionary<Type, object> _defaultValues = new Dictionary<Type, object>();
        public static bool IsNullOrDefault(object obj)
        {
            if (obj == null) return true;
            Type type = obj.GetType();
            if (!type.IsValueType) return false;

            object defaultValue;
            if (!_defaultValues.TryGetValue(type, out defaultValue))
            {
                defaultValue = TypeHelper.CreateInstance(type);
                _defaultValues.Add(type, defaultValue);
            }
            return obj.Equals(defaultValue);
        }
        private static Dictionary<Type, string> type_warp = new Dictionary<Type, string>()
        {
            { typeof(byte),"a"},
            {typeof(bool),"b" },
            { typeof(char),"c" },
            { typeof(short),"d"},
            { typeof(ushort),"e"},
            { typeof(int),"f"},
            { typeof(uint),"g"},
            { typeof(long),"h"},
            { typeof(ulong),"i"},
            { typeof(float),"j"},
            { typeof(double),"k"},
            { typeof(string),"l"},
            { typeof(DateTime),"m"},
            { typeof(TimeSpan),"n"},
        };
        private static Dictionary<string, string> type_warp_2;
        public static object CreateInstance(Type type) => Activator.CreateInstance(type);
        public static string GetTypeName(Type type)
        {
            if (type_warp.TryGetValue(type, out var result))
                return result;
            return type.FullName;
        }
        public static string GetRealTypeName(string src)
        {
            if (type_warp_2 == null)
            {
                type_warp_2 = new Dictionary<string, string>();
                foreach (var t in type_warp)
                    type_warp_2.Add(t.Value, t.Key.FullName);
            }

            if (type_warp_2.TryGetValue(src, out var result))
                return result;
            return src;
        }
    }
}
