using Border.Audio;
using DiaBlackJack.Content;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class DemonCardView : MonoBehaviour
    {
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private TMP_Text englishNameText;
        [SerializeField] private CardContentCatalogSO cardContentCatalog;

        [Header("Hover badge anchor")]
        [Tooltip("World-space anchor projected to the HUD while this demon card is hovered.")]
        [SerializeField] private Transform topPosition;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float scaleLerp = 12f;
        [SerializeField] private string hoverSfxId = "cardHover";
        private SpriteRenderer _frontSpriteRenderer;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private bool _showingFrontFace = true;
        private bool _showBadgeOnHover;
        private bool _hovered;

        public int CardId { get; private set; } = -1;

        public bool CanUse { get; private set; }

        public string HoverBadgeTitle { get; private set; } = string.Empty;

        public string HoverBadgeDescription { get; private set; } = string.Empty;

        public string HoverBadgeText => string.IsNullOrEmpty(HoverBadgeDescription)
            ? HoverBadgeTitle
            : $"{HoverBadgeTitle}\n{HoverBadgeDescription}";

        public bool ShouldShowHoverBadge =>
            _hovered && _showBadgeOnHover && !string.IsNullOrEmpty(HoverBadgeTitle);

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
        }

        private void Update()
        {
            Vector3 current = transform.localScale;
            if ((current - _targetScale).sqrMagnitude > 0.0000001f)
            {
                transform.localScale = Vector3.Lerp(current, _targetScale, Time.deltaTime * scaleLerp);
            }
        }

        public void Bind(GameSceneDemonCardViewModel card)
        {
            if (card == null)
            {
                return;
            }

            CardId = card.CardId;
            CanUse = card.CanUse;
            _showingFrontFace = card.IsFaceUp;
            _showBadgeOnHover = CanUse || card.ShowHoverBadgeWhenUnavailable;

            if (front != null)
            {
                front.SetActive(_showingFrontFace);
            }

            if (_showingFrontFace)
            {
                ApplyFaceSprite(card.DefinitionKey);
            }

            if (back != null)
            {
                back.SetActive(!_showingFrontFace);
            }

            string englishName = card.DefinitionKey.ToUpperInvariant();
            if (englishNameText != null)
            {
                englishNameText.text = _showingFrontFace ? englishName : string.Empty;
                englishNameText.gameObject.SetActive(_showingFrontFace);
            }

            HoverBadgeTitle = !card.IsFaceUp ? string.Empty : englishName;
            HoverBadgeDescription = !card.IsFaceUp
                ? string.Empty
                : FormatBadgeDescription(card);

            _hovered = false;
            transform.localScale = _baseScale;
            _targetScale = _baseScale;
        }

        public void SetHovered(bool hovered)
        {
            if (_hovered == hovered)
            {
                return;
            }

            _hovered = hovered;
            PlayHoverSfx(hovered);
            _targetScale = hovered ? _baseScale * hoverScale : _baseScale;
        }

        public Sprite GetFaceSprite(string definitionKey)
        {
            return cardContentCatalog == null
                ? null
                : cardContentCatalog.GetDemonFaceSprite(definitionKey);
        }

        public bool TryGetHoverBadgeScreenPosition(Camera camera, out Vector2 screenPosition)
        {
            screenPosition = default;
            if (camera == null || topPosition == null)
            {
                return false;
            }

            Vector3 projected = camera.WorldToScreenPoint(topPosition.position);
            if (projected.z <= 0f)
            {
                return false;
            }

            screenPosition = new Vector2(projected.x, projected.y);
            return true;
        }

        private static string FormatBadgeDescription(GameSceneDemonCardViewModel card)
        {
            string text = string.Empty;
            if (!string.IsNullOrEmpty(card.Summary))
            {
                text = card.Summary;
            }

            if (!string.IsNullOrEmpty(card.CostSummary))
            {
                text = string.IsNullOrEmpty(text)
                    ? card.CostSummary
                    : text + "\n" + card.CostSummary;
            }

            return text;
        }

        private void ApplyFaceSprite(string definitionKey)
        {
            SpriteRenderer renderer = FrontSpriteRenderer();
            Sprite sprite = GetFaceSprite(definitionKey);
            if (renderer != null && sprite != null)
            {
                renderer.sprite = sprite;
            }
        }

        private void PlayHoverSfx(bool hovered)
        {
            if (!hovered || string.IsNullOrWhiteSpace(hoverSfxId))
            {
                return;
            }

            SoundManager.Current?.PlaySfx(hoverSfxId);
        }

        private SpriteRenderer FrontSpriteRenderer()
        {
            if (_frontSpriteRenderer == null && front != null)
            {
                _frontSpriteRenderer = front.GetComponent<SpriteRenderer>();
            }

            return _frontSpriteRenderer;
        }
    }
}
