using System;

namespace Border.Settings
{
    [Serializable]
    public readonly struct GameSettingsSnapshot : IEquatable<GameSettingsSnapshot>
    {
        public GameSettingsSnapshot(
            HoverTooltipSize hoverTooltipSize,
            float masterVolume,
            float bgmVolume,
            float sfxVolume)
        {
            HoverTooltipSize =
                HoverTooltipSizeUtility.Normalize(hoverTooltipSize);
            MasterVolume = ClampVolume(masterVolume);
            BgmVolume = ClampVolume(bgmVolume);
            SfxVolume = ClampVolume(sfxVolume);
        }

        public HoverTooltipSize HoverTooltipSize { get; }
        public float MasterVolume { get; }
        public float BgmVolume { get; }
        public float SfxVolume { get; }

        public GameSettingsSnapshot WithHoverTooltipSize(
            HoverTooltipSize hoverTooltipSize)
        {
            return new GameSettingsSnapshot(
                hoverTooltipSize,
                MasterVolume,
                BgmVolume,
                SfxVolume);
        }

        public GameSettingsSnapshot WithAudio(
            float masterVolume,
            float bgmVolume,
            float sfxVolume)
        {
            return new GameSettingsSnapshot(
                HoverTooltipSize,
                masterVolume,
                bgmVolume,
                sfxVolume);
        }

        public bool Equals(GameSettingsSnapshot other)
        {
            return HoverTooltipSize == other.HoverTooltipSize &&
                MasterVolume.Equals(other.MasterVolume) &&
                BgmVolume.Equals(other.BgmVolume) &&
                SfxVolume.Equals(other.SfxVolume);
        }

        public override bool Equals(object obj)
        {
            return obj is GameSettingsSnapshot other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)HoverTooltipSize;
                hash = (hash * 397) ^ MasterVolume.GetHashCode();
                hash = (hash * 397) ^ BgmVolume.GetHashCode();
                hash = (hash * 397) ^ SfxVolume.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(
            GameSettingsSnapshot left,
            GameSettingsSnapshot right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(
            GameSettingsSnapshot left,
            GameSettingsSnapshot right)
        {
            return !left.Equals(right);
        }

        internal static float ClampVolume(float value)
        {
            if (float.IsNaN(value) || float.IsNegativeInfinity(value))
            {
                return 0f;
            }

            if (float.IsPositiveInfinity(value))
            {
                return 1f;
            }

            if (value < 0f)
            {
                return 0f;
            }

            return value > 1f ? 1f : value;
        }
    }
}
