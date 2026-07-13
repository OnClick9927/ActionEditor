namespace ActionEditor.Nodes.BT
{
    [Name("ĞÅºÅÁ¿"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Semaphore")]
    public class BTSemaphore : BTDecorateSingle
    {
        public bool wait = true;
        [ReadOnly] public int semaphore;

        protected override State OnUpdate()
        {
            var succ = this.runtimeTree.WaitSemaphore(semaphore);
            if (succ)
                return base.OnUpdate();
            if (wait) return State.Running;
            return State.Failure;
        }
        protected override void OnStop()
        {
            base.OnStop();
            this.runtimeTree.ReleaseSemaphore(semaphore);
        }
        protected override void OnAbort()
        {
            base.OnAbort();
            this.runtimeTree.ReleaseSemaphore(semaphore);

        }
        protected override State Decorate(State state) => state;
    }
}
