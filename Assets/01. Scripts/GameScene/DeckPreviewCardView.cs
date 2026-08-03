using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>One pre-authored scroll-grid slot in <see cref="DeckPreviewView"/>.</summary>
    [DisallowMultipleComponent]
    public sealed class DeckPreviewCardView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private Image faceImage;
        [SerializeField] private GameObject hoverFrame;
        [SerializeField] private GameObject selectedFrame;
        [SerializeField] private TMP_Text fallbackText;
        [SerializeField] private TMP_Text countText;

        [Header("Deck card hover badge")]
        [Tooltip("Local UI offset from the deck card's right-center edge.")]
        [SerializeField] private Vector2 deckCardHoverBadgeOffset =
            new Vector2(16f, 0f);

        private GameSceneCardViewModel _card;

        public event Action<DeckPreviewCardView> Clicked;

        public event Action<DeckPreviewCardView, bool> HoverChanged;

        public bool CanSelect { get; private set; }

        public int CardId => _card == null ? -1 : _card.CardId;

        public bool IsSelected { get; private set; }

        public void Render(
            GameSceneDeckCardGroupViewModel group,
            Sprite faceSprite,
            bool? canSelect = null)
        {
            GameSceneCardViewModel card = group?.Card;
            _card = card;
            CanSelect = card != null && canSelect == true;
            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
                faceImage.color = !canSelect.HasValue || CanSelect
                    ? Color.white
                    : new Color(0.45f, 0.45f, 0.45f, 1f);
            }

            if (fallbackText != null)
            {
                fallbackText.text = card == null
                    ? string.Empty
                    : $"{card.Rank}\n{card.DisplayName}";
                fallbackText.gameObject.SetActive(faceSprite == null && card != null);
            }

            if (countText != null)
            {
                countText.text = group == null ? string.Empty : $"x{group.Count}";
            }

            SetHoverFrame(false);
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = CanSelect && selected;
            if (selectedFrame != null)
            {
                selectedFrame.SetActive(IsSelected);
            }
        }

        public CardHoverBadgeRequest CreateHoverBadgeRequest()
        {
            return _card == null
                ? null
                : CardHoverBadgeRequest.CreateForDeckRect(
                    transform as RectTransform,
                    $"{_card.Rank}. {_card.DisplayName}",
                    _card.AbilityDescription,
                    deckCardHoverBadgeOffset);
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!CanSelect ||
                eventData == null ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Clicked?.Invoke(this);
        }

        private void OnDisable()
        {
            SetHoverFrame(false);
            SetSelected(false);
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
