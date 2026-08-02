using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ShopCardOfferStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text soldOutText;
        [SerializeField] private Color availablePriceColor =
            new Color(0.95f, 0.82f, 0.55f, 1f);
        [SerializeField] private Color soldOutPriceColor =
            new Color(0.42f, 0.42f, 0.42f, 1f);

        private Vector3 _authoredLocalPosition;
        private Quaternion _authoredLocalRotation = Quaternion.identity;
        private Vector3 _authoredLocalScale = Vector3.one;
        private bool _isDetached;

        public string PriceLabel => priceText == null
            ? string.Empty
            : priceText.text;

        public bool IsSoldOut =>
            soldOutText != null && soldOutText.gameObject.activeSelf;

        internal Color PriceColor => priceText == null
            ? default
            : priceText.color;

        internal Vector3 AuthoredLocalPosition => _authoredLocalPosition;

        internal Vector3 AuthoredLocalScale => _authoredLocalScale;

        internal bool IsDetached => _isDetached;

        private void Awake()
        {
            DisableRaycastTargets();
        }

        public void Bind(int price, bool isSoldOut)
        {
            DisableRaycastTargets();
            if (priceText != null)
            {
                priceText.text = $"돈 : {price}";
                priceText.color = isSoldOut
                    ? soldOutPriceColor
                    : availablePriceColor;
            }

            if (soldOutText != null)
            {
                soldOutText.text = "SOLD OUT";
                soldOutText.gameObject.SetActive(isSoldOut);
            }
        }

        internal void DetachFromCard(Transform holder)
        {
            if (holder == null || _isDetached)
            {
                return;
            }

            _authoredLocalPosition = transform.localPosition;
            _authoredLocalRotation = transform.localRotation;
            _authoredLocalScale = transform.localScale;
            transform.SetParent(holder, false);
            transform.localRotation = _authoredLocalRotation;
            transform.localScale = _authoredLocalScale;
            gameObject.SetActive(true);
            _isDetached = true;
        }

        internal void LayoutAt(Vector3 cardLocalPosition)
        {
            if (!_isDetached)
            {
                return;
            }

            transform.localPosition = cardLocalPosition + _authoredLocalPosition;
            transform.localRotation = _authoredLocalRotation;
            transform.localScale = _authoredLocalScale;
        }

        private void DisableRaycastTargets()
        {
            if (priceText != null)
            {
                priceText.raycastTarget = false;
            }

            if (soldOutText != null)
            {
                soldOutText.raycastTarget = false;
            }
        }
    }
}
