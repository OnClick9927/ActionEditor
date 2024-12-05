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

        string Name { get; }

        bool IsActive { get; set; }
        bool IsCollapsed { get; set; }
        bool IsLocked { get; set; }

        float StartTime { get; }
        float EndTime { get; }

        void Validate(IDirector root, IDirectable parent);
        void AfterDeserialize();

        void BeforeSerialize();
    }

    public interface IClip : IDirectable
    {
        float BlendIn { get; set; }
        float BlendOut { get; set; }
        bool CanCrossBlend { get; }
    }
    public interface IDirector : IData
    {
        float Length { get; }

        float ViewTimeMin { get; set; }
        float ViewTimeMax { get; set; }

        float ViewTime { get; }


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