using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ActionEditor
{
    class AssetPick : PopupWindowContent
    {
        private string[] paths;
        private Action<UnityEngine.Object> itemSelectedCallback;

        private Tree tree;
        private class Tree : TreeView
        {
            private AssetPick pick;
            private SearchField field;
            public Tree(AssetPick pick) : base(new TreeViewState())
            {
                this.pick = pick;
                field = new SearchField();
                this.showBorder = true;
                this.showAlternatingRowBackgrounds = true;
                this.useScrollView = true;
                this.depthIndentWidth = 500;
                Reload();
            }

            protected override TreeViewItem BuildRoot()
            {
                return new TreeViewItem() { depth = -1 };
            }
            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                var rows = GetRows() ?? new List<TreeViewItem>();
                rows.Clear();
                for (int i = 0; i < pick.paths.Length; i++)
                {

                    bool create = true;
                    var path = pick.paths[i];
                    if (!string.IsNullOrEmpty(this.searchString))
                    {
                        create = path.Contains(this.searchString);
                    }
                    if (!create) continue;
                    var item = new TreeViewItem()
                    {
                        depth = root.depth + 1,
                        displayName = path,
                        id = i + 1,
                        parent = root,
                    };
                    rows.Add(item);
                }
                SetupParentsAndChildrenFromDepths(root, rows);
                return rows;
            }
            public override void OnGUI(Rect rect)
            {
                var tmp = field.OnGUI(new Rect(rect.position, new Vector2(rect.width, 20)), this.searchString);
                if (tmp != this.searchString)
                {
                    this.searchString = tmp;
                    Reload();
                }

                base.OnGUI(new Rect(new Vector2(rect.x, rect.y + 20), new Vector2(rect.width, rect.height - 20)));
            }
            protected override void RowGUI(RowGUIArgs args)
            {
                base.RowGUI(args);
                if (args.rowRect.Contains(Event.current.mousePosition))
                {
                    GUI.Box(args.rowRect, "", new GUIStyle("SelectionRect"));
                }

            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override void SingleClickedItem(int id)
            {
                var item = FindItem(id, rootItem);
                pick.editorWindow.Close();
                pick.itemSelectedCallback?.Invoke(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.displayName));

            }
        }
        private AssetPick(string[] paths, Action<UnityEngine.Object> itemSelectedCallback) /*: base(new AdvancedDropdownState())*/
        {
            this.paths = paths;
            this.itemSelectedCallback = itemSelectedCallback;
            tree = new Tree(this);
        }
        public static void ShowObjectPicker(Rect rect, string floder, string filter, Action<UnityEngine.Object> itemSelectedCallback,
        Func<string, bool> fit = null)
        {
            var paths = AssetDatabase.FindAssets(filter, new string[] { floder })
                .Select(x => AssetDatabase.GUIDToAssetPath(x)).Where(x =>
             {
                 if (fit != null)
                     return fit(x);
                 return true;
             });
            AssetPick pick = new AssetPick(paths.ToArray(), itemSelectedCallback);
            PopupWindow.Show(rect, pick);
        }

        public override void OnGUI(Rect rect)
        {
            tree.OnGUI(rect);
            this.editorWindow.Repaint();
        }
        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 400);
        }

    }
}