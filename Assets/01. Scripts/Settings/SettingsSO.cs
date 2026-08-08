using System;
using UnityEngine;
using Border.Core;
using Border.Events;
using Border.UI;
using Border.Localization;

namespace Border.Settings
{
    // [CreateAssetMenu(fileName = "SettingsSO", menuName = "Settings/Settings SO")]
    [Serializable]
    public class SettingsSO : ScriptableObject
    {
        private const string DefaultLanguageCode = "ko";

        private float masterVolume;
        private float musicVolume;
        private float sfxVolume;
        private bool isHudOn = false;
        private bool isCameraShakeOn = true;
        private string languageCode = DefaultLanguageCode;

        public float MasterVolume => masterVolume;
        public float MusicVolume => musicVolume;
        public float SfxVolume => sfxVolume;
        public bool IsHudOn => isHudOn;
        public bool IsCameraShakeOn => isCameraShakeOn;
        public string LanguageCode => string.IsNullOrWhiteSpace(languageCode) ? DefaultLanguageCode : languageCode;

        /// <summary>
        /// 오디오 관련 설정값(마스터/음악/효과음 볼륨)을 저장한다.
        /// </summary>
        /// <param name="masterVolume">마스터 볼륨 값</param>
        /// <param name="musicVolume">음악 볼륨 값</param>
        /// <param name="sfxVolume">효과음 볼륨 값</param>
        public void SaveAudioSettings(float masterVolume, float musicVolume, float sfxVolume)
        {
            this.masterVolume = masterVolume;
            this.musicVolume = musicVolume;
            this.sfxVolume = sfxVolume;
        }

        /// <summary>
        /// HUD 표시 여부를 저장한다.
        /// </summary>
        /// <param name="enable">HUD 표시 여부</param>
        public void SaveEnableHud(bool enable)
        {
            this.isHudOn = enable;
        }

        /// <summary>
        /// 카메라 쉐이크 사용 여부를 저장한다.
        /// </summary>
        /// <param name="enable">카메라 쉐이크 사용 여부</param>
        public void SaveEnableCameraShake(bool enable)
        {
            this.isCameraShakeOn = enable;
        }

        /// <summary>
        /// 언어 코드를 저장한다.
        /// </summary>
        /// <param name="code">저장할 언어 코드(예: en, ko, ja, zh)</param>
        public void SaveLanguageCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                languageCode = DefaultLanguageCode;
                return;
            }

            languageCode = code.Trim().ToLowerInvariant();
        }

    }

}
