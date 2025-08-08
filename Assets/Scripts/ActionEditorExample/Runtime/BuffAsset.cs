using System;
using UnityEngine;

namespace ActionEditor
{
    [Name("技能Buff")]
    [Serializable]
    public class BuffAsset : Asset
    {
        [Name("测试")]
        public string test;
    }
#if UNITY_EDITOR
    [CustomActionView(typeof(BuffAsset))]
    public class BuffAssetIns : ActonEditorView
    {
        public override void OnAssetFooterGUI()
        {
            GUILayout.Button("BuffAsset OnAssetFooterGUI");
        }
        public override void OnAssetHeaderGUI()
        {
            GUILayout.Button("BuffAsset OnAssetHeaderGUI");
        }
        public override void OnInspectorGUI()
        {
            GUILayout.Label("789498");
            base.OnInspectorGUI();
        }
    }
#endif
}