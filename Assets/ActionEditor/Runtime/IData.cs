using System.Collections.Generic;

namespace ActionEditor
{
    public interface IData { }

    public interface IDirectable : IData
    {
        Asset Root { get; }
        IDirectable Parent { get; }
        IEnumerable<IDirectable> Children { get; }

        string Name { get; }

        bool IsActive { get; set; }
        bool IsCollapsed { get; set; }
        bool IsLocked { get; set; }

        float StartTime { get; }
        float EndTime { get; }
        float Length { get; set; }

        void Validate(Asset root, IDirectable parent);
        void AfterDeserialize();

        void BeforeSerialize();
    }

    public interface IClip : IDirectable
    {
 
    }
    public interface IResizeAble : IClip { }
    public interface IBlendAble : IClip
    {
        float BlendIn { get; set; }
        float BlendOut { get; set; }
    }
    public interface ILengthMatchAble : IClip
    {
        float MatchAbleLength { get; }
    }
}