using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActionEditor
{
    public static class ClipDrawer
    {
        private static Dictionary<Clip, ClipDrawBase> _clipDraws = new Dictionary<Clip, ClipDrawBase>();

        public static List<IDirectable> GetClips(Rect rect)
        {
            List<IDirectable> list = new List<IDirectable>();
            foreach (var clip in _clipDraws.Keys)
            {
                var draw = _clipDraws[clip];
                if (rect.Overlaps(draw.ClipRealRect))
                {
                    list.Add(clip);
                }
            }

            return list;
        }

        public static bool ClipContainsByRealRect(Vector2 pos)
        {
            foreach (var clip in _clipDraws.Keys)
            {
                var draw = _clipDraws[clip];
                if (draw.ClipRealRect.Contains(pos))
                {
                    return true;
                }
            }

            return false;
        }

        public static Clip GetClipByTrackPosition(IDirectable track, Vector2 mousePosition)
        {
            foreach (var clip in _clipDraws.Keys)
            {
                if (clip.Parent != track) continue;
                var draw = _clipDraws[clip];
                if (draw.ClipRect.Contains(mousePosition))
                {
                    return clip;
                }
            }

            return null;
        }



        public static void Reset()
        {
            List<Clip> clips = new List<Clip>();
            var asset = AppInternal.AssetData;
            if (asset == null) return;
            foreach (var group in asset.groups)
            {
                foreach (var track in group.Tracks)
                {
                    clips.AddRange(track.Clips);
                }
            }

            List<Clip> invalidClips = _clipDraws.Keys.Where(clip => !clips.Contains(clip)).ToList();
            foreach (var clip in invalidClips)
            {
                _clipDraws.Remove(clip);
            }

            foreach (var clip in clips)
            {
                if (!_clipDraws.ContainsKey(clip))
                {
                    var type = typeof(BasicClipDraw);
                    if (clip is ClipSignal)
                    {
                        type = typeof(SignalClipDraw);
                    }

                    _clipDraws[clip] = Activator.CreateInstance(type) as BasicClipDraw;
                }
            }
        }

        public static ClipDrawBase GetDraw<T>(T clip) where T : Clip
        {
            ClipDrawBase result = null;

            _clipDraws.TryGetValue(clip, out result);
            return result;
        }



    }
}