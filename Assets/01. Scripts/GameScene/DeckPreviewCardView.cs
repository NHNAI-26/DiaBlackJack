using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>One pre-authored scroll-grid slot in <see cref="DeckPreviewView"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckPreviewCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image faceImage;
        [SerializeField] private GameObject hoverFrame;
        [SerializeField] private TMP_Text fallbackText;

        private GameSceneCardViewModel _card;

        public event Action<DeckPreviewCardView, bool> HoverChanged;

        public string HoverText { get; private set; } = string.Empty;

        public void Render(GameSceneCardViewModel card, Sprite faceSprite)
        {
            _card = card;
            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }

            if (fallbackText != null)
            {
                fallbackText.text = card == null
                    ? string.Empty
                    : $"{card.Rank}\n{card.DisplayName}";
                fallbackText.gameObject.SetActive(faceSprite == null && card != null);
            }

            HoverText = card == null
                ? string.Empty
                : string.IsNullOrEmpty(card.AbilityDescription)
                    ? $"{card.Rank} {card.DisplayName}"
                    : $"{card.Rank} {card.DisplayName}\n{card.AbilityDescription}";
            SetHoverFrame(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_card == null)
            {
                return;
            }

            SetHoverFrame(true);
            HoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHoverFrame(false);
            HoverChanged?.Invoke(this, false);
        }

        private void SetHoverFrame(bool visible)
        {
            if (hoverFrame != null)
            {
                hoverFrame.SetActive(visible);
            }
        }
    }
}
