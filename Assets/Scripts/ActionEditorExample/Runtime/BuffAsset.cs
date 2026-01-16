using System;
using System.Collections.Generic;
using UnityEngine;
namespace ActionEditor
{
    [Name("技能Buff")]
    [Serializable]
    public class BuffAsset : Asset
    {
        [System.Serializable]
        public class TestBuff 
        {
            public int text;
            public int text2;

 
        }
        [Name("测试")]
        [ReadOnly]
        public string test;
        [Name("测试")]
        [Buffer("das")]
        public double test2;
        public double test3;

        public TestBuff buff = new TestBuff();
        public List<int> gaga = new List<int>();
        public string[] strs = new string[1] { "xx"};

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