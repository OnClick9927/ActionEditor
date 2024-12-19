using System;

namespace ActionEditor
{
    /// <summary>
    /// 自定义检视面板
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomInspectors : Attribute
    {
        public Type InspectedType;
        public bool _editorForChildClasses;

        public CustomInspectors(Type inspectedType)
        {
            InspectedType = inspectedType;
        }

        public CustomInspectors(Type inspectedType, bool editorForChildClasses)
        {
            InspectedType = inspectedType;
            _editorForChildClasses = editorForChildClasses;
        }
    }


    /// <summary>
    /// 自定义片段预览
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomPreviewAttribute : Attribute
    {
        public Type PreviewType;

        public CustomPreviewAttribute(Type type)
        {
            PreviewType = type;
        }
    }

}