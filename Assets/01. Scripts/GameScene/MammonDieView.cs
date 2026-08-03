using System.Collections;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class MammonDieView : MonoBehaviour
    {
        [SerializeField] private Transform dieVisual;
        [SerializeField] private Collider inputCollider;
        [SerializeField] private float liftHeight = 1.15f;
        [SerializeField] private float rollDuration = 1.2f;
        [SerializeField] private float spinTurns = 2.5f;

        private Vector3 _restLocalPosition;
        private Quaternion _restLocalRotation;
        private Coroutine _rollRoutine;
        private bool _initialized;
        private bool _requestedInteractable;

        public bool IsInteractable { get; private set; }

        public int CurrentValue { get; private set; }

        private void Awake()
        {
            Initialize();
            gameObject.SetActive(false);
        }

        private void OnValidate()
        {
            liftHeight = Mathf.Max(0.1f, liftHeight);
            rollDuration = Mathf.Max(0.1f, rollDuration);
            spinTurns = Mathf.Max(0.5f, spinTurns);
            AutoBind();
        }

        public void Render(int? value, bool isInteractable)
        {
            Initialize();
            bool visible = value.HasValue;
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }

            _requestedInteractable = visible && isInteractable;
            IsInteractable = _rollRoutine == null && _requestedInteractable;
            if (inputCollider != null)
            {
                inputCollider.enabled = visible;
            }

            if (!visible || _rollRoutine != null)
            {
                return;
            }

            CurrentValue = value.Value;
            ApplyResultRotation(CurrentValue);
        }

        public void PlayRoll(int result)
        {
            if (result < 1 || result > 6)
            {
                return;
            }

            Initialize();
            gameObject.SetActive(true);
            if (_rollRoutine != null)
            {
                StopCoroutine(_rollRoutine);
            }

            _rollRoutine = StartCoroutine(Roll(result));
        }

        private IEnumerator Roll(int result)
        {
            IsInteractable = false;
            float elapsed = 0f;
            Vector3 startPosition = _restLocalPosition;
            Quaternion startRotation = dieVisual.localRotation;
            Vector3 spinEuler = new Vector3(
                360f * spinTurns,
                360f * (spinTurns + 0.75f),
                360f * (spinTurns + 0.35f));

            while (elapsed < rollDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rollDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float height = Mathf.Sin(t * Mathf.PI) * liftHeight;
                dieVisual.localPosition = startPosition + Vector3.up * height;
                dieVisual.localRotation = startRotation *
                    Quaternion.Euler(spinEuler * eased);
                yield return null;
            }

            CurrentValue = result;
            dieVisual.localPosition = _restLocalPosition;
            ApplyResultRotation(result);
            _rollRoutine = null;
            IsInteractable = _requestedInteractable;
        }

        private void ApplyResultRotation(int value)
        {
            dieVisual.localPosition = _restLocalPosition;
            dieVisual.localRotation = _restLocalRotation *
                Quaternion.Euler(GetResultEuler(value));
        }

        private static Vector3 GetResultEuler(int value)
        {
            switch (value)
            {
                case 1: return new Vector3(-90f, 0f, 0f);
                case 2: return new Vector3(0f, 0f, 90f);
                case 3: return Vector3.zero;
                case 4: return new Vector3(0f, 0f, -90f);
                case 5: return new Vector3(180f, 0f, 0f);
                case 6: return new Vector3(90f, 0f, 0f);
                default: return Vector3.zero;
            }
        }

        private void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            AutoBind();
            if (dieVisual == null)
            {
                dieVisual = transform;
            }

            _restLocalPosition = dieVisual.localPosition;
            _restLocalRotation = dieVisual.localRotation;
            _initialized = true;
        }

        private void AutoBind()
        {
            if (dieVisual == null && transform.childCount > 0)
            {
                dieVisual = transform.GetChild(0);
            }

            inputCollider ??= GetComponentInChildren<Collider>(true);
        }
    }
}
