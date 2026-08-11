using static ActionEditor.Nodes.BT.BTComposite;
using ActionAttribute;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("把唯一子分支登记为可外部触发的中断目标；收到匹配标识后按配置范围中止正在运行的节点。")]
    [Name("中断"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Decorate), Icon("Interrupt")]

    public class BTInterrupt : BTDecorateSingle
    {

        [Name("中止方式", "指定触发时中止本节点自身分支，还是交由父组合节点处理中止范围；只会影响当前正在运行的节点。")]
        public AbortType abortType;
        private bool abortLower;
        private bool abortSelf;
        private BTComposite CompositeParent;
        [ReadOnly, Name("中断标识", "运行树中查找该中断节点的唯一键，由编辑器统一维护；同一主树内重复标识会在准备运行时报错。")]
        public string flag;

        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            CompositeParent = null;
            abortLower = abortType == AbortType.Both || abortType == AbortType.LowerPriority;
            abortSelf = abortType == AbortType.Both || abortType == AbortType.Self;
            if (abortLower)
            {
                CompositeParent = FindParentComposite();
                if (CompositeParent == null)
                    throw new System.Exception($" {this.abortType} need {nameof(CompositeParent)}");
            }
            if (abortSelf || abortLower)
                context.RegisterInterrupt(flag);
        }
        internal bool CanInterrupt => abortSelf || abortLower;
        public void Interrupt(Blackboard blackboard)
        {
            if (abortLower)
                CompositeParent.Abort(blackboard);
            if (abortSelf && GetStateFast(blackboard) == State.Running)
                Abort(blackboard);
        }
        protected override State Decorate(Blackboard blackboard, State state)
        {
            return state;
        }
    }
}
