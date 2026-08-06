using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute
{
    internal static class SerializedPropertyMemberUtility
    {
        private const BindingFlags Flags = BindingFlags.Instance |
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Dictionary<Type, Dictionary<string, MemberInfo>>
            MemberCache = new();
        private static readonly Dictionary<Type, Dictionary<string, MethodInfo>>
            MethodCache = new();

        public static bool TryGetMemberValue(SerializedProperty property,
            string memberName, out object value)
        {
            value = null;
            object parent = GetParentObject(property.serializedObject.targetObject,
                property.propertyPath);
            return parent != null && TryGetMemberValue(parent, memberName,
                out value);
        }

        public static bool TryCompareCondition(SerializedProperty property,
            string memberName, object expected, out bool result)
        {
            result = false;
            if (!TryGetMemberValue(property, memberName, out object actual))
                return false;
            if (expected is bool expectedBoolean &&
                TryConvertToBoolean(actual, out bool actualBoolean))
            {
                result = actualBoolean == expectedBoolean;
                return true;
            }
            if (actual == null || expected == null)
            {
                result = actual == null && expected == null;
                return true;
            }
            Type actualType = actual.GetType();
            if (TryConvertValue(expected, actualType, out object converted))
                result = Equals(actual, converted);
            else
                result = Equals(actual, expected);
            return true;
        }

        public static bool TryGetDropdownValues(SerializedProperty property,
            string memberName, out string[] labels, out object[] values)
        {
            labels = Array.Empty<string>();
            values = Array.Empty<object>();
            if (!TryGetMemberValue(property, memberName, out object raw) ||
                !(raw is IEnumerable enumerable) || raw is string)
                return false;

            var labelList = new List<string>();
            var valueList = new List<object>();
            foreach (object item in enumerable)
            {
                if (TryUnpackDropdownItem(item, out string label,
                    out object value))
                {
                    labelList.Add(label ?? "NULL");
                    valueList.Add(value);
                }
                else
                {
                    labelList.Add(item?.ToString() ?? "NULL");
                    valueList.Add(item);
                }
            }
            labels = labelList.ToArray();
            values = valueList.ToArray();
            return true;
        }

        public static object GetSerializedValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean: return property.boolValue;
                case SerializedPropertyType.Integer: return property.longValue;
                case SerializedPropertyType.Float: return property.doubleValue;
                case SerializedPropertyType.String: return property.stringValue;
                case SerializedPropertyType.Color: return property.colorValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue;
                case SerializedPropertyType.LayerMask: return property.intValue;
                case SerializedPropertyType.Enum: return property.intValue;
                case SerializedPropertyType.Vector2: return property.vector2Value;
                case SerializedPropertyType.Vector3: return property.vector3Value;
                case SerializedPropertyType.Vector4: return property.vector4Value;
                case SerializedPropertyType.Rect: return property.rectValue;
                case SerializedPropertyType.Bounds: return property.boundsValue;
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue;
                case SerializedPropertyType.Vector2Int:
                    return property.vector2IntValue;
                case SerializedPropertyType.Vector3Int:
                    return property.vector3IntValue;
                case SerializedPropertyType.RectInt: return property.rectIntValue;
                case SerializedPropertyType.BoundsInt:
                    return property.boundsIntValue;
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceValue;
                default:
                    object parent = GetParentObject(
                        property.serializedObject.targetObject,
                        property.propertyPath);
                    return GetFinalValue(parent, property.propertyPath);
            }
        }

        public static bool SetSerializedValue(SerializedProperty property,
            object value)
        {
            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Boolean:
                        property.boolValue = Convert.ToBoolean(value); break;
                    case SerializedPropertyType.Integer:
                    case SerializedPropertyType.LayerMask:
                        property.longValue = Convert.ToInt64(value); break;
                    case SerializedPropertyType.Float:
                        property.doubleValue = Convert.ToDouble(value); break;
                    case SerializedPropertyType.String:
                        property.stringValue = value?.ToString(); break;
                    case SerializedPropertyType.Color:
                        property.colorValue = (Color)value; break;
                    case SerializedPropertyType.ObjectReference:
                        property.objectReferenceValue = value as UnityEngine.Object;
                        break;
                    case SerializedPropertyType.Enum:
                        property.intValue = Convert.ToInt32(value); break;
                    case SerializedPropertyType.Vector2:
                        property.vector2Value = (Vector2)value; break;
                    case SerializedPropertyType.Vector3:
                        property.vector3Value = (Vector3)value; break;
                    case SerializedPropertyType.Vector4:
                        property.vector4Value = (Vector4)value; break;
                    case SerializedPropertyType.Rect:
                        property.rectValue = (Rect)value; break;
                    case SerializedPropertyType.Bounds:
                        property.boundsValue = (Bounds)value; break;
                    case SerializedPropertyType.Quaternion:
                        property.quaternionValue = (Quaternion)value; break;
                    case SerializedPropertyType.Vector2Int:
                        property.vector2IntValue = (Vector2Int)value; break;
                    case SerializedPropertyType.Vector3Int:
                        property.vector3IntValue = (Vector3Int)value; break;
                    case SerializedPropertyType.RectInt:
                        property.rectIntValue = (RectInt)value; break;
                    case SerializedPropertyType.BoundsInt:
                        property.boundsIntValue = (BoundsInt)value; break;
                    case SerializedPropertyType.ManagedReference:
                        property.managedReferenceValue = value; break;
                    default: return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        public static void InvokeMethod(SerializedProperty property,
            string methodName)
        {
            string path = property.propertyPath;
            SerializedObject serializedObject = property.serializedObject;
            serializedObject.ApplyModifiedProperties();
            UnityEngine.Object[] targets = serializedObject.targetObjects;
            Undo.RecordObjects(targets, $"Invoke {methodName}");
            for (int i = 0; i < targets.Length; i++)
            {
                object parent = GetParentObject(targets[i], path);
                MethodInfo method = parent == null
                    ? null
                    : GetMethod(parent.GetType(), methodName);
                if (method == null || method.GetParameters().Length != 0)
                {
                    Debug.LogWarning($"Could not invoke parameterless method " +
                        $"'{methodName}'.");
                    continue;
                }
                try
                {
                    method.Invoke(method.IsStatic ? null : parent, null);
                    EditorUtility.SetDirty(targets[i]);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception.InnerException ?? exception);
                }
            }
            serializedObject.Update();
        }

        public static bool TryValidate(SerializedProperty property,
            string methodName, out bool valid)
        {
            valid = false;
            object parent = GetParentObject(property.serializedObject.targetObject,
                property.propertyPath);
            if (parent == null) return false;
            MethodInfo method = GetMethod(parent.GetType(), methodName);
            if (method == null || method.ReturnType != typeof(bool)) return false;

            try
            {
                ParameterInfo[] parameters = method.GetParameters();
                object result;
                if (parameters.Length == 0)
                    result = method.Invoke(method.IsStatic ? null : parent, null);
                else if (parameters.Length == 1 &&
                    TryGetSerializedValue(property, parameters[0].ParameterType,
                        out object argument))
                    result = method.Invoke(method.IsStatic ? null : parent,
                        new[] { argument });
                else
                    return false;
                valid = (bool)result;
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
                return false;
            }
        }

        public static void InvokeCallbacks(SerializedProperty property,
            OnValueChangedAttribute[] callbacks)
        {
            if (callbacks == null || callbacks.Length == 0) return;
            string path = property.propertyPath;
            SerializedObject serializedObject = property.serializedObject;
            serializedObject.ApplyModifiedProperties();

            UnityEngine.Object[] targets = serializedObject.targetObjects;
            for (int targetIndex = 0; targetIndex < targets.Length; targetIndex++)
            {
                object parent = GetParentObject(targets[targetIndex], path);
                if (parent == null) continue;
                object fieldValue = GetFinalValue(parent, path);
                for (int i = 0; i < callbacks.Length; i++)
                    InvokeCallback(parent, fieldValue, callbacks[i].callback);
            }
            serializedObject.Update();
        }

        private static void InvokeCallback(object parent, object fieldValue,
            string methodName)
        {
            MethodInfo method = GetMethod(parent.GetType(), methodName);
            if (method == null)
            {
                Debug.LogWarning($"Could not find callback '{methodName}' on " +
                    parent.GetType());
                return;
            }

            try
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 0)
                    method.Invoke(method.IsStatic ? null : parent, null);
                else if (parameters.Length == 1 &&
                    TryConvertValue(fieldValue, parameters[0].ParameterType,
                        out object argument))
                    method.Invoke(method.IsStatic ? null : parent,
                        new[] { argument });
                else
                    Debug.LogWarning($"Callback '{methodName}' must have zero " +
                        "parameters or one parameter matching the field type.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
            }
        }

        private static bool TryUnpackDropdownItem(object item, out string label,
            out object value)
        {
            label = null;
            value = null;
            if (item == null) return false;
            Type type = item.GetType();
            FieldInfo textField = type.GetField("text", Flags);
            FieldInfo valueField = type.GetField("value", Flags);
            if (textField != null && valueField != null)
            {
                label = textField.GetValue(item)?.ToString();
                value = valueField.GetValue(item);
                return true;
            }
            PropertyInfo keyProperty = type.GetProperty("Key", Flags);
            PropertyInfo valueProperty = type.GetProperty("Value", Flags);
            if (keyProperty != null && valueProperty != null)
            {
                label = keyProperty.GetValue(item, null)?.ToString();
                value = valueProperty.GetValue(item, null);
                return true;
            }
            return false;
        }

        private static object GetParentObject(object root, string propertyPath)
        {
            if (root == null) return null;
            string path = propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');
            object current = root;
            for (int i = 0; i < elements.Length - 1 && current != null; i++)
                current = GetPathElement(current, elements[i]);
            return current;
        }

        private static object GetFinalValue(object parent, string propertyPath)
        {
            string path = propertyPath.Replace(".Array.data[", "[");
            int separator = path.LastIndexOf('.');
            string element = separator < 0 ? path : path.Substring(separator + 1);
            return GetPathElement(parent, element);
        }

        private static object GetPathElement(object source, string element)
        {
            if (source == null) return null;
            int bracket = element.IndexOf('[');
            string name = bracket < 0 ? element : element.Substring(0, bracket);
            if (!TryGetMemberValue(source, name, out object value)) return null;
            if (bracket < 0) return value;

            int end = element.IndexOf(']', bracket + 1);
            if (end < 0 || !int.TryParse(element.Substring(bracket + 1,
                end - bracket - 1), out int index)) return null;
            return value is IList list && index >= 0 && index < list.Count
                ? list[index]
                : null;
        }

        private static bool TryGetMemberValue(object source, string name,
            out object value)
        {
            value = null;
            if (source == null || string.IsNullOrEmpty(name)) return false;
            MemberInfo member = GetMember(source.GetType(), name);
            switch (member)
            {
                case FieldInfo field:
                    value = field.GetValue(field.IsStatic ? null : source);
                    return true;
                case PropertyInfo property when
                    property.GetIndexParameters().Length == 0 &&
                    property.GetGetMethod(true) != null:
                    value = property.GetValue(property.GetGetMethod(true).IsStatic
                        ? null
                        : source);
                    return true;
                case MethodInfo method when method.GetParameters().Length == 0:
                    value = method.Invoke(method.IsStatic ? null : source, null);
                    return true;
                default:
                    return false;
            }
        }

        private static MemberInfo GetMember(Type type, string name)
        {
            if (!MemberCache.TryGetValue(type, out var members))
            {
                members = new Dictionary<string, MemberInfo>();
                MemberCache.Add(type, members);
            }
            if (members.TryGetValue(name, out MemberInfo cached)) return cached;

            MemberInfo result = null;
            for (Type current = type; current != null && result == null;
                current = current.BaseType)
            {
                result = current.GetField(name, Flags | BindingFlags.DeclaredOnly) ??
                    (MemberInfo)current.GetProperty(name,
                        Flags | BindingFlags.DeclaredOnly) ??
                    current.GetMethod(name, Flags | BindingFlags.DeclaredOnly,
                        null, Type.EmptyTypes, null);
            }
            members[name] = result;
            return result;
        }

        private static MethodInfo GetMethod(Type type, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (!MethodCache.TryGetValue(type, out var methods))
            {
                methods = new Dictionary<string, MethodInfo>();
                MethodCache.Add(type, methods);
            }
            if (methods.TryGetValue(name, out MethodInfo cached)) return cached;

            MethodInfo result = null;
            for (Type current = type; current != null && result == null;
                current = current.BaseType)
            {
                MethodInfo[] candidates = current.GetMethods(Flags |
                    BindingFlags.DeclaredOnly);
                for (int i = 0; i < candidates.Length; i++)
                {
                    MethodInfo candidate = candidates[i];
                    if (candidate.Name != name || candidate.GetParameters().Length > 1)
                        continue;
                    result = candidate;
                    if (candidate.GetParameters().Length == 0) break;
                }
            }
            methods[name] = result;
            return result;
        }

        private static bool TryGetSerializedValue(SerializedProperty property,
            Type targetType, out object value)
        {
            value = null;
            if (targetType.IsEnum &&
                property.propertyType == SerializedPropertyType.Enum)
            {
                value = Enum.ToObject(targetType, property.intValue);
                return true;
            }
            switch (property.propertyType)
            {
                case SerializedPropertyType.Boolean:
                    return TryConvertValue(property.boolValue, targetType, out value);
                case SerializedPropertyType.Integer:
                    return TryConvertValue(property.longValue, targetType, out value);
                case SerializedPropertyType.Float:
                    return TryConvertValue(property.doubleValue, targetType, out value);
                case SerializedPropertyType.String:
                    return TryConvertValue(property.stringValue, targetType, out value);
                case SerializedPropertyType.ObjectReference:
                    return TryConvertValue(property.objectReferenceValue, targetType,
                        out value);
                default:
                    object parent = GetParentObject(
                        property.serializedObject.targetObject, property.propertyPath);
                    return TryConvertValue(GetFinalValue(parent, property.propertyPath),
                        targetType, out value);
            }
        }

        private static bool TryConvertValue(object source, Type targetType,
            out object value)
        {
            value = null;
            if (source == null) return !targetType.IsValueType;
            if (targetType.IsInstanceOfType(source))
            {
                value = source;
                return true;
            }
            try
            {
                value = targetType.IsEnum
                    ? Enum.ToObject(targetType, source)
                    : Convert.ChangeType(source, targetType);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryConvertToBoolean(object source, out bool value)
        {
            switch (source)
            {
                case bool boolean:
                    value = boolean;
                    return true;
                case null:
                    value = false;
                    return true;
                case UnityEngine.Object unityObject:
                    value = unityObject != null;
                    return true;
                case sbyte number:
                    value = number != 0;
                    return true;
                case byte number:
                    value = number != 0;
                    return true;
                case short number:
                    value = number != 0;
                    return true;
                case ushort number:
                    value = number != 0;
                    return true;
                case int number:
                    value = number != 0;
                    return true;
                case uint number:
                    value = number != 0;
                    return true;
                case long number:
                    value = number != 0;
                    return true;
                case ulong number:
                    value = number != 0;
                    return true;
                default:
                    value = true;
                    return false;
            }
        }
    }
}
