using System;
using System.Collections.Generic;




namespace ActionEditor
{
    [Serializable]
    public abstract class Asset : IAction
    {
        public const string FileEx = "action.bytes";



        [Buffer] public List<Group> groups = new List<Group>();
        [Buffer] private float length = 5f;
        [Buffer] private float viewTimeMin;
        [Buffer] private float viewTimeMax = 5f;
        void IBufferObject.WriteField(string id, BufferWriter writer) => WriteField(id, writer);
        void IBufferObject.ReadField(string id, BufferReader reader) => ReadField(id, reader);
        protected virtual void ReadField(string id, BufferReader reader)
        {
            if (id == nameof(groups))
                groups = reader.ReadList(reader.ReadObject<Group>);

        }

        protected virtual void WriteField(string id, BufferWriter writer)
        {
            if (id == nameof(groups))
                writer.WriteList(groups, writer.WriteObject);

        }
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



        public byte[] Serialize()
        {
            this.BeforeSerialize();
            BufferWriter writer = new BufferWriter(1024);
            writer.WriteObject(this);
            return writer.GetValidBuffer();
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


        public static Asset Deserialize(Type type, byte[] buffer)
        {
            BufferReader reader = new BufferReader(buffer);
            var asset = reader.ReadObject<Asset>();
            asset.AfterDeserialize();
            return asset;
        }
        protected virtual void OnAfterDeserialize() { }
        protected virtual void OnBeforeSerialize() { }



        private void AfterDeserialize()
        {

            OnAfterDeserialize();
            Validate();
        }

        private void BeforeSerialize()
        {
            OnBeforeSerialize();
        }


    }
}