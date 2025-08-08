using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ActionEditor
{
    public enum AssetPickListType
    {
        Tiled,
        Folder,
    }
    class AssetPick : AdvancedDropdown
    {
        private string[] paths;
        private Action<UnityEngine.Object> itemSelectedCallback;
        private AssetPickListType listType;

        private AssetPick(string[] paths, Action<UnityEngine.Object> itemSelectedCallback, AssetPickListType listType) : base(new AdvancedDropdownState())
        {
            this.paths = paths;
            this.itemSelectedCallback = itemSelectedCallback;
            this.minimumSize = new Vector2(200, 400);
            this.listType = listType;
        }
        public static AssetPick ShowObjectPicker(Rect rect, string folder, string filter, AssetPickListType listType, Action<UnityEngine.Object> itemSelectedCallback,
        Func<string, bool> fit = null)
        {
            var paths = AssetDatabase.FindAssets(filter, new string[] { folder })
                .Select(x => AssetDatabase.GUIDToAssetPath(x)).Where(x =>
                {
                    if (fit != null)
                        return fit(x);
                    return true;
                });
            AssetPick pick = new AssetPick(paths.ToArray(), itemSelectedCallback, listType);
            pick.Show(rect);
            return pick;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            EditorWindow.mouseOverWindow.Close();

            itemSelectedCallback?.Invoke(EditorUtility.InstanceIDToObject(item.id));
            //base.ItemSelected(item);
        }

        private class Temp
        {
            public string name;
            public string path;
            public List<Temp> temps = new List<Temp>();
            public List<string> parentNames = new List<string>();
            public void ReadPath(string path)
            {
                var names = path.Split("/");
                var index = Array.IndexOf(names, name);
                if (index == names.Length - 2)
                {
                    temps.Add(new Temp()
                    {
                        path = path,
                        name = names.Last()
                    });
                }
                else
                {
                    var next_name = names[index + 1];
                    var find = temps.Find(x => x.name == next_name);
                    if (find == null)
                    {
                        find = new Temp() { name = next_name };
                        temps.Add(find);
                    }
                    find.ReadPath(path);
                }
            }


            public void Build(AdvancedDropdownItem parent)
            {


                if (temps.Count == 0)
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                    var now = new AdvancedDropdownItem(Path.GetFileNameWithoutExtension(name))
                    {
                        id = obj.GetInstanceID(),
                        icon = AssetPreview.GetMiniThumbnail(obj),
                        
                    };
                    parent.AddChild(now);
                }
                else
                {
                    bool next = true;
                    foreach (var temp in temps)
                    {
                        if (!string.IsNullOrEmpty(temp.path))
                        {
                            next = false;
                        }
                    }
                    if (!next)
                    {
                        if (parentNames.Count != 0)
                        {
                            name = $"{string.Join("/", parentNames)}/{name}";
                        }
                        AdvancedDropdownItem now = new AdvancedDropdownItem(name);
                        parent.AddChild(now);
                        parent = now;
                    }
                    else
                    {
                        foreach (var item in temps)
                        {
                            item.parentNames.Add(name);
                        }

                    }
                    foreach (var item in temps)
                    {
                        item.Build(parent);
                    }
                }


            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new AdvancedDropdownItem("Assets");
            if (listType == AssetPickListType.Tiled)
            {

                for (int i = 0; i < paths.Length; i++)
                {
                    var path = paths[i];
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(paths[i]);
                    root.AddChild(new AdvancedDropdownItem($"{Path.GetDirectoryName(path)}/{Path.GetFileNameWithoutExtension(path)}")
                    {
                        icon = AssetPreview.GetMiniThumbnail(obj),
                        id = obj.GetInstanceID()

                    });
                }
            }
            else
            {

                Temp _base = new Temp() { name = "Assets" };
                for (int i = 0; i < paths.Length; i++)
                {
                    _base.ReadPath(paths[i]);
                }

                _base.Build(root);
            }
            return root;
        }

    }
}