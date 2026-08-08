using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Border.UI
{
    /// <summary>
    /// Generic button hover/press feedback: scales up on hover, scales down while
    /// pressed, animated rather than snapping. Shared by every <c>DefaultButton</c>
    /// instance instead of a per-button color transition.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Selectable))]
    public sealed class UIButtonScaleFeedback : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField, Min(1f)] private float hoverScale = 1.08f;
        [SerializeField, Min(0f)] private float pressedScale = 0.92f;
        [SerializeField, Min(0f)] private float animationDuration = 0.12f;

        private Selectable _selectable;
        private RectTransform _rectTransform;
        private Vector3 _baseScale = Vector3.one;
        private bool _isHovered;
        private bool _isPressed;
        private Tween _scaleTween;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _rectTransform = transform as RectTransform;
            CaptureRestingGeometry();
        }

        private void OnEnable()
        {
            CaptureRestingGeometry();
        }

        private void OnDisable()
        {
            _isHovered = false;
            _isPressed = false;
            _scaleTween?.Kill();
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _baseScale;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            Refresh();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            Refresh();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            Refresh();
        }

        private void Refresh()
        {
            if (_rectTransform == null)
            {
                return;
            }

            bool interactable = _selectable == null || _selectable.IsInteractable();
            float scale = !interactable
                ? 1f
                : _isPressed
                    ? pressedScale
                    : _isHovered
                        ? hoverScale
                        : 1f;

            _scaleTween?.Kill();
            _scaleTween = _rectTransform
                .DOScale(_baseScale * scale, animationDuration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
        }

        private void CaptureRestingGeometry()
        {
            if (_rectTransform == null)
            {
                return;
            }

            CenterPivotWithoutMovingVisuals(_rectTransform);
            _baseScale = _rectTransform.localScale;
        }

        public void SynchronizeRestingGeometry()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
            if (_rectTransform == null)
            {
                _rectTransform = transform as RectTransform;
            }

            if (_rectTransform == null)
            {
                return;
            }

            _rectTransform.localScale = _baseScale;
            CenterPivotWithoutMovingVisuals(_rectTransform);
            _baseScale = _rectTransform.localScale;
            Refresh();
        }

        internal static void CenterPivotWithoutMovingVisuals(
            RectTransform rectTransform)
        {
            Vector2 centeredPivot = new Vector2(0.5f, 0.5f);
            if (rectTransform == null || rectTransform.pivot == centeredPivot)
            {
                return;
            }

            Vector3 worldCenter =
                rectTransform.TransformPoint(rectTransform.rect.center);
            rectTransform.pivot = centeredPivot;
            Vector3 centeredWorldCenter =
                rectTransform.TransformPoint(rectTransform.rect.center);
            rectTransform.position += worldCenter - centeredWorldCenter;
        }
    }
}
