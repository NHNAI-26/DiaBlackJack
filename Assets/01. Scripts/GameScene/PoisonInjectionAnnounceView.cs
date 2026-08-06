using System;
using DG.Tweening;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Plays the Enforcer poison-injection announcement: a card appears where it's placed
    /// (authored center-screen), holds briefly, then drops away and fades out. Purpose-built —
    /// no source weapon/UI animation was reusable for this (see ContractPaperClickable's
    /// disappear effect for the same situation) — using the same DOTween + OnComplete idiom
    /// CharacterView.PlayExitAnimation and ContractPaperClickable.PlayDisappearAnimation already
    /// use elsewhere in this codebase.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PoisonInjectionAnnounceView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer cardRenderer;
        [SerializeField] private float holdSeconds = 0.6f;
        [SerializeField] private float dropSeconds = 0.45f;
        [SerializeField] private float dropDistance = 3f;
        [SerializeField] private Ease dropEase = Ease.InQuad;

        private Vector3 _baseLocalPosition;
        private bool _hasBaseLocalPosition;
        private Sequence _sequence;

        private void Awake()
        {
            EnsureBaseLocalPosition();
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            _sequence?.Kill();
            _sequence = null;
        }

        public void Play(Sprite frontSprite, Action onComplete)
        {
            if (cardRenderer == null || frontSprite == null)
            {
                onComplete?.Invoke();
                return;
            }

            EnsureBaseLocalPosition();
            _sequence?.Kill();

            cardRenderer.sprite = frontSprite;
            Color color = cardRenderer.color;
            color.a = 1f;
            cardRenderer.color = color;
            transform.localPosition = _baseLocalPosition;
            gameObject.SetActive(true);

            Sequence sequence = DOTween.Sequence();
            sequence.AppendInterval(Mathf.Max(0f, holdSeconds));
            sequence.Append(transform
                .DOLocalMoveY(
                    _baseLocalPosition.y - dropDistance,
                    Mathf.Max(0.01f, dropSeconds))
                .SetEase(dropEase));
            sequence.Join(DOTween.To(
                    () => cardRenderer.color.a,
                    alpha => SetRendererAlpha(cardRenderer, alpha),
                    0f,
                    Mathf.Max(0.01f, dropSeconds))
                .SetEase(Ease.InQuad));
            sequence.OnComplete(() =>
            {
                _sequence = null;
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
            _sequence = sequence;
        }

        private void EnsureBaseLocalPosition()
        {
            if (_hasBaseLocalPosition)
            {
                return;
            }

            _baseLocalPosition = transform.localPosition;
            _hasBaseLocalPosition = true;
        }

        private static void SetRendererAlpha(SpriteRenderer renderer, float alpha)
        {
            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }
}
