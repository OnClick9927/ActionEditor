using System.Drawing;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes.BT
{
    class BTSubTreeView : BTNodeView<BTSubTree>
    {
        public override void OnInspectorGUI()
        {
            ActionEditor.EditorEX.CreateEditor(Data).OnInspectorGUI();

            if (GUILayout.Button("Select"))
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

                    if (!x.EndsWith(GraphAsset.FileEx)) return false;
                    if (!view.IsFileFitAsset(x)) return false;
                    if (x == App.assetPath) return false;
                    return true;
                });
            }

        }
        public override void OnCreated(NodeGraphView view)
        {
            base.OnCreated(view);
            this.GeneratePort(Direction.Input, typeof(BTNode));
            RegisterCallback<PointerDownEvent>((evt) =>
            {
                // 检查点击次数是否为 2
                if (evt.clickCount == 2)
                {
                    App.OnObjectPickerConfig(this.data.path);
                    //this.data.path
                    // 在这里执行你的双击逻辑
                }
            });
        }
    }
}
