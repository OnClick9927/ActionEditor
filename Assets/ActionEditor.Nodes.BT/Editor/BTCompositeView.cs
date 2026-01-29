using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{
    class SequenceView : BTCompositeView<BTSeuquence> { }
    class SelectorView : BTCompositeView<BTSelector> { }
    class ParallelView : BTCompositeView<BTParallel> { }
    public class BTCompositeView<T> : BTNodeView<T> where T : BTComposite, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            IMGUIContainer abort = new IMGUIContainer(DrawAbort);
            abort.style.position = Position.Absolute;
            abort.style.width = abort.style.height = 20;
            abort.style.left = abort.style.top = 10;
            this.Add(abort);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode), Port.Capacity.Multi);
        }

        private void DrawAbort()
        {
            string icon = string.Empty;
            switch (this.data.abortType)
            {
                case BTComposite.AbortType.None:
                    break;
                case BTComposite.AbortType.Self:
                    icon = "ConditionalAbortLowerPriorityIcon";
                    break;
                case BTComposite.AbortType.LowerPriority:
                    icon = "ConditionalAbortSelfIcon";
                    break;
                case BTComposite.AbortType.Both:
                    icon = "ConditionalAbortBothIcon";
                    break;
                default:
                    break;
            }
            if (!string.IsNullOrEmpty(icon))
                GUILayout.Box(Resources.Load<Texture2D>(icon), EditorStyles.iconButton);

        }
    }

}
