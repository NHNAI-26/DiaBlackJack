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
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject hoverBadge;
        [SerializeField] private TMP_Text hoverText;

        [Header("Hover feel")]
        [SerializeField] private float hoverScale = 1.12f;
        [SerializeField] private float scaleLerp = 12f;
        [SerializeField] private Color availableColor = new Color(0.9f, 0.78f, 0.42f);
        [SerializeField] private Color unavailableColor = new Color(0.32f, 0.32f, 0.32f);
        [SerializeField] private Color hoverColor = new Color(1f, 0.9f, 0.25f);

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private MaterialPropertyBlock _propertyBlock;
        private Vector3 _baseScale = Vector3.one;
        private Vector3 _targetScale = Vector3.one;
        private Camera _camera;
        private bool _hovered;

        public ShopUtilityItemKind Kind => kind;

        public bool CanUse { get; private set; }

        private void Awake()
        {
            _baseScale = transform.localScale;
            _targetScale = _baseScale;
            if (hoverBadge != null)
            {
                hoverBadge.SetActive(false);
            }
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
                nameText.text = displayName ?? string.Empty;
            }

            if (hoverText != null)
            {
                hoverText.text = description ?? string.Empty;
            }

            ApplyHoverVisuals();
        }

        public void SetHovered(bool hovered)
        {
            _hovered = hovered;
            _targetScale = hovered ? _baseScale * hoverScale : _baseScale;
            ApplyHoverVisuals();
        }

        private void ApplyHoverVisuals()
        {
            if (hoverBadge != null)
            {
                hoverBadge.SetActive(_hovered);
            }

            Color color = _hovered && CanUse
                ? hoverColor
                : CanUse ? availableColor : unavailableColor;
            ApplyTint(color);
        }

        private void ApplyTint(Color color)
        {
            Renderer renderer = BodyRenderer();
            if (renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private Renderer BodyRenderer()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponentInChildren<Renderer>();
            }

            return bodyRenderer;
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
