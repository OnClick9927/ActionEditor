using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ActionBuffer;
using ActionAttribute;


namespace ActionEditor
{
    static class Prefs
    {
        [Serializable]
        public enum TimeStepMode
        {
            Seconds,
            Frames
        }



        [Serializable]
        public class SerializedData
        {
            public TimeStepMode TimeStepMode = TimeStepMode.Seconds;
            public AssetPickListType searchListType = AssetPickListType.Tiled;
            public float SnapInterval = 0.1f;
            public int FrameRate = 30;

            public int AutoSaveSeconds = 10;
            public string SavePath = "Assets";
            [HideInInspector] public string Lan_key = string.Empty;
            [HideInInspector] public string LastAssetPath = string.Empty;
            //public bool ScrollWheelZooms = true;

            public bool MagnetSnapping = true;
            public float TrackListLeftMargin = 180f;


            [System.Serializable]
            public class ColorPref
            {
                public string assetType;
                public string type;
                public Color color;
                public List<string> attach;
                public List<string> asset;
                Type _type;
                public Type GetRealType()
                {
                    if (_type == null)
                        _type = TypeHelper.GetTypeByFullName(type);
                    return _type;
                }
            }

            [HideInInspector] public List<ColorPref> clips = new List<ColorPref>();
            [HideInInspector] public List<ColorPref> tracks = new List<ColorPref>();

            public void valid()
            {
                var metas = EditorEX.GetTypeMetaDerivedFrom(typeof(Clip));
                clips.RemoveAll(x => !metas.Any(y => y.type.FullName == x.type));
                foreach (var meta in metas)
                {
                    var find = clips.Find(x => x.type == meta.type.FullName &&
                        string.IsNullOrEmpty(x.assetType));
                    if (find == null)
                    {
                        find = new ColorPref
                        {
                            type = meta.type.FullName,
                            color = UnityEngine.Random.ColorHSV(0.2f, 0.8f),
                        };
                        clips.Add(find);

                    }
                    List<string> attach = meta.attachableTypes?
                        .Select(x => x.FullName).ToList();
                    for (int i = 0; i < clips.Count; i++)
                        if (clips[i].type == meta.type.FullName)
                            clips[i].attach = attach == null
                                ? null : new List<string>(attach);



                }
                metas = EditorEX.GetTypeMetaDerivedFrom(typeof(Track));
                tracks.RemoveAll(x => !metas.Any(y => y.type.FullName == x.type));

                var metas_group = EditorEX.GetTypeMetaDerivedFrom(typeof(Group));
                var metas_asset = EditorEX.GetTypeMetaDerivedFrom(typeof(Asset));


                foreach (var meta in metas)
                {
                    var find = tracks.Find(x => x.type == meta.type.FullName &&
                        string.IsNullOrEmpty(x.assetType));
                    if (find == null)
                    {
                        find = new ColorPref
                        {
                            type = meta.type.FullName,
                            color = Color.white,
                        };
                        tracks.Add(find);
                    }

                    List<string> attach = meta.attachableTypes?
                        .Select(x => x.FullName).ToList();
                    List<string> assets = null;
                    if (attach != null && attach.Count > 0)
                    {
                        var groups = attach.Select(x => metas_group.Find(y =>
                                y.type.FullName == x))
                            .Where(x => x.type != null &&
                                x.attachableTypes != null);
                        assets = groups.SelectMany(x => x.attachableTypes)
                            .Select(x => metas_asset.Find(y => y.type == x))
                            .Where(x => x.type != null)
                            .Select(x => x.type.FullName).ToList();
                    }
                    for (int i = 0; i < tracks.Count; i++)
                    {
                        if (tracks[i].type != meta.type.FullName) continue;
                        tracks[i].attach = attach == null
                            ? null : new List<string>(attach);
                        tracks[i].asset = assets == null
                            ? null : new List<string>(assets);
                    }
                }
            }

            internal ColorPref GetColor(Type valueType, Type ownerType,
                bool isClip)
            {
                List<ColorPref> source = isClip ? clips : tracks;
                string valueName = valueType.FullName;
                string ownerName = ownerType?.FullName ?? string.Empty;
                ColorPref result = source.Find(x => x.type == valueName &&
                    x.assetType == ownerName);
                if (result != null) return result;

                ColorPref template = source.Find(x => x.type == valueName &&
                    string.IsNullOrEmpty(x.assetType));
                result = new ColorPref
                {
                    assetType = ownerName,
                    type = valueName,
                    color = template?.color ?? Color.white,
                    attach = template?.attach == null ? null :
                        new List<string>(template.attach),
                    asset = template?.asset == null ? null :
                        new List<string>(template.asset)
                };
                source.Add(result);
                Save();
                return result;
            }

            internal List<ColorPref> GetTrackColors(Type ownerType)
            {
                var result = new List<ColorPref>();
                int count = tracks.Count;
                for (int i = 0; i < count; i++)
                {
                    ColorPref template = tracks[i];
                    if (!string.IsNullOrEmpty(template.assetType) ||
                        !AppliesToAsset(template, ownerType)) continue;
                    Type type = template.GetRealType();
                    if (type != null)
                        result.Add(GetColor(type, ownerType, false));
                }
                return result;
            }

            internal List<ColorPref> GetClipColors(Type ownerType,
                string trackType)
            {
                var result = new List<ColorPref>();
                int count = clips.Count;
                for (int i = 0; i < count; i++)
                {
                    ColorPref template = clips[i];
                    if (!string.IsNullOrEmpty(template.assetType) ||
                        template.attach == null ||
                        !template.attach.Contains(trackType)) continue;
                    Type type = template.GetRealType();
                    if (type != null)
                        result.Add(GetColor(type, ownerType, true));
                }
                return result;
            }

            private static bool AppliesToAsset(ColorPref value, Type assetType)
            {
                if (value.asset == null || assetType == null) return false;
                for (Type current = assetType; current != null &&
                    current != typeof(object); current = current.BaseType)
                    if (value.asset.Contains(current.FullName)) return true;
                return false;
            }


        }
        public static void Valid()
        {
            data.valid();
            Save();
        }

        public static SerializedData data =>
            ActionEditorProjectSettings.instance.Data;

        public static readonly float[] snapIntervals = new float[] { 0.001f, 0.01f, 0.1f };
        public static readonly int[] frameRates = new int[] { 24, 25, 30, 60 };

        //public static bool scrollWheelZooms
        //{
        //    get => data.ScrollWheelZooms;
        //    set
        //    {
        //        if (data.ScrollWheelZooms != value)
        //        {
        //            data.ScrollWheelZooms = value;
        //            Save();
        //        }
        //    }
        //}
        public static string Lan_key
        {
            get => data.Lan_key;
            set
            {
                if (data.Lan_key != value)
                {
                    data.Lan_key = value;
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


        public static bool MagnetSnapping
        {
            get => data.MagnetSnapping;
            set
            {
                if (data.MagnetSnapping != value)
                {
                    data.MagnetSnapping = value;
                    //Save();
                }
            }
        }

        public static float trackListLeftMargin
        {
            get => data.TrackListLeftMargin;
            set
            {
                if (Math.Abs(data.TrackListLeftMargin - value) > 0.001f)
                {
                    data.TrackListLeftMargin = value;
                    //Save();
                }
            }
        }

        public static TimeStepMode timeStepMode
        {
            get => data.TimeStepMode;
            set
            {
                if (data.TimeStepMode != value)
                {
                    data.TimeStepMode = value;
                    FrameRate = value == TimeStepMode.Frames ? 30 : 10;
                    //Save();
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

        public static int FrameRate
        {
            get => data.FrameRate;
            set
            {
                if (data.FrameRate != value)
                {
                    data.FrameRate = value;
                    SnapInterval = 1f / value;
                    //Save();
                }
            }
        }

        public static float SnapInterval
        {
            get => Mathf.Max(data.SnapInterval, 0.001f);
            set
            {
                if (Math.Abs(data.SnapInterval - value) > 0.001f)
                {
                    data.SnapInterval = Mathf.Max(value, 0.001f);
                    //Save();
                }
            }
        }

        public static void Save()
        {
            ActionEditorProjectSettings.instance.SaveSettings();
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
