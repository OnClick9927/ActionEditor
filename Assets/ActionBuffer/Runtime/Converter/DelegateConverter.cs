using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace ActionBuffer
{
    internal struct DelegateMethodReference
    {
        public string DeclaringType;
        public string AssemblyName;
        public string MethodName;
        public string Signature;
        public byte Binding;
        public int TargetReferenceId;
        public string TargetType;
        public string TargetAssembly;
        public string[] GenericArgumentTypes;
        public string[] GenericArgumentAssemblies;
        public object Target;
    }

    internal sealed class DelegateConverter<TDelegate> : BuffConverter<TDelegate>
        where TDelegate : Delegate
    {
        private const byte NoTarget = 0;
        private const byte CurrentObjectTarget = 1;
        private const byte ObjectReferenceTarget = 2;
        private const byte EmbeddedTarget = 3;
        private const byte ClosedStaticNullTarget = 4;
        private static readonly int InvokeParameterCount =
            typeof(TDelegate).GetMethod("Invoke").GetParameters().Length;
        private readonly object _methodSync = new object();
        private readonly Dictionary<MethodInfo, MethodDescriptor> _methodDescriptors =
            new Dictionary<MethodInfo, MethodDescriptor>();
        private readonly Dictionary<MethodCacheKey, MethodInfo> _resolvedMethods =
            new Dictionary<MethodCacheKey, MethodInfo>();

        private sealed class MethodDescriptor
        {
            internal DelegateMethodReference Reference;
            internal int ParameterCount;
        }

        private readonly struct MethodCacheKey : IEquatable<MethodCacheKey>
        {
            private readonly Type _declaringType;
            private readonly string _name;
            private readonly string _signature;
            private readonly string[] _genericTypes;
            private readonly string[] _genericAssemblies;

            internal MethodCacheKey(Type declaringType, DelegateMethodReference reference)
            {
                _declaringType = declaringType;
                _name = reference.MethodName;
                _signature = reference.Signature;
                _genericTypes = reference.GenericArgumentTypes;
                _genericAssemblies = reference.GenericArgumentAssemblies;
            }

            public bool Equals(MethodCacheKey other) =>
                _declaringType == other._declaringType &&
                string.Equals(_name, other._name, StringComparison.Ordinal) &&
                string.Equals(_signature, other._signature, StringComparison.Ordinal) &&
                Equal(_genericTypes, other._genericTypes) &&
                Equal(_genericAssemblies, other._genericAssemblies);
            public override bool Equals(object obj) => obj is MethodCacheKey other && Equals(other);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = _declaringType.GetHashCode();
                    hash = hash * 397 ^ (_name?.GetHashCode() ?? 0);
                    hash = hash * 397 ^ (_signature?.GetHashCode() ?? 0);
                    hash = AddHash(hash, _genericTypes);
                    return AddHash(hash, _genericAssemblies);
                }
            }

            private static bool Equal(string[] left, string[] right)
            {
                if (ReferenceEquals(left, right)) return true;
                if (left == null || right == null || left.Length != right.Length) return false;
                for (int i = 0; i < left.Length; i++)
                    if (!string.Equals(left[i], right[i], StringComparison.Ordinal)) return false;
                return true;
            }

            private static int AddHash(int hash, string[] values)
            {
                if (values == null) return hash * 397;
                for (int i = 0; i < values.Length; i++)
                    hash = hash * 397 ^ (values[i]?.GetHashCode() ?? 0);
                return hash;
            }
        }

        protected override void OnScan(BufferScan scan, TDelegate value)
        {
            if (value == null)
            {
                scan.ScanEnumerable<DelegateMethodReference>(null,
                    ConverterCache<DelegateMethodReference>.Get(scan), trackReference: false);
                return;
            }

            var invocationList = value.GetInvocationList();
            var references = ClassPool.GetList<DelegateMethodReference>(invocationList.Length);
            try
            {
                for (int i = 0; i < invocationList.Length; i++)
                    references.Add(CreateReference(scan, invocationList[i]));
                scan.ScanEnumerable(references,
                    ConverterCache<DelegateMethodReference>.Get(scan), trackReference: false);
            }
            finally
            {
                ClassPool.BackList(references);
            }
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TDelegate value) =>
            writer.WriteIEnumerable(scan, ConverterCache<DelegateMethodReference>.Get(scan));

        protected override TDelegate OnRead(IBufferReader reader, Type type)
        {
            var references = ClassPool.GetList<DelegateMethodReference>();
            try
            {
                var values = reader.ReadIEnumerable(references,
                    ConverterCache<DelegateMethodReference>.Get(reader));
                if (values == null) return null;

                var delegates = new Delegate[values.Count];
                for (int i = 0; i < values.Count; i++)
                    delegates[i] = CreateDelegate(reader, values[i]);
                return (TDelegate)(object)Delegate.Combine(delegates);
            }
            finally
            {
                ClassPool.BackList(references);
            }
        }

        private DelegateMethodReference CreateReference(BufferScan scan, Delegate value)
        {
            var method = value.Method;
            var declaringType = method.DeclaringType;
            if (declaringType == null)
                throw new NotSupportedException(
                    $"Dynamic delegate method '{method}' has no stable declaring type and " +
                    "cannot be reconstructed after an application restart.");
            if (method.ContainsGenericParameters || declaringType.ContainsGenericParameters)
                throw new NotSupportedException(
                    $"Delegate method '{method}' contains unbound generic parameters.");
            var descriptor = GetMethodDescriptor(method);

            byte binding;
            if (value.Target == null)
            {
                if (method.IsStatic && descriptor.ParameterCount != InvokeParameterCount)
                    binding = ClosedStaticNullTarget;
                else
                    binding = NoTarget;
            }
            else if (ReferenceEquals(value.Target, scan.CurrentObject))
            {
                binding = CurrentObjectTarget;
            }
            else
            {
                binding = EmbeddedTarget;
            }

            var reference = descriptor.Reference;
            reference.Binding = binding;
            reference.TargetReferenceId = -1;
            reference.TargetType = null;
            reference.TargetAssembly = null;
            reference.Target = null;
            if (binding == EmbeddedTarget)
            {
                reference.Target = value.Target;
            }
            return reference;
        }

        private static bool IsDeclaredMethod(MethodInfo method, Type declaringType)
        {
            var expected = method.IsGenericMethod
                ? method.GetGenericMethodDefinition()
                : method;
            var methods = declaringType.GetMethods(BindingFlags.Public |
                BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance |
                BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
                if (methods[i].Equals(expected)) return true;
            return false;
        }

        private Delegate CreateDelegate(IBufferReader reader, DelegateMethodReference reference)
        {
            if (reference.Binding != NoTarget && reference.Binding != CurrentObjectTarget &&
                reference.Binding != ObjectReferenceTarget &&
                reference.Binding != EmbeddedTarget &&
                reference.Binding != ClosedStaticNullTarget)
                throw new FormatException($"Unknown delegate binding kind '{reference.Binding}'.");

            var declaringType = TypeHelper.GetTypeByFullName(reference.DeclaringType, reference.AssemblyName);
            if (declaringType == null)
                throw new FormatException(
                    $"Cannot resolve delegate declaring type '{reference.DeclaringType}, {reference.AssemblyName}'.");

            var match = ResolveMethod(declaringType, reference);
            if (match == null)
                throw new FormatException(
                    $"Cannot resolve delegate method '{reference.DeclaringType}.{reference.MethodName}'.");

            object target = null;
            if (reference.Binding == CurrentObjectTarget)
            {
                target = (reader as IObjectContextReader)?.CurrentObject;
                if (target == null)
                    throw new FormatException(
                        $"Delegate method '{match}' requires a containing object context.");
            }
            else if (reference.Binding == ObjectReferenceTarget)
            {
                var targetType = TypeHelper.GetTypeByFullName(
                    reference.TargetType, reference.TargetAssembly);
                if (targetType == null)
                    throw new FormatException(
                        $"Cannot resolve delegate target type '{reference.TargetType}, " +
                        $"{reference.TargetAssembly}'.");
                var context = reader as IObjectContextReader;
                if (context == null)
                    throw new FormatException("The reader does not support object references.");
                target = context.GetOrCreateReference(reference.TargetReferenceId, targetType);
            }
            else if (reference.Binding == EmbeddedTarget)
            {
                target = reference.Target;
                if (target == null)
                    throw new FormatException(
                        $"Delegate method '{match}' requires an embedded target.");
            }

            Delegate result;
            if (reference.Binding == ClosedStaticNullTarget)
                result = Delegate.CreateDelegate(typeof(TDelegate), null, match, false);
            else if (reference.Binding == NoTarget)
                result = Delegate.CreateDelegate(typeof(TDelegate), match, false);
            else
                result = Delegate.CreateDelegate(typeof(TDelegate), target, match, false);
            if (result == null)
                throw new FormatException(
                    $"Method '{match}' is not compatible with delegate type '{typeof(TDelegate)}'.");
            return result;
        }

        private MethodInfo ResolveMethod(Type declaringType, DelegateMethodReference reference)
        {
            var key = new MethodCacheKey(declaringType, reference);
            lock (_methodSync)
            {
                if (_resolvedMethods.TryGetValue(key, out var cached)) return cached;
                MethodInfo match = null;
                var methods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                       BindingFlags.Static | BindingFlags.Instance |
                                                       BindingFlags.DeclaredOnly);
                for (int i = 0; i < methods.Length; i++)
                {
                    var candidate = methods[i];
                    if (candidate.Name != reference.MethodName)
                        continue;
                    candidate = CloseGenericMethod(candidate, reference);
                    if (candidate == null ||
                        GetMethodDescriptor(candidate).Reference.Signature != reference.Signature)
                        continue;
                    if (match != null)
                        throw new FormatException(
                            $"Delegate method '{reference.DeclaringType}.{reference.MethodName}' is ambiguous.");
                    match = candidate;
                }
                if (match != null)
                    _resolvedMethods.Add(key, match);
                return match;
            }
        }

        private static MethodInfo CloseGenericMethod(MethodInfo candidate,
            DelegateMethodReference reference)
        {
            var typeNames = reference.GenericArgumentTypes;
            var assemblyNames = reference.GenericArgumentAssemblies;
            if (typeNames == null || typeNames.Length == 0)
                return candidate.IsGenericMethod ? null : candidate;
            if (!candidate.IsGenericMethodDefinition || assemblyNames == null ||
                assemblyNames.Length != typeNames.Length ||
                candidate.GetGenericArguments().Length != typeNames.Length)
                return null;

            var arguments = new Type[typeNames.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                arguments[i] = TypeHelper.GetTypeByFullName(typeNames[i], assemblyNames[i]);
                if (arguments[i] == null) return null;
            }
            try
            {
                return candidate.MakeGenericMethod(arguments);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private MethodDescriptor GetMethodDescriptor(MethodInfo method)
        {
            lock (_methodSync)
            {
                if (_methodDescriptors.TryGetValue(method, out var cached)) return cached;
                var declaringType = method.DeclaringType;
                if (!IsDeclaredMethod(method, declaringType))
                    throw new NotSupportedException(
                        $"Dynamic delegate method '{method}' is not discoverable from its " +
                        "declaring type and cannot be reconstructed after an application restart.");
                var descriptor = new MethodDescriptor
                {
                    ParameterCount = method.GetParameters().Length,
                    Reference = new DelegateMethodReference
                    {
                        DeclaringType = declaringType.FullName,
                        AssemblyName = declaringType.Assembly.FullName,
                        MethodName = method.Name,
                        Signature = GetMethodSignature(method)
                    }
                };
                if (method.IsGenericMethod)
                {
                    var arguments = method.GetGenericArguments();
                    descriptor.Reference.GenericArgumentTypes = new string[arguments.Length];
                    descriptor.Reference.GenericArgumentAssemblies = new string[arguments.Length];
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        descriptor.Reference.GenericArgumentTypes[i] = arguments[i].FullName;
                        descriptor.Reference.GenericArgumentAssemblies[i] =
                            arguments[i].Assembly.FullName;
                    }
                }
                _methodDescriptors.Add(method, descriptor);
                return descriptor;
            }
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            var builder = ClassPool.Get<StringBuilder>();
            builder.Clear();
            try
            {
                AppendTypeSignature(builder, method.ReturnType);
                var parameters = method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    builder.Append('\n');
                    AppendTypeSignature(builder, parameters[i].ParameterType);
                }
                return builder.ToString();
            }
            finally
            {
                builder.Clear();
                ClassPool.Back(builder);
            }
        }

        private static void AppendTypeSignature(StringBuilder builder, Type type)
        {
            if (type.IsByRef)
            {
                builder.Append("ref(");
                AppendTypeSignature(builder, type.GetElementType());
                builder.Append(')');
                return;
            }
            if (type.IsPointer)
            {
                builder.Append("ptr(");
                AppendTypeSignature(builder, type.GetElementType());
                builder.Append(')');
                return;
            }
            if (type.IsArray)
            {
                builder.Append("array");
                builder.Append(type.GetArrayRank());
                builder.Append('(');
                AppendTypeSignature(builder, type.GetElementType());
                builder.Append(')');
                return;
            }
            if (type.IsGenericType)
            {
                AppendNamedType(builder, type.GetGenericTypeDefinition());
                builder.Append('[');
                var arguments = type.GetGenericArguments();
                for (int i = 0; i < arguments.Length; i++)
                {
                    if (i != 0) builder.Append(';');
                    AppendTypeSignature(builder, arguments[i]);
                }
                builder.Append(']');
                return;
            }
            AppendNamedType(builder, type);
        }

        private static void AppendNamedType(StringBuilder builder, Type type)
        {
            builder.Append(type.FullName);
            builder.Append(',');
            builder.Append(type.Assembly.GetName().Name);
        }

    }
}
