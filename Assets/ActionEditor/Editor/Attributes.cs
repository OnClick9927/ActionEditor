using System;

namespace ActionEditor
{
    /// <summary>
    /// 自定义检视面板
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CustomInspectorAttribute : Attribute
    {
        public Type InspectedType;
        public CustomInspectorAttribute(Type inspectedType)
        {
            InspectedType = inspectedType;
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
    [AttributeUsage(AttributeTargets.Class)]

    public class CustomHeaderAttribute : System.Attribute
    {
        public Type InspectedType;

        public CustomHeaderAttribute(Type inspectedType)
        {
            InspectedType = inspectedType;
        }
    }
    [AttributeUsage(AttributeTargets.Class)]

    public class CustomFooterAttribute : System.Attribute
    {
        public Type InspectedType;

        public CustomFooterAttribute(Type inspectedType)
        {
            InspectedType = inspectedType;
        }
    }
}