using System;
using System.Collections.Generic;
using System.Linq;


namespace ActionEditor
{
    [Serializable]
    public abstract class Group : SegmentBase
    {



        [Buffer] public string name;
        [ReadOnly][Buffer] public bool isCollapsed;
        [ReadOnly][Buffer] public bool isLocked;
        [ReadOnly][Buffer] public bool active = true;

        [Buffer] private List<Track> tracks = new List<Track>();



        public List<Track> Tracks
        {
            get => tracks;
            set => tracks = value;
        }





        public override sealed bool IsLocked
        {
            get => isLocked;
            set => isLocked = value;
        }
        public override sealed bool IsActive
        {
            get => active;
            set
            {
                if (active != value)
                {
                    active = value;
                    if (Root != null) Root.Validate();
                }
            }
        }
        public sealed override IEnumerable<ISegment> Children => Tracks;


        //public bool IsCollapsed { get => isCollapsed; set => isCollapsed = value; }
        public override float StartTime { get => 0; set { } }
        public override float EndTime { get => ((IAction)Root).Length; set { } }
        public sealed override float Length { get => EndTime - StartTime; set { } }
        //public bool ExistSameTypeTrack(Type type) => Tracks.FirstOrDefault(t => t.GetType() == type) != null;

        internal T AddTrack<T>(T track) where T : Track
        {
            if (track != null)
            {
                var parent = track.Parent;
                // if (!clip.CanAdd(this)) return null;

                if (parent != null && parent is Group group)
                {
                    group.DeleteTrack(track);
                }
                Tracks.Add(track);

                //Clips.Add(clip);
                Root.Validate();
            }

            return track;
        }
        internal T AddTrack<T>(string _name = null) where T : Track => (T)AddTrack(typeof(T), _name);

        internal Track AddTrack(Type type, string _name = null)
        {
            var newTrack = Activator.CreateInstance(type);
            if (newTrack is Track track)
            {
                // if (!track.CanAdd(this)) return null;
                //track.Name = type.Name;
                Tracks.Add(track);

                Root?.Validate();

                return track;
            }

            return null;
        }

        internal int InsertTrack<T>(T track, int index) where T : Track
        {
            if (tracks.Contains(track))
            {
                DeleteTrack(track);
            }

            if (index >= tracks.Count)
            {
                index = tracks.Count;
                tracks.Add(track);
            }
            else
            {
                if (index < 0) index = 0;
                tracks.Insert(index, track);
            }

            Root?.Validate();
            return index;
        }

        internal void DeleteTrack(Track track)
        {
            Tracks.Remove(track);
            Root?.Validate();
        }

        internal int GetTrackIndex(Track track) => tracks.FindIndex(t => t == track);






    }
}