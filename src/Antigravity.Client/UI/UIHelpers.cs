using UnityEngine;

namespace Antigravity.Client.UI
{
    /// <summary>
    /// Extension methods for RectTransform positioning and sizing.
    /// </summary>
    public static class UIHelpers
    {
        /// <summary>
        /// Set RectTransform to fill its parent completely.
        /// </summary>
        public static void SetFullScreen(this RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }
        
        /// <summary>
        /// Set RectTransform to fill its parent completely.
        /// </summary>
        public static void SetFullScreen(this GameObject obj)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
                rect.SetFullScreen();
        }

        /// <summary>
        /// Center the RectTransform with specified size.
        /// </summary>
        public static void SetCenteredRect(this RectTransform rect, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = Vector2.zero;
        }
        
        /// <summary>
        /// Center the RectTransform with specified size.
        /// </summary>
        public static void SetCenteredRect(this GameObject obj, float width, float height)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
                rect.SetCenteredRect(width, height);
        }

        /// <summary>
        /// Set anchored position relative to center.
        /// </summary>
        public static void SetAnchoredPosition(this RectTransform rect, float x, float y)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(x, y);
        }
        
        /// <summary>
        /// Set anchored position relative to center.
        /// </summary>
        public static void SetAnchoredPosition(this GameObject obj, float x, float y)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
                rect.SetAnchoredPosition(x, y);
        }

        /// <summary>
        /// Set size delta.
        /// </summary>
        public static void SetSize(this RectTransform rect, float width, float height)
        {
            rect.sizeDelta = new Vector2(width, height);
        }
        
        /// <summary>
        /// Set size delta.
        /// </summary>
        public static void SetSize(this GameObject obj, float width, float height)
        {
            var rect = obj.GetComponent<RectTransform>();
            if (rect != null)
                rect.SetSize(width, height);
        }
        
        /// <summary>
        /// Set anchors to stretch horizontally.
        /// </summary>
        public static void SetHorizontalStretch(this RectTransform rect, float leftMargin = 0, float rightMargin = 0)
        {
            rect.anchorMin = new Vector2(0, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.offsetMin = new Vector2(leftMargin, rect.offsetMin.y);
            rect.offsetMax = new Vector2(-rightMargin, rect.offsetMax.y);
        }
    }
}
