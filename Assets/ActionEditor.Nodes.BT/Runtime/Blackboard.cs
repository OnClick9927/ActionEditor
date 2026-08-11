using ActionBuffer;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ActionEditor.Nodes.BT
{
    [Serializable]
    public abstract class Blackboard
    {
        [NonSerialized] private TypeHelper.TypeFields typeFields;
        [NonSerialized] private BTTree runtimeTree;
        [NonSerialized] private BTRuntimeLayout runtimeLayout;
        [NonSerialized] private int[] runtimeValues;

        private TypeHelper.TypeFields Fields =>
            typeFields ?? (typeFields = TypeHelper.GetTypeFields(GetType()));

        internal Type GetValueType(string fieldName) =>
            Fields.FindField(fieldName)?.FieldType;

        public virtual object GetValue(string fieldName)
        {
            var field = Fields.FindField(fieldName);
            if (field == null) return default;
            return field.GetValue(this);
        }

        public virtual void SetValue(string fieldName, object value)
        {
            var field = Fields.FindField(fieldName);
            if (field == null) return;
            if (value != null && field.FieldType.IsEnum &&
                value.GetType() != field.FieldType)
            {
                if (value is string valueName)
                {
                    if (!BTEnumValueUtility.TryGetValue(field.FieldType,
                            valueName, out value))
                        throw new InvalidOperationException(
                            $"Blackboard enum variable '{fieldName}' cannot " +
                            $"use value '{valueName}'");
                }
                else
                    value = Enum.ToObject(field.FieldType, value);
            }
            field.SetValue(this, value);
        }

        public void CopyFieldsFrom(Blackboard source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Type type = GetType();
            if (source.GetType() != type)
                throw new ArgumentException(
                    "Blackboards must have the same concrete type",
                    nameof(source));

            bool copyRuntimeValues = source.runtimeTree != null;
            if (copyRuntimeValues)
            {
                EnsureRuntimeInitialized();
                if (!ReferenceEquals(runtimeTree, source.runtimeTree) ||
                    !ReferenceEquals(runtimeLayout, source.runtimeLayout))
                    throw new ArgumentException(
                        "Blackboards must be initialized for the same behavior tree",
                        nameof(source));
            }

            TypeHelper.TypeFields.FieldCollection fields = Fields.GetFields();
            for (int i = 0; i < fields.Count; i++)
                fields[i].CopyValue(source, this);
            if (copyRuntimeValues)
                Array.Copy(source.runtimeValues, runtimeValues,
                    runtimeValues.Length);
        }

        public void Initialize(BTTree tree,
            IReadOnlyList<int> sourceRuntimeValues = null)
        {
            if (tree == null) throw new ArgumentNullException(nameof(tree));
            tree.EnsureRelationsPrepared();
            if (tree.IsSubTree)
                throw new InvalidOperationException(
                    "A subtree must be initialized through its parent tree");

            BTRuntimeLayout layout = tree.RuntimeLayout;
            if (runtimeValues == null || runtimeValues.Length != layout.ValueCount)
                runtimeValues = new int[layout.ValueCount];
            runtimeTree = null;
            runtimeLayout = null;

            try
            {
                for (int i = 0; i < layout.Nodes.Length; i++)
                    layout.Nodes[i].ValidateBlackboard(this);
                if (sourceRuntimeValues == null)
                    Array.Copy(layout.DefaultRuntimeValues, runtimeValues,
                        runtimeValues.Length);
                else
                {
                    int count = sourceRuntimeValues.Count;
                    if (count != runtimeValues.Length)
                        throw new ArgumentException(
                            count < runtimeValues.Length
                                ? "Runtime status does not contain enough values"
                                : "Runtime status contains extra values",
                            nameof(sourceRuntimeValues));
                    for (int i = 0; i < count; i++)
                    {
                        int value = sourceRuntimeValues[i];
                        if (value < layout.MinimumValues[i] ||
                            value > layout.MaximumValues[i])
                            throw new ArgumentException(
                                $"Invalid runtime status value at index {i}",
                                nameof(sourceRuntimeValues));
                    }

                    if (sourceRuntimeValues is int[] sourceArray)
                        Array.Copy(sourceArray, runtimeValues, count);
                    else
                        for (int i = 0; i < count; i++)
                            runtimeValues[i] = sourceRuntimeValues[i];
                }
                runtimeTree = tree;
                runtimeLayout = layout;
            }
            catch
            {
                runtimeTree = null;
                runtimeLayout = null;
                throw;
            }
        }

        internal bool Abort(string flag)
        {
            if (string.IsNullOrEmpty(flag)) return false;
            if (!runtimeLayout.Interrupts.TryGetValue(flag,
                    out BTInterrupt interrupt))
                return false;
            interrupt.Interrupt(this);
            return true;
        }

        internal bool PushEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName)) return false;
            if (!runtimeLayout.EventReceivers.TryGetValue(eventName,
                    out IBTEventReceiver[] receivers)) return false;
            for (int i = 0; i < receivers.Length; i++)
                receivers[i].ReceiveEvent(this);
            return true;
        }

        public IReadOnlyList<int> RuntimeValues => runtimeValues;

        public bool IsInitializedFor(BTTree tree) =>
            tree != null && runtimeValues != null &&
            ReferenceEquals(runtimeTree, tree) &&
            tree.HasRuntimeLayout(runtimeLayout);

        public BTNode.State? GetState(BTNode node)
        {
            if (node == null || runtimeTree == null || runtimeLayout == null ||
                runtimeValues == null ||
                !node.BelongsTo(runtimeTree, runtimeLayout.Token)) return null;
            return (BTNode.State)runtimeValues[node.RuntimeOffset];
        }

        internal BTNode.State Update()
        {
            BTComposite[] abortComposites = runtimeLayout.AbortComposites;
            for (int i = 0; i < abortComposites.Length; i++)
                abortComposites[i].TryAutoAbort(this);
            return runtimeTree.root.Update(this);
        }

        internal void Abort() => runtimeTree.root.Abort(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool WaitSemaphore(int index)
        {
            int offset = runtimeLayout.SemaphoreOffset + index;
            int value = runtimeValues[offset];
            if (value >= runtimeLayout.MaximumValues[offset]) return false;
            runtimeValues[offset] = value + 1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ReleaseSemaphore(int index)
        {
            int offset = runtimeLayout.SemaphoreOffset + index;
            int value = runtimeValues[offset];
            if (value > 0) runtimeValues[offset] = value - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetRuntimeValue(int index) => runtimeValues[index];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void SetRuntimeValue(int index, int value) =>
            runtimeValues[index] = value;

        private void EnsureRuntimeInitialized()
        {
            if (runtimeTree == null || runtimeLayout == null ||
                runtimeValues == null)
                throw new InvalidOperationException(
                    "The Blackboard has not been initialized for a behavior tree");
        }
    }
}
