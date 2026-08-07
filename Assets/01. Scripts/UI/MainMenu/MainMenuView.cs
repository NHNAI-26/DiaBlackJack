using System;
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

        private readonly List<TelegraphButton> _buttons =
            new List<TelegraphButton>();
        private Tween _logoTween;
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
        }

        private void OnDestroy()
        {
            UnsubscribeFromButtons();
            _logoTween?.Kill();
            _logoTween = null;
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
