﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ActionEditor
{
    [Serializable]
    public abstract class Asset : IDirector
    {
        public const string FileEx = "action.json";


        [HideInInspector] public List<Group> groups = new List<Group>();
        [SerializeField] private float length = 5f;
        [SerializeField] private float viewTimeMin;
        [SerializeField] private float viewTimeMax = 5f;

        public Asset()
        {
            Init();
        }
        public List<IDirectable> directables { get; private set; }

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
                group.Validate(this, null);
                foreach (var track in group.Children.Reverse())
                {
                    directables.Add(track);
                    track.Validate(this, group);
                    foreach (var clip in track.Children)
                    {
                        directables.Add(clip);
                        clip.Validate(this, track);
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
            this.BeforeSerialize();
            return $"{GetType().FullName}\n{JsonUtility.ToJson(this, false)}";
        }

        public static event Func<string, System.Type> GetTypeByTypeName;

        internal static Type GetType(string typeName)
        {
            Type type = null;
#if !UNITY_EDITOR

            if (GetTypeByTypeName != null)
            {
                type = GetTypeByTypeName.Invoke(typeName);
                if (type != null)
                    return type;
            }

#endif
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }


        public static Asset Deserialize(Type type, string serializedState)
        {
            var sps = serializedState.Split('\n');
            var typename = sps[0];
#if UNITY_EDITOR
            type = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == typename);
#endif
            var asset = JsonUtility.FromJson(serializedState.Remove(0, sps[0].Length), type) as Asset;
            asset.AfterDeserialize();
            return asset;
        }
        protected virtual void OnAfterDeserialize() { }
        protected virtual void OnBeforeSerialize() { }
        private void AfterDeserialize()
        {

            for (int i = 0; i < groups.Count; i++)
                (groups[i] as IDirectable).AfterDeserialize();
            OnAfterDeserialize();
        }

        private void BeforeSerialize()
        {
            for (int i = 0; i < groups.Count; i++)
                (groups[i] as IDirectable).BeforeSerialize();
            OnBeforeSerialize();
        }
    }
}