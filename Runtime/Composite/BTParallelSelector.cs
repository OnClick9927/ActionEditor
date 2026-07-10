namespace ActionEditor.Nodes.BT
{
    [Name("²¢ÐÐÑ¡Ôñ"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Composite), Icon("ParallelSelector")]

    public class BTParallelSelector : BTComposite
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


        protected override State OnUpdate()
        {
            bool stillRunning = false;
            
            for (int i = 0; i < running.Length; ++i)
            {
                if (running[i] == State.Running)
                {
                    var status = children[i].Update();
                    if (status == State.Success)
                    {
                        AbortRunningChildren();
                        return State.Success;
                    }

                    if (status == State.Running)
                    {
                        stillRunning = true;
                    }
                    running[i] = status;
                }
            }

            return stillRunning ? State.Running : State.Failure;
        }


    }
}
