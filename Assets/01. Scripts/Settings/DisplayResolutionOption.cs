using System;

namespace Border.Settings
{
    public readonly struct DisplayResolutionOption :
        IEquatable<DisplayResolutionOption>
    {
        public DisplayResolutionOption(
            int width,
            int height,
            uint refreshNumerator,
            uint refreshDenominator)
        {
            Width = width;
            Height = height;
            RefreshNumerator = refreshNumerator;
            RefreshDenominator = refreshDenominator == 0
                ? 1u
                : refreshDenominator;
        }

        public int Width { get; }
        public int Height { get; }
        public uint RefreshNumerator { get; }
        public uint RefreshDenominator { get; }
        public double RefreshRate =>
            RefreshNumerator / (double)RefreshDenominator;
        public string DisplayName => $"{Width} x {Height}";

        public bool Equals(DisplayResolutionOption other)
        {
            return Width == other.Width &&
                Height == other.Height &&
                RefreshNumerator == other.RefreshNumerator &&
                RefreshDenominator == other.RefreshDenominator;
        }

        public override bool Equals(object obj)
        {
            return obj is DisplayResolutionOption other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Width;
                hash = (hash * 397) ^ Height;
                hash = (hash * 397) ^ (int)RefreshNumerator;
                hash = (hash * 397) ^ (int)RefreshDenominator;
                return hash;
            }
        }
    }
}
