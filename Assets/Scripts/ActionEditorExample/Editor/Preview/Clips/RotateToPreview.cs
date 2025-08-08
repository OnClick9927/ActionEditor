using ActionEditor;
using UnityEngine;

namespace ActionEditorExample
{
    /// <summary>
    /// 旋转角度预览
    /// </summary>
    [CustomActionView(typeof(RotateTo))]
    public class RotateToPreview : ClipEditorView<RotateTo>
    {
        private Vector3 originalRot;

        public override void OnPreviewUpdate(float time, float previousTime)
        {
            var target = originalRot + clip.targetRotation;
            ModelSampler.EditModel.transform.localEulerAngles =
                Easing.Ease(clip.interpolation, originalRot, target, time / clip.Length);
        }

        public override void OnPreviewEnter()
        {
            if (ModelSampler.EditModel != null)
            {
                originalRot = ModelSampler.EditModel.transform.localEulerAngles;
            }
        }
    }
}