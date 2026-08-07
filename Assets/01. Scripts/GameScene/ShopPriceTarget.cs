using System;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal readonly struct ShopPriceBadgeRequest
    {
        public ShopPriceBadgeRequest(
            int price,
            bool isSoldOut,
            Vector2 screenPosition)
        {
            Price = price;
            IsSoldOut = isSoldOut;
            ScreenPosition = screenPosition;
        }

        public int Price { get; }

        public bool IsSoldOut { get; }

        public Vector2 ScreenPosition { get; }
    }

    [DisallowMultipleComponent]
    public sealed class ShopPriceTarget : MonoBehaviour
    {
        [Tooltip("Per-product world anchor projected to the shop price UI.")]
        [SerializeField] private Transform priceAnchor;

        private string _productName = string.Empty;
        private int _price;
        private bool _isSoldOut;

        public Transform PriceAnchor => priceAnchor;

        public string ProductName => _productName;

        public int Price => _price;

        public bool IsSoldOut => _isSoldOut;

        public bool HasRequiredReferences => priceAnchor != null;

        internal void Bind(string productName, int price, bool isSoldOut)
        {
            if (string.IsNullOrWhiteSpace(productName))
            {
                throw new ArgumentException(
                    "Shop price product name must not be empty.",
                    nameof(productName));
            }

            _productName = productName.Trim();
            _price = Mathf.Max(0, price);
            _isSoldOut = isSoldOut;
        }

        internal bool TryCreateRequest(
            Camera camera,
            out ShopPriceBadgeRequest request)
        {
            request = default;
            if (camera == null ||
                priceAnchor == null ||
                !isActiveAndEnabled ||
                !gameObject.activeInHierarchy ||
                string.IsNullOrEmpty(_productName))
            {
                return false;
            }

            Vector3 projected = camera.WorldToScreenPoint(priceAnchor.position);
            if (projected.z <= 0f)
            {
                return false;
            }

            request = new ShopPriceBadgeRequest(
                _price,
                _isSoldOut,
                new Vector2(projected.x, projected.y));
            return true;
        }
    }
}
