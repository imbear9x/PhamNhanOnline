using UnityEngine;

namespace PhamNhanOnline.Client.UI.Common
{
    public abstract class CursorPopupViewModelBase : ViewModelBase
    {
        protected void PositionViewNearCursor(Vector2 cursorOffsetBelow, Vector2 cursorOffsetAbove, Vector2 screenPadding)
        {
            var panelTransform = ResolveViewRectTransform();
            if (panelTransform == null)
                return;

            var parent = panelTransform.parent as RectTransform;
            if (parent == null)
                return;

            Canvas.ForceUpdateCanvases();

            var canvas = parent.GetComponentInParent<Canvas>();
            var eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            var cursorScreenPoint = (Vector2)Input.mousePosition;
            var panelSize = panelTransform.rect.size;
            var offset = cursorOffsetBelow;

            if (cursorScreenPoint.y - panelSize.y - Mathf.Abs(cursorOffsetBelow.y) < screenPadding.y)
                offset = cursorOffsetAbove;

            var targetScreenPoint = cursorScreenPoint + offset;
            var minX = screenPadding.x + (panelSize.x * panelTransform.pivot.x);
            var maxX = Screen.width - screenPadding.x - (panelSize.x * (1f - panelTransform.pivot.x));
            var minY = screenPadding.y + (panelSize.y * panelTransform.pivot.y);
            var maxY = Screen.height - screenPadding.y - (panelSize.y * (1f - panelTransform.pivot.y));
            targetScreenPoint.x = Mathf.Clamp(targetScreenPoint.x, minX, maxX);
            targetScreenPoint.y = Mathf.Clamp(targetScreenPoint.y, minY, maxY);

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, targetScreenPoint, eventCamera, out var localPoint))
                return;

            panelTransform.anchoredPosition = localPoint;
        }
    }
}
