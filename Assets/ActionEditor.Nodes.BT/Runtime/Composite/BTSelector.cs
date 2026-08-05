using System.Collections.Generic;
using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("选择", "按顺序执行子节点，直到某个子节点成功。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite),Icon("Selector")]

    public class BTSelector : BTComposite
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
                        return State.Success;
                    case State.Failure:
                        continue;
                    case State.Running:
                        return State.Running;
                }
            }
            return State.Failure;
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
                    "Invalid selector runtime status", nameof(values));
            _current = value;
        }
    }
}
