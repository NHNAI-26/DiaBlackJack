using TMPro;
using DiaBlackJack.Rendering;
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
        [SerializeField] private HoverDescriptionTarget hoverDescriptionTarget;

        [Header("Display model")]
        [SerializeField] private GameObject displayModelPrefab;
        [SerializeField] private Renderer placeholderRenderer;

        [Header("Hover feel")]
        [SerializeField] private Color hoverOutlineColor =
            new Color(1f, 0.72f, 0.08f, 1f);
        [SerializeField] private float hoverOutlineWidthPixels = 4f;
        private Vector3 _baseScale = Vector3.one;
        private GameObject _displayModelRoot;
        private Renderer[] _outlineRenderers;
        private Camera _camera;

        public ShopUtilityItemKind Kind => kind;

        internal GameObject DisplayModelPrefab => displayModelPrefab;

        public bool CanUse { get; private set; }

        internal HoverDescriptionTarget HoverDescriptionTarget =>
            hoverDescriptionTarget;

        private void Awake()
        {
            CreateDisplayModel();
            _baseScale = transform.localScale;
        }

        private void CreateDisplayModel()
        {
            GameObject model = null;
            if (displayModelPrefab == null)
            {
                model = gameObject;
            }
            else
            {
                if (placeholderRenderer != null)
                {
                    placeholderRenderer.enabled = false;
                }

                model = Instantiate(displayModelPrefab, transform);
                model.name = displayModelPrefab.name;
                model.transform.SetLocalPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
            }

            _displayModelRoot = model;
            RefreshOutlineRenderers();
        }

        private void Update()
        {
            FaceLabelsToCamera();
        }

        public void Bind(
            ShopUtilityItemKind itemKind,
            string displayName,
            int price,
            int amount,
            bool isPlayerSoulFull,
            bool canUse)
        {
            kind = itemKind;
            CanUse = canUse;

            if (nameText != null)
            {
                CurrencyIconText.Set(nameText, displayName);
            }

            hoverDescriptionTarget?.Configure(
                itemKind == ShopUtilityItemKind.Whiskey && isPlayerSoulFull
                    ? "soul-full"
                    : null,
                new HoverDescriptionValue("gold", CurrencyIconMarkup.GoldTag),
                new HoverDescriptionValue("price", price),
                new HoverDescriptionValue("amount", amount));
        }

        public void SetHovered(bool hovered)
        {
            transform.localScale = _baseScale;
            ApplyHoverOutline(hovered);
        }

        private void ApplyHoverOutline(bool visible)
        {
            if (_outlineRenderers == null)
            {
                RefreshOutlineRenderers();
            }

            if (_outlineRenderers != null)
            {
                for (int i = 0; i < _outlineRenderers.Length; i++)
                {
                    Renderer outlineRenderer = _outlineRenderers[i];
                    if (outlineRenderer == null)
                    {
                        continue;
                    }

                    if (visible)
                    {
                        PostProcessOutlineRegistry.Register(
                            outlineRenderer,
                            hoverOutlineColor,
                            hoverOutlineWidthPixels);
                    }
                    else
                    {
                        PostProcessOutlineRegistry.Unregister(outlineRenderer);
                    }
                }
            }
        }

        private void RefreshOutlineRenderers()
        {
            GameObject root = _displayModelRoot != null ? _displayModelRoot : gameObject;
            _outlineRenderers = root != null
                ? root.GetComponentsInChildren<Renderer>(true)
                : null;
        }

        private void OnDisable()
        {
            ApplyHoverOutline(false);
        }

        private void OnDestroy()
        {
            ApplyHoverOutline(false);
        }

        private void OnValidate()
        {
            hoverOutlineWidthPixels = Mathf.Max(0f, hoverOutlineWidthPixels);
            hoverDescriptionTarget ??= GetComponent<HoverDescriptionTarget>();
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
