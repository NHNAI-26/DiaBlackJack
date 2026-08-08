using System;
using UnityEngine;

namespace Border.Settings
{
    public sealed class PlayerPrefsSettingsRepository : ISettingsRepository
    {
        private const int LegacyVersion = 1;
        private const int CurrentVersion = 2;
        private const string DefaultPrefix = "DiaBlackJack.Settings";

        private readonly string _versionKey;
        private readonly string _widthKey;
        private readonly string _heightKey;
        private readonly string _windowModeKey;
        private readonly string _hoverTooltipSizeKey;
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
            _hoverTooltipSizeKey = prefix + ".HoverTooltipSize";
            _masterVolumeKey = prefix + ".MasterVolume";
            _bgmVolumeKey = prefix + ".BgmVolume";
            _sfxVolumeKey = prefix + ".SfxVolume";
        }

        public bool TryLoad(out GameSettingsSnapshot settings)
        {
            settings = default;
            try
            {
                if (!PlayerPrefs.HasKey(_versionKey))
                {
                    return false;
                }

                int version = PlayerPrefs.GetInt(_versionKey);
                if (version != LegacyVersion && version != CurrentVersion)
                {
                    return false;
                }

                HoverTooltipSize hoverTooltipSize = version == LegacyVersion
                    ? HoverTooltipSize.Normal
                    : (HoverTooltipSize)PlayerPrefs.GetInt(
                        _hoverTooltipSizeKey,
                        (int)HoverTooltipSize.Normal);
                settings = new GameSettingsSnapshot(
                    hoverTooltipSize,
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
                PlayerPrefs.SetInt(
                    _hoverTooltipSizeKey,
                    (int)settings.HoverTooltipSize);
                PlayerPrefs.SetFloat(_masterVolumeKey, settings.MasterVolume);
                PlayerPrefs.SetFloat(_bgmVolumeKey, settings.BgmVolume);
                PlayerPrefs.SetFloat(_sfxVolumeKey, settings.SfxVolume);
                DeleteDeprecatedDisplayKeys();
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
            PlayerPrefs.DeleteKey(_hoverTooltipSizeKey);
            PlayerPrefs.DeleteKey(_masterVolumeKey);
            PlayerPrefs.DeleteKey(_bgmVolumeKey);
            PlayerPrefs.DeleteKey(_sfxVolumeKey);
            PlayerPrefs.Save();
        }

        private void DeleteDeprecatedDisplayKeys()
        {
            PlayerPrefs.DeleteKey(_widthKey);
            PlayerPrefs.DeleteKey(_heightKey);
            PlayerPrefs.DeleteKey(_windowModeKey);
        }
    }
}
