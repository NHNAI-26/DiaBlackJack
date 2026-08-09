using System;
using Border.Audio;
using DG.Tweening;
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
        private const string PixelOutlineKeyword = "_PIXEL_OUTLINE_ON";
        private static readonly int PixelOutlineColorId =
            Shader.PropertyToID("_PixelOutlineColor");
        private static readonly int PixelOutlineVisibilityId =
            Shader.PropertyToID("_PixelOutlineVisibility");
        private static readonly int BaseSpriteUvRectId =
            Shader.PropertyToID("_BaseSpriteUVRect");
        private static readonly int PixelOutlineWidthId =
            Shader.PropertyToID("_PixelOutlineWidth");
        private static readonly int PixelOutlineGlowWidthId =
            Shader.PropertyToID("_PixelOutlineGlowWidth");
        private static readonly int PixelOutlineMeshPaddingId =
            Shader.PropertyToID("_PixelOutlineMeshPadding");

        [SerializeField] private Image faceImage;
        [SerializeField] private Material hoverOutlineMaterial;
        [SerializeField] private TMP_Text fallbackText;
        [SerializeField] private TMP_Text countText;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.02f;
        [Min(0.01f)]
        [SerializeField] private float hoverScaleDuration = 0.08f;
        [SerializeField] private AnimationCurve hoverScaleCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private string hoverSfxId = "cardHover";

        [Header("Hover outline state colors")]
        [ColorUsage(true, true)]
        [SerializeField] private Color basicHoverOutlineColor =
            new Color(5.3403134f, 5.3403134f, 5.3403134f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color unavailableHoverOutlineColor =
            new Color(2.1145804f, 0.6420973f, 0.6084406f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color availableHoverOutlineColor =
            new Color(1.9118087f, 3.4278064f, 0.48455897f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color automaticHoverOutlineColor =
            new Color(3.9999995f, 3.3765495f, 0f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color usedHoverOutlineColor = Color.black;

        [Header("Deck card hover badge")]
        [Tooltip("Local outward offset from the deck card's side-center edge.")]
        [SerializeField] private Vector2 deckCardHoverBadgeOffset =
            new Vector2(16f, 0f);

        private GameSceneCardViewModel _card;
        private Material _hoverOutlineMaterialInstance;
        private GameSceneCardHoverOutlineState _hoverOutlineState =
            GameSceneCardHoverOutlineState.Basic;
        private bool _outlineEnabled;
        private bool _hoverEnabled;
        private string _hoverTitle = string.Empty;
        private string _hoverDescription = string.Empty;
        private Vector3 _hoverRestingScale;
        private Tween _hoverScaleTween;
        private bool _hovered;
        private bool _pointerHovered;
        private bool _hasHoverRestingScale;

        public event Action<DeckPreviewCardView> Clicked;

        public event Action<DeckPreviewCardView, bool> HoverChanged;

        public bool CanSelect { get; private set; }

        public int CardId => _card == null ? -1 : _card.CardId;

        public bool IsSelected { get; private set; }

        internal bool IsVisuallyEmphasized => _hovered;

        internal Color CurrentHoverOutlineColor => ResolveHoverOutlineColor();

        internal float CurrentHoverOutlineVisibility =>
            _hoverOutlineMaterialInstance == null
                ? 0f
                : _hoverOutlineMaterialInstance.GetFloat(PixelOutlineVisibilityId);

        internal Vector4 CurrentHoverOutlineMeshPadding =>
            _hoverOutlineMaterialInstance == null
                ? Vector4.zero
                : _hoverOutlineMaterialInstance.GetVector(
                    PixelOutlineMeshPaddingId);

        private void Awake()
        {
            CaptureHoverRestingScale();
        }

        private void OnEnable()
        {
            CaptureHoverRestingScale();
            _hovered = false;
            _pointerHovered = false;
        }

        public void Render(
            GameSceneDeckCardGroupViewModel group,
            Sprite faceSprite,
            bool? canSelect = null)
        {
            GameSceneCardViewModel card = group?.Card;
            _card = card;
            CanSelect = card != null && canSelect == true;
            _hoverOutlineState = card == null
                ? GameSceneCardHoverOutlineState.Basic
                : card.HoverOutlineState;
            _hoverEnabled = card != null;
            _hoverTitle = card == null
                ? string.Empty
                : $"{card.Rank}. {card.DisplayName}";
            _hoverDescription = card?.AbilityDescription ?? string.Empty;
            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
                faceImage.color = !canSelect.HasValue || CanSelect
                    ? Color.white
                    : new Color(0.45f, 0.45f, 0.45f, 1f);
            }

            ConfigureHoverOutline(faceSprite);

            if (fallbackText != null)
            {
                fallbackText.text = card == null
                    ? string.Empty
                    : $"{card.Rank}\n{card.DisplayName}";
                fallbackText.gameObject.SetActive(faceSprite == null && card != null);
            }

            if (countText != null)
            {
                countText.richText = false;
                countText.text = group == null ? string.Empty : $"x{group.Count}";
            }

            ResetInteractionState();
        }

        internal void RenderCodex(
            Sprite faceSprite,
            int? count,
            string hoverTitle,
            string hoverDescription,
            GameSceneCardHoverOutlineState hoverOutlineState)
        {
            if (count.HasValue && count.Value < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            _card = null;
            CanSelect = false;
            _hoverOutlineState = hoverOutlineState;
            _hoverEnabled = !string.IsNullOrWhiteSpace(hoverTitle);
            _hoverTitle = hoverTitle ?? string.Empty;
            _hoverDescription = hoverDescription ?? string.Empty;

            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
                faceImage.color = Color.white;
            }

            ConfigureHoverOutline(faceSprite);

            if (fallbackText != null)
            {
                fallbackText.text = string.Empty;
                fallbackText.gameObject.SetActive(false);
            }

            if (countText != null)
            {
                countText.richText = false;
                countText.text = count.HasValue
                    ? $"x{count.Value}"
                    : string.Empty;
                countText.gameObject.SetActive(count.HasValue);
            }

            ResetInteractionState();
        }

        public void SetSelected(bool selected)
        {
            IsSelected = CanSelect && selected;
            RefreshVisualEmphasis();
        }

        public CardHoverBadgeRequest CreateHoverBadgeRequest(
            bool showOnLeft = false)
        {
            return !_hoverEnabled
                ? null
                : CardHoverBadgeRequest.CreateForDeckRect(
                    transform as RectTransform,
                    _hoverTitle,
                    _hoverDescription,
                    deckCardHoverBadgeOffset,
                    showOnLeft);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_hoverEnabled && !_outlineEnabled)
            {
                return;
            }

            _pointerHovered = true;
            PlayHoverSound();
            RefreshVisualEmphasis();
            if (_hoverEnabled)
            {
                HoverChanged?.Invoke(this, true);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _pointerHovered = false;
            RefreshVisualEmphasis();
            if (_hoverEnabled)
            {
                HoverChanged?.Invoke(this, false);
            }
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
            StopHoverScaleTween();
            if (_hasHoverRestingScale)
            {
                transform.localScale = _hoverRestingScale;
            }
            _hovered = false;
            _pointerHovered = false;
            IsSelected = false;
            SetHoverOutline(false);
        }

        private void OnDestroy()
        {
            StopHoverScaleTween();
            if (_hoverOutlineMaterialInstance == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_hoverOutlineMaterialInstance);
            }
            else
            {
                DestroyImmediate(_hoverOutlineMaterialInstance);
            }

            _hoverOutlineMaterialInstance = null;
        }

        private void ConfigureHoverOutline(Sprite sprite)
        {
            _outlineEnabled = sprite != null && hoverOutlineMaterial != null;
            if (!_outlineEnabled || faceImage == null)
            {
                return;
            }

            EnsureHoverOutlineMaterialInstance();
            _hoverOutlineMaterialInstance.SetVector(
                BaseSpriteUvRectId,
                GetSpriteUvRect(sprite));
            _hoverOutlineMaterialInstance.SetColor(
                PixelOutlineColorId,
                ResolveHoverOutlineColor());
            _hoverOutlineMaterialInstance.SetVector(
                PixelOutlineMeshPaddingId,
                GetOutlineMeshPadding(sprite));
            faceImage.SetMaterialDirty();
        }

        private Vector4 GetOutlineMeshPadding(Sprite sprite)
        {
            if (faceImage == null || sprite == null || sprite.texture == null)
            {
                return Vector4.zero;
            }

            float outlineWidth = _hoverOutlineMaterialInstance.HasProperty(
                    PixelOutlineWidthId)
                ? _hoverOutlineMaterialInstance.GetFloat(PixelOutlineWidthId)
                : 1f;
            float glowWidth = _hoverOutlineMaterialInstance.HasProperty(
                    PixelOutlineGlowWidthId)
                ? _hoverOutlineMaterialInstance.GetFloat(PixelOutlineGlowWidthId)
                : outlineWidth;
            float paddingPixels = Mathf.Max(outlineWidth, glowWidth);

            Vector2 drawingSize = faceImage.rectTransform.rect.size;
            Vector2 spriteSize = sprite.rect.size;
            if (faceImage.preserveAspect &&
                drawingSize.x > 0f &&
                drawingSize.y > 0f &&
                spriteSize.x > 0f &&
                spriteSize.y > 0f)
            {
                float spriteAspect = spriteSize.x / spriteSize.y;
                float rectAspect = drawingSize.x / drawingSize.y;
                if (spriteAspect > rectAspect)
                {
                    drawingSize.y = drawingSize.x / spriteAspect;
                }
                else
                {
                    drawingSize.x = drawingSize.y * spriteAspect;
                }
            }

            float localPaddingX = spriteSize.x <= 0f
                ? 0f
                : paddingPixels * drawingSize.x / spriteSize.x;
            float localPaddingY = spriteSize.y <= 0f
                ? 0f
                : paddingPixels * drawingSize.y / spriteSize.y;
            return new Vector4(
                localPaddingX,
                localPaddingY,
                paddingPixels / sprite.texture.width,
                paddingPixels / sprite.texture.height);
        }

        private void SetHoverFeedback(bool hovered)
        {
            if (_hovered == hovered)
            {
                return;
            }

            if (!_hasHoverRestingScale)
            {
                CaptureHoverRestingScale();
            }

            _hovered = hovered;
            StopHoverScaleTween();
            Vector3 targetScale = hovered
                ? _hoverRestingScale * hoverScale
                : _hoverRestingScale;
            if (!Application.isPlaying || !gameObject.activeInHierarchy)
            {
                transform.localScale = targetScale;
                return;
            }

            _hoverScaleTween = transform
                .DOScale(targetScale, Mathf.Max(hoverScaleDuration, 0.01f))
                .SetEase(hoverScaleCurve)
                .SetTarget(this);
        }

        private void RefreshVisualEmphasis()
        {
            bool emphasized = _pointerHovered || IsSelected;
            SetHoverFeedback(emphasized);
            SetHoverOutline(emphasized);
        }

        private void ResetInteractionState()
        {
            _pointerHovered = false;
            IsSelected = false;
            RefreshVisualEmphasis();
        }

        private void PlayHoverSound()
        {
            if (!string.IsNullOrWhiteSpace(hoverSfxId))
            {
                SoundManager.Current?.PlaySfx(hoverSfxId);
            }
        }

        private void CaptureHoverRestingScale()
        {
            _hoverRestingScale = transform.localScale;
            _hasHoverRestingScale = true;
        }

        private void StopHoverScaleTween()
        {
            if (_hoverScaleTween == null)
            {
                return;
            }

            _hoverScaleTween.Kill();
            _hoverScaleTween = null;
        }

        private void EnsureHoverOutlineMaterialInstance()
        {
            if (_hoverOutlineMaterialInstance != null)
            {
                return;
            }

            _hoverOutlineMaterialInstance = new Material(hoverOutlineMaterial)
            {
                name = hoverOutlineMaterial.name + " (Deck Card Instance)"
            };
            _hoverOutlineMaterialInstance.EnableKeyword(PixelOutlineKeyword);
            _hoverOutlineMaterialInstance.SetFloat(PixelOutlineVisibilityId, 0f);
            faceImage.material = _hoverOutlineMaterialInstance;
        }

        private void SetHoverOutline(bool visible)
        {
            if (_hoverOutlineMaterialInstance == null)
            {
                return;
            }

            _hoverOutlineMaterialInstance.SetColor(
                PixelOutlineColorId,
                ResolveHoverOutlineColor());
            _hoverOutlineMaterialInstance.SetFloat(
                PixelOutlineVisibilityId,
                visible && _outlineEnabled ? 1f : 0f);
            faceImage.SetMaterialDirty();
        }

        private Color ResolveHoverOutlineColor()
        {
            switch (_hoverOutlineState)
            {
                case GameSceneCardHoverOutlineState.ManualUnavailable:
                    return unavailableHoverOutlineColor;
                case GameSceneCardHoverOutlineState.ManualAvailable:
                    return availableHoverOutlineColor;
                case GameSceneCardHoverOutlineState.Automatic:
                    return automaticHoverOutlineColor;
                case GameSceneCardHoverOutlineState.Used:
                    return usedHoverOutlineColor;
                default:
                    return basicHoverOutlineColor;
            }
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
    }
}
