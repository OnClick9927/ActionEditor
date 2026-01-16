using System;
using System.IO;
using System.Linq;
using UnityEditor;

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
        public static void Select(params ISegment[] objs) => AppInternal.Select(objs);
        public static bool IsSelect(ISegment directable) => AppInternal.IsSelect(directable);
        public static ISegment[] SelectItems => AppInternal.SelectItems;


        public static ISegment FistSelect => SelectItems.Length > 0 ? SelectItems.First() : null;


        public static void CopyOrCutAsset(ISegment asset, bool cut) => AppInternal.SetCopyAsset(asset, cut);
        public static void PasteCopyTo(ISegment target) => AppInternal.PasteCopyTo(target);

 

    }
}