namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("序列"), Attachable(typeof(BTTree)), Node("组合/序列")]
    public class BTSeuquence : BTComposite
    {
        protected override State OnUpdate()
        {
            for (int i = current; i < children.Count; i++)
            {
                current = i;
                var child = children[i];
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
    }
}
