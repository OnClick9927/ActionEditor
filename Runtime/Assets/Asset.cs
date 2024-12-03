using System;
using System.Collections.Generic;
using System.Linq;
//using FullSerializer;
using UnityEngine;

namespace ActionEditor
{
    [Serializable]
    public abstract class Asset : IDirector
    {
        [HideInInspector][SerializeReference] public List<Group> groups = new();
        [SerializeField] private float length = 5f;
        [SerializeField] private float viewTimeMin;
        [SerializeField] private float viewTimeMax = 5f;

        [SerializeField] private float rangeMin;
        [SerializeField] private float rangeMax = 5f;

        public Asset()
        {
            Init();
        }


       /* [fsIgnore]*/ public List<IDirectable> directables { get; private set; }

        public float Length
        {
            get => length;
            set => length = Mathf.Max(value, 0.1f);
        }

        public float ViewTimeMin
        {
            get => viewTimeMin;
            set
            {
                if (ViewTimeMax > 0) viewTimeMin = Mathf.Min(value, ViewTimeMax - 0.25f);
            }
        }

        public float ViewTimeMax
        {
            get => viewTimeMax;
            set => viewTimeMax = Mathf.Max(value, ViewTimeMin + 0.25f, 0);
        }


        public float ViewTime => ViewTimeMax - ViewTimeMin;

        public float RangeMin
        {
            get => rangeMin;
            set
            {
                rangeMin = value;
                if (rangeMin < 0) rangeMin = 0;
            }
        }

        public float RangeMax
        {
            get => rangeMax;
            set
            {
                rangeMax = value;
                if (rangeMax < length) rangeMax = length;
            }
        }


        public void UpdateMaxTime()
        {
            var t = 0f;
            foreach (var group in groups)
            {
                if (!group.IsActive) continue;
                foreach (var track in group.Tracks)
                {
                    if (!track.IsActive) continue;
                    foreach (var clip in track.Clips)
                        if (clip.EndTime > t)
                            t = clip.EndTime;
                }
            }

            Length = t;
        }

        public void DeleteGroup(Group group)
        {
            groups.Remove(group);
            Validate();
        }

        public void Validate()
        {
            directables = new List<IDirectable>();
            foreach (IDirectable group in groups.AsEnumerable().Reverse())
            {
                directables.Add(group);
                try
                {
                    group.Validate(this, null);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }

                foreach (var track in group.Children.Reverse())
                {
                    directables.Add(track);
                    try
                    {
                        track.Validate(this, group);
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }

                    foreach (var clip in track.Children)
                    {
                        directables.Add(clip);
                        try
                        {
                            clip.Validate(this, track);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
            }
            UpdateMaxTime();
        }

        public Group AddGroup(Type type)
        {
            if (!typeof(Group).IsAssignableFrom(type)) return null;
            var newGroup = Activator.CreateInstance(type) as Group;
            if (newGroup != null)
            {
                newGroup.Name = "New Group";
                groups.Add(newGroup);
                Validate();
            }

            return newGroup;
        }

        public T AddGroup<T>(string name = "") where T : Group, new()
        {
            var newGroup = new T();
            if (string.IsNullOrEmpty(name))
            {
                name = newGroup.GetType().Name;
            }

            newGroup.Name = name;
            groups.Add(newGroup);
            Validate();
            return newGroup;
        }


        public void Init()
        {
            Validate();
        }
        public string Serialize()
        {
            return $"{GetType().FullName}\n{JsonUtility.ToJson(this, true)}";
        }

        public static Asset Deserialize(Type type, string serializedState)
        {
            var sps = serializedState.Split('\n');
            var typename = sps[0];
#if UNITY_EDITOR
            type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == typename);
#endif
            return JsonUtility.FromJson(serializedState.Remove(0, sps[0].Length), type) as Asset;
        }
    }
}