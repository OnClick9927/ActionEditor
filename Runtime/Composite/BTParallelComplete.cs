using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("每个 Tick 按固定顺序更新所有运行分支，采用最先结束分支的成功或失败结果，并立即中止仍在运行的其他分支。")]
    [Name("并行等待"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ParallelComplete")]
    public class BTParallelComplete : BTComposite
    {
        protected override State OnUpdate()
        {
            for (int i = 0; i < ChildCount; ++i)
            {
                var status = ChildAt(i).Update();
                if (status == State.Failure || status == State.Success)
                {
                    AbortRunningChildren();
                    return status;
                }
            }

            return State.Running;
        }
    }
}
