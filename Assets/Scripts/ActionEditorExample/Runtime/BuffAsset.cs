using System;
using ActionEditor;
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
    [CustomInspector(typeof(BuffAsset))]
    public class BuffAssetIns : InspectorsBase
    {
        public override void OnInspectorGUI()
        {
            GUILayout.Label("789498");
            base.OnInspectorGUI();
        }
    }
#endif
}