namespace ActionEditor.Nodes.BT
{
    [ Name("ађСа"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite),Icon("Sequence")]
    public class BTSequence : BTComposite
    {
        public int current { get; protected set; }
        protected override void OnStart()
        {
            base.OnStart();
            current = 0;
        }
        protected override void OnAbort()
        {
            base.OnAbort();
            current = 0;
        }
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
