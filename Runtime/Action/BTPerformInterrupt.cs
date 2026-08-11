using ActionAttribute;
using System;

namespace ActionEditor.Nodes.BT
{
    [TypeInfoBox("按唯一标识查找已注册的中断节点，中止其当前运行分支后返回成功；标识不存在时不会修改其他节点。")]
    [Name("执行中断"), Attachable(typeof(BTTree)), Node(BTNodeTypes.Action), Icon("PerformInterrupt")]

    public class BTPerformInterrupt : BTAction
    {
        [Name("中断标识", "目标中断节点在运行树中登记的唯一键；重复键会在初始化阶段报错，避免触发对象不确定。")]
        public string flag;
        internal override void Init(BTNode parent, BTPrepareContext context)
        {
            base.Init(parent, context);
            if (string.IsNullOrEmpty(flag))
                throw new InvalidOperationException(
                    $"{GetType()} requires an interrupt flag");
        }

        protected override State OnUpdate(Blackboard blackboard)
        {
            var succ = blackboard.Abort(flag);
            return succ? State.Success: State.Failure;
        }
    }
}
