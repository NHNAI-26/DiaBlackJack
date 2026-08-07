using System;

namespace DiaBlackJack.CoreLoop
{
    internal enum SoulLossCause
    {
        RoundDamage,
        ChangeCost,
        DemonContractCost,
        AutomaticCardCost
    }

    internal readonly struct SoulLossRecord
    {
        public SoulLossRecord(
            long id,
            CombatantSide targetSide,
            int soulBefore,
            int soulAfter,
            int maximumSoul,
            SoulLossCause cause,
            long? resolutionId = null)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (soulBefore < 0 || soulAfter < 0 || soulAfter > soulBefore)
            {
                throw new ArgumentOutOfRangeException(nameof(soulAfter));
            }

            if (maximumSoul <= 0 || soulBefore > maximumSoul)
            {
                throw new ArgumentOutOfRangeException(nameof(maximumSoul));
            }

            if (!Enum.IsDefined(typeof(SoulLossCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            if (resolutionId.HasValue && resolutionId.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolutionId));
            }

            Id = id;
            TargetSide = targetSide;
            SoulBefore = soulBefore;
            SoulAfter = soulAfter;
            MaximumSoul = maximumSoul;
            Cause = cause;
            ResolutionId = resolutionId;
        }

        public SoulLossCause Cause { get; }

        public long Id { get; }

        public int LossAmount => SoulBefore - SoulAfter;

        public int MaximumSoul { get; }

        public long? ResolutionId { get; }

        public int SoulAfter { get; }

        public int SoulBefore { get; }

        public CombatantSide TargetSide { get; }
    }
}
