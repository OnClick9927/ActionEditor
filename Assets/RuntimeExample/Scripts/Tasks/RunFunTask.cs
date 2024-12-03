using System;

namespace ActionEditor
{
    public class RunFunTask : NTask
    {
        private readonly Action _action;

        public RunFunTask(Action action)
        {
            _action = action;
        }

        protected override void OnStart()
        {
            _action?.Invoke();

            Finish();
        }
    }
}