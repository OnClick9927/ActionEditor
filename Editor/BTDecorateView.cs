using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace ActionEditor.Nodes.BT
{
    class BTRecEventConditionView : BTConditionView<BTRecEventCondition>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var flags = (App.asset as BTTree).events;
            var index = EditorGUILayout.Popup(nameof(data.eventName), Mathf.Max(flags.IndexOf(data.eventName), 0), flags.ToArray());
            if (flags.Count == 0)
                data.eventName = string.Empty;
            else
                data.eventName = flags[index];

        }

    }
    class BTPushEventView : BTActionView<BTPushEvent>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var flags = (App.asset as BTTree).events;
            var index = EditorGUILayout.Popup(nameof(data.eventName), Mathf.Max(flags.IndexOf(data.eventName), 0), flags.ToArray());
            if (flags.Count == 0)
                data.eventName = string.Empty;
            else
                data.eventName = flags[index];

        }

    }
    class BTSemaphoreView : BTDecorateSingleView<BTSemaphore>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var flags = (App.asset as BTTree).semaphores;
            var index = EditorGUILayout.Popup(nameof(data.semaphore), Mathf.Max(data.semaphore, 0), flags.Select(x=>x.name).ToArray());
            data.semaphore = index;

        }

    }
    class BTInterruptView : BTDecorateSingleView<BTInterrupt>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var flags = (App.asset as BTTree).interruptFlags;
            var index = EditorGUILayout.Popup(nameof(data.flag), Mathf.Max(flags.IndexOf(data.flag),0), flags.ToArray());
            if (flags.Count == 0)
                data.flag = string.Empty;
            else
                data.flag = flags[index];
        }

    }
    public class BTDecorateSingleView<T> : BTNodeView<T> where T : BTDecorateSingle, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode));
        }
    }
    public class BTDecorateMutiView<T> : BTNodeView<T> where T : BTDecorateMuti, new()
    {
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            this.GeneratePort(Direction.Output, typeof(BTNode), Port.Capacity.Multi);
        }
    }
}
