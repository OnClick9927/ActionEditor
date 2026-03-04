using System.Collections.Generic;
namespace ActionEditor.Nodes.BT
{
    [System.Serializable, Name("¸ù½Úµã"), Attachable(typeof(BTTree)), Icon("Entry")]
    public class BTRoot : BTNode
    {
        public BTNode child { get; internal set; }
        internal override List<BTCondition> Init(Blackboard blackBord, BTNode parent, List<BTCondition> result)
        {
            base.Init(blackBord, parent, result);
            if (child == null)
                throw new System.Exception($"{GetType()} {nameof(child)} is Null");
            return child.Init(blackBord, this, result);
        }
        protected sealed override void OnAbort()
        {
            child.Abort();
        }
        protected override State OnUpdate()
        {
            return child.Update();
        }
    }
}
