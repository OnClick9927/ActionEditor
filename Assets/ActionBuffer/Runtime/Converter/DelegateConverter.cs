using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    }

    internal sealed class DelegateConverter<TDelegate> : BuffConverter<TDelegate>
        where TDelegate : Delegate
    {
        private const byte NoTarget = 0;
        private const byte CurrentObjectTarget = 1;
        private const byte ObjectReferenceTarget = 2;
        private static readonly int InvokeParameterCount =
            typeof(TDelegate).GetMethod("Invoke").GetParameters().Length;
        private readonly Dictionary<MethodInfo, MethodDescriptor> _methodDescriptors =
            new Dictionary<MethodInfo, MethodDescriptor>();
        private readonly Func<IBufferReader, DelegateMethodReference> _readReference;
        private readonly Action<IBufferWriter, BufferScan, DelegateMethodReference> _writeReference;
        private BuffConverter<DelegateMethodReference> _referenceConverter;
        private long _converterVersion = -1;

        private sealed class MethodDescriptor
        {
            internal DelegateMethodReference Reference;
            internal int ParameterCount;
        }

        public DelegateConverter()
        {
            _readReference = ReadReference;
            _writeReference = WriteReference;
        }

        private BuffConverter<DelegateMethodReference> GetReferenceConverter(
            BufferSerializerSettings settings)
        {
            long version = BufferSerializer.GetResolverVersion(settings);
            if (_converterVersion == version) return _referenceConverter;
            _referenceConverter = BufferSerializer.GetConverter<DelegateMethodReference>(settings);
            _converterVersion = version;
            return _referenceConverter;
        }

        protected override void OnScan(BufferScan scan, TDelegate value)
        {
            if (value == null)
            {
                scan.ScanEnumerable<DelegateMethodReference>(null,
                    GetReferenceConverter(scan.Settings));
                return;
            }

            var invocationList = value.GetInvocationList();
            var references = ListPool<DelegateMethodReference>.Get(invocationList.Length);
            try
            {
                for (int i = 0; i < invocationList.Length; i++)
                    references.Add(CreateReference(scan, invocationList[i]));
                scan.ScanEnumerable(references, GetReferenceConverter(scan.Settings));
            }
            finally
            {
                ListPool<DelegateMethodReference>.Back(references);
            }
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan, TDelegate value) =>
            writer.WriteIEnumerable<DelegateMethodReference>(scan, null, _writeReference);

        protected override TDelegate OnRead(IBufferReader reader, Type type)
        {
            var references = ListPool<DelegateMethodReference>.Get();
            try
            {
                var values = reader.ReadIEnumerable(references, _readReference);
                if (values == null) return null;

                Delegate result = null;
                for (int i = 0; i < values.Count; i++)
                    result = Delegate.Combine(result, CreateDelegate(reader, values[i]));
                return (TDelegate)(object)result;
            }
            finally
            {
                ListPool<DelegateMethodReference>.Back(references);
            }
        }

        private DelegateMethodReference CreateReference(BufferScan scan, Delegate value)
        {
            var method = value.Method;
            var declaringType = method.DeclaringType;
            if (declaringType == null || method.IsGenericMethod || declaringType.ContainsGenericParameters)
                throw new NotSupportedException(
                    $"Delegate method '{method}' is generic, dynamic, or has no concrete declaring type.");
            var descriptor = GetMethodDescriptor(method);

            byte binding;
            if (value.Target == null)
            {
                if (method.IsStatic && descriptor.ParameterCount != InvokeParameterCount)
                    throw new NotSupportedException(
                        $"Delegate '{typeof(TDelegate)}' closes static method '{method}' over a null target, " +
                        "which cannot be reconstructed safely.");
                binding = NoTarget;
            }
            else if (ReferenceEquals(value.Target, scan.CurrentObject))
            {
                binding = CurrentObjectTarget;
            }
            else
            {
                var targetType = value.Target.GetType();
                if (targetType.IsDefined(typeof(CompilerGeneratedAttribute), false))
                    throw new NotSupportedException(
                        $"Delegate '{typeof(TDelegate)}' uses compiler-generated closure target '{targetType}'. " +
                        "Closure delegates are not supported.");
                if (!BufferSerializer.GetConverter(targetType, scan.Settings).UsesObjectLayout)
                    throw new NotSupportedException(
                        $"Delegate target type '{targetType}' must use object-field serialization.");
                binding = ObjectReferenceTarget;
            }

            var reference = descriptor.Reference;
            reference.Binding = binding;
            reference.TargetReferenceId = -1;
            if (binding == ObjectReferenceTarget)
            {
                var targetType = value.Target.GetType();
                reference.TargetReferenceId = scan.RequireObjectReference(value.Target);
                reference.TargetType = targetType.FullName;
                reference.TargetAssembly = targetType.Assembly.FullName;
            }
            return reference;
        }

        private Delegate CreateDelegate(IBufferReader reader, DelegateMethodReference reference)
        {
            if (reference.Binding != NoTarget && reference.Binding != CurrentObjectTarget &&
                reference.Binding != ObjectReferenceTarget)
                throw new FormatException($"Unknown delegate binding kind '{reference.Binding}'.");

            var declaringType = TypeHelper.GetTypeByFullName(reference.DeclaringType, reference.AssemblyName);
            if (declaringType == null)
                throw new FormatException(
                    $"Cannot resolve delegate declaring type '{reference.DeclaringType}, {reference.AssemblyName}'.");

            MethodInfo match = null;
            var methods = declaringType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                                                   BindingFlags.Static | BindingFlags.Instance |
                                                   BindingFlags.DeclaredOnly);
            for (int i = 0; i < methods.Length; i++)
            {
                var candidate = methods[i];
                if (candidate.Name != reference.MethodName || candidate.IsGenericMethod ||
                    GetMethodDescriptor(candidate).Reference.Signature != reference.Signature) continue;
                if (match != null)
                    throw new FormatException(
                        $"Delegate method '{reference.DeclaringType}.{reference.MethodName}' is ambiguous.");
                match = candidate;
            }
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

            var result = target == null
                ? Delegate.CreateDelegate(typeof(TDelegate), match, false)
                : Delegate.CreateDelegate(typeof(TDelegate), target, match, false);
            if (result == null)
                throw new FormatException(
                    $"Method '{match}' is not compatible with delegate type '{typeof(TDelegate)}'.");
            return result;
        }

        private DelegateMethodReference ReadReference(IBufferReader reader) =>
            GetReferenceConverter(BufferSerializerSettings.DefaultSetting)
                .ReadValue(reader, typeof(DelegateMethodReference));

        private void WriteReference(IBufferWriter writer, BufferScan scan,
            DelegateMethodReference reference) =>
            GetReferenceConverter(scan.Settings).WriteValue(writer, scan, reference);

        private MethodDescriptor GetMethodDescriptor(MethodInfo method)
        {
            if (_methodDescriptors.TryGetValue(method, out var descriptor)) return descriptor;
            var declaringType = method.DeclaringType;
            descriptor = new MethodDescriptor
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
            _methodDescriptors.Add(method, descriptor);
            return descriptor;
        }

        private static string GetMethodSignature(MethodInfo method)
        {
            var builder = ClassPool<StringBuilder>.Get();
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
                ClassPool<StringBuilder>.Back(builder);
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
