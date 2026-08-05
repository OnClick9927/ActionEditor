using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("执行中断", "按标识触发对应的行为树中断。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("PerformInterrupt")]

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
