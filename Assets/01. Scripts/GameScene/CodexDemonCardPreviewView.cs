using System;
using Border.Audio;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CodexDemonCardPreviewView : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
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
        [SerializeField] private TMP_Text englishNameText;
        [SerializeField] private Material hoverOutlineMaterial;
        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.02f;
        [Min(0.01f)]
        [SerializeField] private float hoverScaleDuration = 0.08f;
        [SerializeField] private AnimationCurve hoverScaleCurve =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private string hoverSfxId = "cardHover";
        [ColorUsage(true, true)]
        [SerializeField] private Color basicHoverOutlineColor =
            new Color(5.3403134f, 5.3403134f, 5.3403134f, 1f);

        private Material _hoverOutlineMaterialInstance;
        private bool _outlineEnabled;
        private Vector3 _hoverRestingScale;
        private Tween _hoverScaleTween;
        private bool _hovered;
        private bool _hasHoverRestingScale;
        private string _definitionKey;

        internal event Action<string> Clicked;

        internal Color CurrentHoverOutlineColor => basicHoverOutlineColor;

        internal float CurrentHoverOutlineVisibility =>
            _hoverOutlineMaterialInstance == null
                ? 0f
                : _hoverOutlineMaterialInstance.GetFloat(
                    PixelOutlineVisibilityId);

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
        }

        internal void Render(
            Sprite faceSprite,
            string englishName,
            string definitionKey)
        {
            _definitionKey = definitionKey ??
                throw new ArgumentNullException(nameof(definitionKey));

            if (faceImage != null)
            {
                faceImage.sprite = faceSprite;
                faceImage.enabled = faceSprite != null;
            }

            ConfigureHoverOutline(faceSprite);
            SetHoverFeedback(false);
            SetHoverOutline(false);

            if (englishNameText != null)
            {
                CurrencyIconText.Set(englishNameText, englishName);
                englishNameText.gameObject.SetActive(
                    !string.IsNullOrWhiteSpace(englishName));
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null ||
                eventData.button != PointerEventData.InputButton.Left ||
                string.IsNullOrEmpty(_definitionKey))
            {
                return;
            }

            Clicked?.Invoke(_definitionKey);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHoverFeedback(true);
            SetHoverOutline(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHoverFeedback(false);
            SetHoverOutline(false);
        }

        private void OnDisable()
        {
            StopHoverScaleTween();
            if (_hasHoverRestingScale)
            {
                transform.localScale = _hoverRestingScale;
            }
            _hovered = false;
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
            _outlineEnabled =
                sprite != null &&
                faceImage != null &&
                hoverOutlineMaterial != null;
            if (!_outlineEnabled)
            {
                return;
            }

            EnsureHoverOutlineMaterialInstance();
            _hoverOutlineMaterialInstance.SetVector(
                BaseSpriteUvRectId,
                GetSpriteUvRect(sprite));
            _hoverOutlineMaterialInstance.SetColor(
                PixelOutlineColorId,
                basicHoverOutlineColor);
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

            return new Vector4(
                spriteSize.x <= 0f
                    ? 0f
                    : paddingPixels * drawingSize.x / spriteSize.x,
                spriteSize.y <= 0f
                    ? 0f
                    : paddingPixels * drawingSize.y / spriteSize.y,
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
            if (hovered && !string.IsNullOrWhiteSpace(hoverSfxId))
            {
                SoundManager.Current?.PlaySfx(hoverSfxId);
            }

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
                name = hoverOutlineMaterial.name + " (Codex Demon Instance)"
            };
            _hoverOutlineMaterialInstance.EnableKeyword(PixelOutlineKeyword);
            _hoverOutlineMaterialInstance.SetFloat(
                PixelOutlineVisibilityId,
                0f);
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
                basicHoverOutlineColor);
            _hoverOutlineMaterialInstance.SetFloat(
                PixelOutlineVisibilityId,
                visible && _outlineEnabled ? 1f : 0f);
            faceImage.SetMaterialDirty();
        }

        private static Vector4 GetSpriteUvRect(Sprite sprite)
        {
            Vector2[] uvs = sprite.uv;
            if (uvs == null || uvs.Length == 0)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int index = 1; index < uvs.Length; index++)
            {
                minimum = Vector2.Min(minimum, uvs[index]);
                maximum = Vector2.Max(maximum, uvs[index]);
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
