using ActionEditor;

namespace ActionEditorExample
{
    /// <summary>
    /// VisibleTo预览
    /// </summary>
    [CustomActionView(typeof(VisibleTo))]
    public class VisibleToPreview : ClipEditorView<VisibleTo>
    {
        public override void OnPreviewUpdate(float time, float previousTime)
        {
            if (ModelSampler.EditModel != null)
            {
                ModelSampler.EditModel.SetActive(clip.visible);
            }
        }
    }
}