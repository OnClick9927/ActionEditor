using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
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
        [System.NonSerialized] private Blackboard _blackboard;
        [System.NonSerialized] private BTNode _parent;
        [System.NonSerialized] private BTTree _runtimeTree;
        [System.NonSerialized] private State _state;

        protected Blackboard blackboard => _blackboard;
        internal BTNode parent => _parent;
        internal BTTree runtimeTree => _runtimeTree;
        public State state => _state;

        internal State Update()
        {
            if (_state == State.Inactive)
            {
                OnStart();
                _state = State.Running;
            }
            var result = OnUpdate();
            _state = result;
            if (_state != State.Running)
            {
                OnStop();
                _state = State.Inactive;
            }
            return result;
        }
        protected abstract State OnUpdate();
        protected virtual void OnStart() { }
        protected virtual void OnStop() { }
        public void Abort()
        {
            if (_state != State.Running) return;
            OnAbort();
            _state = State.Inactive;
        }

        protected abstract void OnAbort();
        protected BTComposite FindParentComposite()
        {
            var _node = _parent;
            while (_node != null)
            {
                if (_node is BTComposite composite)
                {
                    return composite;
                }
                _node = _node.parent;
            }
            return null;
        }

        internal virtual void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            _blackboard = blackboard;
            _parent = parent;
            _runtimeTree = tree;
            _state = State.Inactive;
        }

        protected virtual int RuntimeChildCount => 0;
        protected virtual BTNode GetRuntimeChild(int index) => null;
        internal int RuntimeChildrenCount => RuntimeChildCount;
        internal BTNode GetRuntimeChildAt(int index) => GetRuntimeChild(index);
        protected virtual void OnCollectStatus(List<int> values) { }
        protected virtual void OnReadStatus(List<int> values, ref int index) { }

        protected static int ReadStatusValue(List<int> values, ref int index)
        {
            if (index >= values.Count)
                throw new System.ArgumentException(
                    "Runtime status does not contain enough values",
                    nameof(values));
            return values[index++];
        }

        internal void CollectRuntimeStatus(List<int> values)
        {
            values.Add((int)_state);
            OnCollectStatus(values);
            int childCount = RuntimeChildCount;
            for (int i = 0; i < childCount; i++)
            {
                BTNode child = GetRuntimeChild(i);
                if (child == null)
                    throw new System.InvalidOperationException(
                        $"{GetType()} runtime child {i} is null");
                child.CollectRuntimeStatus(values);
            }
        }

        internal void ReadRuntimeStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < (int)State.Inactive || value > (int)State.Running)
                throw new System.ArgumentException(
                    $"Invalid runtime status for {GetType()}",
                    nameof(values));
            _state = (State)value;
            OnReadStatus(values, ref index);
            int childCount = RuntimeChildCount;
            for (int i = 0; i < childCount; i++)
            {
                BTNode child = GetRuntimeChild(i);
                if (child == null)
                    throw new System.InvalidOperationException(
                        $"{GetType()} runtime child {i} is null");
                child.ReadRuntimeStatus(values, ref index);
            }
        }
    }
}
