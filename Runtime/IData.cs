using System.Collections.Generic;

namespace ActionEditor
{
    public interface IData
    {
        
    }

    public interface IDirectable : IData
    {
        IDirector Root { get; }
        IDirectable Parent { get; }
        IEnumerable<IDirectable> Children { get; }

        //GameObject Actor { get; }
        string Name { get; }

        bool IsActive { get; set; }
        bool IsCollapsed { get; set; }
        bool IsLocked { get; set; }

        float StartTime { get; }
        float EndTime { get; }

        float BlendIn { get; set; }
        float BlendOut { get; set; }
        bool CanCrossBlend { get; }

        void Validate(IDirector root, IDirectable parent);
    }


    public interface IClip : IDirectable
    {
        object AnimatedParametersTarget { get; }
    }
    public interface IDirector : IData
    {
        float Length { get; }

        public float ViewTimeMin { get; set; }
        public float ViewTimeMax { get; set; }

        public float ViewTime { get; }

        public float RangeMin { get; set; }
        public float RangeMax { get; set; }

        void DeleteGroup(Group group);

        void UpdateMaxTime();
        void Validate();
    }

    public interface ISubClipContainable : IDirectable
    {
        float SubClipOffset { get; set; }

        float SubClipSpeed { get; }

        float SubClipLength { get; }
    }

}