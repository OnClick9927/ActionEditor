using ActionUnity;
using System;
using System.Collections.Generic;

namespace ActionEditor.Nodes.BT
{
    [Name("轮询", "按顺序轮流执行子节点，并保存下一次执行位置。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("Repeater")]
    public class BTRoundRobin : BTComposite
    {
        public bool advanceOnSuccess = true;
        public bool advanceOnFailure = true;
        public bool resetOnAbort;
        [NonSerialized] private int currentIndex;

        protected override void OnInitialized()
        {
            currentIndex = 0;
        }

        protected override State OnUpdate()
        {
            if (ChildCount == 0) return State.Failure;
            if (currentIndex >= ChildCount) currentIndex = 0;

            State result = ChildAt(currentIndex).Update();
            if (result == State.Running) return State.Running;
            if ((result == State.Success && advanceOnSuccess) ||
                (result == State.Failure && advanceOnFailure))
                currentIndex = (currentIndex + 1) % ChildCount;
            return result;
        }

        protected override void OnAbort()
        {
            base.OnAbort();
            if (resetOnAbort) currentIndex = 0;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(currentIndex);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > Math.Max(0, ChildCount - 1))
                throw new ArgumentException("Invalid round-robin runtime status",
                    nameof(values));
            currentIndex = value;
        }
    }
}
