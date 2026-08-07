using ActionAttribute;
using ActionBuffer;
using ActionEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor.Nodes
{
  internal  static class Prefs
    {
        public static Color GetColor(this object value)
        {
            Type type = value as Type ?? value?.GetType();
            return type == null ? Color.white : data.GetColor(type,
                App.asset?.GetType(), value is NodeData);
        }




        [Serializable]
        public class SerializedData
        {
            public AssetPickListType searchListType = AssetPickListType.Tiled;


            public int AutoSaveSeconds = 10;
            public string SavePath = "Assets";
            [HideInInspector] public string LastAssetPath = string.Empty;
            public Vector2 NodePrettySpacing = new Vector2(200,200);


            [System.Serializable]
            public class ColorPref
            {
                public string type;
                public string ownerType;
                public Color color;
                public List<string> attach;
                [NonSerialized] private bool _null;
                Type _type;
                public Type GetRealType()
                {
                    if (!_null && _type == null)
                    {
                        _type = TypeHelper.GetTypeByFullName(type);
                        _null = _type == null;
                    }
                    return _type;
                }
            }

            [HideInInspector] public List<ColorPref> nodes = new List<ColorPref>();
            [HideInInspector] public List<ColorPref> other = new List<ColorPref>();
            public Color GetColor(Type type, Type graphType, bool nodeColor)
            {
                List<ColorPref> colors = nodeColor ? nodes : other;
                return EnsureColor(colors, type, graphType, nodeColor).color;
            }

            public List<ColorPref> GetNodeColors(Type graphType)
            {
                if (graphType == null) return new List<ColorPref>();
                var metas = EditorEX.GetTypeMetaDerivedFrom(typeof(NodeData));
                for (int i = 0; i < metas.Count; i++)
                {
                    var meta = metas[i];
                    if (!CanAttach(meta.attachableTypes, graphType)) continue;
                    ColorPref color = EnsureColor(nodes, meta.type, graphType,
                        true);
                    color.attach = meta.attachableTypes?.Select(x => x.FullName)
                        .ToList();
                }
                return nodes.Where(x => x.ownerType == graphType.FullName &&
                    x.GetRealType() != null).ToList();
            }

            public List<ColorPref> GetPortColors(Type graphType)
            {
                if (graphType == null) return new List<ColorPref>();
                return other.Where(x => x.ownerType == graphType.FullName &&
                    x.GetRealType() != null).ToList();
            }

            private static bool CanAttach(IReadOnlyList<Type> attachableTypes,
                Type graphType)
            {
                if (attachableTypes == null || graphType == null) return false;
                for (Type current = graphType; current != null;
                    current = current.BaseType)
                    for (int i = 0; i < attachableTypes.Count; i++)
                        if (attachableTypes[i] == current) return true;
                return false;
            }

            private static ColorPref EnsureColor(List<ColorPref> colors,
                Type type, Type ownerType, bool nodeColor)
            {
                string owner = ownerType?.FullName ?? string.Empty;
                ColorPref result = colors.FirstOrDefault(x =>
                    x.type == type.FullName &&
                    (string.IsNullOrEmpty(owner)
                        ? string.IsNullOrEmpty(x.ownerType)
                        : x.ownerType == owner));
                if (result != null) return result;
                ColorPref legacy = colors.FirstOrDefault(x =>
                    x.type == type.FullName && string.IsNullOrEmpty(x.ownerType));
                result = new ColorPref
                {
                    type = type.FullName,
                    ownerType = owner,
                    color = legacy?.color ?? (nodeColor
                        ? UnityEngine.Random.ColorHSV(0.2f, 0.8f)
                        : UnityEngine.Random.ColorHSV()),
                    attach = legacy?.attach == null ? null :
                        new List<string>(legacy.attach)
                };
                colors.Add(result);
                Save();
                return result;
            }
            public void valid()
            {
                var metas = EditorEX.GetTypeMetaDerivedFrom(typeof(NodeData));
                nodes.RemoveAll(x => !metas.Any(y => y.type.FullName == x.type));
                other.RemoveAll(x => x.GetRealType() == null);
                foreach (var meta in metas)
                {
                    var find = nodes.Find(x => x.type == meta.type.FullName &&
                        string.IsNullOrEmpty(x.ownerType));
                    if (find == null)
                    {
                        find = new ColorPref
                        {
                            type = meta.type.FullName,
                            color = UnityEngine.Random.ColorHSV(0.2f, 0.8f),
                        };
                        nodes.Add(find);

                    }
                    List<string> attach = meta.attachableTypes?
                        .Select(x => x.FullName).ToList();
                    for (int i = 0; i < nodes.Count; i++)
                        if (nodes[i].type == meta.type.FullName)
                            nodes[i].attach = attach == null ? null :
                                new List<string>(attach);



                }
                nodes.Sort((a, b) =>
                {

                    int owner = string.CompareOrdinal(a.ownerType, b.ownerType);
                    if (owner != 0) return owner;
                    return string.CompareOrdinal(App.GetNodePath(a.GetRealType()),
                        App.GetNodePath(b.GetRealType()));
                });
                other.Sort((a, b) =>
                {
                    int owner = string.CompareOrdinal(a.ownerType, b.ownerType);
                    if (owner != 0) return owner;
                    return string.CompareOrdinal(EditorEX.GetTypeName(a.GetRealType()),
                        EditorEX.GetTypeName(b.GetRealType()));
                });
            }


        }
        public static void Valid()
        {
            data.valid();
            Save();
        }

        public static SerializedData data =>
            ActionNodeEditorProjectSettings.instance.Data;

        public static readonly float[] snapIntervals = new float[] { 0.001f, 0.01f, 0.1f };
        public static readonly int[] frameRates = new int[] { 24, 25, 30, 60 };


        public static Vector2 NodePrettySpacing
        {
            get => data.NodePrettySpacing;
            set
            {
                if (data.NodePrettySpacing != value)
                {
                    data.NodePrettySpacing = value;
                    //Save();
                }
            }
        }

        
        public static int autoSaveSeconds
        {
            get => data.AutoSaveSeconds;
            set
            {
                if (data.AutoSaveSeconds != value)
                {
                    data.AutoSaveSeconds = value;
                    //Save();
                }
            }
        }

        public static string savePath
        {
            get => data.SavePath;
            set
            {
                if (data.SavePath != value)
                {
                    data.SavePath = value;
                    Save();
                }
            }
        }








        public static AssetPickListType pickListType
        {
            get => data.searchListType;
            set
            {
                if (data.searchListType != value)
                {
                    data.searchListType = value;
                    //Save();
                }
            }
        }




        public static void Save()
        {
            ActionNodeEditorProjectSettings.instance.SaveSettings();
        }

        public static string lastAssetPath
        {
            get => data.LastAssetPath;
            set
            {
                if (data.LastAssetPath == value) return;
                data.LastAssetPath = value ?? string.Empty;
                Save();
            }
        }



    }

}
