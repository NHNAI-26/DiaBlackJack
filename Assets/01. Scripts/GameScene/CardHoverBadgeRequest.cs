using UnityEngine;

namespace DiaBlackJack.GameScene
{
    public sealed class CardHoverBadgeRequest
    {
        public CardHoverBadgeRequest(
            string title,
            string description,
            Vector2 screenPosition,
            bool showBelow)
            : this(
                title,
                description,
                screenPosition,
                showBelow,
                new Vector2(0.5f, showBelow ? 1f : 0f))
        {
        }

        public CardHoverBadgeRequest(
            string title,
            string description,
            Vector2 screenPosition,
            bool showBelow,
            Vector2 tooltipPivot)
        {
            Title = title ?? string.Empty;
            Description = description ?? string.Empty;
            ScreenPosition = screenPosition;
            ShowBelow = showBelow;
            TooltipPivot = tooltipPivot;
        }

        public string Description { get; }

        public Vector2 ScreenPosition { get; }

        public bool ShowBelow { get; }

        public string Title { get; }

        public Vector2 TooltipPivot { get; }

        public static CardHoverBadgeRequest CreateForRect(
            RectTransform rect,
            string title,
            string description)
        {
            if (rect == null)
            {
                return null;
            }

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 bottomWorld = (corners[0] + corners[3]) * 0.5f;
            Vector3 topWorld = (corners[1] + corners[2]) * 0.5f;
            Vector2 bottomScreen = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                bottomWorld);
            Vector2 topScreen = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                topWorld);
            bool showBelow = (bottomScreen.y + topScreen.y) * 0.5f >
                Screen.height * 0.5f;
            return new CardHoverBadgeRequest(
                title,
                description,
                showBelow ? bottomScreen : topScreen,
                showBelow);
        }

        public static CardHoverBadgeRequest CreateForDeckRect(
            RectTransform rect,
            string title,
            string description,
            Vector2 deckCardOffset)
        {
            if (rect == null)
            {
                return null;
            }

            Canvas canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay
                    ? canvas.worldCamera
                    : null;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 rightWorld = (corners[2] + corners[3]) * 0.5f;
            rightWorld += rect.TransformVector(deckCardOffset);
            Vector2 rightScreen = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                rightWorld);
            return new CardHoverBadgeRequest(
                title,
                description,
                rightScreen,
                showBelow: false,
                tooltipPivot: new Vector2(0f, 0.5f));
        }
    }
}
