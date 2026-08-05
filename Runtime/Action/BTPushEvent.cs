using ActionUnity;

namespace ActionEditor.Nodes.BT
{
    [Name("发送事件", "向行为树发送一次命名事件。"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("EventSend")]
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
