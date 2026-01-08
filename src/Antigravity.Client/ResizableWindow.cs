using UnityEngine;
using UnityEngine.EventSystems;

namespace Antigravity.Client
{
    /// <summary>
    /// Makes a UI window resizable by dragging a corner handle.
    /// Attach to a resize handle element (usually bottom-right corner).
    /// </summary>
    public class ResizableWindow : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        private RectTransform _windowRect;
        private Vector2 _initialSize;
        private Vector2 _dragStartPos;
        
        // Size constraints
        public float MinWidth = 300f;
        public float MinHeight = 200f;
        public float MaxWidth = 800f;
        public float MaxHeight = 600f;
        
        // Callback when size changes
        public System.Action<Vector2> OnSizeChanged;
        
        public void SetWindowTransform(RectTransform windowRect)
        {
            _windowRect = windowRect;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            if (_windowRect == null) return;
            
            _initialSize = _windowRect.sizeDelta;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out _dragStartPos);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (_windowRect == null) return;
            
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _windowRect.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 currentPos);
            
            // Calculate delta from start position
            Vector2 delta = currentPos - _dragStartPos;
            
            // Apply delta to size (positive X = wider, negative Y = taller because of coordinate system)
            float newWidth = Mathf.Clamp(_initialSize.x + delta.x, MinWidth, MaxWidth);
            float newHeight = Mathf.Clamp(_initialSize.y - delta.y, MinHeight, MaxHeight);
            
            _windowRect.sizeDelta = new Vector2(newWidth, newHeight);
            
            OnSizeChanged?.Invoke(_windowRect.sizeDelta);
        }
    }
}
