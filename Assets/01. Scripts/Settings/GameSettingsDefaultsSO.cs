using UnityEngine;

namespace Border.Settings
{
    [CreateAssetMenu(
        fileName = "GameSettingsDefaults",
        menuName = "Border/Settings/Defaults")]
    public sealed class GameSettingsDefaultsSO : ScriptableObject
    {
        [SerializeField] private HoverTooltipSize hoverTooltipSize =
            HoverTooltipSize.Normal;
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;

        public GameSettingsSnapshot CreateSnapshot()
        {
            return new GameSettingsSnapshot(
                hoverTooltipSize,
                masterVolume,
                bgmVolume,
                sfxVolume);
        }
    }
}
