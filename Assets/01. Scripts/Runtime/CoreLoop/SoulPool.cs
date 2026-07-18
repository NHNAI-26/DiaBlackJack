using System;

namespace DiaBlackJack.CoreLoop
{
    public sealed class SoulPool
    {
        public SoulPool(int maximum)
        {
            if (maximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum), "Maximum soul must be positive.");
            }

            Maximum = maximum;
            Current = maximum;
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
    }
}
