using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Reusable world-space speech bubble. The bubble follows its authored parent position while
    /// facing the active camera and compensating for uniform parent-scale changes.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class SpeechBubbleView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private bool faceCamera = true;
        [SerializeField] private bool preserveWorldScale = true;

        private Camera _camera;
        private Vector3 _authoredLocalScale;
        private Vector3 _authoredParentLossyScale;
        private bool _initialized;
        private Coroutine _typingRoutine;
        private int _totalCharacterCount;

        public string DisplayedText => messageText == null
            ? string.Empty
            : messageText.text;

        public bool IsVisible => gameObject.activeSelf;

        public bool IsComplete { get; private set; } = true;

        private void Awake()
        {
            EnsureInitialized();
            Hide();
        }

        private void LateUpdate()
        {
            UpdateFacingAndScale();
        }

        private void OnDisable()
        {
            StopTyping();
        }

        public void Show(string message)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new System.ArgumentException(
                    "Speech bubble message must contain visible text.",
                    nameof(message));
            }

            gameObject.SetActive(true);
            StopTyping();
            messageText.text = message;
            messageText.ForceMeshUpdate();
            _totalCharacterCount = messageText.textInfo.characterCount;
            messageText.maxVisibleCharacters = int.MaxValue;
            IsComplete = true;
            UpdateFacingAndScale();
        }

        public void Play(string message, float charactersPerSecond)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new System.ArgumentException(
                    "Speech bubble message must contain visible text.",
                    nameof(message));
            }

            gameObject.SetActive(true);
            StopTyping();
            messageText.text = message;
            messageText.ForceMeshUpdate();
            _totalCharacterCount = messageText.textInfo.characterCount;
            messageText.maxVisibleCharacters = 0;
            IsComplete = _totalCharacterCount == 0;
            UpdateFacingAndScale();

            if (!IsComplete && isActiveAndEnabled)
            {
                _typingRoutine = StartCoroutine(
                    TypeRoutine(Mathf.Max(charactersPerSecond, 1f)));
            }
        }

        public void CompleteImmediately()
        {
            EnsureInitialized();
            StopTyping();
            messageText.maxVisibleCharacters = _totalCharacterCount;
            IsComplete = true;
        }

        public void Hide()
        {
            StopTyping();
            if (messageText != null)
            {
                messageText.text = string.Empty;
                messageText.maxVisibleCharacters = 0;
            }

            _totalCharacterCount = 0;
            IsComplete = true;
            gameObject.SetActive(false);
        }

        internal void UpdateFacingAndScale()
        {
            EnsureInitialized();
            if (faceCamera)
            {
                if (_camera == null)
                {
                    _camera = Camera.main;
                }

                if (_camera != null)
                {
                    transform.rotation = _camera.transform.rotation;
                }
            }

            if (!preserveWorldScale || transform.parent == null)
            {
                return;
            }

            Vector3 parentScale = transform.parent.lossyScale;
            transform.localScale = new Vector3(
                CompensateScale(
                    _authoredLocalScale.x,
                    _authoredParentLossyScale.x,
                    parentScale.x),
                CompensateScale(
                    _authoredLocalScale.y,
                    _authoredParentLossyScale.y,
                    parentScale.y),
                CompensateScale(
                    _authoredLocalScale.z,
                    _authoredParentLossyScale.z,
                    parentScale.z));
        }

        internal void SetCameraForTesting(Camera camera)
        {
            _camera = camera;
        }

        private IEnumerator TypeRoutine(float charactersPerSecond)
        {
            float secondsPerCharacter = 1f / charactersPerSecond;
            float elapsed = 0f;
            int visibleCount = 0;

            while (visibleCount < _totalCharacterCount)
            {
                elapsed += Time.unscaledDeltaTime;
                int nextVisibleCount = Mathf.Min(
                    _totalCharacterCount,
                    Mathf.FloorToInt(elapsed / secondsPerCharacter));
                if (nextVisibleCount != visibleCount)
                {
                    visibleCount = nextVisibleCount;
                    messageText.maxVisibleCharacters = visibleCount;
                }

                yield return null;
            }

            messageText.maxVisibleCharacters = _totalCharacterCount;
            IsComplete = true;
            _typingRoutine = null;
        }

        private void StopTyping()
        {
            if (_typingRoutine == null)
            {
                return;
            }

            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            if (messageText == null)
            {
                messageText = GetComponentInChildren<TMP_Text>(true);
            }

            if (messageText == null)
            {
                throw new MissingReferenceException(
                    "SpeechBubbleView requires a child TMP_Text reference.");
            }

            Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                graphics[i].raycastTarget = false;
            }

            _authoredLocalScale = transform.localScale;
            _authoredParentLossyScale = transform.parent == null
                ? Vector3.one
                : transform.parent.lossyScale;
            _initialized = true;
        }

        private static float CompensateScale(
            float authoredLocalScale,
            float authoredParentScale,
            float currentParentScale)
        {
            return Mathf.Approximately(currentParentScale, 0f)
                ? authoredLocalScale
                : authoredLocalScale * authoredParentScale / currentParentScale;
        }
    }
}
