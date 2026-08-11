using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute
{
    internal sealed class ActionInspectorRenderer
    {
        private const BindingFlags Flags = BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<Type, TypeMetadata> Metadata = new();
        private readonly List<SerializedProperty> properties = new();
        private readonly List<PropertyEntry> entries = new();
        private readonly Dictionary<FieldInfo, ActionPropertyDrawer> drawers =
            new();
        private readonly Dictionary<string, Vector2> scrollPositions = new();
        private object currentTarget;

        internal void SetTarget(object value)
        {
            if (ReferenceEquals(currentTarget, value)) return;
            currentTarget = value;
            drawers.Clear();
            scrollPositions.Clear();
        }

        internal void Dispose()
        {
            currentTarget = null;
            drawers.Clear();
            scrollPositions.Clear();
        }

        internal void DrawRoot(SerializedObject serializedObject, object target)
        {
            SetTarget(target);
            serializedObject.Update();
            properties.Clear();
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                properties.Add(iterator.Copy());
            }
            DrawProperties(serializedObject, target, properties);
        }

        internal void DrawChildren(SerializedObject serializedObject,
            SerializedProperty parent, object target,
            IReadOnlyCollection<string> excludedProperties = null)
        {
            SetTarget(target);
            serializedObject.Update();
            properties.Clear();
            if (parent != null && parent.hasChildren)
            {
                SerializedProperty child = parent.Copy();
                bool hasNext = child.Next(true);
                int childDepth = parent.depth + 1;
                while (hasNext && child.depth >= childDepth)
                {
                    if (child.depth == childDepth &&
                        !Contains(excludedProperties, child.name))
                        properties.Add(child.Copy());
                    hasNext = child.NextVisible(false);
                }
            }
            DrawProperties(serializedObject, target, properties);
        }

        private static bool Contains(IReadOnlyCollection<string> values,
            string value)
        {
            if (values == null) return false;
            foreach (string item in values)
                if (string.Equals(item, value, StringComparison.Ordinal))
                    return true;
            return false;
        }

        private void DrawProperties(SerializedObject serializedObject,
            object target, List<SerializedProperty> source)
        {
            TypeMetadata metadata = GetMetadata(target?.GetType());
            for (int i = 0; i < source.Count; i++)
            {
                if (source[i].propertyPath != "m_Script") continue;
                DrawProperty(new PropertyEntry(source[i], null));
                break;
            }
            for (int i = 0; i < metadata.TypeInfoBoxes.Length; i++)
            {
                TypeInfoBoxAttribute info = metadata.TypeInfoBoxes[i];
                EditorGUILayout.HelpBox(info.message, ToMessageType(info.type));
            }
            entries.Clear();
            for (int i = 0; i < source.Count; i++)
            {
                SerializedProperty property = source[i];
                if (property.propertyPath == "m_Script") continue;
                metadata.Fields.TryGetValue(property.name, out FieldInfo field);
                if (field != null &&
                    field.IsDefined(typeof(HideInInspector), true)) continue;
                entries.Add(new PropertyEntry(property, field));
            }

            var renderedGroups = new HashSet<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                PropertyEntry entry = entries[i];
                if (entry.Group == null)
                {
                    DrawProperty(entry);
                    continue;
                }
                string key = GetGroupKey(entry.Group);
                if (!renderedGroups.Add(key)) continue;
                var groupEntries = new List<PropertyEntry>();
                for (int j = 0; j < entries.Count; j++)
                {
                    if (entries[j].Group != null &&
                        GetGroupKey(entries[j].Group) == key)
                        groupEntries.Add(entries[j]);
                }
                DrawGroup(target, entry.Group, groupEntries);
            }

            DrawReflectedMembers(target, metadata, entries);
            DrawButtons(target, metadata);
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(PropertyEntry entry)
        {
            if (entry.Property.propertyPath == "m_Script")
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(entry.Property, true);
                return;
            }
            if (entry.Field == null)
            {
                EditorGUILayout.PropertyField(entry.Property, true);
                return;
            }
            if (!RequiresActionDrawer(entry.Field))
            {
                EditorGUILayout.PropertyField(entry.Property, true);
                return;
            }
            if (!drawers.TryGetValue(entry.Field,
                    out ActionPropertyDrawer drawer))
            {
                drawer = ActionPropertyDrawer.Create(entry.Field);
                drawers.Add(entry.Field, drawer);
            }
            GUIContent label = new GUIContent(entry.Property.displayName,
                entry.Property.tooltip);
            float height = drawer.GetPropertyHeight(entry.Property, label);
            if (height <= 0) return;
            Rect position = EditorGUILayout.GetControlRect(true, height);
            drawer.OnGUI(position, entry.Property, label);
        }

        private static bool RequiresActionDrawer(FieldInfo field) =>
            field != null && field.IsDefined(typeof(ActionAttributeBase), true);

        private void DrawGroup(object target, GroupAttribute group,
            List<PropertyEntry> groupEntries)
        {
            if (group.type == GroupType.Foldout ||
                group.type == GroupType.FoldoutBox)
            {
                string key = GetStateKey(target, "foldout", group.group);
                bool expanded = SessionState.GetBool(key, group.Expanded);
                bool foldoutBoxed = group.type == GroupType.FoldoutBox;
                if (foldoutBoxed)
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                expanded = EditorGUILayout.Foldout(expanded,
                    group.ShowLabel ? group.group : string.Empty, true);
                SessionState.SetBool(key, expanded);
                if (expanded)
                    using (new EditorGUI.IndentLevelScope())
                        DrawGroupEntries(groupEntries);
                if (foldoutBoxed) EditorGUILayout.EndVertical();
                return;
            }
            if (group.type == GroupType.Tab)
            {
                DrawTabGroup(target, group.group, groupEntries);
                return;
            }
            if (group.type == GroupType.Horizontal)
            {
                if (!string.IsNullOrEmpty(group.group))
                    EditorGUILayout.LabelField(group.group,
                        EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < groupEntries.Count; i++)
                {
                    float width = groupEntries[i].Group.Width;
                    if (width > 0)
                        EditorGUILayout.BeginVertical(GUILayout.Width(width));
                    else EditorGUILayout.BeginVertical();
                    DrawProperty(groupEntries[i]);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
                return;
            }
            if (group.type == GroupType.Grid)
            {
                DrawGridGroup(group, groupEntries);
                return;
            }
            if (group.type == GroupType.Indent)
            {
                if (group.ShowLabel && !string.IsNullOrEmpty(group.group))
                    EditorGUILayout.LabelField(group.group,
                        EditorStyles.boldLabel);
                using (new EditorGUI.IndentLevelScope())
                    DrawGroupEntries(groupEntries);
                return;
            }
            if (group.type == GroupType.Scroll)
            {
                DrawScrollGroup(target, group, groupEntries);
                return;
            }
            if (group.type == GroupType.Toggle)
            {
                DrawToggleGroup(target, group, groupEntries);
                return;
            }

            bool boxed = group.type == GroupType.Box;
            if (boxed) EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            else EditorGUILayout.BeginVertical();
            if (boxed && group.ShowLabel &&
                !string.IsNullOrEmpty(group.group))
                EditorGUILayout.LabelField(group.group, EditorStyles.boldLabel);
            else if (group.type == GroupType.Title)
            {
                EditorGUILayout.LabelField(group.group, EditorStyles.boldLabel);
                if (!string.IsNullOrEmpty(group.Subtitle))
                    EditorGUILayout.LabelField(group.Subtitle,
                        EditorStyles.wordWrappedMiniLabel);
            }
            DrawGroupEntries(groupEntries);
            EditorGUILayout.EndVertical();
        }

        private void DrawGroupEntries(List<PropertyEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++) DrawProperty(entries[i]);
        }

        private void DrawGridGroup(GroupAttribute group,
            List<PropertyEntry> entries)
        {
            if (group.ShowLabel && !string.IsNullOrEmpty(group.group))
                EditorGUILayout.LabelField(group.group, EditorStyles.boldLabel);
            int columns = Math.Max(1, group.Columns);
            for (int index = 0; index < entries.Count; index += columns)
            {
                EditorGUILayout.BeginHorizontal();
                for (int column = 0; column < columns; column++)
                {
                    int entryIndex = index + column;
                    if (entryIndex >= entries.Count)
                    {
                        GUILayout.FlexibleSpace();
                        continue;
                    }
                    float width = entries[entryIndex].Group.Width;
                    if (width > 0)
                        EditorGUILayout.BeginVertical(GUILayout.Width(width));
                    else EditorGUILayout.BeginVertical();
                    DrawProperty(entries[entryIndex]);
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawScrollGroup(object target, GroupAttribute group,
            List<PropertyEntry> entries)
        {
            if (group.ShowLabel && !string.IsNullOrEmpty(group.group))
                EditorGUILayout.LabelField(group.group, EditorStyles.boldLabel);
            string key = GetStateKey(target, "scroll", group.group);
            scrollPositions.TryGetValue(key, out Vector2 position);
            position = EditorGUILayout.BeginScrollView(position,
                GUILayout.Height(Mathf.Max(EditorGUIUtility.singleLineHeight,
                    group.Height)));
            DrawGroupEntries(entries);
            EditorGUILayout.EndScrollView();
            scrollPositions[key] = position;
        }

        private void DrawTabGroup(object target, string group,
            List<PropertyEntry> entries)
        {
            var tabs = new List<string>();
            for (int i = 0; i < entries.Count; i++)
            {
                string tab = entries[i].Group.Tab ?? string.Empty;
                if (!tabs.Contains(tab)) tabs.Add(tab);
            }
            string key = GetStateKey(target, "tab", group);
            int selected = Mathf.Clamp(SessionState.GetInt(key, 0), 0,
                Math.Max(0, tabs.Count - 1));
            if (!string.IsNullOrEmpty(group))
                EditorGUILayout.LabelField(group, EditorStyles.boldLabel);
            selected = GUILayout.Toolbar(selected, tabs.ToArray());
            SessionState.SetInt(key, selected);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            for (int i = 0; i < entries.Count; i++)
            {
                string tab = entries[i].Group.Tab ?? string.Empty;
                if (tabs.IndexOf(tab) == selected) DrawProperty(entries[i]);
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawToggleGroup(object target,
            GroupAttribute group, List<PropertyEntry> entries)
        {
            PropertyEntry toggleEntry = null;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].Property.name == group.ToggleMember)
                    toggleEntry = entries[i];

            bool enabled = GetBooleanMember(target, group.ToggleMember);
            if (toggleEntry != null && toggleEntry.Property.propertyType ==
                SerializedPropertyType.Boolean)
            {
                enabled = EditorGUILayout.ToggleLeft(group.group,
                    toggleEntry.Property.boolValue, EditorStyles.boldLabel);
                toggleEntry.Property.boolValue = enabled;
            }
            else EditorGUILayout.LabelField(group.group, EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!enabled))
            using (new EditorGUI.IndentLevelScope())
            {
                for (int i = 0; i < entries.Count; i++)
                    if (!ReferenceEquals(entries[i], toggleEntry))
                        DrawProperty(entries[i]);
            }
        }

        private static bool GetBooleanMember(object target, string memberName)
        {
            if (target == null || string.IsNullOrEmpty(memberName)) return false;
            TypeMetadata metadata = GetMetadata(target.GetType());
            if (metadata.Fields.TryGetValue(memberName, out FieldInfo field) &&
                field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);
            PropertyInfo property = target.GetType().GetProperty(memberName, Flags);
            return property?.PropertyType == typeof(bool) &&
                (bool)property.GetValue(target, null);
        }

        private static void DrawReflectedMembers(object target,
            TypeMetadata metadata, List<PropertyEntry> serializedEntries)
        {
            if (target == null) return;
            var serializedNames = new HashSet<string>();
            for (int i = 0; i < serializedEntries.Count; i++)
                serializedNames.Add(serializedEntries[i].Property.name);

            for (int i = 0; i < metadata.ReflectedMembers.Count; i++)
            {
                MemberInfo member = metadata.ReflectedMembers[i];
                if (serializedNames.Contains(member.Name)) continue;
                object value;
                try
                {
                    value = member is FieldInfo field
                        ? field.GetValue(field.IsStatic ? null : target)
                        : ((PropertyInfo)member).GetValue(target, null);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception.InnerException ?? exception);
                    continue;
                }
                NameAttribute name = member.GetCustomAttribute<NameAttribute>();
                string label = name?.name ?? ObjectNames.NicifyVariableName(
                    member.Name);
                using (new EditorGUI.DisabledScope(true))
                {
                    if (value is UnityEngine.Object unityObject)
                        EditorGUILayout.ObjectField(new GUIContent(label,
                            name?.comment), unityObject,
                            unityObject.GetType(), true);
                    else EditorGUILayout.TextField(new GUIContent(label,
                        name?.comment), value?.ToString() ?? "NULL");
                }
            }
        }

        private static void DrawButtons(object target, TypeMetadata metadata)
        {
            var renderedGroups = new HashSet<string>();
            for (int i = 0; i < metadata.Buttons.Count; i++)
            {
                MethodInfo method = metadata.Buttons[i];
                GroupAttribute group = method.GetCustomAttribute<GroupAttribute>();
                if (group?.type == GroupType.Button)
                {
                    if (!renderedGroups.Add(group.group)) continue;
                    EditorGUILayout.BeginHorizontal();
                    for (int j = 0; j < metadata.Buttons.Count; j++)
                    {
                        GroupAttribute candidate = metadata.Buttons[j]
                            .GetCustomAttribute<GroupAttribute>();
                        if (candidate?.type == GroupType.Button &&
                            candidate.group == group.group)
                            DrawButton(target, metadata.Buttons[j]);
                    }
                    EditorGUILayout.EndHorizontal();
                    continue;
                }
                DrawButton(target, method);
            }
        }

        private static void DrawButton(object target, MethodInfo method)
        {
                ButtonAttribute button = method.GetCustomAttribute<ButtonAttribute>();
                if (button == null) return;
                bool enabled = button.mode == InspectorMode.Always ||
                    (button.mode == InspectorMode.EditMode &&
                     !EditorApplication.isPlaying) ||
                    (button.mode == InspectorMode.PlayMode &&
                     EditorApplication.isPlaying);
                string text = string.IsNullOrEmpty(button.text)
                    ? ObjectNames.NicifyVariableName(method.Name)
                    : button.text;
                using (new EditorGUI.DisabledScope(!enabled ||
                    method.GetParameters().Length != 0))
                {
                    if (!GUILayout.Button(text)) return;
                    InvokeMethod(target, method);
                    if (target is UnityEngine.Object unityObject)
                        EditorUtility.SetDirty(unityObject);
                }
        }

        private static void InvokeMethod(object target, MethodInfo method)
        {
            if (method == null || method.GetParameters().Length != 0) return;
            try
            {
                method.Invoke(method.IsStatic ? null : target, null);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
            }
        }

        private static TypeMetadata GetMetadata(Type type)
        {
            if (type == null) return TypeMetadata.Empty;
            if (Metadata.TryGetValue(type, out TypeMetadata result)) return result;
            result = new TypeMetadata();
            object[] typeInfoBoxes = type.GetCustomAttributes(
                typeof(TypeInfoBoxAttribute), false);
            if (typeInfoBoxes.Length == 0)
                typeInfoBoxes = type.GetCustomAttributes(
                    typeof(TypeInfoBoxAttribute), true);
            result.TypeInfoBoxes =
                new TypeInfoBoxAttribute[typeInfoBoxes.Length];
            for (int i = 0; i < typeInfoBoxes.Length; i++)
                result.TypeInfoBoxes[i] =
                    (TypeInfoBoxAttribute)typeInfoBoxes[i];
            var methods = new List<MethodInfo>();
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(Flags |
                    BindingFlags.DeclaredOnly);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!result.Fields.ContainsKey(fields[i].Name))
                        result.Fields.Add(fields[i].Name, fields[i]);
                    if (fields[i].IsDefined(typeof(ShowInInspectorAttribute), true))
                        result.ReflectedMembers.Add(fields[i]);
                }
                PropertyInfo[] properties = current.GetProperties(Flags |
                    BindingFlags.DeclaredOnly);
                for (int i = 0; i < properties.Length; i++)
                    if (properties[i].GetIndexParameters().Length == 0 &&
                        properties[i].GetGetMethod(true) != null &&
                        properties[i].IsDefined(typeof(ShowInInspectorAttribute),
                            true))
                        result.ReflectedMembers.Add(properties[i]);
                methods.AddRange(current.GetMethods(Flags |
                    BindingFlags.DeclaredOnly));
            }
            for (int i = 0; i < methods.Count; i++)
            {
                MethodInfo method = methods[i];
                if (method.IsDefined(typeof(ButtonAttribute), true))
                    result.Buttons.Add(method);
            }
            Metadata.Add(type, result);
            return result;
        }

        private static string GetGroupKey(GroupAttribute group) =>
            group.type + ":" + group.group;

        private static string GetStateKey(object target, string kind,
            string group) => "ActionAttribute." + kind + "." +
            (target?.GetType().FullName ?? "null") + "." + group;

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

        private sealed class PropertyEntry
        {
            internal readonly SerializedProperty Property;
            internal readonly FieldInfo Field;
            internal readonly GroupAttribute Group;

            internal PropertyEntry(SerializedProperty property, FieldInfo field)
            {
                Property = property;
                Field = field;
                Group = field?.GetCustomAttribute<GroupAttribute>(true);
            }
        }

        private sealed class TypeMetadata
        {
            internal static readonly TypeMetadata Empty = new TypeMetadata();
            internal readonly Dictionary<string, FieldInfo> Fields = new();
            internal readonly List<MemberInfo> ReflectedMembers = new();
            internal readonly List<MethodInfo> Buttons = new();
            internal TypeInfoBoxAttribute[] TypeInfoBoxes =
                Array.Empty<TypeInfoBoxAttribute>();
        }
    }
}
