using System;
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
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            _repository = new PlayerPrefsSettingsRepository();

            GameSettingsSnapshot fallback = defaults != null
                ? defaults.CreateSnapshot()
                : new GameSettingsSnapshot(
                    HoverTooltipSize.Normal,
                    1f,
                    0.8f,
                    1f);
            _snapshot = _repository.TryLoad(
                out GameSettingsSnapshot loaded)
                ? loaded
                : fallback;
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

        public void PreviewHoverTooltipSize(HoverTooltipSize size)
        {
            _snapshot = _snapshot.WithHoverTooltipSize(size);
            Changed?.Invoke(_snapshot);
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

    }
}
