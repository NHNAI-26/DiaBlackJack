using UnityEngine;

namespace DiaBlackJack.MainMenu.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class OptionalTextJitterMotion : MonoBehaviour
    {
        [SerializeField] private bool effectEnabled = true;
        [SerializeField, Min(0f)] private float verticalDrift = 3f;
        [SerializeField, Min(0f)] private float driftFrequency = 1.2f;
        [SerializeField] private Vector2 jitterAmplitude = new Vector2(1.4f, 0.8f);
        [SerializeField, Min(0f)] private float jitterFrequency = 18f;

        private RectTransform _rectTransform;
        private Vector2 _restingPosition;
        private bool _hasRestingPosition;

        internal bool EffectEnabled => effectEnabled;

        private void Awake()
        {
            CaptureRestingPosition();
        }

        private void OnEnable()
        {
            CaptureRestingPosition();
        }

        private void LateUpdate()
        {
            CaptureRestingPosition();
            if (!effectEnabled)
            {
                RestoreRestingPosition();
                return;
            }

            float time = Time.unscaledTime;
            float drift = Mathf.Sin(time * driftFrequency * Mathf.PI * 2f) *
                verticalDrift;
            float jitterX = CenteredPerlin(time * jitterFrequency, 11.3f) *
                jitterAmplitude.x;
            float jitterY = CenteredPerlin(29.7f, time * jitterFrequency) *
                jitterAmplitude.y;
            _rectTransform.anchoredPosition = _restingPosition +
                new Vector2(jitterX, drift + jitterY);
        }

        private void OnDisable()
        {
            RestoreRestingPosition();
        }

        private void OnDestroy()
        {
            RestoreRestingPosition();
        }

        private void CaptureRestingPosition()
        {
            if (_hasRestingPosition)
            {
                return;
            }

            _rectTransform ??= GetComponent<RectTransform>();
            _restingPosition = _rectTransform.anchoredPosition;
            _hasRestingPosition = true;
        }

        private void RestoreRestingPosition()
        {
            if (_hasRestingPosition && _rectTransform != null)
            {
                _rectTransform.anchoredPosition = _restingPosition;
            }
        }

        private static float CenteredPerlin(float x, float y)
        {
            return Mathf.PerlinNoise(x, y) * 2f - 1f;
        }

        private void OnValidate()
        {
            verticalDrift = Mathf.Max(0f, verticalDrift);
            driftFrequency = Mathf.Max(0f, driftFrequency);
            jitterAmplitude = new Vector2(
                Mathf.Max(0f, jitterAmplitude.x),
                Mathf.Max(0f, jitterAmplitude.y));
            jitterFrequency = Mathf.Max(0f, jitterFrequency);
        }
    }
}
