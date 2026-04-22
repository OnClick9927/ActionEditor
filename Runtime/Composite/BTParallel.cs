namespace ActionEditor.Nodes.BT
{
    [Name("²¢ÐÐ"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite),Icon("Parallel")]

    public class BTParallel : BTComposite
    {

        [System.NonSerialized] private State[] running;
        protected override void OnAbort()
        {
            base.OnAbort();
            for (int i = 0; i < running.Length; i++)
                running[i] = State.Running;
        }
        protected override void OnStart()
        {
            base.OnStart();
            running = running ?? new State[children.Count];
            for (int i = 0; i < running.Length; i++)
                running[i] = State.Running;
        }
        void AbortRunningChildren()
        {
            for (int i = 0; i < running.Length; ++i)
                children[i].Abort();
        }

        protected override State OnUpdate()
        {
            bool stillRunning = false;
            for (int i = 0; i < running.Length; ++i)
            {
                if (running[i] == State.Running)
                {
                    var status = children[i].Update();
                    if (status == State.Failure)
                    {
                        AbortRunningChildren();
                        return State.Failure;
                    }

                    if (status == State.Running)
                    {
                        stillRunning = true;
                    }
                    running[i] = status;
                }
            }

            return stillRunning ? State.Running : State.Success;
        }


    }
}
