using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    public enum ShopUtilityItemKind
    {
        Lighter = 0,
        Whiskey = 1
    }

    [DisallowMultipleComponent]
    public sealed class ShopUtilityItemView : MonoBehaviour
    {
        [SerializeField] private ShopUtilityItemKind kind;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject hoverBadge;
        [SerializeField] private TMP_Text hoverText;

        [Header("Display model")]
        [SerializeField] private GameObject displayModelPrefab;
        [SerializeField] private Renderer placeholderRenderer;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.12f;
        [SerializeField] private float scaleLerp = 12f;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private Camera _camera;
        private bool _hovered;

        public ShopUtilityItemKind Kind => kind;

        internal GameObject DisplayModelPrefab => displayModelPrefab;

        public bool CanUse { get; private set; }

        private void Awake()
        {
            CreateDisplayModel();
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
            if (hoverBadge != null)
            {
                hoverBadge.SetActive(false);
            }
        }

        private void CreateDisplayModel()
        {
            if (displayModelPrefab == null)
            {
                return;
            }

            if (placeholderRenderer != null)
            {
                placeholderRenderer.enabled = false;
            }

            GameObject model = Instantiate(displayModelPrefab, transform);
            model.name = displayModelPrefab.name;
            model.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        }

        private void Update()
        {
            Vector3 current = transform.localScale;
            if ((current - _targetScale).sqrMagnitude > 0.0000001f)
            {
                transform.localScale = Vector3.Lerp(
                    current,
                    _targetScale,
                    Time.deltaTime * scaleLerp);
            }

            FaceLabelsToCamera();
        }

        public void Bind(
            ShopUtilityItemKind itemKind,
            string displayName,
            string description,
            bool canUse)
        {
            kind = itemKind;
            CanUse = canUse;

            if (nameText != null)
            {
                CurrencyIconText.Set(nameText, displayName);
            }

            if (hoverText != null)
            {
                CurrencyIconText.Set(hoverText, description);
            }

            UpdateHoverBadge();
        }

        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
            _targetScale = hovered ? _baseScale * hoverScale : _baseScale;
            UpdateHoverBadge();
        }

        private void UpdateHoverBadge()
        {
            if (hoverBadge != null)
            {
                hoverBadge.SetActive(_hovered);
            }

        }

        private void FaceLabelsToCamera()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                return;
            }

            FaceTextToCamera(nameText);
            FaceTextToCamera(hoverText);
        }

        private void FaceTextToCamera(TMP_Text text)
        {
            if (text == null)
            {
                return;
            }

            Transform textTransform = text.transform;
            Vector3 direction = textTransform.position - _camera.transform.position;
            if (direction.sqrMagnitude > 0.0001f)
            {
                textTransform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }
        }
    }
}
