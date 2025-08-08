using ActionEditor;
using UnityEngine;

namespace ActionEditorExample
{
    /// <summary>
    /// 移动至预览
    /// </summary>
    [CustomActionView(typeof(MoveBy))]
    public class MoveByPreview : ClipEditorView<MoveBy>
    {
        private Vector3 originalPos;


        public override void OnPreviewUpdate(float time, float previousTime)
        {
            var target = originalPos + clip.move;
            ModelSampler.EditModel.transform.position =
                Easing.Ease(clip.interpolation, originalPos, target, time / clip.Length);
            
            
        }

        public override void OnPreviewEnter()
        {
            if (ModelSampler.EditModel != null)
            {
                originalPos = ModelSampler.EditModel.transform.position;
            }
        }
    }
}