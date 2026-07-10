namespace ActionEditor.Nodes.BT
{
    [Name("Ö´ÐÐÖÐ¶Ï"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("PerformInterrupt")]

    public class BTPerformInterrupt : BTAction
    {
        public string flag;
        protected override State OnUpdate()
        {
            var succ = runtimeTree.Abort(flag);
            return succ? State.Success: State.Failure;
        }
    }
}
