using UnityEngine;

namespace ActionEditor.Events
{
    public interface IPointerClickHandler
    {
        void OnPointerClick(PointerEventData ev);
    }
}