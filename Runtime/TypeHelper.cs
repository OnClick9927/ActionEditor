using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;


namespace ActionBuffer
{
    public static class TypeHelper
    {
        public class TypeFields
        {
            private readonly Type type;
            public class Field
            {
                public readonly string name;
                public readonly Type FieldType;
                private readonly FieldInfo field;
                private readonly object defaultValue;
                private BuffConverter converter;
                private long converterVersion = -1;
                public readonly Type DeclaringType;
                public readonly bool IsEvent;
                public object GetValue(object target) => field.GetValue(target);
                public void SetValue(object target, object value) => field.SetValue(target, value);
                internal void SetDefaultValue(object target) => field.SetValue(target, defaultValue);
                internal BuffConverter GetConverter(BufferSerializerSettings settings = null)
                {
                    long version = BufferSerializer.GetResolverVersion(settings);
                    if (converterVersion == version) return converter;
                    converter = BufferSerializer.GetConverter(FieldType, settings);
                    converterVersion = version;
                    return converter;
                }
                public Field(FieldInfo field, string name)
                {
                    DeclaringType = field.DeclaringType;
                    FieldType = field.FieldType;
                    this.field = field;
                    this.name = name;
                    IsEvent = field.DeclaringType?.GetEvent(field.Name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly) != null;
                    defaultValue = GetDefaultValue(FieldType);
                }
            }

            public sealed class FieldCollection : IReadOnlyList<Field>
            {
                private readonly List<Field> _items;

                internal FieldCollection(List<Field> items)
                {
                    _items = items;
                }

                public int Count => _items.Count;
                public Field this[int index] => _items[index];
                public List<Field> FindAll(Predicate<Field> match) => _items.FindAll(match);
                public List<Field>.Enumerator GetEnumerator() => _items.GetEnumerator();
                System.Collections.Generic.IEnumerator<Field>
                    System.Collections.Generic.IEnumerable<Field>.GetEnumerator() => _items.GetEnumerator();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _items.GetEnumerator();
            }

            public TypeFields(Type type, bool usePropertyBackingFields)
            {
                this.type = type;
                fieldView = new FieldCollection(fields);
                this.usePropertyBackingFields = usePropertyBackingFields;
                useUninitializedObject = usePropertyBackingFields ||
                                         (!type.IsValueType && type.GetConstructor(
                                             BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                                             null, Type.EmptyTypes, null) == null);
            }
            private readonly List<Field> fields = new();
            private readonly Dictionary<string, Field> map = new();
            private readonly FieldCollection fieldView;
            private readonly bool usePropertyBackingFields;
            private readonly bool useUninitializedObject;
            public FieldCollection GetFields() => fieldView;
            public Field FindField(string name)
            {
                if (map.TryGetValue(name, out var field)) return field;
                return null;
            }
            internal void SetDefaultValues(object target)
            {
                for (int i = 0; i < fields.Count; i++)
                    fields[i].SetDefaultValue(target);
            }
            internal void AddField(Field field)
            {
                if (map.ContainsKey(field.name)) return;
                map[field.name] = field;
                fields.Add(field);
            }
            internal void AddField(FieldInfo field, bool force = false)
            {
                if (field.IsDefined(typeof(System.NonSerializedAttribute))) return;
                var attr = field.GetCustomAttribute<BufferAttribute>();
                if (typeof(Delegate).IsAssignableFrom(field.FieldType) && attr == null)
                    return;
                if (!force)
                    if (!field.IsPublic && attr == null) return;
                var name = attr?.bufferName ?? field.Name;
                if (map.TryGetValue(name, out var info))
                    throw new InvalidOperationException(
                        $"Type '{type}' contains duplicate serialized field name '{name}' " +
                        $"in '{info.DeclaringType}' and '{field.DeclaringType}'.");
                var _f = new Field(field, name);
                AddField(_f);
            }

            internal void AddField(FieldInfo field, string name)
            {
                if (field == null) return;
                if (typeof(Delegate).IsAssignableFrom(field.FieldType) &&
                    field.GetCustomAttribute<BufferAttribute>() == null) return;
                if (map.TryGetValue(name, out var info))
                    throw new InvalidOperationException(
                        $"Type '{type}' contains duplicate serialized field name '{name}' " +
                        $"in '{info.DeclaringType}' and '{field.DeclaringType}'.");
                AddField(new Field(field, name));
            }

            internal bool Contains(string name) => map.ContainsKey(name);

            internal void Sort()
            {
                fields.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            }

            internal object CreateInstance()
            {
                if (useUninitializedObject)
                    return FormatterServices.GetUninitializedObject(type);
                return Activator.CreateInstance(type, true);
            }

            internal bool UsePropertyBackingFields => usePropertyBackingFields;
        }
        private static readonly Dictionary<Type, TypeFields> map =
            new Dictionary<Type, TypeFields>();
        public static TypeFields GetTypeFields(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (map.TryGetValue(type, out var typefield)) return typefield;
            typefield = CreateTypeFields(type);
            map[type] = typefield;
            return typefield;
        }

        private static TypeFields CreateTypeFields(Type type)
        {
            bool usePropertyBackingFields = UsesPropertyBackingFields(type);
            var typefield = new TypeFields(type, usePropertyBackingFields);
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
            if (usePropertyBackingFields)
                AddPropertyBackingFields(typefield, type, declaredFields);
            typefield.Sort();
            return typefield;
        }

        private static void AddPropertyBackingFields(TypeFields typeFields, Type type, FieldInfo[] fields)
        {
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly);
            for (int i = 0; i < properties.Length; i++)
            {
                var property = properties[i];
                if (!property.CanRead || property.GetIndexParameters().Length != 0 ||
                    typeof(Delegate).IsAssignableFrom(property.PropertyType) ||
                    typeFields.Contains(property.Name))
                    continue;

                var backingField = FindPropertyBackingField(fields, property);
                if (backingField != null)
                    typeFields.AddField(backingField, property.Name);
            }
        }

        private static FieldInfo FindPropertyBackingField(FieldInfo[] fields, PropertyInfo property)
        {
            string compilerName = $"<{property.Name}>k__BackingField";
            string anonymousName = $"<{property.Name}>i__Field";
            string tupleName = "m_" + property.Name;
            for (int i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.FieldType != property.PropertyType) continue;
                if (field.Name == compilerName || field.Name == anonymousName || field.Name == tupleName)
                    return field;
            }
            return null;
        }

        private static bool UsesPropertyBackingFields(Type type)
        {
            if (type == null) return false;
            if (!type.IsValueType && type.Namespace == "System" && type.IsGenericType &&
                type.GetGenericTypeDefinition().FullName.StartsWith("System.Tuple`", StringComparison.Ordinal))
                return true;
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), false) &&
                type.Name.IndexOf("AnonymousType", StringComparison.Ordinal) >= 0)
                return true;
            if (type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public |
                    BindingFlags.NonPublic) != null ||
                type.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic) != null)
                return true;
            return type.GetMethod("PrintMembers", BindingFlags.Instance | BindingFlags.NonPublic,
                       null, new[] { typeof(StringBuilder) }, null) != null;
        }

        private static bool IsSubclassOfGeneric(Type self, Type genericType)
        {
            if (!genericType.IsGenericTypeDefinition)
                return false;

            if (self.IsGenericType && self.GetGenericTypeDefinition().Equals(genericType))
                return true;

            Type baseType = self.BaseType;
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

        private static IReadOnlyList<Type> types;
        private static int typeAssemblyCount;
        private static readonly Dictionary<Type, IReadOnlyList<Type>> subMap =
            new Dictionary<Type, IReadOnlyList<Type>>();

        private static IReadOnlyList<Type> GetConcreteTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (types != null && typeAssemblyCount == assemblies.Length) return types;

            var result = new List<Type>();
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
                for (int j = 0; j < assemblyTypes.Length; j++)
                {
                    var assemblyType = assemblyTypes[j];
                    if (assemblyType != null)
                        result.Add(assemblyType);
                }
            }

            types = result.AsReadOnly();
            typeAssemblyCount = assemblies.Length;
            subMap.Clear();
            return types;
        }

        public static IReadOnlyList<Type> GetSubTypes(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var source = GetConcreteTypes();
            if (!subMap.TryGetValue(type, out var result))
            {
                var matches = new List<Type>();
                for (int i = 0; i < source.Count; i++)
                {
                    var candidate = source[i];
                    if (candidate == type) continue;
                    if (candidate.IsAbstract || candidate.IsInterface)
                        continue;

                    var isMatch = type.IsGenericTypeDefinition
                        ? IsSubclassOfGeneric(candidate, type)
                        : type.IsAssignableFrom(candidate);
                    if (isMatch)
                        matches.Add(candidate);
                }

                result = matches.AsReadOnly();
                subMap.Add(type, result);
            }
            return result;
        }

        private readonly struct TypeCacheKey : IEquatable<TypeCacheKey>
        {
            private readonly string _typeName;
            private readonly string _assemblyName;

            public TypeCacheKey(string typeName, string assemblyName)
            {
                _typeName = typeName;
                _assemblyName = assemblyName ?? string.Empty;
            }

            public bool Equals(TypeCacheKey other) =>
                string.Equals(_typeName, other._typeName, StringComparison.Ordinal) &&
                string.Equals(_assemblyName, other._assemblyName, StringComparison.Ordinal);
            public override bool Equals(object obj) => obj is TypeCacheKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_typeName?.GetHashCode() ?? 0) * 397) ^ _assemblyName.GetHashCode();
                }
            }
        }

        private static readonly Dictionary<TypeCacheKey, Type> _typeMap =
            new Dictionary<TypeCacheKey, Type>();
        private static int _typeMapAssemblyCount;

        internal static Type ResolveSerializedType(Type declaredType, string typeFullName, string assemblyName)
        {
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
            if (string.IsNullOrEmpty(typeFullName)) return declaredType;

            var actualType = GetTypeByFullName(typeFullName, assemblyName);
            if (actualType == null)
                throw new FormatException($"Cannot resolve type '{typeFullName}, {assemblyName}'.");
            if (!declaredType.IsAssignableFrom(actualType))
                throw new FormatException($"Type '{actualType}' is not assignable to '{declaredType}'.");
            if (actualType.IsAbstract || actualType.IsInterface || actualType.ContainsGenericParameters)
                throw new FormatException($"Type '{actualType}' is not a concrete serializable type.");
            return actualType;
        }

        public static Type GetTypeByFullName(string typeFullName, string assemblyName = null)
        {
            if (string.IsNullOrEmpty(typeFullName))
                return null;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (_typeMapAssemblyCount != assemblies.Length)
            {
                _typeMap.Clear();
                _typeMapAssemblyCount = assemblies.Length;
            }
            var cacheKey = new TypeCacheKey(typeFullName, assemblyName);
            if (_typeMap.TryGetValue(cacheKey, out var type)) return type;

            if (!string.IsNullOrEmpty(assemblyName))
            {
                string requestedAssembly;
                try
                {
                    requestedAssembly = new AssemblyName(assemblyName).Name;
                }
                catch
                {
                    return null;
                }
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (!string.Equals(assemblies[i].GetName().Name, requestedAssembly, StringComparison.Ordinal))
                        continue;
                    type = assemblies[i].GetType(typeFullName, false);
                    if (type != null) break;
                }
            }
            else
            {
                Type match = null;
                for (int i = 0; i < assemblies.Length; i++)
                {
                    var candidate = assemblies[i].GetType(typeFullName, false);
                    if (candidate == null) continue;
                    if (match != null && match != candidate) return null;
                    match = candidate;
                }
                type = match;
            }

            _typeMap[cacheKey] = type;
            return type;
        }

        public static T DeepCopyByBuffer<T>(this T value) =>
            BufferSerializer.ToObject<T>(BufferSerializer.ToBytes(value));

        private static readonly Dictionary<Type, object> _defaultValues =
            new Dictionary<Type, object>();
        internal static object GetDefaultValue(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null) return null;
            if (_defaultValues.TryGetValue(type, out var defaultValue)) return defaultValue;
            defaultValue = Activator.CreateInstance(type);
            _defaultValues[type] = defaultValue;
            return defaultValue;
        }

        internal static bool IsNullOrDefault(object obj, Type declaredType)
        {
            if (obj == null) return true;
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
            if (!declaredType.IsValueType || Nullable.GetUnderlyingType(declaredType) != null) return false;
            return obj.Equals(GetDefaultValue(declaredType));
        }
        private static readonly Dictionary<Type, string> type_warp = new Dictionary<Type, string>()
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
        private static readonly Dictionary<string, string> type_warp_2 = CreateReverseTypeMap();
        internal static object CreateInstance(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return GetTypeFields(type).CreateInstance();
        }
        internal static string GetTypeName(Type type)
        {
            if (type_warp.TryGetValue(type, out var result))
                return result;
            return type.FullName;
        }
        internal static string GetRealTypeName(string src)
        {
            if (type_warp_2.TryGetValue(src, out var result))
                return result;
            return src;
        }

        private static Dictionary<string, string> CreateReverseTypeMap()
        {
            var result = new Dictionary<string, string>();
            foreach (var item in type_warp)
                result.Add(item.Value, item.Key.FullName);
            return result;
        }
    }
}
