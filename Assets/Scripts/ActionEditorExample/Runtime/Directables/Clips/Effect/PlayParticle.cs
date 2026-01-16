#if UNITY_EDITOR
using System;
using UnityEditor;
#endif
using UnityEngine;

namespace ActionEditor
{
    /// <summary>
    /// 选择对象路径
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ObjectPathAttribute :
#if UNITY_5_3_OR_NEWER
            UnityEngine.PropertyAttribute
#else
        Attribute
#endif
    {
        public Type type;

        public ObjectPathAttribute(Type type)
        {
            this.type = type;
        }
    }
    [CustomPropertyDrawer(typeof(ObjectPathAttribute))]
    public class ObjectPathAttributePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();
            var value = this.attribute as ObjectPathAttribute;
            var type = value.type;
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(property.stringValue);
            var result = EditorGUI.ObjectField(position, label, obj, type, false);
            if (result != obj)
            {
                obj = result;
                property.stringValue = obj ? AssetDatabase.GetAssetPath(obj) : null;
                property.serializedObject.ApplyModifiedProperties();
            }
        }
    }
    [Name("普通粒子片段")]
    [Attachable(typeof(EffectTrack))]
    public class PlayParticle : Clip
    {
        [Space(20)]
        [HeaderAttribute("hhhh")]
        [Name("特效对象")]
        [ObjectPath(typeof(GameObject))]
        public string resPath = "";

        [Name("是否变形")] public bool scale;

        [TextArea(1,4)]
        public string test;

        private GameObject _effectObject;

        private GameObject audioClip
        {
            get
            {
                if (_effectObject == null)
                {
#if UNITY_EDITOR
                    _effectObject = AssetDatabase.LoadAssetAtPath<GameObject>(resPath);
#endif
                }

                return _effectObject;
            }
        }


        public override float Length
        {
            get => length;
            set => length = value;
        }

        public override bool IsValid => !string.IsNullOrEmpty(resPath) && audioClip != null;


    }
}