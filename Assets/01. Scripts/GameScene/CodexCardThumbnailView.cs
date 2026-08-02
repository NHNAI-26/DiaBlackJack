using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CodexCardThumbnailView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        [SerializeField] private Image faceImage;
        [SerializeField] private TMP_Text fallbackText;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;

        [Header("Deck card hover badge")]
        [Tooltip("Local UI offset from the deck card's right-center edge.")]
        [SerializeField] private Vector2 deckCardHoverBadgeOffset =
            new Vector2(16f, 0f);

        private string _hoverTitle = string.Empty;
        private string _hoverDescription = string.Empty;
        private bool _hoverEnabled;
        private bool _hovered;

        public event Action<CodexCardThumbnailView, bool> HoverChanged;

        public void Render(string displayName, Sprite faceSprite)
        {
            RenderVisual(displayName, faceSprite);
            _hoverEnabled = false;
            _hoverTitle = string.Empty;
            _hoverDescription = string.Empty;
            if (countText != null)
            {
                countText.gameObject.SetActive(false);
            }
        }

        public void RenderDeck(
            string displayName,
            Sprite faceSprite,
            int count,
            string hoverTitle,
            string hoverDescription)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            RenderVisual(displayName, faceSprite);
            _hoverEnabled = true;
            _hoverTitle = hoverTitle ?? string.Empty;
            _hoverDescription = hoverDescription ?? string.Empty;
            if (countText != null)
            {
                countText.text = $"x{count}";
                countText.gameObject.SetActive(true);
            }
        }

        public CardHoverBadgeRequest CreateHoverBadgeRequest()
        {
            return !_hoverEnabled
                ? null
                : CardHoverBadgeRequest.CreateForDeckRect(
                    transform as RectTransform,
                    _hoverTitle,
                    _hoverDescription,
                    deckCardHoverBadgeOffset);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hoverEnabled)
            {
                return;
            }

            _hovered = true;
            HoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_hovered)
            {
                return;
            }

            _hovered = false;
            HoverChanged?.Invoke(this, false);
        }

        private void OnDisable()
        {
            _hovered = false;
        }

        private void RenderVisual(string displayName, Sprite faceSprite)
        {
            string safeName = displayName ?? string.Empty;
            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }

            if (fallbackText != null)
            {
                fallbackText.text = safeName;
                fallbackText.gameObject.SetActive(faceSprite == null);
            }

            if (nameText != null)
            {
                nameText.text = safeName;
            }
        }
    }
}
