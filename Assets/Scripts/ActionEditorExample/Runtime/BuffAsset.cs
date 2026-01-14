using System;
using UnityEngine;
namespace ActionEditor
{
    [Name("技能Buff")]
    [Serializable]
    public class BuffAsset : Asset
    {
        public class TestBuff : IBufferObject
        {
            public int text;
            public int text2;

            void IBufferObject.ReadField(string id, BufferReader reader)
            {

            }

            void IBufferObject.WriteField(string id, BufferWriter writer)
            {

            }
        }
        [Name("测试")]
        public string test;
        [Name("测试")]
        [Buffer("das")]
        public double test2;
        public double test3;

        public TestBuff buff = new TestBuff();
        protected override void ReadField(string id, BufferReader reader)
        {
            base.ReadField(id, reader);
            if (id == nameof(buff))
                buff = reader.ReadObject<TestBuff>();
        }
        protected override void WriteField(string id, BufferWriter writer)
        {
            base.WriteField(id, writer);
            if (id == nameof(buff))
                writer.WriteObject<TestBuff>(buff);
        }
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