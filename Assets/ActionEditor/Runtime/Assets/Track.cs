using System;
using System.Collections.Generic;
using System.Linq;


namespace ActionEditor
{



    [Serializable]
    [Attachable(typeof(Group))]
    public abstract class Track : SegmentBase, ISegment
    {



        [ReadOnly][Buffer] public bool isLocked;
        [ReadOnly][Buffer] public bool active = true;
        [Buffer] private List<Clip> clips = new List<Clip>();




        public override sealed bool IsLocked
        {
            get => Parent != null && (Parent.IsLocked || isLocked);
            set => isLocked = value;
        }
        public override sealed bool IsActive
        {
            get => Parent != null && Parent.IsActive && active;
            set
            {
                if (active != value)
                {
                    active = value;
                    if (Root != null) Root.Validate();
                }
            }
        }

        public List<Clip> Clips
        {
            get => clips;
            set => clips = value;
        }

        public Group Group => Parent as Group;

        public sealed override IEnumerable<ISegment> Children => clips;
        //public override bool IsCollapsed
        //{
        //    get => Parent != null && Parent.IsCollapsed;
        //    set { }
        //}
        public sealed override float Length { get => EndTime - StartTime; set { } }


        public sealed override float StartTime
        {
            get => Parent?.StartTime ?? 0;
            set { }
        }


        public sealed override float EndTime
        {
            get => Parent?.EndTime ?? 0;
            set { }
        }

        internal T AddClip<T>(float time) where T : Clip
        {
            return (T)AddClip(typeof(T), time);
        }

        internal Clip AddClip(Type type, float time)
        {


            var newAction = Activator.CreateInstance(type) as Clip;

            //Debug.Log($"type={type} newAction={newAction}");

            if (newAction != null)
            {
                // if (!newAction.CanAdd(this)) return null;

                newAction.StartTime = time;
                //newAction.Name = type.Name;
                Clips.Add(newAction);
                //newAction.PostCreate(this);

                var nextAction = Clips.FirstOrDefault(a => a.StartTime > newAction.StartTime);
                if (nextAction != null) newAction.EndTime = SegmentExtensions.Min(newAction.EndTime, nextAction.StartTime);

                Root.Validate();
                // DirectorUtility.selectedObject = newAction;
            }

            return newAction;
        }

        internal Clip AddClip(Clip clip)
        {
            if (clip != null && clip.CanValidTime(this, clip.StartTime, clip.EndTime))
            {
                // if (!clip.CanAdd(this)) return null;
                if (clip.Parent != null && clip.Parent is Track track)
                {
                    track.DeleteClip(clip);
                }

                Clips.Add(clip);
                Root.Validate();
            }

            return clip;
        }

        internal void DeleteClip(Clip action)
        {
            Clips.Remove(action);
            Root.Validate();
        }





    }
}