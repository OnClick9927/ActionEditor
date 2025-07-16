using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static System.Collections.Specialized.BitVector32;
using Object = UnityEngine.Object;

namespace ActionEditor
{
    public class InspectorsBase
    {
        protected IAction target;

        private Dictionary<int, bool> _unfoldDictionary = new Dictionary<int, bool>();

        public void SetTarget(IAction t)
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
            if (target is IDirectable)
            {
                var action = target as IDirectable;
                using (new EditorGUI.DisabledScope(action.IsLocked))
                {
                    DrawDefaultInspector(target);
                }
            }
            else
            {

                DrawDefaultInspector(target);
            }


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
            GUILayout.BeginVertical();
            foreach (var field in needShowField)
            {
                FieldDefaultInspector(field, obj);
            }
            GUILayout.EndVertical();
            return obj;
        }

        static List<Type> _base = new List<Type>()
            {
                typeof(int),typeof(float),typeof(double),typeof(bool),typeof(long),typeof(string),
                typeof(Color),typeof(Vector2),typeof(Vector3),typeof(Vector4),typeof(Vector2Int),typeof(Vector3Int),
                typeof(Rect),typeof(RectInt),typeof(Bounds),typeof(UnityEngine.Object),typeof(AnimationCurve),
            };
        private bool IsBaseType(Type type)
        {
            if (type.IsSubclassOf(typeof(UnityEngine.Object)))
                return true;
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
            else if (fieldType.IsSubclassOf(typeof(UnityEngine.Object))) return EditorGUILayout.ObjectField(name, (UnityEngine.Object)value, fieldType, true);
            else if (fieldType == typeof(AnimationCurve))
            {
                AnimationCurve curve = value as AnimationCurve;
                if (curve == null)
                {
                    curve = new AnimationCurve();
                }

                return EditorGUILayout.CurveField(name, curve);
            }
            else if (fieldType == typeof(Gradient))
            {
                Gradient curve = value as Gradient;
                if (curve == null)
                {
                    curve = new Gradient();
                }

                return EditorGUILayout.GradientField(name, curve);
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
        private string DrawMutiLine(string name, string value, int lines)
        {
            GUILayout.Label(name);
            return EditorGUILayout.TextArea(value, GUILayout.MinHeight(lines * 18));
        }

        static MethodInfo method;
        static private Dictionary<FieldInfo, Vector2> scrolls = new Dictionary<FieldInfo, Vector2>();

        private string DrawTextArea(string name, string value, FieldInfo field)
        {
            GUILayout.Label(name);
            if (method == null)
                method = typeof(EditorGUI).GetMethod("ScrollableTextAreaInternal", BindingFlags.Static | BindingFlags.NonPublic);
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Height(80));
            Vector2 scroll = Vector2.zero;
            scrolls.TryGetValue(field, out scroll);
            object[] parameters = new object[] {
                rect,
                value,
                scroll,
                EditorStyles.textArea
             };
            object methodResult = method.Invoke(null, parameters);
            scroll = (Vector2)(parameters[2]);
            scrolls[field] = scroll;
            return methodResult?.ToString();
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



     
        public void FieldDefaultInspector(FieldInfo field, object obj)
        {

            //if (!(field is FieldInfo) && !(field is PropertyInfo)) return;

            Type fieldType = null;
            object value = null;
            if (field is FieldInfo)
            {
                fieldType = (field as FieldInfo).FieldType;
                //showType = (field as FieldInfo).FieldType;
                value = (field as FieldInfo).GetValue(obj);
            }

            else if (typeof(Delegate).IsAssignableFrom(fieldType))
            {
                DrawDelegate(fieldType, value as Delegate);
                return;
            }

        Again:
            var newValue = value;
            var name = field.Name;
            var attributes = field.GetCustomAttributes();
            SpaceAttribute space = attributes.FirstOrDefault(x => x is SpaceAttribute) as SpaceAttribute;
            if (space != null)
            {
                GUILayout.Space(space.height);
            }
            ValidCheckAttribute notNull = attributes.FirstOrDefault(x => x is ValidCheckAttribute) as ValidCheckAttribute;
            if (notNull != null)
            {
                string err;
                bool valid = notNull.IsValid(field, value, out err);
                if (!valid)
                {
                    EditorGUILayout.HelpBox(err, MessageType.Error);
                }

            }



            HeaderAttribute header = attributes.FirstOrDefault(x => x is HeaderAttribute) as HeaderAttribute;
            if (header != null)
                GUILayout.Label(header.header, EditorStyles.boldLabel);


            ReadOnlyAttribute readOnly = attributes.FirstOrDefault(x => x is ReadOnlyAttribute) as ReadOnlyAttribute;
            using (new EditorGUI.DisabledScope(readOnly != null))
            {

                RangeAttribute range = attributes.FirstOrDefault(x => x is RangeAttribute) as RangeAttribute;
                MultilineAttribute mutiline = attributes.FirstOrDefault(x => x is MultilineAttribute) as MultilineAttribute;
                ObjectPathAttribute selectObjectPath = attributes.FirstOrDefault(x => x is ObjectPathAttribute) as ObjectPathAttribute;
                TextAreaAttribute textarea = attributes.FirstOrDefault(x => x is TextAreaAttribute) as TextAreaAttribute;


                if (range != null && fieldType == typeof(float))
                    newValue = DrawRange(name, (float)value, range.min, range.max);
                else if (selectObjectPath != null && fieldType == typeof(string))
                    newValue = DrawSelectObj(name, (string)value, selectObjectPath.type);
                else if (mutiline != null && fieldType == typeof(string))
                    newValue = DrawMutiLine(name, (string)value, mutiline.lines);
                else if (textarea != null && fieldType == typeof(string))
                    newValue = DrawTextArea(name, (string)value, field);
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
                else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(Queue<>))
                {
                    Type elementType = fieldType.GetGenericArguments()[0];
                    ICollection array = (ICollection)value;

                    if (array == null)
                        array = Activator.CreateInstance(typeof(Queue<>).MakeGenericType(elementType)) as ICollection;
                    var fold = GetFoldout(value);
                    var result = DrawArr(ref fold, name, array, elementType);

                    fieldType.GetMethod(nameof(Queue.Clear)).Invoke(value, null);
                    //array.Clear();
                    SetFoldout(array, fold);
                    for (int i = 0; i < result.Count; i++)
                        fieldType.GetMethod(nameof(Queue.Enqueue)).Invoke(value, new object[] { result[i] });
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
                else if (IsBaseType(fieldType))
                {
                    newValue = DrawBase(value, name, fieldType);
                }
                else
                {
                    if (fieldType != typeof(System.Object))
                    {
                        newValue = DrawObj(value, name, fieldType);
                    }
                    else
                    {
                        if (value != null)
                        {
                            fieldType = value.GetType();
                            //DrawTypeObj(value, name);
                            if (fieldType != typeof(System.Object))
                                goto Again;
                            else
                                return;

                        }
                        else
                        {
                            newValue = DrawObj(value, name, fieldType);

                        }
                    }
                }
            }


            if (value != newValue)
            {
                if (field is FieldInfo)
                    (field as FieldInfo).SetValue(obj, newValue);
                //else if ((field as PropertyInfo).CanWrite)
                //    (field as PropertyInfo).SetValue(obj, newValue);
            }
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









        private object DrawObj(object value, string name, Type fieldType)
        {
            bool fold = false;

            if (value == null)
            {
                EditorGUILayout.LabelField(name, "Null");
            }

            else
            {
                fold = GetFoldout(value);
                fold = EditorGUILayout.Foldout(fold, $"{name}", true);
                EditorGUI.LabelField(GUILayoutUtility.GetLastRect(), "   ", value.GetType().FullName);
                SetFoldout(value, fold);
            }
            if (fold)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(20);
                var Newvalue = DrawDefaultInspector(value);
                GUILayout.EndHorizontal();
                return Newvalue;
            }
            return value;
        }





        private IList DrawArr(ref bool fold, string name, IEnumerable arr, Type ele)
        {
            GUILayout.BeginVertical();
            IList array = Activator.CreateInstance(typeof(List<>).MakeGenericType(ele)) as IList;
            var ie = arr.GetEnumerator();
            while (ie.MoveNext())
            {
                array.Add(ie.Current);
            }
            //for (int i = 0; i < arr.Count; i++)
            //    array.Add(arr[i]);
            var cout = array.Count;
            //GUILayout.Label("", EditorStyles.toolbar);
            var rect = EditorGUILayout.GetControlRect(GUILayout.Height(20));
            GUI.Label(rect, "", EditorStyles.toolbarPopup);

            //var rs_second = RectEx.VerticalSplit(rect, rect.width - 20);
            var rs_second_0 = rect;
            var rs_second_1 = rect;

            rs_second_0.width -= 20;
            rs_second_1.x = rect.xMax-20;
            rs_second_1.width = 20;
            fold = EditorGUI.Foldout(rs_second_0, fold, $"{name}({ele.Name}): {cout}", true);
            if (GUI.Button(rs_second_1, EditorGUIUtility.TrIconContent("d_Toolbar Plus"), EditorStyles.toolbarButton))
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

            if (fold)
            {
                //GUILayout.Space(6);
                GUILayout.BeginVertical();
                for (int i = 0; i < array.Count; i++)
                {
                    object listItem = array[i];
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(20);

                        if (IsBaseType(ele))
                            array[i] = DrawBase(listItem, $"Element {i}", ele);
                        else
                            array[i] = DrawDefaultInspector(listItem);

                        if (GUILayout.Button(EditorGUIUtility.TrIconContent("d_Toolbar Minus"), GUILayout.Width(20)))
                        {
                            array.Remove(listItem);
                            break;
                        }

                        using (new EditorGUI.DisabledGroupScope(i == 0))
                            if (GUILayout.Button(EditorGUIUtility.TrIconContent("d_scrollup"), GUILayout.Width(20)))
                            {
                                var temp = array[i];
                                array[i] = array[i - 1];
                                array[i - 1] = temp;

                            }
                        using (new EditorGUI.DisabledGroupScope(i == array.Count - 1))

                            if (GUILayout.Button(EditorGUIUtility.TrIconContent("d_scrolldown"), GUILayout.Width(20)))
                            {
                                var temp = array[i];
                                array[i] = array[i + 1];
                                array[i + 1] = temp;
                            }
                    }

                    EditorGUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            return array;
        }

        private void DrawDelegate(MemberInfo field, Delegate value)
        {
            EditorGUILayout.LabelField($"{value.Target} <--> {value.Method.Name}");
        }


    }
}