using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("按固定顺序执行子节点；成功后推进到下一项，运行中时保存当前位置，任一失败立即失败，全部成功后才返回成功。")]
    [Name("序列"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite),Icon("Sequence")]
    public class BTSequence : BTComposite
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
                        continue;
                    case State.Failure:
                        return State.Failure;
                    case State.Running:
                        return State.Running;
                }
            }
            return State.Success;
        }

    }
}
