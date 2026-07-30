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
            running = IsCurrentTreeRunning(BTTree.instance, App.asset.guid);
        }

        private static bool IsCurrentTreeRunning(BTTree tree, string guid)
        {
            if (tree == null)
                return false;
            if (tree.guid == guid)
                return true;

            var subTrees = tree.subs;
            if (subTrees == null)
                return false;
            for (int i = 0; i < subTrees.Count; i++)
            {
                if (IsCurrentTreeRunning(subTrees[i], guid))
                    return true;
            }
            return false;
        }
        protected override bool IsRunning()
        {
            return running;
        }


    }





}
