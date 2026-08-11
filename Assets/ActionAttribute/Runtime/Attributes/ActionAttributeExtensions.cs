using System;
using System.Collections.Generic;
using System.Reflection;

namespace ActionAttribute
{
    /// <summary>传递给外部特性逻辑的轻量上下文，不依赖 UnityEditor。</summary>
    public sealed class ActionAttributeContext
    {
        private const BindingFlags MemberFlags = BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly object MemberCacheLock = new object();
        private static readonly Dictionary<Type,
            Dictionary<string, MemberInfo>> MemberCache =
                new Dictionary<Type, Dictionary<string, MemberInfo>>();

        public object Target { get; }
        public object Owner { get; }
        public object Value { get; }
        public Type ValueType { get; }
        public string PropertyPath { get; }
        public string MemberName { get; }
        public bool IsPlaying { get; }

        public ActionAttributeContext(object target, object owner, object value,
            Type valueType, string propertyPath, string memberName,
            bool isPlaying)
        {
            Target = target;
            Owner = owner;
            Value = value;
            ValueType = valueType ?? value?.GetType() ?? typeof(object);
            PropertyPath = propertyPath ?? string.Empty;
            MemberName = memberName ?? string.Empty;
            IsPlaying = isPlaying;
        }

        /// <summary>读取当前值，并在可安全转换时转换为指定类型。</summary>
        public bool TryGetValue<T>(out T value)
        {
            return TryConvert(Value, out value);
        }

        /// <summary>从 Owner（没有 Owner 时从 Target）读取字段、属性或无参方法，并转换为指定类型。</summary>
        public bool TryGetMemberValue<T>(string memberName, out T value)
        {
            value = default;
            object source = Owner ?? Target;
            if (source == null || string.IsNullOrEmpty(memberName)) return false;
            MemberInfo member = FindMember(source.GetType(), memberName);
            if (member == null) return false;
            try
            {
                object memberValue;
                switch (member)
                {
                    case FieldInfo field:
                        memberValue = field.GetValue(field.IsStatic
                            ? null
                            : source);
                        break;
                    case PropertyInfo property:
                        MethodInfo getter = property.GetGetMethod(true);
                        memberValue = property.GetValue(getter.IsStatic
                            ? null
                            : source, null);
                        break;
                    case MethodInfo method:
                        memberValue = method.Invoke(method.IsStatic
                            ? null
                            : source, null);
                        break;
                    default:
                        return false;
                }
                return TryConvert(memberValue, out value);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvert<T>(object source, out T value)
        {
            if (source is T exact)
            {
                value = exact;
                return true;
            }
            Type requestedType = typeof(T);
            Type targetType = Nullable.GetUnderlyingType(requestedType) ??
                requestedType;
            if (source == null)
            {
                value = default;
                return !targetType.IsValueType ||
                    Nullable.GetUnderlyingType(requestedType) != null;
            }

            try
            {
                object converted;
                if (targetType.IsEnum)
                {
                    converted = source is string text
                        ? Enum.Parse(targetType, text, true)
                        : Enum.ToObject(targetType, source);
                }
                else converted = Convert.ChangeType(source, targetType);
                value = (T)converted;
                return true;
            }
            catch
            {
                value = default;
                return false;
            }
        }

        private static MemberInfo FindMember(Type type, string memberName)
        {
            lock (MemberCacheLock)
            {
                if (!MemberCache.TryGetValue(type,
                        out Dictionary<string, MemberInfo> members))
                {
                    members = new Dictionary<string, MemberInfo>(
                        StringComparer.Ordinal);
                    MemberCache.Add(type, members);
                }
                if (members.TryGetValue(memberName, out MemberInfo cached))
                    return cached;

                MemberInfo result = null;
                for (Type current = type; current != null && result == null;
                    current = current.BaseType)
                {
                    result = current.GetField(memberName, MemberFlags |
                        BindingFlags.DeclaredOnly);
                    if (result == null)
                    {
                        PropertyInfo property = current.GetProperty(memberName,
                            MemberFlags | BindingFlags.DeclaredOnly);
                        if (property?.GetIndexParameters().Length == 0 &&
                            property.GetGetMethod(true) != null)
                            result = property;
                    }
                    if (result != null) continue;
                    MethodInfo[] methods = current.GetMethods(MemberFlags |
                        BindingFlags.DeclaredOnly);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo method = methods[i];
                        if (method.Name != memberName || method.IsGenericMethod ||
                            method.GetParameters().Length != 0) continue;
                        result = method;
                        break;
                    }
                }
                members.Add(memberName, result);
                return result;
            }
        }
    }

    /// <summary>通过少量业务逻辑控制字段显示或编辑状态。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true,
        Inherited = true)]
    public abstract class ActionConditionAttribute : ActionAttributeBase
    {
        public readonly ConditionMode mode;

        protected ActionConditionAttribute(ConditionMode mode)
        {
            this.mode = Enum.IsDefined(typeof(ConditionMode), mode)
                ? mode
                : ConditionMode.Show;
        }

        public abstract bool Evaluate(ActionAttributeContext context);
    }

    /// <summary>通过业务逻辑校验字段值，并由组合 Drawer 展示结果。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true,
        Inherited = true)]
    public abstract class ActionValidationAttribute : ActionAttributeBase
    {
        public readonly string message;
        public readonly InspectorMessageType type;

        protected ActionValidationAttribute(string message = null,
            InspectorMessageType type = InspectorMessageType.Error)
        {
            this.message = message;
            this.type = type;
        }

        public abstract bool IsValid(ActionAttributeContext context);

        public virtual string GetMessage(ActionAttributeContext context)
        {
            return string.IsNullOrEmpty(message)
                ? $"{context.MemberName} 的值无效。"
                : message;
        }
    }

    /// <summary>在字段值提交后通过业务逻辑修正其值。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true,
        Inherited = true)]
    public abstract class ActionValueModifierAttribute : ActionAttributeBase
    {
        public abstract object Modify(ActionAttributeContext context);
    }

    /// <summary>Runtime 扩展返回的 Inspector 提示数据。</summary>
    public struct ActionMessage
    {
        public readonly string text;
        public readonly InspectorMessageType type;

        public ActionMessage(string text,
            InspectorMessageType type = InspectorMessageType.Info)
        {
            this.text = text;
            this.type = type;
        }
    }

    /// <summary>根据当前字段上下文生成动态提示。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true,
        Inherited = true)]
    public abstract class ActionMessageAttribute : ActionAttributeBase
    {
        public abstract ActionMessage GetMessage(
            ActionAttributeContext context);
    }

    /// <summary>根据当前字段上下文生成动态标签和 tooltip。</summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true,
        Inherited = true)]
    public abstract class ActionLabelAttribute : ActionAttributeBase
    {
        public abstract string GetLabel(ActionAttributeContext context);

        public virtual string GetTooltip(ActionAttributeContext context) =>
            null;
    }
}
