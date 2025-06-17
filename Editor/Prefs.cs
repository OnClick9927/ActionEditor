using System;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;


namespace ActionEditor
{
    static class Prefs
    {
        public static readonly string CONFIG_PATH =
            $"{Application.dataPath}/Editor/ActionEditor.txt";

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
            public float SnapInterval = 0.1f;
            public int FrameRate = 30;

            public int AutoSaveSeconds = 10;
            public string SavePath = "Assets";
            public string Lan_key = string.Empty;
            //public bool ScrollWheelZooms = true;

            public bool MagnetSnapping = true;
            public float TrackListLeftMargin = 180f;

 
            [System.Serializable]
            public class ColorPref
            {
                public string type;
                public Color color;
                Type _type;
                public Type GetRealType()
                {
                    if (_type == null)
                        _type = EditorEX.GetAllTypes().First(x => x.FullName == type);
                    return _type;
                }
            }

            public List<ColorPref> clips = new List<ColorPref>();
            public List<ColorPref> tracks = new List<ColorPref>();


            public void valid()
            {
                var types = EditorEX.GetImplementationsOf(typeof(Clip));
                foreach (var type in types)
                {
                    if (clips.Any(x => x.type == type.FullName)) continue;
                    clips.Add(new ColorPref
                    {
                        type = type.FullName,
                        color = Color.white,
                    });
                }
                types = EditorEX.GetImplementationsOf(typeof(Track));
                foreach (var type in types)
                {
                    if (tracks.Any(x => x.type == type.FullName)) continue;
                    tracks.Add(new ColorPref
                    {
                        type = type.FullName,
                        color = Color.white,
                    });
                }
            }

        }
        public static void Valid()
        {
            data.valid();
            Save();
        }

        private static SerializedData _data;

        public static SerializedData data
        {
            get
            {
                if (_data == null)
                {
                    if (!Directory.Exists("Assets/Editor"))
                        Directory.CreateDirectory("Assets/Editor");

                    if (File.Exists(CONFIG_PATH))
                    {
                        var json = File.ReadAllText(CONFIG_PATH);
                        _data = JsonUtility.FromJson<SerializedData>(json);
                    }

                    if (_data == null)
                    {
                        _data = new SerializedData();
                    }
                }

                return _data;
            }
        }

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
                    //Save();
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
            System.IO.File.WriteAllText(CONFIG_PATH, JsonUtility.ToJson(data));
        }



    }
}
