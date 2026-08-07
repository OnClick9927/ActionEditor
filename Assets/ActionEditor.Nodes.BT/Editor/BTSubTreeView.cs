using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{
    class BTSubTreeView : BTNodeView<BTSubTree>
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button(Lan.Text("Select", "Select")))
            {

                ActionEditor.AssetPick.ShowObjectPicker(GUILayoutUtility.GetLastRect(), "Assets", "t:TextAsset", Prefs.pickListType, (o) =>
                {
                    if (o is TextAsset txt)
                    {
                        try
                        {
                            if (BTTree.FromBytes(typeof(BTTree), txt.bytes) is BTTree tree && tree.IsSubTree &&
                            tree.GetType() == App.asset.GetType())
                            {
                                this.data.path = AssetDatabase.GetAssetPath(o);
                                GUIUtility.ExitGUI();
                            }
                        }
                        catch (System.Exception)
                        {

                        }

                    }
                }, (x) =>
                {

                    string extension = App.GetFileExtension(
                        App.asset?.GetType() ?? typeof(GraphAsset));
                    if (!x.EndsWith("." + extension,
                        System.StringComparison.OrdinalIgnoreCase)) return false;
                    if (!view.IsFileFitAsset(x)) return false;
                    if (x == App.assetPath) return false;
                    return true;
                });
            }
            if (GUILayout.Button(Lan.Text("SyncSubTree",
                "Sync Interrupts, Semaphores and Events")))
            {
                var path = this.data.path;
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (text != null)
                {
                    var tree = BTTree.FromBytes(typeof(BTTree), text.bytes) as BTTree;
                    var src = App.asset as BTTree;
                    tree.semaphores = src.semaphores;
                    tree.interruptFlags = src.interruptFlags;
                    tree.events = src.events;

                    System.IO.File.WriteAllBytes(data.path, tree.ToBytes());
                    AssetDatabase.Refresh();
                }
            }
        }
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            RegisterCallback<PointerDownEvent>((evt) =>
            {
                // Open a subtree after a double-click.
                if (evt.clickCount == 2)
                {
                    App.OnObjectPickerConfig(this.data.path);
                    //this.data.path
                }
            });
        }

        public override void OnBTTreeChanged(BTTree tree)
        {
            base.OnBTTreeChanged(tree);
            if (tree != null)
            {
                var node_sub = tree.FindRuntimeTreeNode<BTSubTree>(this.data.guid);
                runningNode = node_sub.runtimeNode;
            }
        }
   
    }
}
