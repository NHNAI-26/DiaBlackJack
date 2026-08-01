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

        public string PriceLabel => priceText == null
            ? string.Empty
            : priceText.text;

        public bool IsSoldOut =>
            soldOutText != null && soldOutText.gameObject.activeSelf;

        internal Color PriceColor => priceText == null
            ? default
            : priceText.color;

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
