using TMPro;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Component on the card prefab root. <see cref="front"/> / <see cref="back"/> toggle by whether
    /// the viewer may see the rank; the face-up sprite is swapped by rank. Hover feedback is driven
    /// by <see cref="SetHovered"/> (called from
    /// <c>GameManager</c>'s pointer raycast): any hovered card scales up, a hovered usable card glows,
    /// and a card whose public model enables hover information exposes its label and screen position
    /// to the shared HUD badge. Usability is orientation-independent — a face-down player card can be
    /// usable — so the glow is gated on <see cref="CanUse"/> and tints whichever face is showing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardView : MonoBehaviour
    {
        [SerializeField] private GameObject front;
        [SerializeField] private GameObject back;
        [SerializeField] private TMP_Text rankText;
        [SerializeField] private Sprite[] faceSpritesByRank = new Sprite[11];
        [SerializeField] private Sprite[] cloverFaceSpritesByRank = new Sprite[11];

        [Header("Hover badge anchor")]
        [Tooltip("World-space anchor projected to the HUD while this card is hovered.")]
        [SerializeField] private Transform topPosition;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.15f;
        [SerializeField] private float scaleLerp = 12f;
        [SerializeField] private Color glowColor = new Color(1f, 0.85f, 0.3f);

        [Header("Face-down tint")]
        [Tooltip("Tint applied to the card back (face-down) so a hidden card is distinguishable from a "
            + "face-up one even when both faces share the same sprite material. Per-instance, no extra material.")]
        [SerializeField] private Color backTint = new Color(0.72f, 0.28f, 0.28f, 1f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int BaseSpriteUvRectId = Shader.PropertyToID("_BaseSpriteUVRect");

        private MaterialPropertyBlock _propertyBlock;
        private SpriteRenderer _frontSpriteRenderer;
        private SpriteRenderer _backSpriteRenderer;
        private Renderer _frontRenderer;
        private Renderer _backRenderer;
        private Sprite _frontUvSprite;
        private Sprite _backUvSprite;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private bool _showingFrontFace = true;
        private bool _showBadgeOnHover;
        private bool _hovered;

        /// <summary>Run card id of the bound card, for pointer routing. -1 when unbound.</summary>
        public int CardId { get; private set; } = -1;

        /// <summary>Whether this card's manual effect can be activated right now (player, usable only).</summary>
        public bool CanUse { get; private set; }

        /// <summary>Text displayed by the shared HUD badge while this card is hovered.</summary>
        public string HoverBadgeText { get; private set; } = string.Empty;

        /// <summary>Whether the shared HUD badge should currently be visible for this card.</summary>
        public bool ShouldShowHoverBadge =>
            _hovered && _showBadgeOnHover && !string.IsNullOrEmpty(HoverBadgeText);

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;

            HideRankText();
            RefreshSpriteUvRects();
        }

        private void Update()
        {
            RefreshSpriteUvRects();

            Vector3 current = transform.localScale;
            if ((current - _targetScale).sqrMagnitude > 0.0000001f)
            {
                transform.localScale = Vector3.Lerp(current, _targetScale, Time.deltaTime * scaleLerp);
            }
        }

        public void Bind(GameSceneCardViewModel card)
        {
            if (card == null)
            {
                return;
            }

            CardId = card.CardId;
            CanUse = card.CanUse;
            _showingFrontFace = card.RevealRank;
            _showBadgeOnHover = CanUse || card.ShowHoverBadgeWhenUnavailable;

            if (front != null)
            {
                front.SetActive(_showingFrontFace);
            }

            if (_showingFrontFace)
            {
                ApplyFaceSprite(card.Rank, card.Suit);
            }

            if (back != null)
            {
                back.SetActive(!_showingFrontFace);
            }

            HideRankText();

            HoverBadgeText = !card.RevealRank
                ? string.Empty
                : string.IsNullOrEmpty(card.AbilityDescription)
                    ? $"{card.Rank} {card.DisplayName}"
                    : $"{card.Rank} {card.DisplayName}\n{card.AbilityDescription}";

            // Pooled cards are reused; clear any prior hover state and snap to base size.
            _hovered = false;
            transform.localScale = _baseScale;
            _targetScale = _baseScale;
            ApplyHoverVisuals();
        }

        /// <summary>Called by the pointer raycast when this card gains/loses hover.</summary>
        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
            _targetScale = hovered ? _baseScale * hoverScale : _baseScale;
            ApplyHoverVisuals();
        }

        /// <summary>
        /// Projects the authored top-position anchor into screen space.
        /// </summary>
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

        private void ApplyHoverVisuals()
        {
            bool lit = _hovered && CanUse;

            // Front keeps its own material look (cleared MPB); the hover glow overrides it when it is
            // the face-up card being hovered.
            if (lit && _showingFrontFace)
            {
                ApplyTint(FrontRenderer(), glowColor);
            }
            else
            {
                ClearTint(FrontRenderer());
            }

            // Back is tinted so a face-down card is distinguishable even though it shares the front's
            // sprite material; the glow overrides it when the face-down card is the hovered usable one.
            ApplyTint(BackRenderer(), lit && !_showingFrontFace ? glowColor : backTint);
        }

        private void ApplyFaceSprite(int rank, CardSuit suit)
        {
            SpriteRenderer renderer = FrontSpriteRenderer();
            Sprite sprite = SpriteForCard(rank, suit);
            if (renderer != null && sprite != null)
            {
                renderer.sprite = sprite;
                RefreshSpriteUvRect(renderer, ref _frontUvSprite);
            }
        }

        private Sprite SpriteForCard(int rank, CardSuit suit)
        {
            Sprite[] sprites = suit == CardSuit.Clover
                ? cloverFaceSpritesByRank
                : faceSpritesByRank;
            if (rank < 1 || sprites == null || rank >= sprites.Length)
            {
                return null;
            }

            return sprites[rank];
        }

        private void RefreshSpriteUvRects()
        {
            RefreshSpriteUvRect(FrontSpriteRenderer(), ref _frontUvSprite);
            RefreshSpriteUvRect(BackSpriteRenderer(), ref _backUvSprite);
        }

        private void RefreshSpriteUvRect(SpriteRenderer renderer, ref Sprite trackedSprite)
        {
            if (renderer == null || renderer.sprite == trackedSprite)
            {
                return;
            }

            trackedSprite = renderer.sprite;
            Vector4 uvRect = GetSpriteUvRect(trackedSprite);

            _propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector(BaseSpriteUvRectId, uvRect);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private static Vector4 GetSpriteUvRect(Sprite sprite)
        {
            if (sprite == null)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2[] uvs = sprite.uv;
            if (uvs == null || uvs.Length == 0)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int i = 1; i < uvs.Length; i++)
            {
                minimum = Vector2.Min(minimum, uvs[i]);
                maximum = Vector2.Max(maximum, uvs[i]);
            }

            Vector2 size = maximum - minimum;
            return new Vector4(
                minimum.x,
                minimum.y,
                Mathf.Max(size.x, 0.00001f),
                Mathf.Max(size.y, 0.00001f));
        }

        private void HideRankText()
        {
            if (rankText == null)
            {
                return;
            }

            rankText.enabled = false;
            rankText.gameObject.SetActive(false);
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

        private void ClearTint(Renderer renderer)
        {
            if (renderer == null)
            {
                return;
            }

            Material material = renderer.sharedMaterial;
            Color materialColor = material != null && material.HasProperty(BaseColorId)
                ? material.GetColor(BaseColorId)
                : Color.white;
            ApplyTint(renderer, materialColor);
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
                _backRenderer = BackSpriteRenderer();
            }

            return _backRenderer;
        }

        private SpriteRenderer BackSpriteRenderer()
        {
            if (_backSpriteRenderer == null && back != null)
            {
                _backSpriteRenderer = back.GetComponent<SpriteRenderer>();
            }

            return _backSpriteRenderer;
        }
    }
}
