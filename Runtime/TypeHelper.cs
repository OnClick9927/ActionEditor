using System;
using System.Collections.Generic;
#if !ENABLE_IL2CPP
using System.Linq.Expressions;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

namespace ActionBuffer
{
    public static class TypeHelper
    {
        private static readonly object CacheSync = new object();
        private static IReadOnlyList<Type> _types;
        private static readonly Dictionary<Type, IReadOnlyList<Type>> SubTypes =
            new Dictionary<Type, IReadOnlyList<Type>>();
        private static readonly Dictionary<TypeCacheKey, Type> TypesByName =
            new Dictionary<TypeCacheKey, Type>();
        private static readonly Dictionary<Type, object> DefaultValues =
            new Dictionary<Type, object>();

        static TypeHelper()
        {
            AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        }

        private static void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            lock (CacheSync)
            {
                _types = null;
                SubTypes.Clear();
                TypesByName.Clear();
            }
        }

        public sealed class TypeFields
        {
            public sealed class Field
            {
                public readonly string name;
                public readonly Type FieldType;
                public readonly Type DeclaringType;
                public readonly bool IsEvent;
                private readonly FieldInfo _field;
                private readonly FieldAccess _access;

                internal Field(FieldInfo field, string name)
                {
                    _field = field;
                    this.name = name;
                    FieldType = field.FieldType;
                    DeclaringType = field.DeclaringType;
                    IsEvent = field.DeclaringType?.GetEvent(field.Name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                        BindingFlags.DeclaredOnly) != null;
                    _access = FieldAccess.Create(field);
                }

                public object GetValue(object target) => _access.GetValue(target);
                public void SetValue(object target, object value) =>
                    _access.SetValue(target, value);
                internal void SetDefaultValue(object target) => _access.SetDefaultValue(target);
                internal bool Capture(BufferScan scan, object target, BuffConverter converter,
                    bool fullField, out BufferScan.CachedField cached) =>
                    _access.Capture(scan, this, target, converter, fullField, out cached);
                internal void ReadAndSet(IBufferReader reader, object target,
                    BuffConverter converter) => _access.ReadAndSet(reader, target, converter);
            }

            private abstract class FieldAccess
            {
                internal abstract object GetValue(object target);
                internal abstract void SetValue(object target, object value);
                internal abstract void SetDefaultValue(object target);
                internal abstract bool Capture(BufferScan scan, Field field, object target,
                    BuffConverter converter, bool fullField,
                    out BufferScan.CachedField cached);
                internal abstract void ReadAndSet(IBufferReader reader, object target,
                    BuffConverter converter);

                internal static FieldAccess Create(FieldInfo field)
                {
#if ENABLE_IL2CPP
                    return new ReflectionFieldAccess(field);
#else
                    try
                    {
                        var accessType = typeof(TypedFieldAccess<,>).MakeGenericType(
                            field.DeclaringType, field.FieldType);
                        return (FieldAccess)Activator.CreateInstance(accessType,
                            BindingFlags.Instance | BindingFlags.Public |
                            BindingFlags.NonPublic, null, new object[] { field }, null);
                    }
                    catch
                    {
                        return new ReflectionFieldAccess(field);
                    }
#endif
                }
            }

            private sealed class ReflectionFieldAccess : FieldAccess
            {
                private readonly FieldInfo _field;
                private readonly object _defaultValue;

                internal ReflectionFieldAccess(FieldInfo field)
                {
                    _field = field;
                    _defaultValue = GetDefaultValue(field.FieldType);
                }

                internal override object GetValue(object target) => _field.GetValue(target);
                internal override void SetValue(object target, object value) =>
                    _field.SetValue(target, value);
                internal override void SetDefaultValue(object target) =>
                    _field.SetValue(target, _defaultValue);

                internal override bool Capture(BufferScan scan, Field field, object target,
                    BuffConverter converter, bool fullField,
                    out BufferScan.CachedField cached)
                {
                    var value = _field.GetValue(target);
                    if (!fullField && IsNullOrDefault(value, _field.FieldType))
                    {
                        cached = default;
                        return false;
                    }
                    cached = scan.CacheBoxedFieldValue(field, converter, value);
                    return true;
                }

                internal override void ReadAndSet(IBufferReader reader, object target,
                    BuffConverter converter) =>
                    _field.SetValue(target, converter.Read(reader, _field.FieldType));
            }

#if !ENABLE_IL2CPP
            private sealed class TypedFieldAccess<TTarget, TValue> : FieldAccess
            {
                private readonly FieldInfo _field;
                private readonly Func<TTarget, TValue> _getter;
                private readonly Action<TTarget, TValue> _setter;

                internal TypedFieldAccess(FieldInfo field)
                {
                    _field = field;
                    var target = Expression.Parameter(typeof(TTarget), "target");
                    _getter = Expression.Lambda<Func<TTarget, TValue>>(
                        Expression.Field(target, field), target).Compile();
                    if (!typeof(TTarget).IsValueType && !field.IsInitOnly)
                    {
                        var value = Expression.Parameter(typeof(TValue), "value");
                        _setter = Expression.Lambda<Action<TTarget, TValue>>(
                            Expression.Assign(Expression.Field(target, field), value),
                            target, value).Compile();
                    }
                }

                private TValue Read(object target) => _getter((TTarget)target);

                private void Write(object target, TValue value)
                {
                    if (_setter != null)
                        _setter((TTarget)target, value);
                    else
                        _field.SetValue(target, value);
                }

                internal override object GetValue(object target) => Read(target);
                internal override void SetValue(object target, object value) =>
                    Write(target, (TValue)value);
                internal override void SetDefaultValue(object target) => Write(target, default);

                internal override bool Capture(BufferScan scan, Field field, object target,
                    BuffConverter converter, bool fullField,
                    out BufferScan.CachedField cached)
                {
                    TValue value = Read(target);
                    if (!fullField && IsDefault(value))
                    {
                        cached = default;
                        return false;
                    }
                    cached = scan.CacheFieldValue(field, converter, value);
                    return true;
                }

                internal override void ReadAndSet(IBufferReader reader, object target,
                    BuffConverter converter)
                {
                    if (!(converter is BuffConverter<TValue> typed))
                        throw new InvalidOperationException(
                            $"Converter '{converter?.GetType()}' cannot deserialize field type '{typeof(TValue)}'.");
                    Write(target, typed.ReadValue(reader, typeof(TValue)));
                }

                private static bool IsDefault(TValue value)
                {
                    if (!typeof(TValue).IsValueType) return ReferenceEquals(value, null);
                    return EqualityComparer<TValue>.Default.Equals(value, default);
                }
            }
#endif

            public sealed class FieldCollection : IReadOnlyList<Field>
            {
                private readonly List<Field> _items;

                internal FieldCollection(List<Field> items)
                {
                    _items = items;
                }

                public int Count => _items.Count;
                public Field this[int index] => _items[index];
                public List<Field>.Enumerator GetEnumerator() => _items.GetEnumerator();
                System.Collections.Generic.IEnumerator<Field>
                    System.Collections.Generic.IEnumerable<Field>.GetEnumerator() =>
                    _items.GetEnumerator();
                System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
                    _items.GetEnumerator();
            }

            private static readonly object Sync = new object();
            private static readonly Dictionary<Type, TypeFields> Cache =
                new Dictionary<Type, TypeFields>();
            private readonly Type _type;
            private readonly List<Field> _fields = new List<Field>();
            private readonly Dictionary<string, Field> _fieldsByName =
                new Dictionary<string, Field>();
            private readonly FieldCollection _fieldView;
            private readonly bool _useUninitializedObject;

            private TypeFields(Type type, bool usePropertyBackingFields)
            {
                _type = type;
                _fieldView = new FieldCollection(_fields);
                _useUninitializedObject = usePropertyBackingFields ||
                    (!type.IsValueType && type.GetConstructor(
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                        null, Type.EmptyTypes, null) == null);
            }

            internal static TypeFields Get(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));
                lock (Sync)
                {
                    if (Cache.TryGetValue(type, out var cached)) return cached;
                    var fields = Create(type);
                    Cache.Add(type, fields);
                    return fields;
                }
            }

            public FieldCollection GetFields() => _fieldView;

            public Field FindField(string name)
            {
                _fieldsByName.TryGetValue(name, out var field);
                return field;
            }

            internal void SetMissingDefaultValues(object target, HashSet<Field> presentFields)
            {
                for (int i = 0; i < _fields.Count; i++)
                    if (!presentFields.Contains(_fields[i]))
                        _fields[i].SetDefaultValue(target);
            }

            internal object CreateInstance()
            {
                if (_useUninitializedObject)
                    return FormatterServices.GetUninitializedObject(_type);
                return Activator.CreateInstance(_type, true);
            }

            private static TypeFields Create(Type type)
            {
                bool usePropertyBackingFields = UsesPropertyBackingFields(type);
                var result = new TypeFields(type, usePropertyBackingFields);
                var baseType = type.BaseType;
                if (baseType != null && baseType != typeof(object))
                {
                    var baseFields = Get(baseType).GetFields();
                    for (int i = 0; i < baseFields.Count; i++)
                        result.AddField(baseFields[i]);
                }

                var declaredFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly);
                for (int i = 0; i < declaredFields.Length; i++)
                    result.AddField(declaredFields[i]);
                if (usePropertyBackingFields)
                    AddPropertyBackingFields(result, type, declaredFields);
                result._fields.Sort((left, right) =>
                    string.CompareOrdinal(left.name, right.name));
                return result;
            }

            private void AddField(Field field)
            {
                if (_fieldsByName.ContainsKey(field.name)) return;
                _fieldsByName.Add(field.name, field);
                _fields.Add(field);
            }

            private void AddField(FieldInfo field)
            {
                if (field.IsDefined(typeof(NonSerializedAttribute))) return;
                var attribute = field.GetCustomAttribute<BufferAttribute>();
                if (typeof(Delegate).IsAssignableFrom(field.FieldType) && attribute == null)
                    return;
                if (!field.IsPublic && attribute == null) return;
                AddField(field, attribute?.bufferName ?? field.Name);
            }

            private void AddField(FieldInfo field, string name)
            {
                if (field == null) return;
                if (typeof(Delegate).IsAssignableFrom(field.FieldType) &&
                    field.GetCustomAttribute<BufferAttribute>() == null) return;
                if (_fieldsByName.TryGetValue(name, out var existing))
                    throw new InvalidOperationException(
                        $"Type '{_type}' contains duplicate serialized field name '{name}' " +
                        $"in '{existing.DeclaringType}' and '{field.DeclaringType}'.");
                AddField(new Field(field, name));
            }

            private static void AddPropertyBackingFields(TypeFields typeFields, Type type,
                FieldInfo[] fields)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);
                for (int i = 0; i < properties.Length; i++)
                {
                    var property = properties[i];
                    if (!property.CanRead || property.GetIndexParameters().Length != 0 ||
                        typeof(Delegate).IsAssignableFrom(property.PropertyType) ||
                        typeFields._fieldsByName.ContainsKey(property.Name))
                        continue;
                    var backingField = FindPropertyBackingField(fields, property);
                    if (backingField != null)
                        typeFields.AddField(backingField, property.Name);
                }
            }

            private static FieldInfo FindPropertyBackingField(FieldInfo[] fields,
                PropertyInfo property)
            {
                string compilerName = $"<{property.Name}>k__BackingField";
                string anonymousName = $"<{property.Name}>i__Field";
                string tupleName = "m_" + property.Name;
                for (int i = 0; i < fields.Length; i++)
                {
                    var field = fields[i];
                    if (field.FieldType != property.PropertyType) continue;
                    if (field.Name == compilerName || field.Name == anonymousName ||
                        field.Name == tupleName)
                        return field;
                }
                return null;
            }

            private static bool UsesPropertyBackingFields(Type type)
            {
                if (!type.IsValueType && type.Namespace == "System" && type.IsGenericType &&
                    type.GetGenericTypeDefinition().FullName.StartsWith(
                        "System.Tuple`", StringComparison.Ordinal))
                    return true;
                if (type.IsDefined(typeof(CompilerGeneratedAttribute), false) &&
                    type.Name.IndexOf("AnonymousType", StringComparison.Ordinal) >= 0)
                    return true;
                if (type.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic) != null ||
                    type.GetProperty("EqualityContract", BindingFlags.Instance |
                        BindingFlags.NonPublic) != null)
                    return true;
                return type.GetMethod("PrintMembers",
                    BindingFlags.Instance | BindingFlags.NonPublic, null,
                    new[] { typeof(StringBuilder) }, null) != null;
            }
        }

        public static TypeFields GetTypeFields(Type type) => TypeFields.Get(type);

        internal static object CreateInstance(Type type) =>
            GetTypeFields(type).CreateInstance();

        public static IReadOnlyList<Type> GetSubTypes(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            lock (CacheSync)
            {
                if (SubTypes.TryGetValue(type, out var cached)) return cached;
                var source = GetConcreteTypes();
                var matches = new List<Type>();
                for (int i = 0; i < source.Count; i++)
                {
                    var candidate = source[i];
                    if (candidate == type || candidate.IsAbstract || candidate.IsInterface)
                        continue;
                    bool isMatch = type.IsGenericTypeDefinition
                        ? IsSubclassOfGeneric(candidate, type)
                        : type.IsAssignableFrom(candidate);
                    if (isMatch) matches.Add(candidate);
                }

                var result = matches.AsReadOnly();
                SubTypes.Add(type, result);
                return result;
            }
        }

        public static Type GetTypeByFullName(string typeFullName, string assemblyName = null)
        {
            if (string.IsNullOrEmpty(typeFullName)) return null;
            var cacheKey = new TypeCacheKey(typeFullName, assemblyName);
            lock (CacheSync)
            {
                if (TypesByName.TryGetValue(cacheKey, out var cached)) return cached;
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Type result = string.IsNullOrEmpty(assemblyName)
                    ? FindUniqueType(assemblies, typeFullName)
                    : FindType(assemblies, typeFullName, assemblyName);
                TypesByName.Add(cacheKey, result);
                return result;
            }
        }

        internal static object GetDefaultValue(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null) return null;
            lock (CacheSync)
            {
                if (DefaultValues.TryGetValue(type, out var cached)) return cached;
                var value = Activator.CreateInstance(type);
                DefaultValues.Add(type, value);
                return value;
            }
        }

        internal static bool IsNullOrDefault(object value, Type declaredType)
        {
            if (value == null) return true;
            if (declaredType == null) throw new ArgumentNullException(nameof(declaredType));
            if (!declaredType.IsValueType || Nullable.GetUnderlyingType(declaredType) != null)
                return false;
            return value.Equals(GetDefaultValue(declaredType));
        }

        private static IReadOnlyList<Type> GetConcreteTypes()
        {
            if (_types != null) return _types;
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
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
                    if (assemblyTypes[j] != null)
                        result.Add(assemblyTypes[j]);
            }
            _types = result.AsReadOnly();
            return _types;
        }

        private static bool IsSubclassOfGeneric(Type type, Type genericType)
        {
            if (!genericType.IsGenericTypeDefinition) return false;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == genericType) return true;
            var baseType = type.BaseType;
            if (baseType != null && baseType != typeof(object) &&
                IsSubclassOfGeneric(baseType, genericType))
                return true;
            var interfaces = type.GetInterfaces();
            for (int i = 0; i < interfaces.Length; i++)
                if (IsSubclassOfGeneric(interfaces[i], genericType))
                    return true;
            return false;
        }

        private static Type FindType(Assembly[] assemblies, string typeName, string assemblyName)
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
                if (!string.Equals(assemblies[i].GetName().Name, requestedAssembly,
                        StringComparison.Ordinal))
                    continue;
                var result = assemblies[i].GetType(typeName, false);
                if (result != null) return result;
            }
            return null;
        }

        private static Type FindUniqueType(Assembly[] assemblies, string typeName)
        {
            Type result = null;
            for (int i = 0; i < assemblies.Length; i++)
            {
                var candidate = assemblies[i].GetType(typeName, false);
                if (candidate == null) continue;
                if (result != null && result != candidate) return null;
                result = candidate;
            }
            return result;
        }

        private readonly struct TypeCacheKey : IEquatable<TypeCacheKey>
        {
            private readonly string _typeName;
            private readonly string _assemblyName;

            internal TypeCacheKey(string typeName, string assemblyName)
            {
                _typeName = typeName;
                _assemblyName = assemblyName ?? string.Empty;
            }

            public bool Equals(TypeCacheKey other) =>
                string.Equals(_typeName, other._typeName, StringComparison.Ordinal) &&
                string.Equals(_assemblyName, other._assemblyName, StringComparison.Ordinal);
            public override bool Equals(object obj) =>
                obj is TypeCacheKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_typeName?.GetHashCode() ?? 0) * 397) ^
                        _assemblyName.GetHashCode();
                }
            }
        }
    }
}
