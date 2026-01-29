namespace ActionEditor.Nodes.BT
{
    [Name("Ñ¡Ôñ"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite)]

    public class BTSelector : BTComposite
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
