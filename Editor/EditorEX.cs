using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    public static class EditorEX
    {
        private static readonly Dictionary<Type, Texture2D> _iconDictionary = new Dictionary<Type, Texture2D>();
        private static readonly Dictionary<Type, string> _nameDictionary = new Dictionary<Type, string>();

        public static Texture2D GetIcon(this object track)
        {
            var type = track.GetType();
            if (_iconDictionary.TryGetValue(type, out var icon))
            {
                return icon;
            }

            var att = track.GetType().GetCustomAttribute<IconAttribute>(true);

            if (att != null)
            {

                if (!string.IsNullOrEmpty(att.iconPath))
                {
                    if (att.iconPath.StartsWith("Assets/"))
                        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(att.iconPath);
                    else
                        icon = Resources.Load(att.iconPath) as Texture2D;
                    if (icon == null)
                        icon = EditorGUIUtility.FindTexture(att.iconPath);
                }
                else if (icon == null)
                    icon = AssetPreview.GetMiniTypeThumbnail(att.fromType);

            }

            if (icon != null)
                _iconDictionary[type] = icon;
            return icon;
        }
        public static string GetTypeName(Type type)
        {

            if (_nameDictionary.TryGetValue(type, out var name))
                return name;
            var nameAttribute = type.GetCustomAttribute<NameAttribute>();
            _nameDictionary[type] = nameAttribute != null ? nameAttribute.name : type.Name;
            return _nameDictionary[type];
        }
        public static string GetTypeName(this object track) => GetTypeName(track.GetType());
        public static void DrawDashedLine(float x, float startY, float endY, Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;

            var totalLength = Mathf.Abs(endY - startY);
            var dashes = Mathf.FloorToInt(totalLength / 10); // 每段长度为10

            for (var i = 0; i < dashes; i++)
            {
                var t1 = (float)i / dashes;
                var t2 = (i + 0.5f) / dashes;
                var point1Y = Mathf.Lerp(startY, endY, t1);
                var point2Y = Mathf.Lerp(startY, endY, t2);

                Handles.DrawLine(new Vector2(x, point1Y), new Vector2(x, point2Y));
            }

            Handles.EndGUI();
        }

        public static Color WithAlpha(this Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static IEnumerable<Type> GetAllTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes());

        }
        public static Type[] GetImplementationsOf(Type type)
        {
            if (type.IsInterface)
                return GetAllTypes().Where(x => x.GetInterfaces().Count(y => y == type) != 0).Where(x => !x.IsAbstract).ToArray();
            return GetAllTypes().Where(x => type.IsAssignableFrom(x)).Where(x => !x.IsAbstract).ToArray();

        }



        public struct TypeMetaInfo
        {
            public Type type;
            public string name;
            //public string category;
            public Type[] attachableTypes;
            //public bool isUnique;
        }






        /// <summary>
        /// 用于选择列表中任何元素而不添加NONE的通用弹出窗口
        /// </summary>
        public static T CleanPopup<T>(string prefix, T selected, List<T> options, params GUILayoutOption[] GUIOptions)
        {
            var index = -1;
            if (options.Contains(selected))
            {
                index = options.IndexOf(selected);
            }

            var stringedOptions = options.Select(o => o != null ? o.ToString() : "NONE");

            using (new EditorGUI.DisabledScope(options.Count <= 0))

            {
                if (!string.IsNullOrEmpty(prefix))
                    index = EditorGUILayout.Popup(prefix, index, stringedOptions.ToArray(), GUIOptions);
                else index = EditorGUILayout.Popup(index, stringedOptions.ToArray(), GUIOptions);
            }

            return index == -1 ? default(T) : options[index];
        }

        /// <summary>
        /// 获取当前加载的集合中基类型的所有非抽象派生类
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static List<TypeMetaInfo> GetTypeMetaDerivedFrom(Type baseType)
        {
            var infos = new List<TypeMetaInfo>();
            foreach (var type in EditorEX.GetImplementationsOf(baseType))
            {
                if (type.GetCustomAttributes(typeof(System.ObsoleteAttribute), true).FirstOrDefault() != null)
                {
                    continue;
                }

                var info = new TypeMetaInfo
                {
                    type = type,
                    name = GetTypeName(type),
                };



                if (type.GetCustomAttributes(typeof(AttachableAttribute), true).FirstOrDefault() is AttachableAttribute
                    attachAtt)
                {
                    info.attachableTypes = attachAtt.Types;
                }

                //info.isUnique = type.IsDefined(typeof(UniqueTrackAttribute), true);

                infos.Add(info);
            }

            infos = infos.OrderBy(i => i.name).ToList();
            return infos;
        }



        public static Editor CreateEditor(object target) => DrawerObject.CreateEditor(target);


        [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        public class ReadOnlyPropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUI.PropertyField(position, property, label);
            }
        }
        [CustomPropertyDrawer(typeof(NameAttribute))]
        public class NamePropertyDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var value = this.attribute as NameAttribute;
                EditorGUI.PropertyField(position, property, new GUIContent(value.name));
            }
        }



        class DrawerObject : UnityEngine.ScriptableObject
        {
            public static Editor CreateEditor(object target)
            {
                sto = sto ?? DrawerObject.CreateInstance<DrawerObject>();
                sto.hideFlags = HideFlags.DontSave;
                sto.obj = target;
                if (editor == null) editor = Editor.CreateEditor(sto);
                return editor;
            }
            [SerializeReference]
            public object obj;
            private static DrawerObject sto;
            private static Editor editor;
        }
        [CustomEditor(typeof(DrawerObject))]
        class DrawerObjectEditor : Editor
        {
            public static List<SerializedProperty> GetDirectChildProperties(SerializedProperty parentProp)
            {
                List<SerializedProperty> childProps = new List<SerializedProperty>();
                if (parentProp == null || !parentProp.hasChildren) return childProps;

                // 重置到第一个子属性
                SerializedProperty childProp = parentProp.Copy();
                bool hasNext = childProp.Next(true);

                while (hasNext)

                {
                    // 终止条件：遍历到当前父属性的同级属性时，停止遍历
                    if (childProp.propertyPath == parentProp.propertyPath)
                    {
                        break;
                    }

                    childProps.Add(childProp.Copy()); // 必须Copy！否则后续Next会改变当前引用
                    hasNext = childProp.Next(false);
                }
                return childProps;
            }
            public override void OnInspectorGUI()
            {
                this.serializedObject.Update();
                var p = this.serializedObject.FindProperty(nameof(DrawerObject.obj));
                var children = GetDirectChildProperties(p);

                foreach (var item in children)
                {
                    EditorGUILayout.PropertyField(item);
                }
                this.serializedObject.ApplyModifiedProperties();
            }
        }
    }
}