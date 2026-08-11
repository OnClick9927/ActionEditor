using ActionAttribute;
namespace ActionEditor.Nodes.BT
{
    [Icon("Conditional")]
    public abstract class BTCondition : BTNode
    {
        protected sealed override void OnAbort(Blackboard blackboard) { }
        protected abstract bool Condition(Blackboard blackboard);
        protected sealed override State OnUpdate(Blackboard blackboard)
        {
            return Condition(blackboard) ? State.Success : State.Failure;
        }
    }
}
