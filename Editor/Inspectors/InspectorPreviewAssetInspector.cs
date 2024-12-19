using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    [CustomEditor(typeof(InspectorPreviewAsset))]
    class InspectorPreviewAssetInspector : Editor
    {
        private bool _optionsAssetFold = true;

        //private static bool _willResample;

        private static Dictionary<IData, InspectorsBase> directableEditors =
            new Dictionary<IData, InspectorsBase>();

        private static InspectorsBase _currentDirectableEditor;
        private static InspectorsBase _currentAssetEditor;


        void OnEnable()
        {
            _currentDirectableEditor = null;
            //_willResample = false;
        }

        void OnDisable()
        {
            _currentDirectableEditor = null;
            directableEditors.Clear();
            //_willResample = false;
        }

        protected override void OnHeaderGUI()
        {
            GUILayout.Space(18f);
        }

        public override void OnInspectorGUI()
        {
            var ow = target as InspectorPreviewAsset;
            if (ow == null || App.SelectCount < 1)
            {
                EditorGUILayout.HelpBox(Lan.ins.NotSelectAsset, MessageType.Info);
                return;
            }

            //GUI.skin.GetStyle("label").richText = true;

            GUILayout.Space(5);

            DoAssetInspector();
            DoSelectionInspector();


            //if (_willResample)
            //{
            //    _willResample = false;
            //    EditorApplication.delayCall += () => { Debug.Log("cutscene.ReSample();"); };
            //}

            Repaint();
        }

        GUIStyle _style;
        void DoAssetInspector()
        {
            if (App.AssetData == null) return;
            var assetData = App.AssetData;

            var title = EditorEX.GetAssetTypeName(assetData.GetType());
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.foldout)
                {
                    fontSize = 25,
                    fontStyle = FontStyle.Bold
                };
            }
            _optionsAssetFold = EditorGUILayout.Foldout(_optionsAssetFold, title, _style);

            GUILayout.Space(2);
            if (_optionsAssetFold)
            {
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
        }

        void DoSelectionInspector()
        {
            var data = App.FistSelect;

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

            EditorEX.BoldSeparator();
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
            if (App.AssetData == null) return;
            var type = App.FistSelect.GetType();
            var nameAtt = type.GetCustomAttributes(typeof(NameAttribute), false).FirstOrDefault() as NameAttribute;
            var name = nameAtt != null ? nameAtt.name : type.Name;

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