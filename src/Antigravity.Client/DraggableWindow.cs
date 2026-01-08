using UnityEngine;
using UnityEngine.EventSystems;

namespace Antigravity.Client
{
    /// <summary>
    /// Makes a UI element draggable by its header.
    /// Attach to the header/title bar of a window.
    /// </summary>
    public class DraggableWindow : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform _windowRect;
        private Vector2 _dragOffset;
        
        public void SetWindowTransform(RectTransform windowRect)
        {
            _windowRect = windowRect;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_windowRect == null) return;
            
            // Calculate offset between mouse and window position
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 mousePos);
            
            _dragOffset = _windowRect.anchoredPosition - mousePos;
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (_windowRect == null) return;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 mousePos);
            
            _windowRect.anchoredPosition = mousePos + _dragOffset;
        }
    }
}
