using ActionUnity;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
    [Name("信号量", "通过行为树信号量限制同时运行的分支数量。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Semaphore")]
    public class BTSemaphore : BTDecorateSingle
    {
        public bool wait = true;
        [ReadOnly] public int semaphore;
        [NonSerialized] private bool acquired;

        internal override void Init(Blackboard blackboard, BTNode parent, BTTree tree)
        {
            base.Init(blackboard, parent, tree);
            if (!tree.IsValidSemaphore(semaphore))
                throw new InvalidOperationException(
                    $"{GetType()} has invalid semaphore index {semaphore}");
            acquired = false;
        }

        protected override State OnUpdate()
        {
            if (!acquired)
                acquired = runtimeTree.WaitSemaphore(semaphore);
            if (!acquired) return wait ? State.Running : State.Failure;
            return base.OnUpdate();
        }

        protected override void OnStop()
        {
            base.OnStop();
            ReleaseSemaphore();
        }

        protected override void OnAbort()
        {
            try
            {
                base.OnAbort();
            }
            finally
            {
                ReleaseSemaphore();
            }
        }

        private void ReleaseSemaphore()
        {
            if (!acquired) return;
            runtimeTree.ReleaseSemaphore(semaphore);
            acquired = false;
        }

        protected override State Decorate(State state) => state;

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(acquired ? 1 : 0);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value != 0 && value != 1)
                throw new ArgumentException("Invalid semaphore runtime status",
                    nameof(values));
            if (value != 0 && state != State.Running)
                throw new ArgumentException(
                    "An inactive semaphore node cannot own a semaphore",
                    nameof(values));
            acquired = value != 0;
        }
    }
}
