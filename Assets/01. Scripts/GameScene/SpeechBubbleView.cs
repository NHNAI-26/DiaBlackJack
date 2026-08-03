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

        public string DisplayedText => messageText == null
            ? string.Empty
            : messageText.text;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            EnsureInitialized();
            Hide();
        }

        private void LateUpdate()
        {
            UpdateFacingAndScale();
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

            messageText.text = message;
            gameObject.SetActive(true);
            UpdateFacingAndScale();
        }

        public void Hide()
        {
            if (messageText != null)
            {
                messageText.text = string.Empty;
            }

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
