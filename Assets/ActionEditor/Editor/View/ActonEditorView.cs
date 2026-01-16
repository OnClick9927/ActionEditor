using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{

    partial class ActonEditorView
    {
        private static bool _initHeadersDic = false;
        private static readonly Dictionary<Type, Type> editorTypes = new Dictionary<Type, Type>();
        private static Dictionary<IAction, ActonEditorView> editor_ins = new Dictionary<IAction, ActonEditorView>();
        public static ActonEditorView GetEditor(IAction asset)
        {
            if (asset == null) return null;
            if (!_initHeadersDic)
            {
                InitHeaderDic();
                _initHeadersDic = true;
            }
            //var dic = editorTypes;
            ActonEditorView result = null;

            if (!editor_ins.TryGetValue(asset, out result))
            {
                var type = asset.GetType();
                editorTypes.TryGetValue(type, out var editor_type);
                if (editor_type == null)
                {
                    var _type = type;
                    while (true)
                    {
                        _type = _type.BaseType;
                        if (_type == typeof(System.Object))
                            break;
                        if (editorTypes.TryGetValue(_type, out editor_type))
                        {
                            if (editor_type.IsAbstract)
                                editor_type = null;
                            else
                                break;
                        }
                    }
                    if (editor_type == null) editor_type = typeof(ActonEditorView);
                    editorTypes.Add(type, editor_type);
                }
                result = Activator.CreateInstance(editor_type) as ActonEditorView;
                editor_ins[asset] = result;
            }
            result?.SetTarget(asset);
            return result;
        }


        static void InitHeaderDic()
        {

            Type type = typeof(ActonEditorView);
            var childs = EditorEX.GetTypeMetaDerivedFrom(type);
            foreach (var t in childs)
            {
                var arr = t.type.GetCustomAttribute(typeof(CustomActionViewAttribute), true) as CustomActionViewAttribute;
                if (arr != null)
                {
                    var bindT = arr.InspectedType;
                    var iT = t.type;
                    if (!editorTypes.ContainsKey(bindT))
                    {
                        if (!iT.IsAbstract) editorTypes[bindT] = iT;
                    }
                    else
                    {
                        var old = editorTypes[bindT];
                        if (!iT.IsAbstract && iT.IsSubclassOf(old))
                        {
                            editorTypes[bindT] = iT;
                        }
                    }
                }
            }
        }


    }



    partial class ActonEditorView
    {
        public virtual void OnAssetHeaderGUI() { }
        public virtual void OnAssetFooterGUI() { }
        public virtual void OnGroupTrackLeftGUI() { }
    }
    public partial class ActonEditorView
    {
        public IAction target { get; private set; }

        private Dictionary<int, bool> _unfoldDictionary = new Dictionary<int, bool>();

        internal void SetTarget(IAction t)
        {
            target = t;
            //_unfoldDictionary.Clear();

        }

        public virtual void OnInspectorGUI()
        {
            DrawDefaultInspector();
        }

        public void DrawDefaultInspector()
        {
            GUILayout.Space(10);
            var editor = EditorEX.CreateEditor(target);


            if (target is IDirectable)
            {
                var action = target as IDirectable;
                using (new EditorGUI.DisabledScope(action.IsLocked))
                {
                    editor.OnInspectorGUI();
                }
            }
            else
            {
                editor.OnInspectorGUI();
            }


        }



    }

    partial class ActonEditorView
    {
        public virtual void OnPreviewEnter() { }

        public virtual void OnPreviewExit() { }

        public virtual void OnPreviewReverseEnter() { }

        public virtual void OnPreviewReverse() { }


        public virtual void OnPreviewUpdate(float time, float previousTime) { }
    }
}