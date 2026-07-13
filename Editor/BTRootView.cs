using System.Linq;
using UnityEditor.Experimental.GraphView;

namespace ActionEditor.Nodes.BT
{
    class BTRootView : BTNodeView<BTRoot>
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Output, typeof(BTNode));
        }
        private bool running;
        public override void OnBTTreeChanged(BTTree tree)
        {
            base.OnBTTreeChanged(tree);
            running = false;
            if (tree != null)
            {
                running = BTTree.instance.guid == App.asset.guid ||
                BTTree.instance.subs.Any(x => x.guid == App.asset.guid);
            }
        }
        protected override bool IsRunning()
        {
            return running;
        }


    }





}
