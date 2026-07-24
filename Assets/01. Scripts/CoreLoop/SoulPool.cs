using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class SoulPool
    {
        public SoulPool(int maximum)
            : this(maximum, maximum)
        {
        }

        public SoulPool(int maximum, int current)
        {
            if (maximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum soul must be positive.");
            }

            if (current < 0 || current > maximum)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(current),
                    "Current soul must be between zero and maximum soul.");
            }

            Maximum = maximum;
            Current = current;
        }

        public int Current { get; private set; }

        public int Maximum { get; }

        public bool IsDepleted => Current == 0;

        public void ApplyDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), "Damage cannot be negative.");
            }

            Current = Math.Max(0, Current - amount);
        }

        public void Restore(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Soul restoration cannot be negative.");
            }

            Current = Math.Min(Maximum, Current + amount);
        }
    }
}
