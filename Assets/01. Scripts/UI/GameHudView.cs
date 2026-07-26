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
        [Tooltip("Pixel offset from the hovered card's upper screen-space edge.")]
        [SerializeField] private Vector2 cardHoverBadgeScreenOffset = new Vector2(0f, 24f);

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
            string text,
            Vector2 cardTopScreenPosition,
            Camera worldCamera)
        {
            if (cardHoverBadge == null ||
                cardHoverBadgeText == null ||
                string.IsNullOrEmpty(text))
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
            Vector2 adjustedScreenPosition =
                cardTopScreenPosition + cardHoverBadgeScreenOffset;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parent,
                    adjustedScreenPosition,
                    uiCamera,
                    out Vector2 localPoint))
            {
                HideCardHoverBadge();
                return;
            }

            cardHoverBadgeText.text = text;
            cardHoverBadge.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
            cardHoverBadge.gameObject.SetActive(true);
        }

        public void HideCardHoverBadge()
        {
            if (cardHoverBadge != null)
            {
                cardHoverBadge.gameObject.SetActive(false);
            }
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
