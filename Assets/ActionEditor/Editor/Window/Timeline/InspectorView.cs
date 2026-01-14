using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class InspectorView : ViewBase
    {
        private static string LocateScript(Type targetType)
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

        static Vector2 scroll;
        public override void OnDraw()
        {
            if (_style == null)
            {
                _style = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 25,
                    fontStyle = FontStyle.Bold
                };
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            bool change = false;
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

            DrawPingScript(assetData.GetType());

            var title = EditorEX.GetTypeName(assetData.GetType());


            EditorGUILayout.LabelField(title, _style, GUILayout.Height(30));

            GUILayout.Space(2);
            ActonEditorView.GetEditor(assetData)?.OnInspectorGUI();


        }
        private static Dictionary<Type, UnityEngine.Object> scriptObjs = new Dictionary<Type, UnityEngine.Object>();

        private void DrawPingScript(Type type)
        {
            if (!scriptObjs.TryGetValue(type, out var obj))
            {
                var path = LocateScript(type);
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
        void DoSelectionInspector()
        {
            if (AppInternal.AssetData == null) return;

            var data = AppInternal.FistSelect;
            if (data == null) return;
            GUILayout.Space(4);
            DrawPingScript(data.GetType());
            var name = EditorEX.GetTypeName(data.GetType());
            GUILayout.Label(name, _style);
            GUILayout.Space(2);
            ActonEditorView.GetEditor(data)?.OnInspectorGUI();
        }


    }
}