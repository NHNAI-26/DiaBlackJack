using System;

namespace Border.Settings
{
    [Serializable]
    public readonly struct GameSettingsSnapshot : IEquatable<GameSettingsSnapshot>
    {
        public GameSettingsSnapshot(
            int resolutionWidth,
            int resolutionHeight,
            GameWindowMode windowMode,
            float masterVolume,
            float bgmVolume,
            float sfxVolume)
        {
            ResolutionWidth = resolutionWidth;
            ResolutionHeight = resolutionHeight;
            WindowMode = IsValidWindowMode(windowMode)
                ? windowMode
                : GameWindowMode.BorderlessFullscreen;
            MasterVolume = ClampVolume(masterVolume);
            BgmVolume = ClampVolume(bgmVolume);
            SfxVolume = ClampVolume(sfxVolume);
        }

        public int ResolutionWidth { get; }
        public int ResolutionHeight { get; }
        public GameWindowMode WindowMode { get; }
        public float MasterVolume { get; }
        public float BgmVolume { get; }
        public float SfxVolume { get; }

        public GameSettingsSnapshot WithDisplay(
            int resolutionWidth,
            int resolutionHeight,
            GameWindowMode windowMode)
        {
            return new GameSettingsSnapshot(
                resolutionWidth,
                resolutionHeight,
                windowMode,
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
                ResolutionWidth,
                ResolutionHeight,
                WindowMode,
                masterVolume,
                bgmVolume,
                sfxVolume);
        }

        public bool Equals(GameSettingsSnapshot other)
        {
            return ResolutionWidth == other.ResolutionWidth &&
                ResolutionHeight == other.ResolutionHeight &&
                WindowMode == other.WindowMode &&
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
                int hash = ResolutionWidth;
                hash = (hash * 397) ^ ResolutionHeight;
                hash = (hash * 397) ^ (int)WindowMode;
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

        internal static bool IsValidWindowMode(GameWindowMode windowMode)
        {
            return windowMode == GameWindowMode.Windowed ||
                windowMode == GameWindowMode.ExclusiveFullscreen ||
                windowMode == GameWindowMode.BorderlessFullscreen;
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
