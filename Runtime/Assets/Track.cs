using System;
using System.Collections.Generic;
using System.Linq;
//using FullSerializer;
using UnityEngine;
using static ActionEditor.Group;

namespace ActionEditor
{



    [Serializable]
    [Attachable(typeof(Group))]
    public abstract class Track : DirectableBase, IDirectable
    {
        private List<Clip> clips = new List<Clip>();

        [UnityEngine.SerializeField] private List<Temp> Temps;


        public List<Clip> Clips
        {
            get => clips;
            set => clips = value;
        }

        public Group Group => Parent as Group;

        public sealed override IEnumerable<IDirectable> Children => clips;
        public override bool IsCollapsed
        {
            get => Parent != null && Parent.IsCollapsed;
            set { }
        }


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

            Debug.Log($"type={type} newAction={newAction}");

            if (newAction != null)
            {
                // if (!newAction.CanAdd(this)) return null;

                newAction.StartTime = time;
                newAction.Name = type.Name;
                Clips.Add(newAction);
                //newAction.PostCreate(this);

                var nextAction = Clips.FirstOrDefault(a => a.StartTime > newAction.StartTime);
                if (nextAction != null) newAction.EndTime = Mathf.Min(newAction.EndTime, nextAction.StartTime);

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
            clips = this.Temps.ConvertAll(x =>
            {
                return JsonUtility.FromJson(x.json, Asset.GetType(x.type)) as Clip;
            });
        }
        protected override void OnBeforeSerialize()
        {

            Temps = clips.ConvertAll(x => new Temp()
            {
                type = x.GetType().FullName,
                json = JsonUtility.ToJson(x)

            });
        }


    }
}