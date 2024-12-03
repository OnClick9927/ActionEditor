using UnityEngine;

namespace ActionEditor.Events
{
    public interface IPointerDownHandler
    {
        void OnPointerDown(PointerEventData ev);
    }
}