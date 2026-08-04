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
        [SerializeField] private GameObject selectionFooter;
        [SerializeField] private Button confirmButton;
        [SerializeField] private CanvasGroup confirmButtonGroup;
        [SerializeField] private ScrollRect cardScrollRect;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private DeckPreviewCardView[] cardSlots = Array.Empty<DeckPreviewCardView>();

        private GameSceneDeckViewModel _model;
        private CardView _cardVisualSource;
        private DeckPreviewCardView _hoveredSlot;
        private DeckPreviewCardView _selectedSlot;
        private Coroutine _enableRaycasterRoutine;
        private bool _controlsBound;
        private bool _confirmationPending;
        private bool _isSingleSelection;

        public event Action<CardHoverBadgeRequest> HoverBadgeRequested;

        public event Action HoverBadgeCleared;

        public event Action<int> SelectionConfirmed;

        public event Action SelectionCancelled;

        public bool IsOpen { get; private set; }

        public int CardCount => IsOpen && _model != null ? _model.CardCount : 0;

        public int GroupCount => IsOpen && _model != null ? _model.GroupCount : 0;

        public int CardSlotCount => cardSlots == null ? 0 : cardSlots.Length;

        public bool ConfirmButtonInteractable =>
            confirmButton != null && confirmButton.interactable;

        internal float ConfirmButtonAlpha =>
            confirmButtonGroup == null ? 1f : confirmButtonGroup.alpha;

        public bool HasSelection => _selectedSlot != null;

        public bool IsSingleSelection => IsOpen && _isSingleSelection;

        public int? SelectedCardId => _selectedSlot == null
            ? null
            : _selectedSlot.CardId;

        private void Awake()
        {
            BindControls();
            SetSlotsVisible(false);
        }

        private void OnDisable()
        {
            ClearHoveredSlot();
            ClearSelectedSlot();
            IsOpen = false;
            _confirmationPending = false;
            _isSingleSelection = false;
            _model = null;
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
            OpenInternal(model, isSingleSelection: false);
        }

        public void OpenForSingleSelection(GameSceneDeckViewModel model)
        {
            OpenInternal(model, isSingleSelection: true);
        }

        private void OpenInternal(
            GameSceneDeckViewModel model,
            bool isSingleSelection)
        {
            BindControls();
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _confirmationPending = false;
            _isSingleSelection = isSingleSelection;
            ClearSelectedSlot();
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
            if (selectionFooter != null)
            {
                selectionFooter.SetActive(_isSingleSelection);
            }

            UpdateConfirmButton();
            RenderSlots();
            ResetScrollToTop();

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
            _confirmationPending = false;
            _isSingleSelection = false;
            _model = null;
            ClearHoveredSlot();
            ClearSelectedSlot();
            if (_enableRaycasterRoutine != null)
            {
                StopCoroutine(_enableRaycasterRoutine);
                _enableRaycasterRoutine = null;
            }

            SetSlotsVisible(false);
            if (selectionFooter != null)
            {
                selectionFooter.SetActive(false);
            }

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
                backgroundCloseButton.onClick.AddListener(RequestCancel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(RequestCancel);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(ConfirmSelection);
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
                    cardSlots[i].Clicked += HandleSlotClicked;
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
                backgroundCloseButton.onClick.RemoveListener(RequestCancel);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(RequestCancel);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(ConfirmSelection);
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
                    cardSlots[i].Clicked -= HandleSlotClicked;
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
            int displayedCount = Mathf.Min(_model.GroupCount, slotCount);
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

                GameSceneDeckCardGroupViewModel group =
                    _model.CardGroups[i];
                GameSceneCardViewModel card = group.Card;
                Sprite faceSprite = _cardVisualSource == null
                    ? null
                    : _cardVisualSource.GetFaceSprite(card);
                slot.Render(
                    group,
                    faceSprite,
                    _isSingleSelection ? card.CanUse : null);
            }

            if (slotCount > 0 && _model.GroupCount > slotCount)
            {
                Debug.LogError(
                    $"Deck preview has {_model.GroupCount} groups but only {slotCount} authored slots.",
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
                CardHoverBadgeRequest request =
                    slot.CreateHoverBadgeRequest();
                if (request != null)
                {
                    HoverBadgeRequested?.Invoke(request);
                }

                return;
            }

            if (_hoveredSlot == slot)
            {
                _hoveredSlot = null;
                HoverBadgeCleared?.Invoke();
            }
        }

        private void HandleSlotClicked(DeckPreviewCardView slot)
        {
            if (!IsSingleSelection ||
                _confirmationPending ||
                slot == null ||
                !slot.CanSelect)
            {
                return;
            }

            if (_selectedSlot != null && _selectedSlot != slot)
            {
                _selectedSlot.SetSelected(false);
            }

            _selectedSlot = slot;
            _selectedSlot.SetSelected(true);
            UpdateConfirmButton();
        }

        private void ConfirmSelection()
        {
            if (!IsSingleSelection ||
                _confirmationPending ||
                _selectedSlot == null)
            {
                return;
            }

            _confirmationPending = true;
            UpdateConfirmButton();
            SelectionConfirmed?.Invoke(_selectedSlot.CardId);
        }

        private void RequestCancel()
        {
            bool wasSingleSelection = IsSingleSelection;
            Close();
            if (wasSingleSelection)
            {
                SelectionCancelled?.Invoke();
            }
        }

        private void ClearSelectedSlot()
        {
            if (_selectedSlot != null)
            {
                _selectedSlot.SetSelected(false);
                _selectedSlot = null;
            }

            UpdateConfirmButton();
        }

        private void UpdateConfirmButton()
        {
            bool canConfirm =
                IsOpen &&
                _isSingleSelection &&
                !_confirmationPending &&
                _selectedSlot != null;
            if (confirmButton != null)
            {
                confirmButton.interactable = canConfirm;
            }

            if (confirmButtonGroup != null)
            {
                confirmButtonGroup.alpha = canConfirm ? 1f : 0.5f;
            }
        }

        private void ClearHoveredSlot()
        {
            if (_hoveredSlot == null)
            {
                return;
            }

            _hoveredSlot = null;
            HoverBadgeCleared?.Invoke();
        }
    }
}
