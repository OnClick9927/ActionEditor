using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static ActionEditor.Group;

namespace ActionEditor
{
    [Serializable]
    public abstract class Asset : IAction
    {
        public const string FileEx = "action.bytes";

        [UnityEngine.SerializeField] private List<Temp> Temps;

        [NonSerialized][HideInInspector] public List<Group> groups = new List<Group>();
        [SerializeField] private float length = 5f;
        [SerializeField] private float viewTimeMin;
        [SerializeField] private float viewTimeMax = 5f;

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

        public float StartTime => 0;

        public float EndTime => Length;

        public void DeleteGroup(Group group)
        {
            groups.Remove(group);
            Validate();
        }

        public void Validate()
        {
            var t = 0f;

            foreach (IDirectable group in groups)
            {
                group.Validate(this, null);
                foreach (var track in group.Children)
                {
                    track.Validate(this, group);
                    foreach (var clip in track.Children)
                    {
                        clip.Validate(this, track);
                        if (group.IsActive && track.IsActive && clip.IsActive && clip.EndTime > t)
                            t = clip.EndTime;
                    }
                }
            }
            Length = t;

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
#if UNITY_EDITOR
            var typename = sps[0].Trim();
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
            groups = this.Temps.ConvertAll(x =>
            {
                var type = Asset.GetType(x.type);
                if (type != null)
                    return JsonUtility.FromJson(x.json, type) as Group;
                return null;
            });
            groups.RemoveAll(x => x == null);

            for (int i = 0; i < groups.Count; i++)
                (groups[i] as IDirectable).AfterDeserialize();
            OnAfterDeserialize();
            Validate();
        }

        private void BeforeSerialize()
        {
            for (int i = 0; i < groups.Count; i++)
                (groups[i] as IDirectable).BeforeSerialize();
            Temps = groups.ConvertAll(x => new Temp()
            {
                type = x.GetType().FullName,
                json = JsonUtility.ToJson(x)
            });

            OnBeforeSerialize();
        }
    }
}