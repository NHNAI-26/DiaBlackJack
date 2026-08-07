using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// World-space text box that reveals a line one character at a time via
    /// <see cref="TMP_Text.maxVisibleCharacters"/>. The full string is assigned up front so
    /// layout (wrapping, size) never shifts mid-type; only the visible-character count animates.
    /// Mirrors <see cref="SpeechBubbleView"/>'s camera-facing world-space Canvas setup, but adds
    /// the typewriter/complete state pair the tutorial narrator's click-to-advance flow needs.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public sealed class TutorialTypewriterTextView : MonoBehaviour
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

        public bool IsComplete { get; private set; } = true;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            EnsureInitialized();
            Hide();
        }

        private void OnDisable()
        {
            StopTyping();
        }

        private void LateUpdate()
        {
            UpdateFacingAndScale();
        }

        public void Play(string text, float charactersPerSecond)
        {
            EnsureInitialized();
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new System.ArgumentException(
                    "Tutorial narrator line must contain visible text.",
                    nameof(text));
            }

            gameObject.SetActive(true);
            StopTyping();

            messageText.text = text;
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
                elapsed += Time.deltaTime;
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
            if (_typingRoutine != null)
            {
                StopCoroutine(_typingRoutine);
                _typingRoutine = null;
            }
        }

        private void UpdateFacingAndScale()
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
                    "TutorialTypewriterTextView requires a child TMP_Text reference.");
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
