using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Scene-authored uGUI modal for inspecting a player's pile. Card slots live in the prefab;
    /// this component only binds models and uses the same face sprites as the world card prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DeckPreviewView : MonoBehaviour
    {
        [SerializeField] private Canvas previewCanvas;
        [SerializeField] private GraphicRaycaster previewRaycaster;
        [SerializeField] private Button backgroundCloseButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private ScrollRect cardScrollRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailsText;
        [SerializeField] private DeckPreviewCardView[] cardSlots = Array.Empty<DeckPreviewCardView>();

        private GameSceneDeckViewModel _model;
        private CardView _cardVisualSource;
        private DeckPreviewCardView _hoveredSlot;
        private Coroutine _enableRaycasterRoutine;
        private bool _controlsBound;

        public bool IsOpen { get; private set; }

        public int CardCount => IsOpen && _model != null ? _model.CardCount : 0;

        public int CardSlotCount => cardSlots == null ? 0 : cardSlots.Length;

        private void Awake()
        {
            BindControls();
            SetSlotsVisible(false);
        }

        private void OnDisable()
        {
            IsOpen = false;
            _model = null;
            _hoveredSlot = null;
            _enableRaycasterRoutine = null;
        }

        private void OnDestroy()
        {
            UnbindControls();
        }

        /// <summary>Sets the authored world-card prefab used as this view's face-sprite source.</summary>
        public void Configure(CardView cardVisualSource)
        {
            _cardVisualSource = cardVisualSource;
        }

        public void Open(GameSceneDeckViewModel model)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            IsOpen = true;

            if (previewCanvas != null)
            {
                previewCanvas.enabled = true;
            }

            if (previewRaycaster != null)
            {
                previewRaycaster.enabled = false;
            }

            gameObject.SetActive(true);
            RenderSlots();
            ResetScrollToTop();
            ShowDefaultDetails();

            if (previewRaycaster != null)
            {
                _enableRaycasterRoutine =
                    StartCoroutine(EnableRaycasterNextFrame());
            }
        }

        public void Close()
        {
            if (!IsOpen && !gameObject.activeSelf)
            {
                return;
            }

            IsOpen = false;
            _model = null;
            _hoveredSlot = null;
            if (_enableRaycasterRoutine != null)
            {
                StopCoroutine(_enableRaycasterRoutine);
                _enableRaycasterRoutine = null;
            }

            SetSlotsVisible(false);
            gameObject.SetActive(false);
        }

        private void BindControls()
        {
            if (_controlsBound)
            {
                return;
            }

            _controlsBound = true;
            if (backgroundCloseButton != null)
            {
                backgroundCloseButton.onClick.AddListener(Close);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (cardSlots == null)
            {
                return;
            }

            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null)
                {
                    cardSlots[i].HoverChanged += HandleSlotHoverChanged;
                }
            }
        }

        private void UnbindControls()
        {
            if (!_controlsBound)
            {
                return;
            }

            _controlsBound = false;
            if (backgroundCloseButton != null)
            {
                backgroundCloseButton.onClick.RemoveListener(Close);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Close);
            }

            if (cardSlots == null)
            {
                return;
            }

            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null)
                {
                    cardSlots[i].HoverChanged -= HandleSlotHoverChanged;
                }
            }
        }

        private void RenderSlots()
        {
            if (_model == null)
            {
                return;
            }

            if (titleText != null)
            {
                titleText.text = $"{_model.Title}  {_model.CardCount}장";
            }

            int slotCount = CardSlotCount;
            int displayedCount = Mathf.Min(_model.CardCount, slotCount);
            for (int i = 0; i < slotCount; i++)
            {
                DeckPreviewCardView slot = cardSlots[i];
                if (slot == null)
                {
                    continue;
                }

                bool isVisible = i < displayedCount;
                slot.gameObject.SetActive(isVisible);
                if (!isVisible)
                {
                    continue;
                }

                GameSceneCardViewModel card = _model.Cards[i];
                Sprite faceSprite = _cardVisualSource == null
                    ? null
                    : _cardVisualSource.GetFaceSprite(card);
                slot.Render(card, faceSprite);
            }

            if (slotCount > 0 && _model.CardCount > slotCount)
            {
                Debug.LogError(
                    $"Deck preview has {_model.CardCount} cards but only {slotCount} authored slots.",
                    this);
            }
        }

        private void ResetScrollToTop()
        {
            if (cardScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            cardScrollRect.verticalNormalizedPosition = 1f;
            cardScrollRect.velocity = Vector2.zero;
        }

        private void SetSlotsVisible(bool visible)
        {
            if (cardSlots == null)
            {
                return;
            }

            for (int i = 0; i < cardSlots.Length; i++)
            {
                if (cardSlots[i] != null)
                {
                    cardSlots[i].gameObject.SetActive(visible);
                }
            }
        }

        private IEnumerator EnableRaycasterNextFrame()
        {
            yield return null;
            _enableRaycasterRoutine = null;
            if (IsOpen && previewRaycaster != null)
            {
                previewRaycaster.enabled = true;
            }
        }

        private void HandleSlotHoverChanged(DeckPreviewCardView slot, bool hovered)
        {
            if (!IsOpen)
            {
                return;
            }

            if (hovered)
            {
                _hoveredSlot = slot;
                if (detailsText != null)
                {
                    CurrencyIconText.Set(detailsText, slot.HoverText);
                }

                return;
            }

            if (_hoveredSlot == slot)
            {
                _hoveredSlot = null;
                ShowDefaultDetails();
            }
        }

        private void ShowDefaultDetails()
        {
            if (detailsText != null)
            {
                CurrencyIconText.Set(
                    detailsText,
                    "카드에 마우스를 올리면 이름과 효과를 확인합니다. 휠 또는 드래그로 스크롤합니다.");
            }
        }
    }
}
