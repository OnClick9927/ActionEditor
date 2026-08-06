using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute
{
    public static class EditorEX
    {
        private static readonly Dictionary<Type, Texture2D> IconCache = new();
        private static readonly Dictionary<Type, GUIContent> TypeContentCache = new();
        private static readonly Dictionary<Type, UnityEngine.Object> ScriptObjectCache = new();
        private static readonly Dictionary<Type, string> ScriptPathCache = new();
        private static readonly Dictionary<Type, List<TypeMetaInfo>> TypeMetaCache = new();
        private static readonly Dictionary<Type, HashSet<Type>> AttachableTypeCache = new();

        [InitializeOnLoadMethod]
        private static void InitializeCaches()
        {
            EditorApplication.projectChanged -= ClearAssetCaches;
            EditorApplication.projectChanged += ClearAssetCaches;
        }

        private static void ClearAssetCaches()
        {
            IconCache.Clear();
            ScriptObjectCache.Clear();
            ScriptPathCache.Clear();
        }

        public static void DrawPingScript(Type type)
        {
            if (type == null) return;
            if (!ScriptObjectCache.TryGetValue(type, out UnityEngine.Object obj))
            {
                string path = LocateScript(type);
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                ScriptObjectCache[type] = obj;
            }
            if (obj == null) return;

            GUILayout.Space(10);
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField(string.Empty, obj, obj.GetType(), false);
            GUILayout.Space(10);
        }

        public static string LocateScript(Type targetType)
        {
            if (targetType == null) return string.Empty;
            if (ScriptPathCache.TryGetValue(targetType, out string cached))
                return cached;

            string className = targetType.Name;
            int genericMarker = className.IndexOf('`');
            if (genericMarker >= 0) className = className.Substring(0, genericMarker);

            string fallback = string.Empty;
            string[] scriptGuids = AssetDatabase.FindAssets($"{className} t:Script");
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

        public static Texture2D GetIcon(this object value)
        {
            Type type = value as Type ?? value?.GetType();
            if (type == null) return null;
            if (IconCache.TryGetValue(type, out Texture2D icon)) return icon;

            IconAttribute attribute = type.GetCustomAttribute<IconAttribute>(true);
            if (attribute != null)
            {
                if (!string.IsNullOrEmpty(attribute.base64))
                    icon = LoadBase64Icon(attribute.base64, type.Name);
                else if (!string.IsNullOrEmpty(attribute.iconPath))
                {
                    icon = attribute.iconPath.StartsWith("Assets/", StringComparison.Ordinal)
                        ? AssetDatabase.LoadAssetAtPath<Texture2D>(attribute.iconPath)
                        : Resources.Load<Texture2D>(attribute.iconPath);
                    if (icon == null) icon = EditorGUIUtility.FindTexture(attribute.iconPath);
                    if (icon == null) icon = FindProjectIcon(attribute.iconPath);
                }
                else if (attribute.fromType != null)
                    icon = AssetPreview.GetMiniTypeThumbnail(attribute.fromType);
            }

            if (icon != null) IconCache[type] = icon;
            return icon;
        }

        private static Texture2D FindProjectIcon(string iconPath)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(iconPath);
            string[] guids = AssetDatabase.FindAssets(fileName + " t:Texture2D");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.Equals(System.IO.Path.GetFileNameWithoutExtension(path),
                    fileName, StringComparison.OrdinalIgnoreCase)) continue;
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture != null) return texture;
            }
            return null;
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
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            return null;
        }

        public static string GetTypeName(Type type) => GetTypeContent(type).text;

        public static string GetTypeTooltip(Type type) => GetTypeContent(type).tooltip;

        public static GUIContent GetTypeContent(Type type)
        {
            if (type == null) return GUIContent.none;
            if (TypeContentCache.TryGetValue(type, out GUIContent content))
                return content;

            NameAttribute attribute = type.GetCustomAttribute<NameAttribute>(true);
            content = attribute == null
                ? new GUIContent(type.Name)
                : new GUIContent(attribute.name, attribute.comment);
            TypeContentCache[type] = content;
            return content;
        }

        public static string GetTypeName(this object value) =>
            GetTypeName(value as Type ?? value?.GetType());

        public static void DrawDashedLine(float x, float startY, float endY,
            Color color)
        {
            Handles.BeginGUI();
            Handles.color = color;
            float totalLength = Mathf.Abs(endY - startY);
            int dashes = Mathf.FloorToInt(totalLength / 10);
            for (int i = 0; i < dashes; i++)
            {
                float t1 = (float)i / dashes;
                float t2 = (i + 0.5f) / dashes;
                Handles.DrawLine(new Vector2(x, Mathf.Lerp(startY, endY, t1)),
                    new Vector2(x, Mathf.Lerp(startY, endY, t2)));
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

        public static T CleanPopup<T>(string prefix, T selected, List<T> options,
            params GUILayoutOption[] guiOptions)
        {
            int index = options.IndexOf(selected);
            var labels = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
                labels[i] = options[i] == null ? "NONE" : options[i].ToString();

            using (new EditorGUI.DisabledScope(options.Count == 0))
            {
                index = string.IsNullOrEmpty(prefix)
                    ? EditorGUILayout.Popup(index, labels, guiOptions)
                    : EditorGUILayout.Popup(prefix, index, labels, guiOptions);
            }
            return index < 0 ? default : options[index];
        }

        public static List<TypeMetaInfo> GetTypeMetaDerivedFrom(Type baseType)
        {
            if (TypeMetaCache.TryGetValue(baseType, out List<TypeMetaInfo> cached))
                return cached;

            var infos = new List<TypeMetaInfo>();
            TypeCache.TypeCollection derivedTypes = TypeCache.GetTypesDerivedFrom(baseType);
            foreach (Type type in derivedTypes)
            {
                if (type.IsAbstract || type.IsDefined(typeof(ObsoleteAttribute), true))
                    continue;

                var info = new TypeMetaInfo
                {
                    type = type,
                    name = GetTypeName(type)
                };
                if (type.GetCustomAttribute<AttachableAttribute>(true) is
                    AttachableAttribute attachable)
                    info.attachableTypes = attachable.Types;
                infos.Add(info);
            }

            infos.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            TypeMetaCache.Add(baseType, infos);
            return infos;
        }

        public static bool CanAttachTo(Type type, Type attachTo)
        {
            if (type == null || type.IsAbstract) return false;
            if (!AttachableTypeCache.TryGetValue(type, out HashSet<Type> types))
            {
                AttachableAttribute attribute =
                    type.GetCustomAttribute<AttachableAttribute>(true);
                types = attribute?.Types == null
                    ? new HashSet<Type>()
                    : new HashSet<Type>(attribute.Types);
                AttachableTypeCache.Add(type, types);
            }
            return types.Contains(attachTo);
        }

        public static Editor CreateEditor(object target) =>
            DrawerObject.CreateEditor(target);

        private sealed class DrawerObject : ScriptableObject
        {
            [SerializeReference] public object obj;
            private static DrawerObject instance;
            private static Editor editor;

            public static Editor CreateEditor(object target)
            {
                instance = instance ?? CreateInstance<DrawerObject>();
                instance.hideFlags = HideFlags.DontSave;
                instance.obj = target;
                if (editor == null) editor = Editor.CreateEditor(instance);
                return editor;
            }
        }

        [CustomEditor(typeof(DrawerObject))]
        private sealed class DrawerObjectEditor : Editor
        {
            private readonly ActionInspectorRenderer renderer =
                new ActionInspectorRenderer();

            public override void OnInspectorGUI()
            {
                SerializedProperty property = serializedObject.FindProperty(
                    nameof(DrawerObject.obj));
                renderer.DrawChildren(serializedObject, property,
                    ((DrawerObject)target).obj);
            }

            private void OnDisable() => renderer.Dispose();
        }
    }
}
