using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OptionalWorldSpaceCanvasExperiment : MonoBehaviour
    {
        [SerializeField] private bool experimentEnabled = true;
        [SerializeField] private Vector3 worldPosition = new Vector3(0f, 4.2f, 8f);
        [SerializeField] private Vector3 worldEulerAngles = new Vector3(26f, 180f, 0f);
        [SerializeField, Min(0.0001f)] private float worldScale = 0.006f;
        [SerializeField] private Vector2 referenceSize = new Vector2(1920f, 1080f);

        private Canvas _canvas;
        private RectTransform _rectTransform;
        private RenderMode _legacyRenderMode;
        private Camera _legacyWorldCamera;
        private Vector3 _legacyPosition;
        private Quaternion _legacyRotation;
        private Vector3 _legacyScale;
        private Vector2 _legacySize;
        private bool _legacyCaptured;

        internal bool ExperimentEnabled => experimentEnabled;

        private void Awake()
        {
            CaptureLegacyState();
            ApplyConfiguredState();
        }

        private void OnEnable()
        {
            CaptureLegacyState();
            ApplyConfiguredState();
        }

        private void OnDisable()
        {
            RestoreLegacyState();
        }

        private void OnDestroy()
        {
            RestoreLegacyState();
        }

        private void CaptureLegacyState()
        {
            if (_legacyCaptured)
            {
                return;
            }

            _canvas = GetComponent<Canvas>();
            _rectTransform = GetComponent<RectTransform>();
            _legacyRenderMode = _canvas.renderMode;
            _legacyWorldCamera = _canvas.worldCamera;
            _legacyPosition = _rectTransform.position;
            _legacyRotation = _rectTransform.rotation;
            _legacyScale = _rectTransform.localScale;
            _legacySize = _rectTransform.sizeDelta;
            _legacyCaptured = true;
        }

        private void ApplyConfiguredState()
        {
            if (!_legacyCaptured)
            {
                return;
            }

            if (!experimentEnabled)
            {
                RestoreLegacyState();
                return;
            }

            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.worldCamera = Camera.main;
            _rectTransform.sizeDelta = referenceSize;
            _rectTransform.position = worldPosition;
            _rectTransform.rotation = Quaternion.Euler(worldEulerAngles);
            _rectTransform.localScale = Vector3.one * worldScale;
        }

        private void RestoreLegacyState()
        {
            if (!_legacyCaptured || _canvas == null || _rectTransform == null)
            {
                return;
            }

            _canvas.renderMode = _legacyRenderMode;
            _canvas.worldCamera = _legacyWorldCamera;
            _rectTransform.position = _legacyPosition;
            _rectTransform.rotation = _legacyRotation;
            _rectTransform.localScale = _legacyScale;
            _rectTransform.sizeDelta = _legacySize;
        }

        private void OnValidate()
        {
            worldScale = Mathf.Max(0.0001f, worldScale);
            referenceSize = new Vector2(
                Mathf.Max(1f, referenceSize.x),
                Mathf.Max(1f, referenceSize.y));
        }
    }
}
