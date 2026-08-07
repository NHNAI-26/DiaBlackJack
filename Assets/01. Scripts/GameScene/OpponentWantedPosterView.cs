using System;
using DiaBlackJack.StageProgression.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class OpponentWantedPosterView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
    {
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private TMP_Text soulAmountText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text defeatGoldAmountText;
        [SerializeField] private Outline hoverOutline;
        [Min(1f)]
        [SerializeField] private float hoverScale = 1.04f;

        private bool _interactable;
        private bool _isHovered;
        private bool _hasRestingScale;
        private string _profileKey;
        private Vector3 _restingScale;

        public event Action<string> Selected;

        internal string DisplayedProfileKey => _profileKey;

        internal bool IsHoverOutlineVisible =>
            hoverOutline != null && hoverOutline.enabled;

        internal bool IsHovered => _isHovered;

        private void Awake()
        {
            CaptureRestingScale();
            SetHovered(false);
        }

        private void OnEnable()
        {
            CaptureRestingScale();
            SetHovered(false);
        }

        private void OnDisable()
        {
            SetHovered(false);
        }

        public void Render(
            OpponentCandidateViewModel candidate,
            Sprite portrait,
            bool interactable)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            gameObject.SetActive(true);
            _profileKey = candidate.ProfileKey;
            _interactable = interactable;

            if (portraitImage != null)
            {
                portraitImage.sprite = portrait;
                portraitImage.enabled = portrait != null;
            }

            SetText(enemyNameText, candidate.DisplayName);
            SetText(soulAmountText, candidate.SoulAmountText);
            SetText(descriptionText, candidate.Summary);
            SetText(defeatGoldAmountText, candidate.DefeatGoldAmountText);
            if (descriptionText != null)
            {
                descriptionText.maxVisibleLines = 3;
                descriptionText.overflowMode = TextOverflowModes.Ellipsis;
            }

            SetHovered(false);
        }

        public void Hide()
        {
            _profileKey = null;
            _interactable = false;
            SetHovered(false);
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            _interactable = interactable &&
                !string.IsNullOrWhiteSpace(_profileKey);
            if (!_interactable)
            {
                SetHovered(false);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_interactable)
            {
                SetHovered(true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_interactable ||
                eventData == null ||
                eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _interactable = false;
            SetHovered(false);
            Selected?.Invoke(_profileKey);
        }

        private void CaptureRestingScale()
        {
            if (!_hasRestingScale && !_isHovered)
            {
                _restingScale = transform.localScale;
                _hasRestingScale = true;
            }
        }

        private void SetHovered(bool hovered)
        {
            // The selection root starts inactive, so Hide can run before Awake on
            // nested poster slots. Capture the prefab-authored scale before any reset.
            CaptureRestingScale();
            _isHovered = hovered && _interactable;
            if (hoverOutline != null)
            {
                hoverOutline.enabled = _isHovered;
            }

            transform.localScale = _isHovered
                ? _restingScale * hoverScale
                : _restingScale;
        }

        private static void SetText(TMP_Text label, string value)
        {
            if (label != null)
            {
                label.text = value ?? string.Empty;
            }
        }
    }
}
