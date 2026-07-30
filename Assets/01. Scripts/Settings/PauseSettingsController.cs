using System.Collections.Generic;
using DiaBlackJack.GameScene;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Border.Settings
{
    [DisallowMultipleComponent]
    public sealed class PauseSettingsController : MonoBehaviour
    {
        private static readonly string[] WindowModeNames =
        {
            "창모드",
            "전체화면",
            "테두리없는 전체화면"
        };

        [Header("Panels")]
        [SerializeField] private GameObject backdrop;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject quitConfirmationPanel;

        [Header("Pause menu")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [SerializeField] private UISettingsArrowSelector resolutionSelector;
        [SerializeField] private UISettingsArrowSelector windowModeSelector;
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TMP_Text masterVolumeValue;
        [SerializeField] private TMP_Text bgmVolumeValue;
        [SerializeField] private TMP_Text sfxVolumeValue;
        [SerializeField] private Button settingsBackButton;

        [Header("Quit confirmation")]
        [SerializeField] private Button confirmQuitButton;
        [SerializeField] private Button cancelQuitButton;

        private readonly List<DisplayResolutionOption> _resolutionOptions =
            new List<DisplayResolutionOption>();
        private readonly List<string> _resolutionNames = new List<string>();
        private InputAction _pauseAction;
        private GameManager _gameManager;
        private PauseMenuState _state;
        private float _previousTimeScale = 1f;
        private bool _listenersRegistered;

        internal PauseMenuState State => _state;

        private void Awake()
        {
            _gameManager = FindFirstObjectByType<GameManager>();
            _pauseAction = new InputAction("Pause");
            _pauseAction.AddBinding("<Keyboard>/escape");
            _pauseAction.AddBinding("<Gamepad>/start");
            RegisterListeners();
            ShowState(PauseMenuState.Hidden);
        }

        private void OnEnable()
        {
            _pauseAction?.Enable();
            SettingsSystem settings = SettingsSystem.Current;
            if (settings != null)
            {
                settings.Changed += HandleSettingsChanged;
            }
        }

        private void Start()
        {
            if (settingsButton != null)
            {
                settingsButton.interactable = SettingsSystem.Current != null;
            }
        }

        private void OnDisable()
        {
            _pauseAction?.Disable();
            SettingsSystem settings = SettingsSystem.Current;
            if (settings != null)
            {
                settings.Changed -= HandleSettingsChanged;
            }

            if (_state != PauseMenuState.Hidden)
            {
                ResumeGame();
            }
        }

        private void OnDestroy()
        {
            UnregisterListeners();
            _pauseAction?.Dispose();
        }

        private void RegisterListeners()
        {
            if (_listenersRegistered)
            {
                return;
            }

            _pauseAction.performed += HandlePauseAction;
            continueButton?.onClick.AddListener(ResumeGame);
            settingsButton?.onClick.AddListener(OpenSettings);
            quitButton?.onClick.AddListener(OpenQuitConfirmation);
            settingsBackButton?.onClick.AddListener(CloseSettings);
            confirmQuitButton?.onClick.AddListener(QuitGame);
            cancelQuitButton?.onClick.AddListener(CloseQuitConfirmation);
            if (resolutionSelector != null)
            {
                resolutionSelector.ValueChanged += HandleResolutionChanged;
            }

            if (windowModeSelector != null)
            {
                windowModeSelector.ValueChanged += HandleWindowModeChanged;
            }

            masterVolumeSlider?.onValueChanged.AddListener(
                HandleMasterVolumeChanged);
            bgmVolumeSlider?.onValueChanged.AddListener(
                HandleBgmVolumeChanged);
            sfxVolumeSlider?.onValueChanged.AddListener(
                HandleSfxVolumeChanged);
            _listenersRegistered = true;
        }

        private void UnregisterListeners()
        {
            if (!_listenersRegistered)
            {
                return;
            }

            _pauseAction.performed -= HandlePauseAction;
            continueButton?.onClick.RemoveListener(ResumeGame);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            quitButton?.onClick.RemoveListener(OpenQuitConfirmation);
            settingsBackButton?.onClick.RemoveListener(CloseSettings);
            confirmQuitButton?.onClick.RemoveListener(QuitGame);
            cancelQuitButton?.onClick.RemoveListener(CloseQuitConfirmation);
            if (resolutionSelector != null)
            {
                resolutionSelector.ValueChanged -= HandleResolutionChanged;
            }

            if (windowModeSelector != null)
            {
                windowModeSelector.ValueChanged -= HandleWindowModeChanged;
            }

            masterVolumeSlider?.onValueChanged.RemoveListener(
                HandleMasterVolumeChanged);
            bgmVolumeSlider?.onValueChanged.RemoveListener(
                HandleBgmVolumeChanged);
            sfxVolumeSlider?.onValueChanged.RemoveListener(
                HandleSfxVolumeChanged);
            _listenersRegistered = false;
        }

        private void HandlePauseAction(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            switch (_state)
            {
                case PauseMenuState.Hidden:
                    OpenPauseMenu();
                    break;
                case PauseMenuState.PauseMenu:
                    ResumeGame();
                    break;
                case PauseMenuState.Settings:
                    CloseSettings();
                    break;
                case PauseMenuState.QuitConfirmation:
                    CloseQuitConfirmation();
                    break;
            }
        }

        private void OpenPauseMenu()
        {
            _gameManager ??= FindFirstObjectByType<GameManager>();
            if (_gameManager != null &&
                _gameManager.TryCloseTransientOverlay())
            {
                return;
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _gameManager?.SetPauseInputBlocked(true);
            ShowState(PauseMenuState.PauseMenu);
            Select(continueButton);
        }

        private void ResumeGame()
        {
            if (_state == PauseMenuState.Settings)
            {
                SettingsSystem.Current?.Save();
            }

            Time.timeScale = _previousTimeScale;
            _gameManager?.SetPauseInputBlocked(false);
            ShowState(PauseMenuState.Hidden);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void OpenSettings()
        {
            if (SettingsSystem.Current == null)
            {
                return;
            }

            BuildResolutionOptions();
            RefreshSettings(SettingsSystem.Current.Snapshot);
            ShowState(PauseMenuState.Settings);
            Select(settingsBackButton);
        }

        private void CloseSettings()
        {
            SettingsSystem.Current?.Save();
            ShowState(PauseMenuState.PauseMenu);
            Select(settingsButton);
        }

        private void OpenQuitConfirmation()
        {
            ShowState(PauseMenuState.QuitConfirmation);
            Select(cancelQuitButton);
        }

        private void CloseQuitConfirmation()
        {
            ShowState(PauseMenuState.PauseMenu);
            Select(quitButton);
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void BuildResolutionOptions()
        {
            _resolutionOptions.Clear();
            _resolutionOptions.AddRange(
                SettingsGraphicsUtility.GetResolutionOptions());
            _resolutionNames.Clear();
            for (int i = 0; i < _resolutionOptions.Count; i++)
            {
                _resolutionNames.Add(_resolutionOptions[i].DisplayName);
            }
        }

        private void RefreshSettings(GameSettingsSnapshot snapshot)
        {
            if (_resolutionOptions.Count == 0)
            {
                BuildResolutionOptions();
            }

            int resolutionIndex = FindResolutionIndex(
                snapshot.ResolutionWidth,
                snapshot.ResolutionHeight);
            resolutionSelector?.SetOptions(
                _resolutionNames,
                resolutionIndex);
            windowModeSelector?.SetOptions(
                WindowModeNames,
                (int)snapshot.WindowMode);

            masterVolumeSlider?.SetValueWithoutNotify(
                snapshot.MasterVolume);
            bgmVolumeSlider?.SetValueWithoutNotify(snapshot.BgmVolume);
            sfxVolumeSlider?.SetValueWithoutNotify(snapshot.SfxVolume);
            RefreshVolumeLabels(snapshot);
            RefreshResolutionAvailability(snapshot.WindowMode);
        }

        private void RefreshResolutionAvailability(GameWindowMode mode)
        {
            if (resolutionSelector == null)
            {
                return;
            }

            bool enabled =
                mode != GameWindowMode.BorderlessFullscreen;
            resolutionSelector.SetInteractable(enabled);
            if (!enabled)
            {
                Resolution native = Screen.currentResolution;
                resolutionSelector.SetDisplayText(
                    $"{native.width} x {native.height}");
            }
        }

        private void HandleSettingsChanged(GameSettingsSnapshot snapshot)
        {
            if (_state == PauseMenuState.Settings)
            {
                RefreshSettings(snapshot);
            }
        }

        private void HandleResolutionChanged(int index)
        {
            SettingsSystem settings = SettingsSystem.Current;
            if (settings == null ||
                settings.Snapshot.WindowMode ==
                GameWindowMode.BorderlessFullscreen ||
                index < 0 ||
                index >= _resolutionOptions.Count)
            {
                return;
            }

            DisplayResolutionOption option = _resolutionOptions[index];
            settings.PreviewDisplay(
                option.Width,
                option.Height,
                settings.Snapshot.WindowMode);
        }

        private void HandleWindowModeChanged(int index)
        {
            SettingsSystem settings = SettingsSystem.Current;
            if (settings == null)
            {
                return;
            }

            int resolutionIndex = resolutionSelector == null
                ? 0
                : resolutionSelector.Index;
            if (resolutionIndex < 0 ||
                resolutionIndex >= _resolutionOptions.Count)
            {
                resolutionIndex = FindResolutionIndex(
                    settings.Snapshot.ResolutionWidth,
                    settings.Snapshot.ResolutionHeight);
            }

            DisplayResolutionOption option =
                _resolutionOptions[resolutionIndex];
            settings.PreviewDisplay(
                option.Width,
                option.Height,
                (GameWindowMode)UISettingsArrowSelector.WrapIndex(index, 3));
        }

        private void HandleMasterVolumeChanged(float value)
        {
            SettingsSystem settings = SettingsSystem.Current;
            if (settings == null)
            {
                return;
            }

            GameSettingsSnapshot current = settings.Snapshot;
            settings.PreviewAudio(
                value,
                current.BgmVolume,
                current.SfxVolume);
        }

        private void HandleBgmVolumeChanged(float value)
        {
            SettingsSystem settings = SettingsSystem.Current;
            if (settings == null)
            {
                return;
            }

            GameSettingsSnapshot current = settings.Snapshot;
            settings.PreviewAudio(
                current.MasterVolume,
                value,
                current.SfxVolume);
        }

        private void HandleSfxVolumeChanged(float value)
        {
            SettingsSystem settings = SettingsSystem.Current;
            if (settings == null)
            {
                return;
            }

            GameSettingsSnapshot current = settings.Snapshot;
            settings.PreviewAudio(
                current.MasterVolume,
                current.BgmVolume,
                value);
        }

        private void RefreshVolumeLabels(GameSettingsSnapshot snapshot)
        {
            SetPercent(masterVolumeValue, snapshot.MasterVolume);
            SetPercent(bgmVolumeValue, snapshot.BgmVolume);
            SetPercent(sfxVolumeValue, snapshot.SfxVolume);
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutionOptions.Count; i++)
            {
                if (_resolutionOptions[i].Width == width &&
                    _resolutionOptions[i].Height == height)
                {
                    return i;
                }
            }

            return Mathf.Max(0, _resolutionOptions.Count - 1);
        }

        private void ShowState(PauseMenuState state)
        {
            _state = state;
            bool visible = state != PauseMenuState.Hidden;
            if (backdrop != null)
            {
                backdrop.SetActive(visible);
            }

            pausePanel?.SetActive(state == PauseMenuState.PauseMenu);
            settingsPanel?.SetActive(state == PauseMenuState.Settings);
            quitConfirmationPanel?.SetActive(
                state == PauseMenuState.QuitConfirmation);
        }

        private static void SetPercent(TMP_Text label, float value)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(value * 100f)}%";
            }
        }

        private static void Select(Button button)
        {
            if (EventSystem.current != null && button != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }
    }

    internal enum PauseMenuState
    {
        Hidden = 0,
        PauseMenu = 1,
        Settings = 2,
        QuitConfirmation = 3
    }
}
