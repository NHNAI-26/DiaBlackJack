using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the scene-placed HUD text. The Canvas and the three <see cref="TMP_Text"/> labels are
    /// authored in the scene (player soul top-left, enemy soul top-right, round top-center); this
    /// only writes their <c>.text</c>. Serialized-text convention follows
    /// <c>Localization/UILocalizeText.cs</c>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerSoulText;
        [SerializeField] private TMP_Text enemySoulText;
        [SerializeField] private TMP_Text roundText;
        [SerializeField] private TMP_Text goldText;

        [Header("Card hover badge")]
        [SerializeField] private RectTransform cardHoverBadge;
        [SerializeField] private TMP_Text cardHoverBadgeText;
        [SerializeField] private RectTransform cardHoverHeaderBadge;
        [SerializeField] private TMP_Text cardHoverHeaderText;
        [Tooltip("Pixel offset from the hovered card's screen-space anchor.")]
        [SerializeField] private Vector2 cardHoverBadgeScreenOffset = new Vector2(0f, 24f);
        [Min(0f)]
        [SerializeField] private float cardHoverBadgeSpacing = 8f;

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
            HideCardHoverBadge();
        }

        public void Render(CoreLoopViewModel core)
        {
            if (core == null)
            {
                return;
            }

            if (playerSoulText != null)
            {
                playerSoulText.text = $"YOU\n{core.PlayerSoul}";
            }

            if (enemySoulText != null)
            {
                enemySoulText.text = $"ENEMY\n{core.EnemySoul}";
            }

            if (roundText != null)
            {
                roundText.text = BuildRoundText(core);
            }
        }

        /// <summary>
        /// Writes the run gold counter (top-left, beside souls). Separate from <see cref="Render"/>
        /// because gold is GameScene-local state in the MVP, not part of the battle view-model.
        /// </summary>
        public void SetGold(int gold)
        {
            if (goldText != null)
            {
                goldText.text = $"GOLD\n{gold}";
            }
        }

        /// <summary>Shows the shared badge at a screen-space point supplied by a hovered card.</summary>
        public void ShowCardHoverBadge(
            string title,
            string description,
            Vector2 cardTopScreenPosition,
            Camera worldCamera,
            bool showBelow)
        {
            if (cardHoverBadge == null ||
                cardHoverBadgeText == null ||
                cardHoverHeaderBadge == null ||
                cardHoverHeaderText == null ||
                string.IsNullOrEmpty(title))
            {
                HideCardHoverBadge();
                return;
            }

            RectTransform parent = cardHoverBadge.parent as RectTransform;
            if (parent == null)
            {
                HideCardHoverBadge();
                return;
            }

            if (_canvas == null)
            {
                _canvas = GetComponentInParent<Canvas>();
            }

            Camera uiCamera = _canvas != null &&
                _canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? _canvas.worldCamera != null ? _canvas.worldCamera : worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    cardTopScreenPosition,
                    uiCamera,
                    out Vector2 localPoint))
            {
                HideCardHoverBadge();
                return;
            }

            cardHoverHeaderText.text = title;
            cardHoverBadgeText.text = description ?? string.Empty;

            bool hasDescription = !string.IsNullOrEmpty(description);
            PositionCardHoverTooltip(localPoint, hasDescription, showBelow);
            cardHoverHeaderBadge.gameObject.SetActive(true);
            cardHoverBadge.gameObject.SetActive(hasDescription);
        }

        public void HideCardHoverBadge()
        {
            if (cardHoverBadge != null)
            {
                cardHoverBadge.gameObject.SetActive(false);
            }

            if (cardHoverHeaderBadge != null)
            {
                cardHoverHeaderBadge.gameObject.SetActive(false);
            }
        }

        private void PositionCardHoverTooltip(
            Vector2 localPoint,
            bool hasDescription,
            bool showBelow)
        {
            Vector2 anchor = showBelow
                ? localPoint - cardHoverBadgeScreenOffset
                : localPoint + cardHoverBadgeScreenOffset;
            float bodyHeight = hasDescription ? cardHoverBadge.rect.height : 0f;
            float headerHeight = cardHoverHeaderBadge.rect.height;
            float spacing = hasDescription ? cardHoverBadgeSpacing : 0f;

            if (showBelow)
            {
                cardHoverBadge.pivot = new Vector2(0.5f, 1f);
                cardHoverHeaderBadge.pivot = new Vector2(0.5f, 1f);
                cardHoverBadge.localPosition = new Vector3(anchor.x, anchor.y, 0f);
                cardHoverHeaderBadge.localPosition = new Vector3(
                    anchor.x,
                    anchor.y + headerHeight + spacing,
                    0f);
                return;
            }

            cardHoverBadge.pivot = new Vector2(0.5f, 0f);
            cardHoverHeaderBadge.pivot = new Vector2(0.5f, 0f);
            cardHoverBadge.localPosition = new Vector3(anchor.x, anchor.y, 0f);
            cardHoverHeaderBadge.localPosition = new Vector3(
                anchor.x,
                anchor.y + bodyHeight + spacing,
                0f);
        }

        private static string BuildRoundText(CoreLoopViewModel core)
        {
            switch (core.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    return "VICTORY";
                case BattleOutcome.PlayerDefeat:
                    return "DEFEAT";
                default:
                    return $"ROUND {core.RoundNumber}";
            }
        }
    }
}
