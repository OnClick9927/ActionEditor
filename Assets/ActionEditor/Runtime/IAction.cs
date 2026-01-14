using System.Collections.Generic;

namespace ActionEditor
{
    public interface IAction:IBufferObject {
        float Length { get; set; }
        float StartTime { get; }
        float EndTime { get; }
    }

    public interface IDirectable : IAction
    {
        Asset Root { get; }
        IDirectable Parent { get; }
        IEnumerable<IDirectable> Children { get; }

        //string Name { get; set; }

        bool IsActive { get; set; }
        //bool IsCollapsed { get; set; }
        bool IsLocked { get; set; }



        void Validate(Asset root, IDirectable parent);
        //void AfterDeserialize();

        //void BeforeSerialize();
    }

    public interface IClip : IDirectable { }
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