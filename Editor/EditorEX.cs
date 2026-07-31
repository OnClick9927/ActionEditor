using ActionBuffer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    public static class EditorEX
    {
        private static readonly Dictionary<Type, Texture2D> _iconDictionary = new Dictionary<Type, Texture2D>();
        private static readonly Dictionary<Type, string> _nameDictionary = new Dictionary<Type, string>();
        private static Dictionary<Type, UnityEngine.Object> scriptObjs = new Dictionary<Type, UnityEngine.Object>();

        public static void DrawPingScript(Type type)
        {
            if (!scriptObjs.TryGetValue(type, out var obj))
            {
                var path = EditorEX.LocateScript(type);
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                scriptObjs[type] = obj;
            }
            if (obj != null)
            {
                GUILayout.Space(10);
                GUI.enabled = false;
                EditorGUILayout.ObjectField("", obj, obj.GetType(), false);
                GUI.enabled = true;
                GUILayout.Space(10);
            }
        }
        public static string LocateScript(Type targetType)
        {

            if (targetType == null)
                return string.Empty;

            string fullTypeName = targetType.FullName;
            string className = targetType.Name;

            string[] csGuids = AssetDatabase.FindAssets("t:Script");

            foreach (string guid in csGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;
                string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                if (fileName.Equals(className, StringComparison.OrdinalIgnoreCase))
                    return assetPath;

                string fileContent = System.IO.File.ReadAllText(assetPath);
                string pattern = $@"\b(class|struct|enum)\s+{Regex.Escape(className)}\b";
                if (Regex.IsMatch(fileContent, pattern, RegexOptions.IgnoreCase))
                    return assetPath;

            }

            return string.Empty;
        }

        public static Texture2D GetIcon(this object track)
        {
            Type type = null;
            if (track is Type)
                type = track as Type;
            else
                type = track.GetType();
            if (_iconDictionary.TryGetValue(type, out var icon))
            {
                return icon;
            }

            var att = type.GetCustomAttribute<IconAttribute>(true);

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

            _iconDictionary[type] = icon;
            return icon;
        }
        public static string GetTypeName(Type type)
        {
            if (type == null) return string.Empty;
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



        public struct TypeMetaInfo
        {
            public Type type;
            public string name;
            public Type[] attachableTypes;
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
            foreach (var type in TypeHelper.GetSubTypes(baseType))
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


        public static bool CanAttachTo(Type type, Type attachTo)
        {

            if (type == null || type.IsAbstract) return false;

            var attachAtt = type.GetCustomAttribute<AttachableAttribute>(true);
            if (attachAtt == null || attachAtt.Types == null || attachAtt.Types.All(t => t != attachTo)) return false;

            return true;
        }

        public static Editor CreateEditor(object target) => DrawerObject.CreateEditor(target);


        [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        [CustomPropertyDrawer(typeof(NameAttribute))]
        public class FieldAttributePropertyDrawer : PropertyDrawer
        {
            private bool _initialized;
            private bool _isReadOnly;
            private bool _isCollectionField;
            private GUIContent _nameLabel;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (IsCollectionElement(property))
                    return EditorGUI.GetPropertyHeight(property, label, true);
                return EditorGUI.GetPropertyHeight(property, GetLabel(label), true);
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (IsCollectionElement(property))
                {
                    EditorGUI.PropertyField(position, property, label, true);
                    return;
                }

                using (new EditorGUI.DisabledScope(IsReadOnly()))
                    EditorGUI.PropertyField(position, property, GetLabel(label), true);
            }

            private bool IsCollectionElement(SerializedProperty property)
            {
                Initialize();
                return _isCollectionField && !property.isArray;
            }

            private bool IsReadOnly()
            {
                Initialize();
                return _isReadOnly;
            }

            private GUIContent GetLabel(GUIContent label)
            {
                Initialize();
                if (_nameLabel == null) return label;

                _nameLabel.image = label == null ? null : label.image;
                _nameLabel.tooltip = label == null ? null : label.tooltip;
                return _nameLabel;
            }

            private void Initialize()
            {
                if (_initialized) return;

                var nameAttribute = attribute as NameAttribute;
                _isReadOnly = attribute is ReadOnlyAttribute;
                if (fieldInfo != null)
                {
                    _isCollectionField = typeof(System.Collections.IList)
                        .IsAssignableFrom(fieldInfo.FieldType);
                    if (nameAttribute == null)
                        nameAttribute = fieldInfo.GetCustomAttribute<NameAttribute>(true);
                    if (!_isReadOnly)
                        _isReadOnly = fieldInfo.IsDefined(typeof(ReadOnlyAttribute), true);
                }

                if (nameAttribute != null)
                    _nameLabel = new GUIContent(nameAttribute.name);
                _initialized = true;
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
            private sealed class FieldDisplay
            {
                public readonly bool IsReadOnly;
                public readonly GUIContent Name;

                public FieldDisplay(bool isReadOnly, NameAttribute name)
                {
                    IsReadOnly = isReadOnly;
                    if (name != null) Name = new GUIContent(name.name);
                }
            }

            private static readonly Dictionary<Type, Dictionary<string, FieldDisplay>>
                FieldDisplays = new Dictionary<Type, Dictionary<string, FieldDisplay>>();

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

                    hasNext = childProp.NextVisible(false);
                }
                return childProps;
            }
            private Vector2 scroll;
            public override void OnInspectorGUI()
            {
                this.serializedObject.Update();
                var p = this.serializedObject.FindProperty(nameof(DrawerObject.obj));
                var children = GetDirectChildProperties(p);
                var displays = GetFieldDisplays(((DrawerObject)target).obj?.GetType());
                //scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.BeginVertical();
                foreach (var item in children)
                {
                    if (!displays.TryGetValue(item.name, out var display))
                    {
                        EditorGUILayout.PropertyField(item, true);
                        continue;
                    }

                    using (new EditorGUI.DisabledScope(display.IsReadOnly))
                    {
                        if (display.Name == null)
                            EditorGUILayout.PropertyField(item, true);
                        else
                        {
                            display.Name.tooltip = item.tooltip;
                            EditorGUILayout.PropertyField(item, display.Name, true);
                        }
                    }
                    //GUILayout.Space(2);
                }
                GUILayout.EndVertical();
                //GUILayout.EndScrollView();
                this.serializedObject.ApplyModifiedProperties();
            }

            private static Dictionary<string, FieldDisplay> GetFieldDisplays(Type type)
            {
                if (type == null) return EmptyFieldDisplays;
                if (FieldDisplays.TryGetValue(type, out var displays)) return displays;

                displays = new Dictionary<string, FieldDisplay>();
                for (var current = type; current != null; current = current.BaseType)
                {
                    var fields = current.GetFields(BindingFlags.Instance | BindingFlags.Public |
                        BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                    for (int i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        if (displays.ContainsKey(field.Name)) continue;

                        var name = field.GetCustomAttribute<NameAttribute>(true);
                        bool isReadOnly = field.IsDefined(typeof(ReadOnlyAttribute), true);
                        if (name != null || isReadOnly)
                            displays.Add(field.Name, new FieldDisplay(isReadOnly, name));
                    }
                }

                FieldDisplays.Add(type, displays);
                return displays;
            }

            private static readonly Dictionary<string, FieldDisplay> EmptyFieldDisplays =
                new Dictionary<string, FieldDisplay>();
        }
    }
}
