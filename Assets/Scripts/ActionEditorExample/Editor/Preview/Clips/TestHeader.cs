using ActionEditor;
using UnityEngine;

namespace ActionEditorExample
{
    [CustomHeaderAttribute(typeof(BuffAsset))]
    [CustomFooter(typeof(BuffAsset))]

    public class TestHeader : HeaderFooterBase
    {
        public override void OnGUI(Asset assetData)
        {
            GUILayout.Button("Test");
        }
    }
}