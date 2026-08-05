using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("并行等待", "并行执行子节点，并采用最先结束的子节点结果。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ParallelComplete")]
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
