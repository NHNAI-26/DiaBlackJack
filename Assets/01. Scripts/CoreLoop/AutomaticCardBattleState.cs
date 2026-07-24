using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    internal readonly struct PoisonWinReward
    {
        public PoisonWinReward(
            int sourceCardId,
            CombatantSide ownerSide,
            int roundNumber,
            int healAmount)
        {
            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            if (roundNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (healAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(healAmount));
            }

            SourceCardId = sourceCardId;
            OwnerSide = ownerSide;
            RoundNumber = roundNumber;
            HealAmount = healAmount;
        }

        public int SourceCardId { get; }

        public CombatantSide OwnerSide { get; }

        public int RoundNumber { get; }

        public int HealAmount { get; }
    }

    internal sealed class AutomaticCardBattleState
    {
        private readonly List<PoisonWinReward> _poisonWinRewards =
            new List<PoisonWinReward>();

        public int PendingPoisonWinRewardCount => _poisonWinRewards.Count;

        public void RegisterPoisonWinReward(
            int sourceCardId,
            CombatantSide ownerSide,
            int roundNumber,
            int healAmount)
        {
            _poisonWinRewards.Add(new PoisonWinReward(
                sourceCardId,
                ownerSide,
                roundNumber,
                healAmount));
        }

        public void ResolvePoisonWinRewards(
            RoundResolution resolution,
            int roundNumber,
            BattleParticipant player,
            BattleParticipant enemy)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            foreach (PoisonWinReward reward in _poisonWinRewards)
            {
                if (reward.RoundNumber != roundNumber ||
                    !DidOwnerWin(reward.OwnerSide, resolution.Outcome))
                {
                    continue;
                }

                BattleParticipant owner = reward.OwnerSide ==
                    CombatantSide.Player
                        ? player
                        : enemy;
                if (!owner.Soul.IsDepleted)
                {
                    owner.Soul.Restore(reward.HealAmount);
                }
            }

            _poisonWinRewards.Clear();
        }

        public void ClearRoundState()
        {
            _poisonWinRewards.Clear();
        }

        private static bool DidOwnerWin(
            CombatantSide ownerSide,
            RoundOutcome outcome)
        {
            switch (ownerSide)
            {
                case CombatantSide.Player:
                    return outcome == RoundOutcome.PlayerWin ||
                        outcome == RoundOutcome.PlayerTwentyOneWin ||
                        outcome == RoundOutcome.EnemyBust;
                case CombatantSide.Enemy:
                    return outcome == RoundOutcome.EnemyWin ||
                        outcome == RoundOutcome.PlayerBust;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }
        }
    }
}
