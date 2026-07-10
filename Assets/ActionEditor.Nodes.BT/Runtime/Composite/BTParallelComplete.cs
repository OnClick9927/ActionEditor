namespace ActionEditor.Nodes.BT
{
    [Name("²¢ÐÐµÈ´ý"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ParallelComplete")]
    public class BTParallelComplete : BTComposite
    {
        protected override State OnUpdate()
        {
            for (int i = 0; i < children.Count; ++i)
            {
                var status = children[i].Update();
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
