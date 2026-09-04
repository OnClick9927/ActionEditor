using System.Collections.Generic;
using System.Linq;
using UnityEditor;
namespace ActionEditor.Nodes.Dialog
{
    class StepDataView : GraphNode<StepData>
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePorts(data.GetType());
            var steps = this.view.nodes.Where(x => x.Data is StepData)
                     .Select(x => x.Data as StepData);
            HashSet<int> set = null;
            if (steps != null && steps.Count() > 0)
                set = steps.Select(x => x.id).ToHashSet();
            if (data.id == 0 || (set != null && set.Contains(data.id)))
            {
                if (set == null)
                    data.id =  1;
                else
                    data.id = set.Max() + 1;
            }

        }
    }

}
