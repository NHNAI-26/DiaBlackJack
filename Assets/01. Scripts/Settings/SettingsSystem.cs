using System;
using System.Collections;
using System.Collections.Generic;
using Border.Core;
using Border.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Border.Settings
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class SettingsSystem : MonoBehaviour
    {
        [SerializeField] private GameSettingsDefaultsSO defaults;

        [Header("Broadcasting on")]
        [SerializeField] private FloatEventChannelSO changeMasterVolumeEvent;
        [SerializeField] private FloatEventChannelSO changeMusicVolumeEvent;
        [SerializeField] private FloatEventChannelSO changeSfxVolumeEvent;

        private ISettingsRepository _repository;
        private Coroutine _displayVerification;
        private GameSettingsSnapshot _snapshot;
        private static SettingsSystem _current;

        public static SettingsSystem Current
        {
            get
            {
                if (_current == null && Application.isPlaying)
                {
                    _current = FindFirstObjectByType<SettingsSystem>();
                }

                return _current;
            }
            private set => _current = value;
        }
        public GameSettingsSnapshot Snapshot => _snapshot;
        public event Action<GameSettingsSnapshot> Changed;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _current = null;
        }

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Destroy(gameObject);
                return;
            }

            _current = this;
            DontDestroyOnLoad(gameObject);
            _repository = new PlayerPrefsSettingsRepository();

            Resolution native = Screen.currentResolution;
            GameSettingsSnapshot fallback = defaults != null
                ? defaults.CreateSnapshot(native.width, native.height)
                : new GameSettingsSnapshot(
                    native.width,
                    native.height,
                    GameWindowMode.BorderlessFullscreen,
                    1f,
                    0.8f,
                    1f);
            _snapshot = _repository.TryLoad(
                out GameSettingsSnapshot loaded)
                ? Validate(loaded, fallback)
                : fallback;
            SettingsGraphicsUtility.Apply(_snapshot);
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            BroadcastAudio();
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void OnDestroy()
        {
            if (_current == this)
            {
                _current = null;
            }
        }

        public void PreviewDisplay(
            int width,
            int height,
            GameWindowMode mode)
        {
            List<DisplayResolutionOption> options =
                SettingsGraphicsUtility.GetResolutionOptions();
            DisplayResolutionOption selected =
                SettingsGraphicsUtility.ValidateResolution(
                    options,
                    width,
                    height);
            GameSettingsSnapshot previous = _snapshot;
            _snapshot = _snapshot.WithDisplay(
                selected.Width,
                selected.Height,
                mode);
            SettingsGraphicsUtility.Apply(_snapshot);
            Changed?.Invoke(_snapshot);

            if (_displayVerification != null)
            {
                StopCoroutine(_displayVerification);
            }
            _displayVerification = StartCoroutine(
                VerifyDisplayChange(previous, _snapshot));
        }

        public void PreviewAudio(
            float masterVolume,
            float bgmVolume,
            float sfxVolume)
        {
            _snapshot = _snapshot.WithAudio(
                masterVolume,
                bgmVolume,
                sfxVolume);
            BroadcastAudio();
            Changed?.Invoke(_snapshot);
        }

        public bool Save()
        {
            bool saved = _repository != null &&
                _repository.TrySave(_snapshot);
            if (!saved)
            {
                Log.W("[SettingsSystem] Failed to save settings.", this);
            }

            return saved;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            BroadcastAudio();
        }

        private void BroadcastAudio()
        {
            changeMasterVolumeEvent?.RaiseEvent(_snapshot.MasterVolume);
            changeMusicVolumeEvent?.RaiseEvent(_snapshot.BgmVolume);
            changeSfxVolumeEvent?.RaiseEvent(_snapshot.SfxVolume);
        }

        private IEnumerator VerifyDisplayChange(
            GameSettingsSnapshot previous,
            GameSettingsSnapshot requested)
        {
            yield return null;
            yield return null;

            if (Application.isEditor ||
                requested.WindowMode ==
                GameWindowMode.BorderlessFullscreen ||
                (Screen.width == requested.ResolutionWidth &&
                 Screen.height == requested.ResolutionHeight))
            {
                _displayVerification = null;
                yield break;
            }

            Log.W(
                $"[SettingsSystem] Display change failed: " +
                $"{requested.ResolutionWidth}x{requested.ResolutionHeight}.",
                this);
            _snapshot = previous;
            SettingsGraphicsUtility.Apply(_snapshot);
            Changed?.Invoke(_snapshot);
            _displayVerification = null;
        }

        private static GameSettingsSnapshot Validate(
            GameSettingsSnapshot loaded,
            GameSettingsSnapshot fallback)
        {
            List<DisplayResolutionOption> options =
                SettingsGraphicsUtility.GetResolutionOptions();
            DisplayResolutionOption resolution =
                SettingsGraphicsUtility.ValidateResolution(
                    options,
                    loaded.ResolutionWidth,
                    loaded.ResolutionHeight);
            GameWindowMode mode =
                GameSettingsSnapshot.IsValidWindowMode(loaded.WindowMode)
                    ? loaded.WindowMode
                    : fallback.WindowMode;
            return new GameSettingsSnapshot(
                resolution.Width,
                resolution.Height,
                mode,
                loaded.MasterVolume,
                loaded.BgmVolume,
                loaded.SfxVolume);
        }
    }
}
