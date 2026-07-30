using System;
using UnityEngine;

namespace Border.Settings
{
    public sealed class PlayerPrefsSettingsRepository : ISettingsRepository
    {
        private const int CurrentVersion = 1;
        private const string DefaultPrefix = "DiaBlackJack.Settings";

        private readonly string _versionKey;
        private readonly string _widthKey;
        private readonly string _heightKey;
        private readonly string _windowModeKey;
        private readonly string _masterVolumeKey;
        private readonly string _bgmVolumeKey;
        private readonly string _sfxVolumeKey;

        public PlayerPrefsSettingsRepository()
            : this(DefaultPrefix)
        {
        }

        internal PlayerPrefsSettingsRepository(string prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                throw new ArgumentException(
                    "Settings key prefix cannot be empty.",
                    nameof(prefix));
            }

            _versionKey = prefix + ".Version";
            _widthKey = prefix + ".ResolutionWidth";
            _heightKey = prefix + ".ResolutionHeight";
            _windowModeKey = prefix + ".WindowMode";
            _masterVolumeKey = prefix + ".MasterVolume";
            _bgmVolumeKey = prefix + ".BgmVolume";
            _sfxVolumeKey = prefix + ".SfxVolume";
        }

        public bool TryLoad(out GameSettingsSnapshot settings)
        {
            settings = default;
            try
            {
                if (!PlayerPrefs.HasKey(_versionKey) ||
                    PlayerPrefs.GetInt(_versionKey) != CurrentVersion)
                {
                    return false;
                }

                int width = PlayerPrefs.GetInt(_widthKey, 0);
                int height = PlayerPrefs.GetInt(_heightKey, 0);
                GameWindowMode windowMode =
                    (GameWindowMode)PlayerPrefs.GetInt(
                        _windowModeKey,
                        (int)GameWindowMode.BorderlessFullscreen);
                settings = new GameSettingsSnapshot(
                    width,
                    height,
                    windowMode,
                    PlayerPrefs.GetFloat(_masterVolumeKey, 1f),
                    PlayerPrefs.GetFloat(_bgmVolumeKey, 0.8f),
                    PlayerPrefs.GetFloat(_sfxVolumeKey, 1f));
                return true;
            }
            catch (Exception)
            {
                settings = default;
                return false;
            }
        }

        public bool TrySave(GameSettingsSnapshot settings)
        {
            try
            {
                PlayerPrefs.SetInt(_versionKey, CurrentVersion);
                PlayerPrefs.SetInt(_widthKey, settings.ResolutionWidth);
                PlayerPrefs.SetInt(_heightKey, settings.ResolutionHeight);
                PlayerPrefs.SetInt(_windowModeKey, (int)settings.WindowMode);
                PlayerPrefs.SetFloat(_masterVolumeKey, settings.MasterVolume);
                PlayerPrefs.SetFloat(_bgmVolumeKey, settings.BgmVolume);
                PlayerPrefs.SetFloat(_sfxVolumeKey, settings.SfxVolume);
                PlayerPrefs.Save();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void DeleteAll()
        {
            PlayerPrefs.DeleteKey(_versionKey);
            PlayerPrefs.DeleteKey(_widthKey);
            PlayerPrefs.DeleteKey(_heightKey);
            PlayerPrefs.DeleteKey(_windowModeKey);
            PlayerPrefs.DeleteKey(_masterVolumeKey);
            PlayerPrefs.DeleteKey(_bgmVolumeKey);
            PlayerPrefs.DeleteKey(_sfxVolumeKey);
            PlayerPrefs.Save();
        }
    }
}
