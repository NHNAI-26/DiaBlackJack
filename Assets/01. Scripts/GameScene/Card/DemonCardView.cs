using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class DemonCardView : MonoBehaviour
    {
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private Sprite[] faceSpritesByIndex = new Sprite[13];
        [SerializeField] private int defaultFaceSpriteIndex = 1;

        [Header("Usable badge (hover)")]
        [SerializeField] private GameObject badge;
        [SerializeField] private TMP_Text badgeText;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float scaleLerp = 12f;
        [SerializeField] private Color glowColor = new Color(0.95f, 0.25f, 0.22f);

        [Header("Face-down tint")]
        [SerializeField] private Color backTint = new Color(0.42f, 0.12f, 0.16f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock _propertyBlock;
        private SpriteRenderer _frontSpriteRenderer;
        private Renderer _frontRenderer;
        private Renderer _backRenderer;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private bool _showingFrontFace = true;
        private bool _hovered;

        public int CardId { get; private set; } = -1;

        public bool CanUse { get; private set; }

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
            if (badge != null)
            {
                badge.SetActive(false);
            }
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

            if (front != null)
            {
                front.SetActive(_showingFrontFace);
            }

            if (_showingFrontFace)
            {
                ApplyFaceSprite(card.FaceSpriteIndex);
            }

            if (back != null)
            {
                back.SetActive(!_showingFrontFace);
            }

            if (badgeText != null)
            {
                badgeText.text = FormatBadgeText(card);
            }

            _hovered = false;
            transform.localScale = _baseScale;
            _targetScale = _baseScale;
            ApplyHoverVisuals();
        }

        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
            _targetScale = hovered ? _baseScale * hoverScale : _baseScale;
            ApplyHoverVisuals();
        }

        private static string FormatBadgeText(GameSceneDemonCardViewModel card)
        {
            string text = card.DisplayName;
            if (!string.IsNullOrEmpty(card.Summary))
            {
                text += "\n" + card.Summary;
            }

            if (!string.IsNullOrEmpty(card.CostSummary))
            {
                text += "\n" + card.CostSummary;
            }

            return text;
        }

        private void ApplyHoverVisuals()
        {
            bool lit = _hovered && CanUse;

            if (badge != null)
            {
                badge.SetActive(_hovered);
            }

            if (lit && _showingFrontFace)
            {
                ApplyTint(FrontRenderer(), glowColor);
            }
            else
            {
                ClearTint(FrontRenderer());
            }

            ApplyTint(BackRenderer(), lit && !_showingFrontFace ? glowColor : backTint);
        }

        private void ApplyFaceSprite(int index)
        {
            SpriteRenderer renderer = FrontSpriteRenderer();
            Sprite sprite = SpriteForIndex(index) ?? SpriteForIndex(defaultFaceSpriteIndex);
            if (renderer != null && sprite != null)
            {
                renderer.sprite = sprite;
            }
        }

        private Sprite SpriteForIndex(int index)
        {
            if (index < 1 || faceSpritesByIndex == null || index >= faceSpritesByIndex.Length)
            {
                return null;
            }

            return faceSpritesByIndex[index];
        }

        private void ApplyTint(Renderer renderer, Color color)
        {
            if (renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private static void ClearTint(Renderer renderer)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);
            }
        }

        private Renderer FrontRenderer()
        {
            if (_frontRenderer == null && front != null)
            {
                _frontRenderer = FrontSpriteRenderer();
            }

            return _frontRenderer;
        }

        private SpriteRenderer FrontSpriteRenderer()
        {
            if (_frontSpriteRenderer == null && front != null)
            {
                _frontSpriteRenderer = front.GetComponent<SpriteRenderer>();
            }

            return _frontSpriteRenderer;
        }

        private Renderer BackRenderer()
        {
            if (_backRenderer == null && back != null)
            {
                _backRenderer = back.GetComponent<Renderer>();
            }

            return _backRenderer;
        }
    }
}
