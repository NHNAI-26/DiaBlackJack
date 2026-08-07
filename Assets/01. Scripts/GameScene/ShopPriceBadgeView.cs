using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ShopPriceBadgeView : MonoBehaviour
    {
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Color soldOutColor =
            new Color(0.52f, 0.52f, 0.52f, 1f);

        private RectTransform _root;
        private Color _availableColor = Color.white;
        private bool _isInitialized;

        public string Value => priceText == null
            ? string.Empty
            : priceText.text;

        public bool HasRequiredReferences => priceText != null;

        private void Awake()
        {
            EnsureInitialized();
        }

        internal void Render(ShopPriceBadgeRequest request)
        {
            EnsureInitialized();
            if (_root == null || priceText == null)
            {
                Hide();
                return;
            }

            CurrencyIconText.Set(
                priceText,
                request.IsSoldOut
                    ? "품절"
                    : $"{CurrencyIconMarkup.GoldTag} × {request.Price}");
            priceText.color = request.IsSoldOut
                ? soldOutColor
                : _availableColor;
            _root.gameObject.SetActive(true);
        }

        internal void SetLocalPosition(
            Vector2 localPosition,
            RectTransform bounds)
        {
            EnsureInitialized();
            if (_root == null)
            {
                return;
            }

            if (bounds != null)
            {
                Rect boundsRect = bounds.rect;
                Rect badgeRect = _root.rect;
                Vector2 pivot = _root.pivot;
                localPosition.x = Mathf.Clamp(
                    localPosition.x,
                    boundsRect.xMin + badgeRect.width * pivot.x,
                    boundsRect.xMax - badgeRect.width * (1f - pivot.x));
                localPosition.y = Mathf.Clamp(
                    localPosition.y,
                    boundsRect.yMin + badgeRect.height * pivot.y,
                    boundsRect.yMax - badgeRect.height * (1f - pivot.y));
            }

            _root.anchoredPosition = localPosition;
        }

        internal void Hide()
        {
            EnsureInitialized();
            if (_root != null)
            {
                _root.gameObject.SetActive(false);
            }
        }

        private void EnsureInitialized()
        {
            if (_isInitialized)
            {
                return;
            }

            _root = transform as RectTransform;
            if (priceText != null)
            {
                _availableColor = priceText.color;
            }

            DisableRaycastTargets();
            _isInitialized = true;
        }

        private void DisableRaycastTargets()
        {
            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }
        }
    }
}
