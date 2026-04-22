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
        protected Blackboard blackBoard { get; private set; }
        internal BTNode parent { get; private set; }
      
        public State state { get; private set; }
        internal State Update()
        {
            if (state == State.Inactive)
            {
                OnStart();
                state = State.Running;
            }
            var result = OnUpdate();
            state = result;
            if (state != State.Running)
            {
                OnStop();
                state = State.Inactive;
            }
            return result;
        }
        protected abstract State OnUpdate();
        protected virtual void OnStart() { }
        protected virtual void OnStop() { }
        public void Abort()
        {
            if (state != State.Running) return;
            OnAbort();
            state = State.Inactive;
        }

        protected abstract void OnAbort();


        internal virtual List<BTCondition> Init(Blackboard blackBord, BTNode parent, List<BTCondition> result)
        {
            this.blackBoard = blackBord;
            this.parent = parent;
            return result;
        }

    }
}
