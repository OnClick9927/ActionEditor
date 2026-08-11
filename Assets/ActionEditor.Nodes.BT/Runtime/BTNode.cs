using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ActionEditor.Nodes.BT
{
    internal sealed class BTPrepareContext
    {
        private readonly HashSet<string> interruptFlags =
            new HashSet<string>(StringComparer.Ordinal);

        internal BTPrepareContext(BTTree tree)
        {
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
        }

        internal BTTree Tree { get; }
        internal object LayoutToken { get; } = new object();
        internal int RuntimeValueCount { get; set; }

        internal void RegisterInterrupt(string flag)
        {
            if (string.IsNullOrEmpty(flag))
                throw new InvalidOperationException(
                    "A behavior-tree interrupt requires a flag");
            if (!interruptFlags.Add(flag))
                throw new InvalidOperationException($"Same Flag {flag}");
        }
    }

    [System.Serializable]
    public abstract class BTNode : NodeData
    {
        public enum State
        {
            Inactive,
            Success,
            Failure,
            Running
        }

        [NonSerialized] private BTNode _parent;
        [NonSerialized] private BTTree _runtimeTree;
        [NonSerialized] private object _runtimeLayoutToken;
        [NonSerialized] private int _runtimeOffset = -1;
        [NonSerialized] private int _runtimeValueCount;

        internal BTNode parent => _parent;
        internal BTTree runtimeTree => _runtimeTree;

        internal State Update(Blackboard blackboard)
        {
            State current = GetStateFast(blackboard);
            if (current == State.Inactive)
            {
                OnStart(blackboard);
                SetState(blackboard, State.Running);
                current = State.Running;
            }
            State result = OnUpdate(blackboard);
            if (result == State.Running)
            {
                if (current != State.Running)
                    SetState(blackboard, State.Running);
                return result;
            }

            if (result != State.Success && result != State.Failure)
                throw new InvalidOperationException(
                    $"{GetType()} returned invalid state {result}");

            SetState(blackboard, result);
            try
            {
                OnStop(blackboard);
            }
            finally
            {
                SetState(blackboard, State.Inactive);
            }
            return result;
        }

        protected abstract State OnUpdate(Blackboard blackboard);
        protected virtual void OnStart(Blackboard blackboard) { }
        protected virtual void OnStop(Blackboard blackboard) { }

        public void Abort(Blackboard blackboard)
        {
            if (GetStateFast(blackboard) != State.Running) return;
            try
            {
                OnAbort(blackboard);
            }
            finally
            {
                SetState(blackboard, State.Inactive);
            }
        }

        protected abstract void OnAbort(Blackboard blackboard);

        protected BTComposite FindParentComposite()
        {
            BTNode node = _parent;
            while (node != null)
            {
                if (node is BTComposite composite) return composite;
                node = node.parent;
            }
            return null;
        }

        internal virtual void Init(BTNode parent, BTPrepareContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            _parent = parent;
            _runtimeTree = context.Tree;
            _runtimeLayoutToken = context.LayoutToken;
            _runtimeOffset = context.RuntimeValueCount;

            int dataSize = RuntimeDataSize;
            if (dataSize < 0)
                throw new InvalidOperationException(
                    $"{GetType()} has a negative runtime data size");
            for (int i = 0; i < dataSize; i++)
            {
                int minimum = GetMinRuntimeData(i);
                int maximum = GetMaxRuntimeData(i);
                int initial = GetInitialRuntimeData(i);
                if (minimum > maximum || initial < minimum || initial > maximum)
                    throw new InvalidOperationException(
                        $"{GetType()} has an invalid runtime data rule at {i}");
            }
            context.RuntimeValueCount += 1 + dataSize;
        }

        internal virtual void ValidateBlackboard(Blackboard blackboard) { }

        protected virtual int RuntimeDataSize => 0;
        protected virtual int GetInitialRuntimeData(int index) => 0;
        protected virtual int GetMinRuntimeData(int index) => int.MinValue;
        protected virtual int GetMaxRuntimeData(int index) => int.MaxValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected int GetRuntimeData(Blackboard blackboard, int index) =>
            blackboard.GetRuntimeValue(_runtimeOffset + 1 + index);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetRuntimeData(Blackboard blackboard, int index, int value) =>
            blackboard.SetRuntimeValue(_runtimeOffset + 1 + index, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal State GetStateFast(Blackboard blackboard) =>
            (State)blackboard.GetRuntimeValue(_runtimeOffset);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetState(Blackboard blackboard, State value) =>
            blackboard.SetRuntimeValue(_runtimeOffset, (int)value);

        protected virtual int RuntimeChildCount => 0;
        protected virtual BTNode GetRuntimeChild(int index) => null;
        internal int RuntimeChildrenCount => RuntimeChildCount;
        internal BTNode GetRuntimeChildAt(int index) => GetRuntimeChild(index);

        internal void WriteRuntimeRules(
            int[] defaultRuntimeValues,
            int[] minimumValues, int[] maximumValues)
        {
            defaultRuntimeValues[_runtimeOffset] = (int)State.Inactive;
            minimumValues[_runtimeOffset] = (int)State.Inactive;
            maximumValues[_runtimeOffset] = (int)State.Running;
            int dataSize = RuntimeDataSize;
            for (int i = 0; i < dataSize; i++)
            {
                int offset = _runtimeOffset + 1 + i;
                defaultRuntimeValues[offset] = GetInitialRuntimeData(i);
                minimumValues[offset] = GetMinRuntimeData(i);
                maximumValues[offset] = GetMaxRuntimeData(i);
            }
        }

        internal bool BelongsTo(BTTree tree, object layoutToken) =>
            ReferenceEquals(_runtimeTree, tree) &&
            ReferenceEquals(_runtimeLayoutToken, layoutToken) &&
            _runtimeOffset >= 0;
        internal bool IsPreparedFor(BTTree tree) =>
            ReferenceEquals(_runtimeTree, tree) &&
            _runtimeLayoutToken != null && _runtimeOffset >= 0 &&
            _runtimeValueCount > 0;
        internal int RuntimeOffset => _runtimeOffset;
        internal object RuntimeLayoutToken => _runtimeLayoutToken;
        internal int RuntimeValueCount => _runtimeValueCount;

        internal void CompleteRuntimeLayout(int runtimeValueCount)
        {
            if (_parent != null)
                throw new InvalidOperationException(
                    "Only the behavior-tree root can complete a runtime layout");
            _runtimeValueCount = runtimeValueCount;
        }

        internal void ResetPreparedData()
        {
            _parent = null;
            _runtimeTree = null;
            _runtimeLayoutToken = null;
            _runtimeOffset = -1;
            _runtimeValueCount = 0;
        }
    }
}
