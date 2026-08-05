using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{
    class SequenceView : BTCompositeView<BTSequence> { }
    class SelectorView : BTCompositeView<BTSelector> { }
    class ParallelView : BTCompositeView<BTParallel> { }
    public class BTCompositeView<T> : BTNodeView<T> where T : BTComposite, new()
    {
        private static Texture2D _abortSelfIcon;
        private static Texture2D _abortLowerPriorityIcon;
        private static Texture2D _abortBothIcon;

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
            Texture2D icon = null;
            switch (this.data.abortType)
            {
                case BTComposite.AbortType.None:
                    break;
                case BTComposite.AbortType.Self:
                    if (_abortSelfIcon == null)
                        _abortSelfIcon = Resources.Load<Texture2D>(
                            "ConditionalAbortLowerPriorityIcon");
                    icon = _abortSelfIcon;
                    break;
                case BTComposite.AbortType.LowerPriority:
                    if (_abortLowerPriorityIcon == null)
                        _abortLowerPriorityIcon = Resources.Load<Texture2D>(
                            "ConditionalAbortSelfIcon");
                    icon = _abortLowerPriorityIcon;
                    break;
                case BTComposite.AbortType.Both:
                    if (_abortBothIcon == null)
                        _abortBothIcon = Resources.Load<Texture2D>(
                            "ConditionalAbortBothIcon");
                    icon = _abortBothIcon;
                    break;
                default:
                    break;
            }
            if (icon != null)
                GUILayout.Box(icon, EditorStyles.iconButton);

        }
    }

}
