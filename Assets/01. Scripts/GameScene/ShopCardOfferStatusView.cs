using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class ShopCardOfferStatusView : MonoBehaviour
    {
        [SerializeField] private TMP_Text soldOutText;

        private Vector3 _authoredLocalPosition;
        private Quaternion _authoredLocalRotation = Quaternion.identity;
        private Vector3 _authoredLocalScale = Vector3.one;
        private bool _isDetached;

        public bool IsSoldOut =>
            soldOutText != null && soldOutText.gameObject.activeSelf;

        internal Vector3 AuthoredLocalPosition => _authoredLocalPosition;

        internal Vector3 AuthoredLocalScale => _authoredLocalScale;

        internal bool IsDetached => _isDetached;

        private void Awake()
        {
            DisableRaycastTargets();
        }

        public void Bind(bool isSoldOut)
        {
            DisableRaycastTargets();
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
            if (soldOutText != null)
            {
                soldOutText.raycastTarget = false;
            }
        }
    }
}
