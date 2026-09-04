using ActionAttribute;
using System.Linq;
namespace ActionEditor.Nodes.Dialog
{
    [Name("对话资源"), AssetFileExtension("dialog.bytes")]
    public class DialogAsset : GraphAsset
    {
        private RootData root;
        public override void PrepareForRuntime()
        {
            base.PrepareForRuntime();

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node is DialogData dialog)
                {
                    var nextcon = node.outPorts.FirstOrDefault(x => x.name == nameof(DialogData.step))?.connections?.FirstOrDefault();
                    if (nextcon != null)
                        dialog.step = nextcon.input.node as StepData;
                    if (dialog is RootData root)
                        this.root = (RootData)node;
                    if (dialog is StepData step)
                    {
                        var connections = node.outPorts.FirstOrDefault(x => x.name == nameof(StepData.options))?.connections;
                        if (connections!=null && connections.Count > 0)
                            for (int j = 0; j < connections.Count; j++)
                            {
                                step.options = step.options ?? new System.Collections.Generic.List<OptionData>();
                                step.options.Add(connections[j].input.node as OptionData);
                            }
                    }
                }
            }
        }
    }

}
