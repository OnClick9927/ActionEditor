using System;
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
        public static void Play() => AppInternal.Play();

        public static void Pause(bool pause = true) => AppInternal.Pause(pause);

        public static void Stop() => AppInternal.Stop();

        public static void StepForward() => AppInternal.StepForward();

        public static void StepBackward() => AppInternal.StepBackward();
    }
}