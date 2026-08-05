using UnityEngine;
using UnityEngine.UI;

namespace EndlessRooms.UI
{
    /// <summary>Creates a thin rotated Image between two points in the same RectTransform's local space — the standard no-package way to draw a line in uGUI.</summary>
    public static class UILineFactory
    {
        public static Image CreateLine(Transform parent, Vector2 from, Vector2 to, float thickness, Color color)
        {
            var go = new GameObject("MapConnectionLine", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var rect = go.GetComponent<RectTransform>();
            Vector2 direction = to - from;
            float distance = direction.magnitude;

            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(distance, thickness);
            rect.anchoredPosition = from;
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            return image;
        }
    }
}
