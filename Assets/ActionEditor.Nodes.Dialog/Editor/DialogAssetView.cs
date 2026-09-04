using ActionAttribute;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
namespace ActionEditor.Nodes.Dialog
{
    class DialogAssetView : ActionEditor.Nodes.NodeGraphView<DialogAsset>
    {
        public override void OnSelectNode(GraphNode obj)
        {
        }

        protected override void AfterCreateNode(GraphElement element)
        {

        }

        protected override List<Type> FitterNodeTypes(List<Type> src, GraphElement element)
        {
            src.RemoveAll(x => !EditorEX.CanAttachTo(x, typeof(DialogAsset))
           && !EditorEX.CanAttachTo(x, typeof(DialogAsset))
           );

            return src;
        }

        protected override bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end)
        {
            return start.portType == end.portType;

        }
    }

}
