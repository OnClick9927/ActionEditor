using ActionEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor.Nodes
{
    static class Prefs
    {
        public static readonly string CONFIG_PATH =
            $"{Application.dataPath}/Editor/NodeGraph.txt";
        public static Color GetColor(this NodeData track)
        {
            if (track is NodeData)
            {

                return Prefs.data.nodes.First(x => x.type == track.GetType().FullName).color;
            }
            return Color.white;
        }




        [Serializable]
        public class SerializedData
        {
            public AssetPickListType searchListType = AssetPickListType.Tiled;


            public int AutoSaveSeconds = 10;
            public string SavePath = "Assets";



            [System.Serializable]
            public class ColorPref
            {
                public string type;
                public Color color;
                public List<string> attach;
                Type _type;
                public Type GetRealType()
                {
                    if (_type == null)
                        _type = EditorEX.GetAllTypes().First(x => x.FullName == type);
                    return _type;
                }
            }

            public List<ColorPref> nodes = new List<ColorPref>();

            public void valid()
            {
                var metas = EditorEX.GetTypeMetaDerivedFrom(typeof(NodeData));
                nodes.RemoveAll(x => !metas.Any(y => y.type.FullName == x.type));
                foreach (var meta in metas)
                {
                    var find = nodes.Find(x => x.type == meta.type.FullName);
                    if (find == null)
                    {                        
                        find = new ColorPref
                        {
                            type = meta.type.FullName,
                            color = UnityEngine.Random.ColorHSV(0.2f,0.8f),
                        };
                        nodes.Add(find);

                    }
                    find.attach = meta.attachableTypes?.Select(x => x.FullName).ToList();



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
            System.IO.File.WriteAllText(CONFIG_PATH, JsonUtility.ToJson(data));
        }



    }

}