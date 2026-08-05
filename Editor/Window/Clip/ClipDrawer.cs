using System;
using System.Collections.Generic;
using UnityEngine;

namespace ActionEditor
{
    public static class ClipDrawer
    {
        private static Dictionary<Clip, ClipDrawBase> _clipDraws = new Dictionary<Clip, ClipDrawBase>();
        private static readonly HashSet<Clip> ActiveClips = new HashSet<Clip>();
        private static readonly List<Clip> InvalidClips = new List<Clip>();

        public static List<ISegment> GetClips(Rect rect)
        {
            List<ISegment> list = new List<ISegment>();
            foreach (var pair in _clipDraws)
            {
                if (rect.Overlaps(pair.Value.ClipRealRect))
                {
                    list.Add(pair.Key);
                }
            }

            return list;
        }

        public static bool ClipContainsByRealRect(Vector2 pos)
        {
            foreach (var pair in _clipDraws)
            {
                if (pair.Value.ClipRealRect.Contains(pos))
                {
                    return true;
                }
            }

            return false;
        }

        public static Clip GetClipByTrackPosition(ISegment track, Vector2 mousePosition)
        {
            foreach (var pair in _clipDraws)
            {
                var clip = pair.Key;
                if (clip.Parent != track) continue;
                if (pair.Value.ClipRect.Contains(mousePosition))
                {
                    return clip;
                }
            }

            return null;
        }



        public static void Reset()
        {
            ActiveClips.Clear();
            var asset = AppInternal.AssetData;
            if (asset == null) return;
            foreach (var group in asset.groups)
            {
                foreach (var track in group.Tracks)
                {
                    for (int i = 0; i < track.Clips.Count; i++)
                        ActiveClips.Add(track.Clips[i]);
                }
            }

            InvalidClips.Clear();
            foreach (var clip in _clipDraws.Keys)
            {
                if (!ActiveClips.Contains(clip)) InvalidClips.Add(clip);
            }
            for (int i = 0; i < InvalidClips.Count; i++)
                _clipDraws.Remove(InvalidClips[i]);

            foreach (var clip in ActiveClips)
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
