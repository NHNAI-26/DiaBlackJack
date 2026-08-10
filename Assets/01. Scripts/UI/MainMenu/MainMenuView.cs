using System;
using System.Collections;
using System.Collections.Generic;
using Border.SaveLoad.UI;
using DG.Tweening;
using DiaBlackJack.GameScene;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.MainMenu.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Telegraph telegraph;
        [SerializeField] private CanvasGroup logoCanvasGroup;
        [SerializeField] private TMP_Text statusText;
        [SerializeField, Min(0f)] private float logoExitDuration = 0.5f;

        [Header("Startup Notice")]
        [SerializeField] private GameObject startupNoticeRoot;
        [SerializeField] private CanvasGroup startupOverlayCanvasGroup;
        [SerializeField] private CanvasGroup startupMessageCanvasGroup;
        [SerializeField] private TMP_Text startupMessageText;
        [SerializeField, TextArea]
        private string graphicsAccelerationMessage =
            "원활한 플레이를 위해\n브라우저의 그래픽 가속을 켜 주세요";
        [SerializeField, TextArea]
        private string fullscreenRecommendationMessage =
            "전체 화면 플레이를 권장합니다.";
        [SerializeField, Min(0f)]
        private float startupMessageFadeSeconds = 0.45f;
        [SerializeField, Min(0f)]
        private float startupMessageHoldSeconds = 2.1f;
        [SerializeField]
        private float startupMessageLineSpacing = 50f;
        [SerializeField, Min(0f)]
        private float startupInitialBlackHoldSeconds = 0.25f;
        [SerializeField, Min(0f)]
        private float startupOverlayFadeSeconds = 0.7f;

        private readonly List<TelegraphButton> _buttons =
            new List<TelegraphButton>();
        private Tween _logoTween;
        private Sequence _startupNoticeSequence;
        private Coroutine _startupNoticeRoutine;
        private string _authoredStatusText;
        private bool _statusTextCaptured;
        private bool _inputEnabled = true;
        private bool _exitInProgress;

        public event Action NewRunRequested;

        public event Action SettingsRequested;

        public event Action TutorialRequested;

        private void Awake()
        {
            ResolveReferences();
            CaptureAuthoredStatusText();
            SubscribeToButtons();
            if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = 1f;
            }

            ApplyStartupNoticeTextStyle();
        }

        private void OnDestroy()
        {
            UnsubscribeFromButtons();
            _logoTween?.Kill();
            _logoTween = null;
            if (_startupNoticeRoutine != null)
            {
                StopCoroutine(_startupNoticeRoutine);
                _startupNoticeRoutine = null;
            }

            _startupNoticeSequence?.Kill();
            _startupNoticeSequence = null;
        }

        public void Render(
            RunSaveViewModel model,
            bool showRuntimeStatus = false)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (statusText == null)
            {
                return;
            }

            CaptureAuthoredStatusText();
            statusText.text = showRuntimeStatus &&
                !string.IsNullOrEmpty(model.StatusMessage)
                    ? model.StatusMessage
                    : _authoredStatusText;
            statusText.gameObject.SetActive(
                !string.IsNullOrEmpty(statusText.text));
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled && !_exitInProgress;
            telegraph?.SetInputEnabled(_inputEnabled);
        }

        internal void PlayEntranceAnimation()
        {
            if (telegraph == null)
            {
                return;
            }

            if (!telegraph.gameObject.activeSelf)
            {
                telegraph.gameObject.SetActive(true);
                return;
            }

            telegraph.PlayEntranceAnimation();
        }

        internal bool PrepareStartupNotice(bool shouldShow)
        {
            if (!IsStartupNoticeConfigured())
            {
                HideStartupNotice();
                return false;
            }

            ApplyStartupNoticeTextStyle();
            startupNoticeRoot.SetActive(shouldShow);
            startupOverlayCanvasGroup.alpha = shouldShow ? 1f : 0f;
            startupOverlayCanvasGroup.interactable = shouldShow;
            startupOverlayCanvasGroup.blocksRaycasts = shouldShow;
            startupMessageCanvasGroup.alpha = 0f;
            startupMessageText.text = graphicsAccelerationMessage;
            return shouldShow;
        }

        internal bool TryPlayStartupNotice(Action onComplete)
        {
            if (!IsStartupNoticeConfigured() ||
                !startupNoticeRoot.activeSelf)
            {
                return false;
            }

            _startupNoticeSequence?.Kill();
            startupMessageCanvasGroup.alpha = 0f;
            startupOverlayCanvasGroup.alpha = 1f;
            startupOverlayCanvasGroup.interactable = true;
            startupOverlayCanvasGroup.blocksRaycasts = true;
            startupMessageText.text = graphicsAccelerationMessage;

            if (_startupNoticeRoutine != null)
            {
                StopCoroutine(_startupNoticeRoutine);
            }

            _startupNoticeRoutine = StartCoroutine(
                PlayStartupNoticeAfterFirstRender(onComplete));
            return true;
        }

        private IEnumerator PlayStartupNoticeAfterFirstRender(
            Action onComplete)
        {
            yield return null;
            _startupNoticeRoutine = null;
            if (!IsStartupNoticeConfigured() ||
                !startupNoticeRoot.activeSelf)
            {
                yield break;
            }

            _startupNoticeSequence = DOTween.Sequence()
                .SetUpdate(true)
                .AppendInterval(startupInitialBlackHoldSeconds)
                .Append(CreateCanvasGroupFade(
                    startupMessageCanvasGroup,
                    0f,
                    1f,
                    startupMessageFadeSeconds))
                .AppendInterval(startupMessageHoldSeconds)
                .Append(CreateCanvasGroupFade(
                    startupMessageCanvasGroup,
                    1f,
                    0f,
                    startupMessageFadeSeconds))
                .AppendCallback(() =>
                    startupMessageText.text =
                        fullscreenRecommendationMessage)
                .Append(CreateCanvasGroupFade(
                    startupMessageCanvasGroup,
                    0f,
                    1f,
                    startupMessageFadeSeconds))
                .AppendInterval(startupMessageHoldSeconds)
                .Append(CreateCanvasGroupFade(
                    startupMessageCanvasGroup,
                    1f,
                    0f,
                    startupMessageFadeSeconds))
                .Append(CreateCanvasGroupFade(
                    startupOverlayCanvasGroup,
                    1f,
                    0f,
                    startupOverlayFadeSeconds))
                .OnComplete(() =>
                {
                    _startupNoticeSequence = null;
                    HideStartupNotice();
                    onComplete?.Invoke();
                });
        }

        internal void HideStartupNotice()
        {
            if (startupOverlayCanvasGroup != null)
            {
                startupOverlayCanvasGroup.alpha = 0f;
                startupOverlayCanvasGroup.interactable = false;
                startupOverlayCanvasGroup.blocksRaycasts = false;
            }

            if (startupMessageCanvasGroup != null)
            {
                startupMessageCanvasGroup.alpha = 0f;
            }

            startupNoticeRoot?.SetActive(false);
        }

        public void PlayExitAnimation(Action onComplete)
        {
            if (_exitInProgress)
            {
                return;
            }

            _exitInProgress = true;
            SetInputEnabled(false);
            _logoTween?.Kill();
            _logoTween = null;

            int remainingParts = 0;
            if (telegraph != null)
            {
                remainingParts++;
            }

            if (logoCanvasGroup != null && logoExitDuration > 0f)
            {
                remainingParts++;
            }
            else if (logoCanvasGroup != null)
            {
                logoCanvasGroup.alpha = 0f;
            }

            if (remainingParts == 0)
            {
                onComplete?.Invoke();
                return;
            }

            Action completePart = () =>
            {
                remainingParts--;
                if (remainingParts == 0)
                {
                    onComplete?.Invoke();
                }
            };

            if (telegraph != null)
            {
                telegraph.PlayExitAnimation(completePart);
            }

            if (logoCanvasGroup != null && logoExitDuration > 0f)
            {
                _logoTween = DOVirtual
                    .Float(
                        logoCanvasGroup.alpha,
                        0f,
                        logoExitDuration,
                        value => logoCanvasGroup.alpha = value)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        _logoTween = null;
                        completePart();
                    });
            }
        }

        private void ResolveReferences()
        {
            telegraph ??= FindFirstObjectByType<Telegraph>(
                FindObjectsInactive.Include);
        }

        private bool IsStartupNoticeConfigured()
        {
            return startupNoticeRoot != null &&
                startupOverlayCanvasGroup != null &&
                startupMessageCanvasGroup != null &&
                startupMessageText != null;
        }

        private void ApplyStartupNoticeTextStyle()
        {
            if (startupMessageText == null || statusText == null)
            {
                return;
            }

            startupMessageText.font = statusText.font;
            startupMessageText.fontSharedMaterial =
                statusText.fontSharedMaterial;
            startupMessageText.color = statusText.color;
            startupMessageText.fontSize = statusText.fontSize;
            startupMessageText.lineSpacing = startupMessageLineSpacing;
        }

        private static Tween CreateCanvasGroupFade(
            CanvasGroup canvasGroup,
            float from,
            float to,
            float duration)
        {
            if (duration <= 0f)
            {
                return DOVirtual.DelayedCall(
                    0f,
                    () => canvasGroup.alpha = to,
                    true);
            }

            return DOVirtual.Float(
                    from,
                    to,
                    duration,
                    value => canvasGroup.alpha = value)
                .SetEase(Ease.InOutSine);
        }

        private void CaptureAuthoredStatusText()
        {
            if (_statusTextCaptured || statusText == null)
            {
                return;
            }

            _authoredStatusText = statusText.text;
            _statusTextCaptured = true;
        }

        private void SubscribeToButtons()
        {
            if (telegraph == null)
            {
                return;
            }

            _buttons.Clear();
            _buttons.AddRange(
                telegraph.GetComponentsInChildren<TelegraphButton>(true));
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].Clicked += HandleButtonClicked;
            }
        }

        private void UnsubscribeFromButtons()
        {
            for (int i = 0; i < _buttons.Count; i++)
            {
                if (_buttons[i] != null)
                {
                    _buttons[i].Clicked -= HandleButtonClicked;
                }
            }

            _buttons.Clear();
        }

        private void HandleButtonClicked(TelegraphButtonKind buttonKind)
        {
            if (!_inputEnabled || _exitInProgress)
            {
                return;
            }

            switch (buttonKind)
            {
                case TelegraphButtonKind.NewGame:
                    NewRunRequested?.Invoke();
                    break;
                case TelegraphButtonKind.Tutorial:
                    TutorialRequested?.Invoke();
                    break;
                case TelegraphButtonKind.Setting:
                    SettingsRequested?.Invoke();
                    break;
            }
        }
    }
}
