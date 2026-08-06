using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Events;

namespace ActionBuffer.Unity
{
    internal readonly struct UnityEventListenerRecord
    {
        internal readonly string TargetId;
        internal readonly string MethodName;
        internal readonly int State;

        internal UnityEventListenerRecord(string targetId, string methodName,
            int state)
        {
            TargetId = targetId;
            MethodName = methodName;
            State = state;
        }
    }

    internal sealed class UnityEventListenerStrings : ICollection<string>
    {
        internal UnityEventListenerRecord Value;
        public int Count => 3;
        public bool IsReadOnly => true;

        public void CopyTo(string[] array, int arrayIndex)
        {
            array[arrayIndex] = Value.TargetId;
            array[arrayIndex + 1] = Value.MethodName;
            array[arrayIndex + 2] = StateText(Value.State);
        }

        public IEnumerator<string> GetEnumerator()
        {
            yield return Value.TargetId;
            yield return Value.MethodName;
            yield return StateText(Value.State);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(string item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(string item) => false;
        public bool Remove(string item) => throw new NotSupportedException();

        private static string StateText(int state)
        {
            switch (state)
            {
                case 0: return "0";
                case 1: return "1";
                case 2: return "2";
                default: throw new FormatException(
                    $"UnityEvent listener state '{state}' is invalid.");
            }
        }
    }

    internal sealed class UnityEventListenerBuffConverter :
        BuffConverter<UnityEventListenerRecord>
    {
        private readonly UnityEventListenerStrings strings =
            new UnityEventListenerStrings();
        private readonly List<string> readStrings = new List<string>(3);

        protected override void OnScan(BufferScan scan,
            UnityEventListenerRecord value)
        {
            strings.Value = value;
            scan.ScanEnumerable(strings, UnityStringBuffConverter.Instance,
                trackReference: false);
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            UnityEventListenerRecord value) => writer.WriteIEnumerable(scan,
                UnityStringBuffConverter.Instance);

        protected override UnityEventListenerRecord OnRead(IBufferReader reader,
            Type type)
        {
            readStrings.Clear();
            List<string> values = reader.ReadIEnumerable(readStrings,
                UnityStringBuffConverter.Instance);
            if (values == null || values.Count != 3)
                throw new FormatException(
                    "UnityEvent listener requires exactly three values.");
            int state;
            switch (values[2])
            {
                case "0": state = 0; break;
                case "1": state = 1; break;
                case "2": state = 2; break;
                default: throw new FormatException(
                    $"UnityEvent listener state '{values[2]}' is invalid.");
            }
            var result = new UnityEventListenerRecord(values[0], values[1], state);
            readStrings.Clear();
            return result;
        }
    }

    internal sealed class UnityEventListenerCollection :
        ICollection<UnityEventListenerRecord>
    {
        internal UnityEventBase Source;
        internal Func<int, UnityEventListenerRecord> Capture;
        public int Count => Source == null ? 0 : Source.GetPersistentEventCount();
        public bool IsReadOnly => true;

        public void CopyTo(UnityEventListenerRecord[] array, int arrayIndex)
        {
            int count = Count;
            for (int i = 0; i < count; i++)
                array[arrayIndex + i] = Capture(i);
        }

        public IEnumerator<UnityEventListenerRecord> GetEnumerator()
        {
            int count = Count;
            for (int i = 0; i < count; i++) yield return Capture(i);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(UnityEventListenerRecord item) =>
            throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(UnityEventListenerRecord item) => false;
        public bool Remove(UnityEventListenerRecord item) =>
            throw new NotSupportedException();
    }

    internal sealed class UnityEventBuffConverter<TEvent> : BuffConverter<TEvent>
        where TEvent : UnityEventBase
    {
        private readonly IUnityObjectResolver resolver;
        private readonly Type eventType;
        private readonly Type actionType;
        private readonly MethodInfo addListener;
        private readonly object[] invokeArguments = new object[1];
        private readonly UnityEventListenerBuffConverter listenerConverter =
            new UnityEventListenerBuffConverter();
        private readonly UnityEventListenerCollection listeners =
            new UnityEventListenerCollection();
        private readonly List<UnityEventListenerRecord> readListeners =
            new List<UnityEventListenerRecord>();

        public UnityEventBuffConverter(IUnityObjectResolver resolver)
        {
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            eventType = typeof(TEvent);
            actionType = GetUnityActionType(eventType);
            addListener = eventType.GetMethod("AddListener",
                BindingFlags.Instance | BindingFlags.Public, null,
                new[] { actionType }, null);
            listeners.Capture = CaptureListener;
        }

        protected override void OnScan(BufferScan scan, TEvent value)
        {
            if (value == null)
            {
                scan.ScanEnumerable<UnityEventListenerRecord>(null,
                    listenerConverter, trackReference: false);
                return;
            }
            int count = value.GetPersistentEventCount();
            if (count > BuffSettings.MaxCollectionCount)
                throw new FormatException(
                    $"UnityEvent listener count cannot exceed " +
                    $"{BuffSettings.MaxCollectionCount}.");
            listeners.Source = value;
            try
            {
                scan.ScanEnumerable(listeners, listenerConverter,
                    trackReference: false);
            }
            finally
            {
                listeners.Source = null;
            }
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            TEvent value) => writer.WriteIEnumerable(scan, listenerConverter);

        protected override TEvent OnRead(IBufferReader reader, Type type)
        {
            readListeners.Clear();
            List<UnityEventListenerRecord> values = reader.ReadIEnumerable(
                readListeners, listenerConverter);
            if (values == null) return null;
            if (type == null || type.IsAbstract ||
                !typeof(TEvent).IsAssignableFrom(type))
                throw new InvalidOperationException(
                    $"Cannot create UnityEvent type '{type}'.");

            TEvent result;
            try
            {
                result = (TEvent)Activator.CreateInstance(type, true);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"UnityEvent type '{type}' must have a parameterless constructor.",
                    exception);
            }
            Restore(result, type, values);
            readListeners.Clear();
            return result;
        }

        private UnityEventListenerRecord CaptureListener(int index)
        {
            UnityEventBase value = listeners.Source;
            var target = value.GetPersistentTarget(index);
            string methodName = value.GetPersistentMethodName(index);
            UnityEventCallState state = value.GetPersistentListenerState(index);
            string targetId = target == null ? null : resolver.GetReferenceId(target);
            if (target != null && string.IsNullOrEmpty(targetId))
                throw new InvalidOperationException(
                    $"Resolver returned an empty id for UnityEvent target '{target}'.");
            ValidateText(targetId, BuffSettings.MaxScalarLength, "target id");
            ValidateText(methodName, BuffSettings.MaxScalarLength, "method name");
            if (state != UnityEventCallState.Off && target != null &&
                !string.IsNullOrEmpty(methodName))
                CreateAction(actionType, target, methodName, eventType);
            return new UnityEventListenerRecord(targetId, methodName, (int)state);
        }

        private void Restore(TEvent value, Type type,
            List<UnityEventListenerRecord> values)
        {
            if (values.Count > BuffSettings.MaxCollectionCount)
                throw new FormatException(
                    $"UnityEvent listener count cannot exceed " +
                    $"{BuffSettings.MaxCollectionCount}.");
            if (addListener == null)
                throw new NotSupportedException(
                    $"UnityEvent type '{type}' has no supported AddListener method.");

            for (int i = 0; i < values.Count; i++)
            {
                UnityEventListenerRecord listener = values[i];
                if (listener.State < 0 || listener.State > 2)
                    throw new FormatException(
                        $"UnityEvent listener state '{listener.State}' is invalid.");
                if ((UnityEventCallState)listener.State == UnityEventCallState.Off ||
                    string.IsNullOrEmpty(listener.TargetId) ||
                    string.IsNullOrEmpty(listener.MethodName)) continue;

                var target = resolver.ResolveReference(listener.TargetId,
                    typeof(UnityEngine.Object));
                if (target == null)
                    throw new InvalidOperationException(
                        $"Resolver returned null for UnityEvent target " +
                        $"'{listener.TargetId}'.");
                Delegate action = CreateAction(actionType, target,
                    listener.MethodName, type);
                invokeArguments[0] = action;
                try
                {
                    addListener.Invoke(value, invokeArguments);
                }
                finally
                {
                    invokeArguments[0] = null;
                }
            }
        }

        private static Delegate CreateAction(Type actionType,
            UnityEngine.Object target, string methodName, Type type)
        {
            Delegate action = Delegate.CreateDelegate(actionType, target,
                methodName, false, false);
            if (action != null) return action;
            throw new NotSupportedException(
                $"Persistent listener '{target?.GetType()}.{methodName}' cannot be " +
                $"rebound as '{actionType}' for UnityEvent '{type}'. " +
                "Only dynamic listeners whose parameters match the UnityEvent are supported.");
        }

        private static Type GetUnityActionType(Type type)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                if (current == typeof(UnityEvent)) return typeof(UnityAction);
                if (!current.IsGenericType) continue;
                Type definition = current.GetGenericTypeDefinition();
                Type[] arguments = current.GetGenericArguments();
                if (definition == typeof(UnityEvent<>))
                    return typeof(UnityAction<>).MakeGenericType(arguments);
                if (definition == typeof(UnityEvent<,>))
                    return typeof(UnityAction<,>).MakeGenericType(arguments);
                if (definition == typeof(UnityEvent<,,>))
                    return typeof(UnityAction<,,>).MakeGenericType(arguments);
                if (definition == typeof(UnityEvent<,,,>))
                    return typeof(UnityAction<,,,>).MakeGenericType(arguments);
            }
            throw new NotSupportedException(
                $"UnityEvent type '{type}' is not based on a supported UnityEvent class.");
        }

        private static void ValidateText(string value, int limit, string name)
        {
            if (value != null && value.Length > limit)
                throw new FormatException(
                    $"UnityEvent listener {name} cannot exceed {limit} characters.");
        }
    }
}
