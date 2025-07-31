using System;
using System.Collections.Generic;
using System.Linq;
//using FullSerializer;
//using UnityEngine;
using static ActionEditor.Group;

namespace ActionEditor
{



    [Serializable]
    [Attachable(typeof(Group))]
    public abstract class Track : DirectableBase, IDirectable
    {
        [ReadOnly] public bool isLocked;
        [ReadOnly] public bool active = true;
        private List<Clip> clips = new List<Clip>();

        [UnityEngine.SerializeField] private List<Temp> Temps;



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

        public sealed override IEnumerable<IDirectable> Children => clips;
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

        public T AddClip<T>(float time) where T : Clip
        {
            return (T)AddClip(typeof(T), time);
        }

        public Clip AddClip(Type type, float time)
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
                if (nextAction != null) newAction.EndTime = IDirectableExtensions.Min(newAction.EndTime, nextAction.StartTime);

                Root.Validate();
                // DirectorUtility.selectedObject = newAction;
            }

            return newAction;
        }

        public Clip AddClip(Clip clip)
        {
            if (clip != null && clip.CanValidTime(this, clip.StartTime, clip.EndTime))
            {
                // if (!clip.CanAdd(this)) return null;
                if (clip.Parent != null && clip.Parent is Track track)
                {
                    track.DeleteAction(clip);
                }

                Clips.Add(clip);
                Root.Validate();
            }

            return clip;
        }

        public void DeleteAction(Clip action)
        {
            Clips.Remove(action);
            Root.Validate();
        }


        protected override void OnAfterDeserialize()
        {
            Asset.FromTemp(this.Temps, this.clips);
            //clips = this.Temps.ConvertAll(x =>
            //{
            //    var type = Asset.GetType(x.type);
            //    if (type != null)
            //        return JsonUtility.FromJson(x.json, type) as Clip;
            //    return null;
            //});
            //clips.RemoveAll(x => x == null);
        }
        protected override void OnBeforeSerialize()
        {
            Asset.ToTemp(this.Temps, this.clips);

            //Temps = clips.ConvertAll(x => new Temp()
            //{
            //    type = x.GetType().FullName,
            //    json = JsonUtility.ToJson(x)

            //});
        }


    }
}