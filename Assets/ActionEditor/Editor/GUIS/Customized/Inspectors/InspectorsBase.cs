using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Graphs;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ActionEditor
{
    public class InspectorsBase
    {
        protected object target;

        private Dictionary<int, bool> _unfoldDictionary = new Dictionary<int, bool>();

        public void SetTarget(object t)
        {
            target = t;
            _unfoldDictionary.Clear();
        }

        public virtual void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        public void DrawDefaultInspector()
        {
            DrawDefaultInspector(target);
        }

        public object DrawDefaultInspector(object obj)
        {
            var type = obj.GetType();
            //得到字段的值,只能得到public类型的字典的值
            FieldInfo[] fieldInfos = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            //排序一下，子类的字段在后，父类的在前
            Array.Sort(fieldInfos, FieldsSprtBy);

            //判断需要过滤不显示的字段
            List<FieldInfo> needShowField = new List<FieldInfo>();
            foreach (var field in fieldInfos)
            {
                var need = true;
                var attributes = field.GetCustomAttributes();
                foreach (var attribute in attributes)
                {
                    if (attribute is HideInInspector hide)
                    {
                        need = false;
                        break;
                    }


                }

                if (need)
                {
                    needShowField.Add(field);
                }
            }
            foreach (var field in needShowField)
            {
                FieldDefaultInspector(field, obj);
            }
            return obj;
        }

        static List<Type> _base = new List<Type>()
        {
            typeof(int),typeof(float),typeof(double),typeof(bool),typeof(long),typeof(string),
            typeof(Color),typeof(Vector2),typeof(Vector3),typeof(Vector4),typeof(Vector2Int),typeof(Vector3Int),
            typeof(Rect),typeof(RectInt),typeof(Bounds),typeof(Object),typeof(AnimationCurve),
        };
        private static bool IsBaseType(Type type)
        {
            if (type.IsEnum || _base.Contains(type)) return true;
            return false;
        }
        private object DrawBase(object value, string name, Type fieldType)
        {

            if (fieldType == typeof(int)) return EditorGUILayout.IntField(name, (int)value);
            else if (fieldType == typeof(float)) return EditorGUILayout.FloatField(name, (float)value);
            else if (fieldType == typeof(bool)) return EditorGUILayout.Toggle(name, (bool)value);
            else if (fieldType == typeof(string)) return EditorGUILayout.TextField(name, (string)value);
            else if (fieldType == typeof(long)) return EditorGUILayout.LongField(name, (long)value);
            else if (fieldType == typeof(double)) return EditorGUILayout.DoubleField(name, (double)value);
            else if (fieldType.IsEnum) return EditorGUILayout.EnumPopup(name, (Enum)value);
            else if (fieldType == typeof(Color)) return EditorGUILayout.ColorField(name, (Color)value);
            else if (fieldType == typeof(Vector2)) return EditorGUILayout.Vector2Field(name, (Vector2)value);
            else if (fieldType == typeof(Vector3)) return EditorGUILayout.Vector3Field(name, (Vector3)value);
            else if (fieldType == typeof(Vector4)) return EditorGUILayout.Vector4Field(name, (Vector4)value);
            else if (fieldType == typeof(Vector2Int)) return EditorGUILayout.Vector2IntField(name, (Vector2Int)value);
            else if (fieldType == typeof(Vector3Int)) return EditorGUILayout.Vector3IntField(name, (Vector3Int)value);
            else if (fieldType == typeof(Rect)) return EditorGUILayout.RectField(name, (Rect)value);
            else if (fieldType == typeof(RectInt)) return EditorGUILayout.RectIntField(name, (RectInt)value);
            else if (fieldType == typeof(Bounds)) return EditorGUILayout.BoundsField(name, (Bounds)value);
            else if (fieldType.IsSubclassOf(typeof(Object))) return EditorGUILayout.ObjectField(name, (Object)value, fieldType, false);
            else if (fieldType == typeof(AnimationCurve))
            {
                AnimationCurve curve = value as AnimationCurve;
                if (curve == null)
                {
                    curve = new AnimationCurve();
                }

                return EditorGUILayout.CurveField(name, curve);
            }
            return value;
        }
        private float DrawRange(string name, float value, float min, float max)
        {
            return EditorGUILayout.Slider(name, (float)value, min, max);
        }
        private string DrawSelectObj(string name, string value, Type objType)
        {
            Object o = null;
            var path = value.ToString();
            if (!string.IsNullOrEmpty(path))
                o = AssetDatabase.LoadAssetAtPath(path, objType);
            var newObj = EditorGUILayout.ObjectField(name, o, objType, false);
            return AssetDatabase.GetAssetPath(newObj);
        }

        private IList DrawArr(ref bool fold, string name, IList arr, Type ele)
        {
            IList array = Activator.CreateInstance(typeof(List<>).MakeGenericType(ele)) as IList;
          
            for (int i = 0; i < arr.Count; i++)
                array.Add(arr[i]);
            var cout = array.Count;
            GUILayout.BeginHorizontal();
            fold = EditorGUILayout.Foldout(fold, $"{name}({ele.Name})");
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                Array newArray = Array.CreateInstance(ele, array != null ? array.Count + 1 : 1);
                if (array != null)
                {
                    array.CopyTo(newArray, 0);
                }

                newArray.SetValue(Activator.CreateInstance(ele), newArray.Length - 1);
                array = newArray;
                SetFoldout(newArray, true);
            }
            GUILayout.EndHorizontal();
            if (fold)
            {
                GUILayout.Space(6);
                GUILayout.BeginVertical(GUI.skin.box);
                for (int i = 0; i < array.Count; i++)
                {
                    object listItem = array[i];
                    EditorGUILayout.BeginHorizontal();
                    {
                        if (IsBaseType(ele))
                            array[i] = DrawBase(listItem, $"Element {i}", ele);
                        else
                            array[i] = DrawDefaultInspector(listItem);
                        if (GUILayout.Button("x", GUILayout.Width(20)))
                        {
                            array.Remove(listItem);
                            break;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                    DrawDivider();
                }
                GUILayout.EndVertical();
            }
            return array;
        }



        protected void FieldDefaultInspector(FieldInfo field, object obj)
        {
            var fieldType = field.FieldType;
            var showType = field.FieldType;
            var value = field.GetValue(obj);
            var newValue = value;
            var name = field.Name;
            var attributes = field.GetCustomAttributes();
            NameAttribute Name = attributes.FirstOrDefault(x => x is NameAttribute) as NameAttribute;
            if (Name != null)
                name = Name.name;
            RangeAttribute range = attributes.FirstOrDefault(x => x is RangeAttribute) as RangeAttribute;
            SelectObjectPathAttribute selectObjectPath = attributes.FirstOrDefault(x => x is SelectObjectPathAttribute) as SelectObjectPathAttribute;

            if (range != null && fieldType == typeof(float))
                newValue = DrawRange(name, (float)value, range.min, range.max);
            else if (selectObjectPath != null && fieldType == typeof(string))
                newValue = DrawSelectObj(name, (string)value, selectObjectPath.type);
            else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                Type elementType = fieldType.GetGenericArguments()[0];
                IList array = (IList)value;
                if (array == null)
                    array = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
                var fold = GetFoldout(value);
                var result = DrawArr(ref fold, name, array, elementType);
                array.Clear();
                SetFoldout(array, fold);
                for (int i = 0; i < result.Count; i++)
                    array.Add(result[i]);
                newValue = array;
            }
            // 处理数组类型
            else if (fieldType.IsArray)
            {

                Type elementType = fieldType.GetElementType();
                Array array = (Array)value;

                if (array == null)
                    array = Array.CreateInstance(elementType, 0);
                var fold = GetFoldout(value);
                var result = DrawArr(ref fold, name, array, elementType);
                Array.Clear(array, 0, array.Length);
                if (array.Length != result.Count)
                    array = Array.CreateInstance(elementType, result.Count);
                SetFoldout(array, fold);

                for (int i = 0; i < result.Count; i++)
                    array.SetValue(result[i], i);
                newValue = array;
            }

            else
                newValue = DrawBase(value, name, fieldType);
            if (value != newValue)
                field.SetValue(obj, newValue);

        }

        private int FieldsSprtBy(FieldInfo f1, FieldInfo f2)
        {
            if (f1 == null || f2 == null) return 0;
            var e1 = f1.DeclaringType == f1.ReflectedType;
            var e2 = f2.DeclaringType == f2.ReflectedType;
            if (e1 != e2)
            {
                if (e1)
                {
                    return 1;
                }

                return -1;
            }

            return 0;
        }


        private bool GetFoldout(object obj)
        {
            if (obj == null) return false;
            if (!_unfoldDictionary.TryGetValue(obj.GetHashCode(), out var value))
            {
                _unfoldDictionary[obj.GetHashCode()] = false;
            }

            return value;
        }

        private void SetFoldout(object obj, bool unfold)
        {
            if (obj == null) return;
            _unfoldDictionary[obj.GetHashCode()] = unfold;
        }
        private void DrawDivider()
        {
            GUILayout.Space(2);
            //Color color = Color.black.WithAlpha(0.1f);
            Rect rect = EditorGUILayout.GetControlRect(false, 2);
            GUI.Label(rect, "", (GUIStyle)"WindowBottomResize");
            GUILayout.Space(2);
        }
    }
}