using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("按固定顺序寻找可成功的子节点；失败时继续下一个，运行中时记住当前位置并在下一 Tick 续跑，任一成功即成功。")]
    [Name("选择"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite),Icon("Selector")]

    public class BTSelector : BTComposite
    {
        public int GetCurrent(Blackboard blackboard) =>
            GetRuntimeData(blackboard, 0);
        private void SetCurrent(Blackboard blackboard, int value) =>
            SetRuntimeData(blackboard, 0, value);

        protected override int RuntimeDataSize => 1;
        protected override int GetMinRuntimeData(int index) => 0;
        protected override int GetMaxRuntimeData(int index) =>
            System.Math.Max(0, ChildCount - 1);

        protected override void OnStart(Blackboard blackboard)
        {
            base.OnStart(blackboard);
            SetCurrent(blackboard, 0);
        }
        protected override void OnAbort(Blackboard blackboard)
        {
            base.OnAbort(blackboard);
            SetCurrent(blackboard, 0);
        }
        protected override State OnUpdate(Blackboard blackboard)
        {
            int current = GetCurrent(blackboard);
            for (int i = current; i < ChildCount; i++)
            {
                if (i != current)
                {
                    SetCurrent(blackboard, i);
                    current = i;
                }
                var child = ChildAt(i);
                switch (child.Update(blackboard))
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

    }
}
