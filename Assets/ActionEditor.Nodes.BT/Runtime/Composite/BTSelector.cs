using System.Collections.Generic;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("按固定顺序寻找可成功的子节点；失败时继续下一个，运行中时记住当前位置并在下一 Tick 续跑，任一成功即成功。")]
    [Name("选择"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite),Icon("Selector")]

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
