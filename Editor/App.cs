using System;
using System.Linq;
using UnityEngine;

namespace ActionEditor
{
    public class App
    {
        public static Asset AssetData => AppInternal.AssetData;

        public static TextAsset TextAsset => AppInternal.TextAsset;
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


        public static void EditAsset(TextAsset asset) => AppInternal.OnObjectPickerConfig(asset);
        public static void SaveAsset() => AppInternal.SaveAsset();
        public static void Select(params IDirectable[] objs) => AppInternal.Select(objs);
        public static bool IsSelect(IDirectable directable) => AppInternal.IsSelect(directable);
        public static IDirectable[] SelectItems => AppInternal.SelectItems;


        public static IDirectable FistSelect => SelectItems.Length > 0 ? SelectItems.First() : null;


        public static void CopyOrCutAsset(IDirectable asset, bool cut) => AppInternal.SetCopyAsset(asset, cut);
        public static void PasteCopyTo(IDirectable target) => AppInternal.PasteCopyTo(target);




    }
}