using System.Collections.Generic;
using static UnityEngine.GraphicsBuffer;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using UnityEngine.UIElements;

namespace ActionEditor
{
    class InspectorView : ViewBase
    {
        //private bool _optionsAssetFold = true;

        //private static bool _willResample;

        private static Dictionary<IAction, InspectorsBase> directableEditors =
            new Dictionary<IAction, InspectorsBase>();

        private static InspectorsBase _currentDirectableEditor;
        private static InspectorsBase _currentAssetEditor;
        static Vector2 scroll;
        public override void OnDraw()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            bool change=false;
            EditorGUI.BeginChangeCheck();
            if (AppInternal.SelectCount < 1)
            {
                if (AppInternal.AssetData == null)
                {
                    EditorGUILayout.HelpBox(Lan.ins.NotSelectAsset, MessageType.Info);
                }
                else
                {
                    DoAssetInspector();

                }
            }
            else
            {
                DoSelectionInspector();
            }
            change = EditorGUI.EndChangeCheck();
            EditorGUILayout.EndScrollView();
            if (change)
                AppInternal.Refresh();

        }
        GUIStyle _style;
        void DoAssetInspector()
        {
            if (AppInternal.AssetData == null) return;
            var assetData = AppInternal.AssetData;

            var title = EditorEX.GetTypeName(assetData.GetType());
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 25,
                    fontStyle = FontStyle.Bold
                };
            }
            EditorGUILayout.LabelField(title, _style, GUILayout.Height(30));

            GUILayout.Space(2);
            if (!directableEditors.TryGetValue(assetData, out var newEditor))
            {
                directableEditors[assetData] = newEditor = EditorCustomFactory.GetInspector(assetData);
            }

            if (_currentAssetEditor != newEditor)
            {
                _currentAssetEditor = newEditor;
            }

            if (_currentAssetEditor != null)
            {
                _currentAssetEditor.OnInspectorGUI();
            }
        }

        void DoSelectionInspector()
        {
            var data = AppInternal.FistSelect;

            if (data == null)
            {
                _currentDirectableEditor = null;
                return;
            }


            if (!directableEditors.TryGetValue(data, out var newEditor))
            {
                directableEditors[data] =
                    newEditor = EditorCustomFactory.GetInspector(data);
            }

            if (_currentDirectableEditor != newEditor)
            {
                var enableMethod = newEditor.GetType().GetMethod("OnEnable",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.FlattenHierarchy);
                if (enableMethod != null)
                {
                    enableMethod.Invoke(newEditor, null);
                }

                _currentDirectableEditor = newEditor;
            }

            //EditorEX.BoldSeparator();
            GUILayout.Space(4);
            ShowPreliminaryInspector();

            if (_currentDirectableEditor != null) _currentDirectableEditor.OnInspectorGUI();
        }

        GUIStyle _style2;

        /// <summary>
        /// 选中对象基本信息
        /// </summary>
        void ShowPreliminaryInspector()
        {
            if (AppInternal.AssetData == null) return;
            var type = AppInternal.FistSelect.GetType();
            var name = EditorEX.GetTypeName(type);

            GUILayout.BeginHorizontal();
            if (_style2 == null)
            {
                _style2 = new GUIStyle(EditorStyles.label)
                {
                    //richText = true,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                };
            }
            GUILayout.Label(name, _style2);


            GUILayout.EndHorizontal();


            GUILayout.Space(2);
        }
    }
}