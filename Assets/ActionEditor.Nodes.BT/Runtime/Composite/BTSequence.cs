using System.Collections.Generic;
using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("序列", "按顺序执行子节点，全部成功后返回成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite),Icon("Sequence")]
    public class BTSequence : BTComposite
    {
        [System.NonSerialized] private int _current;
        public int current => _current;

        protected override void OnStart()
        {
            base.OnStart();
            _current = 0;
        }
        protected override void OnAbort()
        {
            base.OnAbort();
            _current = 0;
        }
        protected override State OnUpdate()
        {
            for (int i = _current; i < ChildCount; i++)
            {
                _current = i;
                var child = ChildAt(i);
                switch (child.Update())
                {

                    case State.Success:
                        continue;
                    case State.Failure:
                        return State.Failure;
                    case State.Running:
                        return State.Running;
                }
            }
            return State.Success;
        }

        protected override void OnCollectStatus(List<int> values)
        {
            values.Add(_current);
        }

        protected override void OnReadStatus(List<int> values, ref int index)
        {
            int value = ReadStatusValue(values, ref index);
            if (value < 0 || value > System.Math.Max(0, ChildCount - 1))
                throw new System.ArgumentException(
                    "Invalid sequence runtime status", nameof(values));
            _current = value;
        }
    }
}
