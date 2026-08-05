using ActionBuffer;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionUnity
{

    public static class EditorEX
    {
        private static readonly Dictionary<Type, Texture2D> IconCache =
            new Dictionary<Type, Texture2D>();
        private static readonly Dictionary<Type, GUIContent> TypeContentCache =
            new Dictionary<Type, GUIContent>();
        private static readonly Dictionary<Type, UnityEngine.Object> ScriptObjectCache =
            new Dictionary<Type, UnityEngine.Object>();
        private static readonly Dictionary<Type, string> ScriptPathCache =
            new Dictionary<Type, string>();
        private static readonly Dictionary<Type, List<TypeMetaInfo>> TypeMetaCache =
            new Dictionary<Type, List<TypeMetaInfo>>();
        private static readonly Dictionary<Type, HashSet<Type>> AttachableTypeCache =
            new Dictionary<Type, HashSet<Type>>();

        public static void DrawPingScript(Type type)
        {
            if (type == null) return;
            if (!ScriptObjectCache.TryGetValue(type, out var obj))
            {
                var path = LocateScript(type);
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                ScriptObjectCache[type] = obj;
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
            if (targetType == null) return string.Empty;
            if (ScriptPathCache.TryGetValue(targetType, out string cached))
                return cached;

            string className = targetType.Name;
            int genericMarker = className.IndexOf('`');
            if (genericMarker >= 0)
                className = className.Substring(0, genericMarker);
            string[] scriptGuids = AssetDatabase.FindAssets(
                $"{className} t:Script");
            string fallback = string.Empty;
            for (int i = 0; i < scriptGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(scriptGuids[i]);
                MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script != null && script.GetClass() == targetType)
                {
                    ScriptPathCache[targetType] = path;
                    return path;
                }
                if (string.IsNullOrEmpty(fallback) &&
                    string.Equals(System.IO.Path.GetFileNameWithoutExtension(path),
                        className, StringComparison.OrdinalIgnoreCase))
                    fallback = path;
            }

            ScriptPathCache[targetType] = fallback;
            return fallback;
        }

        public static Texture2D GetIcon(this object track)
        {
            Type type = track as Type ?? track?.GetType();
            if (type == null) return null;
            if (IconCache.TryGetValue(type, out var icon)) return icon;

            var att = type.GetCustomAttribute<IconAttribute>(true);
            if (att != null)
            {
                if (!string.IsNullOrEmpty(att.base64))
                    icon = LoadBase64Icon(att.base64, type.Name);
                else if (!string.IsNullOrEmpty(att.iconPath))
                {
                    if (att.iconPath.StartsWith("Assets/"))
                        icon = AssetDatabase.LoadAssetAtPath<Texture2D>(att.iconPath);
                    else
                        icon = Resources.Load(att.iconPath) as Texture2D;
                    if (icon == null)
                        icon = EditorGUIUtility.FindTexture(att.iconPath);
                }
                else if (att.fromType != null)
                    icon = AssetPreview.GetMiniTypeThumbnail(att.fromType);
            }

            IconCache[type] = icon;
            return icon;
        }

        private static Texture2D LoadBase64Icon(string value, string name)
        {
            Texture2D texture = null;
            try
            {
                int comma = value.IndexOf(',');
                if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                    value = comma >= 0 ? value.Substring(comma + 1) : string.Empty;
                else if (value.StartsWith("base64:", StringComparison.OrdinalIgnoreCase))
                    value = value.Substring("base64:".Length);

                byte[] bytes = Convert.FromBase64String(value);
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
                {
                    name = name + " Icon",
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (texture.LoadImage(bytes, false)) return texture;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Could not decode icon for {name}: {exception.Message}");
            }
            if (texture != null)
                UnityEngine.Object.DestroyImmediate(texture);
            return null;
        }

        public static string GetTypeName(Type type)
        {
            return GetTypeContent(type).text;
        }

        public static string GetTypeTooltip(Type type) => GetTypeContent(type).tooltip;

        public static GUIContent GetTypeContent(Type type)
        {
            if (type == null) return GUIContent.none;
            if (TypeContentCache.TryGetValue(type, out GUIContent content))
                return content;

            var attribute = type.GetCustomAttribute<NameAttribute>(true);
            content = attribute == null
                ? new GUIContent(type.Name)
                : new GUIContent(attribute.name, attribute.comment);
            TypeContentCache[type] = content;
            return content;
        }

        public static string GetTypeName(this object track) =>
            GetTypeName(track as Type ?? track?.GetType());
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
            int index = options.IndexOf(selected);
            var labels = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
                labels[i] = options[i] == null ? "NONE" : options[i].ToString();

            using (new EditorGUI.DisabledScope(options.Count <= 0))
            {
                if (!string.IsNullOrEmpty(prefix))
                    index = EditorGUILayout.Popup(prefix, index, labels, GUIOptions);
                else index = EditorGUILayout.Popup(index, labels, GUIOptions);
            }

            return index < 0 ? default : options[index];
        }

        /// <summary>
        /// 获取当前加载的集合中基类型的所有非抽象派生类
        /// </summary>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static List<TypeMetaInfo> GetTypeMetaDerivedFrom(Type baseType)
        {
            if (TypeMetaCache.TryGetValue(baseType, out var cached)) return cached;

            var infos = new List<TypeMetaInfo>();
            foreach (var type in TypeHelper.GetSubTypes(baseType))
            {
                if (type.IsDefined(typeof(ObsoleteAttribute), true)) continue;

                var info = new TypeMetaInfo
                {
                    type = type,
                    name = GetTypeName(type),
                };



                if (type.GetCustomAttribute<AttachableAttribute>(true) is
                    AttachableAttribute attachAtt)
                    info.attachableTypes = attachAtt.Types;

                //info.isUnique = type.IsDefined(typeof(UniqueTrackAttribute), true);

                infos.Add(info);
            }

            infos.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            TypeMetaCache.Add(baseType, infos);
            return infos;
        }


        public static bool CanAttachTo(Type type, Type attachTo)
        {

            if (type == null || type.IsAbstract) return false;

            if (!AttachableTypeCache.TryGetValue(type, out var attachableTypes))
            {
                var attachAtt = type.GetCustomAttribute<AttachableAttribute>(true);
                attachableTypes = attachAtt?.Types == null
                    ? new HashSet<Type>()
                    : new HashSet<Type>(attachAtt.Types);
                AttachableTypeCache.Add(type, attachableTypes);
            }
            return attachableTypes.Contains(attachTo);
        }

        public static Editor CreateEditor(object target) => DrawerObject.CreateEditor(target);


        [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
        [CustomPropertyDrawer(typeof(NameAttribute))]
        [CustomPropertyDrawer(typeof(ShowIfAttribute))]
        [CustomPropertyDrawer(typeof(EnableIfAttribute))]
        [CustomPropertyDrawer(typeof(ClampAttribute))]
        [CustomPropertyDrawer(typeof(MultilineTextAttribute))]
        [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
        [CustomPropertyDrawer(typeof(RequiredAttribute))]
        public class FieldAttributePropertyDrawer : PropertyDrawer
        {
            private bool _initialized;
            private bool _isReadOnly;
            private bool _isCollectionField;
            private GUIContent _nameLabel;
            private ShowIfAttribute[] _showConditions = Array.Empty<ShowIfAttribute>();
            private EnableIfAttribute[] _enableConditions = Array.Empty<EnableIfAttribute>();
            private HelpBoxAttribute[] _helpBoxes = Array.Empty<HelpBoxAttribute>();
            private ClampAttribute _clamp;
            private MultilineTextAttribute _multiline;
            private RequiredAttribute _required;

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (IsCollectionElement(property))
                    return EditorGUI.GetPropertyHeight(property, label, true);
                if (!ShouldShow(property)) return 0;

                float height = GetFieldHeight(property, GetLabel(label));
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                for (int i = 0; i < _helpBoxes.Length; i++)
                    height += GetHelpBoxHeight(_helpBoxes[i].message,
                        EditorGUIUtility.currentViewWidth - 40) + spacing;
                if (IsRequiredValueMissing(property))
                    height += GetHelpBoxHeight(GetRequiredMessage(property,
                        GetLabel(label)), EditorGUIUtility.currentViewWidth - 40) +
                        spacing;
                return height;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                if (IsCollectionElement(property))
                {
                    EditorGUI.PropertyField(position, property, label, true);
                    return;
                }

                if (!ShouldShow(property)) return;

                GUIContent fieldLabel = GetLabel(label);
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                for (int i = 0; i < _helpBoxes.Length; i++)
                {
                    HelpBoxAttribute help = _helpBoxes[i];
                    float helpHeight = GetHelpBoxHeight(help.message,
                        position.width);
                    Rect helpRect = new Rect(position.x, position.y,
                        position.width, helpHeight);
                    EditorGUI.HelpBox(helpRect, help.message,
                        ToMessageType(help.type));
                    position.y += helpHeight + spacing;
                }

                float fieldHeight = GetFieldHeight(property, fieldLabel);
                Rect fieldRect = new Rect(position.x, position.y,
                    position.width, fieldHeight);
                EditorGUI.BeginChangeCheck();
                using (new EditorGUI.DisabledScope(IsReadOnly() ||
                    !ShouldEnable(property)))
                    DrawField(fieldRect, property, fieldLabel);
                if (EditorGUI.EndChangeCheck()) ApplyClamp(property);
                position.y += fieldHeight + spacing;

                if (IsRequiredValueMissing(property))
                {
                    string message = GetRequiredMessage(property, fieldLabel);
                    float helpHeight = GetHelpBoxHeight(message, position.width);
                    EditorGUI.HelpBox(new Rect(position.x, position.y,
                        position.width, helpHeight), message, MessageType.Error);
                }
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
                if (string.IsNullOrEmpty(_nameLabel.tooltip))
                    _nameLabel.tooltip = label == null ? null : label.tooltip;
                return _nameLabel;
            }

            private bool ShouldShow(SerializedProperty property)
            {
                Initialize();
                for (int i = 0; i < _showConditions.Length; i++)
                {
                    ShowIfAttribute condition = _showConditions[i];
                    if (!EvaluateCondition(property, condition.condition,
                        condition.expected))
                        return false;
                }
                return true;
            }

            private bool ShouldEnable(SerializedProperty property)
            {
                Initialize();
                for (int i = 0; i < _enableConditions.Length; i++)
                {
                    EnableIfAttribute condition = _enableConditions[i];
                    if (!EvaluateCondition(property, condition.condition,
                        condition.expected))
                        return false;
                }
                return true;
            }

            private float GetFieldHeight(SerializedProperty property,
                GUIContent label)
            {
                Initialize();
                if (_multiline != null &&
                    property.propertyType == SerializedPropertyType.String)
                    return EditorGUIUtility.singleLineHeight * _multiline.lines;
                return EditorGUI.GetPropertyHeight(property, label, true);
            }

            private void DrawField(Rect position, SerializedProperty property,
                GUIContent label)
            {
                if (_multiline == null ||
                    property.propertyType != SerializedPropertyType.String)
                {
                    EditorGUI.PropertyField(position, property, label, true);
                    return;
                }

                Rect valueRect = EditorGUI.PrefixLabel(position, label);
                property.stringValue = EditorGUI.TextArea(valueRect,
                    property.stringValue ?? string.Empty);
            }

            private void ApplyClamp(SerializedProperty property)
            {
                if (_clamp == null) return;
                if (property.propertyType == SerializedPropertyType.Integer)
                {
                    long min = (long)Math.Ceiling(_clamp.min);
                    long max = (long)Math.Floor(_clamp.max);
                    property.longValue = Math.Max(min,
                        Math.Min(max, property.longValue));
                }
                else if (property.propertyType == SerializedPropertyType.Float)
                {
                    property.doubleValue = Math.Max(_clamp.min,
                        Math.Min(_clamp.max, property.doubleValue));
                }
            }

            private string GetRequiredMessage(SerializedProperty property,
                GUIContent label)
            {
                Initialize();
                return string.IsNullOrEmpty(_required?.message)
                    ? $"{label?.text ?? property.displayName}不能为空。"
                    : _required.message;
            }

            private bool IsRequiredValueMissing(SerializedProperty property)
            {
                Initialize();
                if (_required == null) return false;
                switch (property.propertyType)
                {
                    case SerializedPropertyType.ObjectReference:
                        return property.objectReferenceValue == null;
                    case SerializedPropertyType.String:
                        return string.IsNullOrWhiteSpace(property.stringValue);
                    case SerializedPropertyType.ManagedReference:
                        return property.managedReferenceValue == null;
                    default:
                        return false;
                }
            }

            private static bool EvaluateCondition(SerializedProperty property,
                string conditionName, bool expected)
            {
                if (string.IsNullOrEmpty(conditionName)) return true;
                string path = property.propertyPath;
                int separator = path.LastIndexOf('.');
                string conditionPath = separator < 0
                    ? conditionName
                    : path.Substring(0, separator + 1) + conditionName;
                SerializedProperty condition = property.serializedObject
                    .FindProperty(conditionPath);
                if (condition == null) return true;

                bool value;
                switch (condition.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        value = condition.boolValue;
                        break;
                    case SerializedPropertyType.Integer:
                        value = condition.longValue != 0;
                        break;
                    case SerializedPropertyType.ObjectReference:
                        value = condition.objectReferenceValue != null;
                        break;
                    default:
                        return true;
                }
                return value == expected;
            }

            private static float GetHelpBoxHeight(string message, float width)
            {
                float minimum = EditorGUIUtility.singleLineHeight * 2;
                if (string.IsNullOrEmpty(message)) return minimum;
                return Mathf.Max(minimum, EditorStyles.helpBox.CalcHeight(
                    new GUIContent(message), Mathf.Max(1, width)));
            }

            private static MessageType ToMessageType(InspectorMessageType type)
            {
                switch (type)
                {
                    case InspectorMessageType.Info: return MessageType.Info;
                    case InspectorMessageType.Warning: return MessageType.Warning;
                    case InspectorMessageType.Error: return MessageType.Error;
                    default: return MessageType.None;
                }
            }

            private void Initialize()
            {
                if (_initialized) return;

                NameAttribute nameAttribute = null;
                var showConditions = new List<ShowIfAttribute>();
                var enableConditions = new List<EnableIfAttribute>();
                var helpBoxes = new List<HelpBoxAttribute>();
                IEnumerable<ActionAttributeBase> attributes = fieldInfo == null
                    ? new[] { attribute as ActionAttributeBase }
                    : fieldInfo.GetCustomAttributes<ActionAttributeBase>(true);
                foreach (ActionAttributeBase item in attributes)
                {
                    if (item == null) continue;
                    if (item is NameAttribute name) nameAttribute = name;
                    else if (item is ReadOnlyAttribute) _isReadOnly = true;
                    else if (item is ShowIfAttribute show) showConditions.Add(show);
                    else if (item is EnableIfAttribute enable)
                        enableConditions.Add(enable);
                    else if (item is ClampAttribute clamp) _clamp = clamp;
                    else if (item is MultilineTextAttribute multiline)
                        _multiline = multiline;
                    else if (item is HelpBoxAttribute help) helpBoxes.Add(help);
                    else if (item is RequiredAttribute required)
                        _required = required;
                }

                if (fieldInfo != null)
                {
                    _isCollectionField = typeof(System.Collections.IList)
                        .IsAssignableFrom(fieldInfo.FieldType);
                }

                if (nameAttribute != null)
                    _nameLabel = new GUIContent(nameAttribute.name,
                        nameAttribute.comment);
                _showConditions = showConditions.ToArray();
                _enableConditions = enableConditions.ToArray();
                _helpBoxes = helpBoxes.ToArray();
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
                    if (name != null)
                        Name = new GUIContent(name.name, name.comment);
                }
            }

            private static readonly Dictionary<Type, Dictionary<string, FieldDisplay>>
                FieldDisplays = new Dictionary<Type, Dictionary<string, FieldDisplay>>();

            private static void GetDirectChildProperties(SerializedProperty parentProp,
                List<SerializedProperty> childProps)
            {
                childProps.Clear();
                if (parentProp == null || !parentProp.hasChildren) return;

                // 重置到第一个子属性
                SerializedProperty childProp = parentProp.Copy();
                bool hasNext = childProp.Next(true);
                int childDepth = parentProp.depth + 1;
                while (hasNext && childProp.depth >= childDepth)
                {
                    if (childProp.depth == childDepth)
                        childProps.Add(childProp.Copy());
                    hasNext = childProp.NextVisible(false);
                }
            }
            private readonly List<SerializedProperty> _childProperties =
                new List<SerializedProperty>();
            public override void OnInspectorGUI()
            {
                this.serializedObject.Update();
                var p = this.serializedObject.FindProperty(nameof(DrawerObject.obj));
                GetDirectChildProperties(p, _childProperties);
                var displays = GetFieldDisplays(((DrawerObject)target).obj?.GetType());
                //scroll = GUILayout.BeginScrollView(scroll);
                GUILayout.BeginVertical();
                foreach (var item in _childProperties)
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
                            if (string.IsNullOrEmpty(display.Name.tooltip))
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
