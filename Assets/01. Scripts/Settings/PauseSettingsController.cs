using System;
using System.Collections.Generic;
using DiaBlackJack.StageProgression.UI;
using DiaBlackJack.GameScene;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Border.Settings
{
    [DisallowMultipleComponent]
    public sealed class PauseSettingsController : MonoBehaviour
    {
        private static readonly HashSet<PauseSettingsController>
            GameplayInputBlockers = new HashSet<PauseSettingsController>();

        private static readonly string[] HoverTooltipSizeNames =
        {
            "작게",
            "보통",
            "크게"
        };

        [Header("Panels")]
        [SerializeField] private bool settingsOnlyMode;
        [SerializeField] private GameObject backdrop;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject quitConfirmationPanel;

        [Header("Pause menu")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button titleButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Settings")]
        [FormerlySerializedAs("resolutionSelector")]
        [SerializeField] private UISettingsArrowSelector tooltipSizeSelector;
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

        private InputAction _pauseAction;
        private GameManager _gameManager;
        private readonly List<BaseRaycaster> _suppressedRaycasters =
            new List<BaseRaycaster>();
        private PauseMenuState _state;
        private float _previousTimeScale = 1f;
        private bool _listenersRegistered;

        internal PauseMenuState State => _state;

        internal bool SettingsOnlyMode => settingsOnlyMode;

        internal static bool IsGameplayInputBlocked =>
            GameplayInputBlockers.Count > 0;

        public event Action SettingsPanelClosed;

        private void Awake()
        {
            UIOverlayCanvasCameraUtility.TryConfigure(
                GetComponentInParent<Canvas>(includeInactive: true));
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
                if (settingsOnlyMode)
                {
                    SettingsSystem.Current?.Save();
                    ShowState(PauseMenuState.Hidden);
                }
                else
                {
                    ResumeGame();
                }
            }
        }

        private void OnDestroy()
        {
            RestoreSuppressedRaycasters();
            GameplayInputBlockers.Remove(this);
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
            titleButton?.onClick.AddListener(ReturnToTitle);
            settingsButton?.onClick.AddListener(OpenSettings);
            quitButton?.onClick.AddListener(OpenQuitConfirmation);
            settingsBackButton?.onClick.AddListener(CloseSettings);
            confirmQuitButton?.onClick.AddListener(QuitGame);
            cancelQuitButton?.onClick.AddListener(CloseQuitConfirmation);
            if (tooltipSizeSelector != null)
            {
                tooltipSizeSelector.ValueChanged +=
                    HandleTooltipSizeChanged;
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
            titleButton?.onClick.RemoveListener(ReturnToTitle);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            quitButton?.onClick.RemoveListener(OpenQuitConfirmation);
            settingsBackButton?.onClick.RemoveListener(CloseSettings);
            confirmQuitButton?.onClick.RemoveListener(QuitGame);
            cancelQuitButton?.onClick.RemoveListener(CloseQuitConfirmation);
            if (tooltipSizeSelector != null)
            {
                tooltipSizeSelector.ValueChanged -=
                    HandleTooltipSizeChanged;
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
                    if (!settingsOnlyMode)
                    {
                        OpenPauseMenu();
                    }
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
            if (settingsOnlyMode)
            {
                return;
            }

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

        private void ReturnToTitle()
        {
            SettingsSystem.Current?.Save();
            Time.timeScale = _previousTimeScale;
            _gameManager?.SetPauseInputBlocked(false);
            ShowState(PauseMenuState.Hidden);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            StageProgressionRuntime runtime = StageProgressionRuntime.Instance;
            if (runtime?.Session?.IsTutorialRun == true)
            {
                StageProgressionRuntime.ReturnToMainMenuAndDestroyInstance();
                return;
            }

            if (runtime != null)
            {
                runtime.LoadMainMenuScene();
                return;
            }

            SceneManager.LoadScene("MainMenuScene");
        }

        private void OpenSettings()
        {
            if (SettingsSystem.Current == null)
            {
                return;
            }

            RefreshSettings(SettingsSystem.Current.Snapshot);
            ShowState(PauseMenuState.Settings);
            Select(settingsBackButton);
        }

        public bool OpenSettingsPanel()
        {
            if (SettingsSystem.Current == null)
            {
                return false;
            }

            OpenSettings();
            return _state == PauseMenuState.Settings;
        }

        private void CloseSettings()
        {
            SettingsSystem.Current?.Save();
            if (settingsOnlyMode)
            {
                ShowState(PauseMenuState.Hidden);
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                SettingsPanelClosed?.Invoke();
                return;
            }

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

        private void RefreshSettings(GameSettingsSnapshot snapshot)
        {
            tooltipSizeSelector?.SetOptions(
                HoverTooltipSizeNames,
                (int)snapshot.HoverTooltipSize);

            masterVolumeSlider?.SetValueWithoutNotify(
                snapshot.MasterVolume);
            bgmVolumeSlider?.SetValueWithoutNotify(snapshot.BgmVolume);
            sfxVolumeSlider?.SetValueWithoutNotify(snapshot.SfxVolume);
            RefreshVolumeLabels(snapshot);
        }

        private void HandleSettingsChanged(GameSettingsSnapshot snapshot)
        {
            if (_state == PauseMenuState.Settings)
            {
                RefreshSettings(snapshot);
            }
        }

        private void HandleTooltipSizeChanged(int index)
        {
            SettingsSystem settings = SettingsSystem.Current;
            if (settings == null)
            {
                return;
            }

            settings.PreviewHoverTooltipSize(
                (HoverTooltipSize)UISettingsArrowSelector.WrapIndex(index, 3));
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

        private void ShowState(PauseMenuState state)
        {
            _state = state;
            bool visible = state != PauseMenuState.Hidden;
            if (!settingsOnlyMode && visible)
            {
                GameplayInputBlockers.Add(this);
                SuppressOtherRaycasters();
            }
            else
            {
                GameplayInputBlockers.Remove(this);
                RestoreSuppressedRaycasters();
            }

            if (backdrop != null)
            {
                backdrop.SetActive(visible);
            }

            pausePanel?.SetActive(state == PauseMenuState.PauseMenu);
            settingsPanel?.SetActive(state == PauseMenuState.Settings);
            quitConfirmationPanel?.SetActive(
                state == PauseMenuState.QuitConfirmation);
        }

        private void SuppressOtherRaycasters()
        {
            Transform pauseRoot = GetComponentInParent<Canvas>(
                includeInactive: true)?.transform ?? transform;
            BaseRaycaster[] raycasters =
                UnityEngine.Object.FindObjectsByType<BaseRaycaster>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            foreach (BaseRaycaster raycaster in raycasters)
            {
                if (raycaster == null ||
                    !raycaster.isActiveAndEnabled ||
                    raycaster.transform == pauseRoot ||
                    raycaster.transform.IsChildOf(pauseRoot))
                {
                    continue;
                }

                raycaster.enabled = false;
                _suppressedRaycasters.Add(raycaster);
            }
        }

        private void RestoreSuppressedRaycasters()
        {
            foreach (BaseRaycaster raycaster in _suppressedRaycasters)
            {
                if (raycaster != null)
                {
                    raycaster.enabled = true;
                }
            }

            _suppressedRaycasters.Clear();
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

    internal static class UIOverlayCanvasCameraUtility
    {
        private const string OverlayCameraName = "UIOverlayCamera";

        internal static bool TryConfigure(Canvas canvas)
        {
            if (canvas == null)
            {
                return false;
            }

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                SetLayerRecursively(canvas.transform, uiLayer);
            }

            Camera overlayCamera = FindOverlayCamera(canvas.gameObject.scene);
            if (overlayCamera == null)
            {
                return false;
            }

            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = overlayCamera;
            return true;
        }

        internal static Canvas FindCanvasInScene(
            Scene scene,
            string canvasName)
        {
            Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Canvas candidate in canvases)
            {
                if (candidate != null &&
                    candidate.gameObject.scene == scene &&
                    candidate.name == canvasName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static Camera FindOverlayCamera(Scene scene)
        {
            Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (Camera candidate in cameras)
            {
                if (candidate != null &&
                    candidate.gameObject.scene == scene &&
                    candidate.name == OverlayCameraName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            root.gameObject.layer = layer;
            for (int index = 0; index < root.childCount; index++)
            {
                SetLayerRecursively(root.GetChild(index), layer);
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
