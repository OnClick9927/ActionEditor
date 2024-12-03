using UnityEngine;

namespace ActionEditor.Events
{
    public interface IPointerUpHandler
    {
        void OnPointerUp(PointerEventData ev);
    }
}