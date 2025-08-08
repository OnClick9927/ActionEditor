using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActionEditor
{

    class AssetPlayer
    {
        private static AssetPlayer _inst;

        public static AssetPlayer Inst
        {
            get
            {
                if (_inst == null)
                {
                    _inst = new AssetPlayer();
                }

                return _inst;
            }
        }

        private List<IDirectableTimePointer> timePointers;

        /// <summary>
        /// 预览器
        /// </summary>
        private List<IDirectableTimePointer> unsortedStartTimePointers;


        private float currentTime;

        public float previousTime { get; private set; }

        private bool preInitialized;

        public Asset Asset => AppInternal.AssetData;

        /// <summary>
        /// 当前时间
        /// </summary>
        public float CurrentTime
        {
            get => currentTime;
            set => currentTime = Mathf.Clamp(value, 0, Length);
        }

        public float Length
        {
            get
            {
                if (Asset != null)
                {
                    return Asset.Length;
                }

                return 0;
            }
        }

        public void Sample()
        {
            Sample(currentTime);
        }

        public void Sample(float time)
        {
            CurrentTime = time;
            if ((currentTime == 0 || currentTime == Length) && previousTime == currentTime) return;

            if (!preInitialized && currentTime > 0 && previousTime == 0)
            {
                InitializePreviewPointers();
            }


            if (timePointers != null)
            {
                InternalSamplePointers(currentTime, previousTime);
            }

            previousTime = currentTime;
        }

        void InternalSamplePointers(float currentTime, float previousTime)
        {
            if (!Application.isPlaying || currentTime > previousTime)
            {
                foreach (var t in timePointers)
                {
                    t.TriggerForward(currentTime, previousTime);

                }
            }


            if (!Application.isPlaying || currentTime < previousTime)
            {
                for (var i = timePointers.Count - 1; i >= 0; i--)
                {
                    timePointers[i].TriggerBackward(currentTime, previousTime);

                }
            }

            if (unsortedStartTimePointers != null)
            {
                foreach (var t in unsortedStartTimePointers)
                {
                    try
                    {
                        t.Update(currentTime, previousTime);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
            }
        }


        public void InitializePreviewPointers()
        {
            timePointers = new List<IDirectableTimePointer>();
            unsortedStartTimePointers = new List<IDirectableTimePointer>();


            foreach (var group in Asset.groups.AsEnumerable().Reverse())
            {
                if (!group.IsActive) continue;
                foreach (var track in group.Tracks.AsEnumerable().Reverse())
                {
                    if (!track.IsActive) continue;
                    //var tType = track.GetType();
                    {

                        var preview_track = ActonEditorView.GetEditor(track);

                        preview_track.SetTarget(track);
                        var p3 = new StartTimePointer(preview_track);
                        timePointers.Add(p3);

                        unsortedStartTimePointers.Add(p3);
                        timePointers.Add(new EndTimePointer(preview_track));
                    }

                    foreach (var clip in track.Clips)
                    {
                        var cType = clip.GetType();
                        var preview_clip = ActonEditorView.GetEditor(clip);
                        preview_clip.SetTarget(clip);
                        var p3 = new StartTimePointer(preview_clip);
                        timePointers.Add(p3);
                        unsortedStartTimePointers.Add(p3);
                        timePointers.Add(new EndTimePointer(preview_clip));
                    }
                }
            }

            preInitialized = true;
        }
    }
}