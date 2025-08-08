using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    public class App
    {
        public static Asset AssetData => AppInternal.AssetData;
        public static string assetPath => AppInternal.assetPath;

        public static event Action OnSave
        {
            add { AppInternal.OnSave += value; }
            remove { AppInternal.OnSave -= value; }
        }
        public static event Action OnPlay
        {
            add { AppInternal.OnPlay += value; }
            remove { AppInternal.OnPlay -= value; }
        }
        public static event Action OnStop
        {
            add { AppInternal.OnStop += value; }
            remove { AppInternal.OnStop -= value; }
        }
        public static bool IsPlay => AppInternal.IsPlay;
        public static bool IsPause => AppInternal.IsPause;
        public static void Play() => AppInternal.Play();

        public static void Pause(bool pause = true) => AppInternal.Pause(pause);

        public static void Stop() => AppInternal.Stop();

        public static void StepForward() => AppInternal.StepForward();

        public static void StepBackward() => AppInternal.StepBackward();


        public static void EditAsset(string path) => AppInternal.OnObjectPickerConfig(path);
        public static void SaveAsset() => AppInternal.SaveAsset();
        public static void Select(params IDirectable[] objs) => AppInternal.Select(objs);
        public static bool IsSelect(IDirectable directable) => AppInternal.IsSelect(directable);
        public static IDirectable[] SelectItems => AppInternal.SelectItems;


        public static IDirectable FistSelect => SelectItems.Length > 0 ? SelectItems.First() : null;


        public static void CopyOrCutAsset(IDirectable asset, bool cut) => AppInternal.SetCopyAsset(asset, cut);
        public static void PasteCopyTo(IDirectable target) => AppInternal.PasteCopyTo(target);

        [InitializeOnLoadMethod]
        //[MenuItem("Tools/Action Editor/Gen type Map", false, 0)]
        static void GenTypeMap()
        {
            string cls = "ActionEditor_ActionTypeMap";
            var cs_file = $"{cls}.cs";
            string path = $"Assets/{cs_file}";

            var paths = AssetDatabase.FindAssets("t:script").Select(x => AssetDatabase.GUIDToAssetPath(x));
            var find = paths.FirstOrDefault(x => x.EndsWith(cs_file));
            path = find ?? path;
            var types = EditorEX.GetImplementationsOf(typeof(IAction));
            string add = "\n";

            foreach (var type in types) {
                add += $"{{ \"{type.FullName}\",typeof({type.FullName}) }},\n";
            }

            string text = $"namespace ActionEditor {{ public class {cls} {{\n" +
                $"private static System.Collections.Generic.Dictionary<string,System.Type> map=new System.Collections.Generic.Dictionary<string,System.Type>(){{{add}}};" +
                "\n#if UNITY_EDITOR\n        [UnityEditor.InitializeOnLoadMethod] \n#endif\n" +
                "public static void Init() => Asset.GetTypeByTypeName += Asset_GetTypeByTypeName;\n" +
                "private static System.Type Asset_GetTypeByTypeName(string name)\n" +
                "{System.Type type = null;map.TryGetValue(name, out type);return type; }\n" +
                "}}";
            File.WriteAllText(path, text);
            AssetDatabase.Refresh();
            //Asset.GetTypeByTypeName
        }

    }
}