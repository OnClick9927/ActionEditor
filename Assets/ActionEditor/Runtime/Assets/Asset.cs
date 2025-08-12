using System;
using System.Collections.Generic;


//using UnityEngine;
using static ActionEditor.Group;

namespace ActionEditor
{
    [Serializable]
    public abstract class Asset : IAction
    {
        public const string FileEx = "action.bytes";
        //public static bool pretty_json = false;

        [NonSerialized] public List<Group> groups = new List<Group>();
        [UnityEngine.SerializeField] private List<Temp> Temps = new List<Temp>();
        [UnityEngine.SerializeField] private float length = 5f;
        [UnityEngine.SerializeField] private float viewTimeMin;
        [UnityEngine.SerializeField] private float viewTimeMax = 5f;

        public float Length
        {
            get => length;
            set => length = IDirectableExtensions.Max(value, 0.1f);
        }

        public float ViewTimeMin
        {
            get => viewTimeMin;
            set
            {
                if (ViewTimeMax > 0) viewTimeMin = IDirectableExtensions.Min(value, ViewTimeMax - 0.25f);
            }
        }

        public float ViewTimeMax
        {
            get => viewTimeMax;
            set => viewTimeMax = IDirectableExtensions.Max(value, ViewTimeMin + 0.25f, 0);
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

            foreach (var group in groups)
            {
                group.Validate(this, null);
                foreach (var track in group.Children)
                {
                    var _tracks = track as Track;
                    track.Validate(this, group);
                    foreach (var clip in track.Children)
                    {
                        clip.Validate(this, track);
                        if (clip.IsActive && clip.EndTime > t)
                            t = clip.EndTime;
                    }
                }
            }
            Length = t;

        }


        public Group AddGroup(Type type, string name)
        {
            if (!typeof(Group).IsAssignableFrom(type)) return null;
            var newGroup = Activator.CreateInstance(type) as Group;
            if (newGroup != null)
            {
                newGroup.name = name;
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

            newGroup.name = name;
            groups.Add(newGroup);
            Validate();
            return newGroup;
        }



        public string Serialize()
        {
            this.BeforeSerialize();
            return $"{GetType().FullName}\n{IDirectableExtensions.ObjectToJson(this)}";
        }

        public static event Func<string, System.Type> GetTypeByTypeName;

        internal static Type GetType(string typeName)
        {
            Type type = null;
            //#if !UNITY_EDITOR

            if (GetTypeByTypeName != null)
            {
                type = GetTypeByTypeName.Invoke(typeName);
                if (type != null)
                    return type;
            }

            //#endif
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
            type = GetType(typename);
#endif
            var asset = IDirectableExtensions.JsonToObject(serializedState.Remove(0, sps[0].Length), type) as Asset;
            asset.AfterDeserialize();
            return asset;
        }
        protected virtual void OnAfterDeserialize() { }
        protected virtual void OnBeforeSerialize() { }

        internal static void FromTemp<T>(List<Temp> src, List<T> result) where T : class, IDirectable
        {
            result.Clear();
            for (int i = 0; i < src.Count; i++)
            {

                var tem = src[i];
                var type = Asset.GetType(tem.type);
                if (type != null)
                {
                    T t = IDirectableExtensions.JsonToObject(tem.json, type) as T;
                    if (t != null)
                    {
                        result.Add(t);
                    }
                }
            }
            for (int i = 0; i < result.Count; i++)
                result[i].AfterDeserialize();
        }
        internal static void ToTemp<T>(List<Temp> result, List<T> src) where T : class, IDirectable
        {
            for (int i = 0; i < src.Count; i++)
                src[i].BeforeSerialize();
            result.Clear();
            for (int i = 0; i < src.Count; i++)
            {

                var tem = src[i];
                result.Add(new Temp()
                {
                    type = tem.GetType().FullName,
                    json = IDirectableExtensions.ObjectToJson(tem)
                });
            }

        }


        private void AfterDeserialize()
        {
            FromTemp(this.Temps, this.groups);


            //for (int i = 0; i < groups.Count; i++)
            //    (groups[i] as IDirectable).AfterDeserialize();
            OnAfterDeserialize();
            Validate();
        }

        private void BeforeSerialize()
        {
            ToTemp(Temps, this.groups);
            OnBeforeSerialize();
        }
    }
}