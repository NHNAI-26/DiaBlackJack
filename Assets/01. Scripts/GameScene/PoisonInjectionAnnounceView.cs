using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Plays the Enforcer poison-injection announcement: a card appears where it's placed
    /// (authored center-screen), holds briefly, then drops away and fades out. A UI element
    /// (not a world-space sprite) on purpose: the combat HUD's full-screen option-panel dimmer
    /// (<c>GameHudView.optionPanel</c>) is active for most of a normal turn and, being Canvas
    /// Screen Space - Overlay, always draws on top of anything in the 3D/sprite scene — a
    /// world-space card here would render underneath it and look cut off or swallowed no
    /// matter how far it dropped. Living under the same HUD canvas, after the option panel in
    /// sibling order, keeps it visible above that dimmer.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class PoisonInjectionAnnounceView : MonoBehaviour
    {
        // Alpha stays at 1 until FadeStartRatio through the drop and finishes exactly
        // when the move finishes, so the card can't fade out while it's still visibly
        // on screen.
        private const float FadeStartRatio = 0.55f;

        [SerializeField] private Image cardImage;
        [SerializeField] private float holdSeconds = 0.6f;
        [SerializeField] private float dropSeconds = 0.6f;
        [SerializeField] private float dropDistancePixels = 700f;
        [SerializeField] private Ease dropEase = Ease.InQuad;

        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPosition;
        private bool _hasBaseAnchoredPosition;
        private Sequence _sequence;

        public bool IsPlaying => _sequence != null;

        public float TotalDurationSeconds =>
            Mathf.Max(0f, holdSeconds) + Mathf.Max(0.01f, dropSeconds);

        private void Awake()
        {
            _rectTransform = transform as RectTransform;
            EnsureBaseAnchoredPosition();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void Play(Sprite frontSprite, Action onComplete)
        {
            if (cardImage == null || frontSprite == null)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureBaseAnchoredPosition();
            _sequence?.Kill();

            cardImage.sprite = frontSprite;
            Color color = cardImage.color;
            color.a = 1f;
            cardImage.color = color;
            _rectTransform.anchoredPosition = _baseAnchoredPosition;
            gameObject.SetActive(true);

            float clampedHoldSeconds = Mathf.Max(0f, holdSeconds);
            float clampedDropSeconds = Mathf.Max(0.01f, dropSeconds);
            float fadeStartTime = clampedDropSeconds * FadeStartRatio;
            float fadeDuration = clampedDropSeconds - fadeStartTime;

            float targetY = _baseAnchoredPosition.y - dropDistancePixels;
            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(clampedHoldSeconds);
            sequence.Append(DOTween.To(
                    () => _rectTransform.anchoredPosition,
                    pos => _rectTransform.anchoredPosition = pos,
                    new Vector2(_baseAnchoredPosition.x, targetY),
                    clampedDropSeconds)
                .SetEase(dropEase));
            sequence.Insert(
                clampedHoldSeconds + fadeStartTime,
                DOTween.To(
                        () => cardImage.color.a,
                        alpha => SetImageAlpha(cardImage, alpha),
                        0f,
                        fadeDuration)
                    .SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                _sequence = null;
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
            _sequence = sequence;
        }

        private void EnsureBaseAnchoredPosition()
        {
            if (_hasBaseAnchoredPosition)
            {
                return;
            }

            _rectTransform ??= transform as RectTransform;
            _baseAnchoredPosition = _rectTransform.anchoredPosition;
            _hasBaseAnchoredPosition = true;
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }
}
