namespace ActionEditor.Nodes.BT
{
    [Name("·¢ËÍÊÂ¼þ"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("EventSend")]
    public class BTPushEvent : BTAction
    {
        [ReadOnly]public string eventName;

        protected override State OnUpdate()
        {
            var succ = this.runtimeTree.PushEvent(eventName);
            return succ ? State.Success : State.Failure;
        }
    }
}
