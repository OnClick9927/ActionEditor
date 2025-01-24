using System;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ActionEditor
{
    class AssetPick : AdvancedDropdown
    {
        private string[] paths;
        private Action<UnityEngine.Object> itemSelectedCallback;

        private AssetPick(string[] paths, Action<UnityEngine.Object> itemSelectedCallback) : base(new AdvancedDropdownState())
        {
            this.paths = paths;
            this.itemSelectedCallback = itemSelectedCallback;
        }
        public static void ShowObjectPicker(Rect rect, string folder, string filter, Action<UnityEngine.Object> itemSelectedCallback,
        Func<string, bool> fit = null)
        {
            var paths = AssetDatabase.FindAssets(filter, new string[] { folder })
                .Select(x => AssetDatabase.GUIDToAssetPath(x)).Where(x =>
                {
                    if (fit != null)
                        return fit(x);
                    return true;
                });
            AssetPick pick = new AssetPick(paths.ToArray(), itemSelectedCallback);
            pick.Show(rect);
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            EditorWindow.mouseOverWindow.Close();
            itemSelectedCallback?.Invoke(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item.name));
            //base.ItemSelected(item);
        }
        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Assets");
            for (int i = 0; i < paths.Length; i++)
            {
                root.AddChild(new AdvancedDropdownItem(paths[i]));
            }
            return root;
        }
    }
}