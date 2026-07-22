using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public enum RoundOutcome
    {
        PlayerBust,
        EnemyBust,
        PlayerWin,
        PlayerTwentyOneWin,
        EnemyWin
    }

    public enum RoundEndCause
    {
        TotalComparison,
        NumericBust,
        CardEffectBust,
        ContractEffectBust
    }

    public readonly struct RoundResolution
    {
        public RoundResolution(long id, RoundOutcome outcome, int playerDamage, int enemyDamage)
            : this(
                id,
                outcome,
                playerDamage,
                enemyDamage,
                GetDefaultCause(outcome),
                sourceCardKey: null)
        {
        }

        public RoundResolution(
            long id,
            RoundOutcome outcome,
            int playerDamage,
            int enemyDamage,
            RoundEndCause cause,
            string sourceCardKey = null)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            if (playerDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerDamage));
            }

            if (enemyDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyDamage));
            }

            if (!Enum.IsDefined(typeof(RoundEndCause), cause))
            {
                throw new ArgumentOutOfRangeException(nameof(cause));
            }

            if (cause == RoundEndCause.CardEffectBust &&
                string.IsNullOrWhiteSpace(sourceCardKey))
            {
                throw new ArgumentException(
                    "Card effect bust requires a source card key.",
                    nameof(sourceCardKey));
            }

            Id = id;
            Outcome = outcome;
            PlayerDamage = playerDamage;
            EnemyDamage = enemyDamage;
            Cause = cause;
            SourceCardKey = sourceCardKey;
        }

        public RoundEndCause Cause { get; }

        public long Id { get; }

        public RoundOutcome Outcome { get; }

        public int PlayerDamage { get; }

        public string SourceCardKey { get; }

        public int EnemyDamage { get; }

        private static RoundEndCause GetDefaultCause(RoundOutcome outcome)
        {
            switch (outcome)
            {
                case RoundOutcome.PlayerBust:
                case RoundOutcome.EnemyBust:
                    return RoundEndCause.NumericBust;
                default:
                    return RoundEndCause.TotalComparison;
            }
        }
    }

    public static class RoundResolver
    {
        public static RoundResolution ResolveNumericBust(
            long resolutionId,
            bool playerIsTarget)
        {
            return playerIsTarget
                ? new RoundResolution(
                    resolutionId,
                    RoundOutcome.PlayerBust,
                    playerDamage: 2,
                    enemyDamage: 0,
                    cause: RoundEndCause.NumericBust)
                : new RoundResolution(
                    resolutionId,
                    RoundOutcome.EnemyBust,
                    playerDamage: 0,
                    enemyDamage: 1,
                    cause: RoundEndCause.NumericBust);
        }

        public static RoundResolution ResolveCardEffectBust(
            long resolutionId,
            bool playerIsTarget,
            string sourceCardKey)
        {
            return playerIsTarget
                ? new RoundResolution(
                    resolutionId,
                    RoundOutcome.PlayerBust,
                    playerDamage: 2,
                    enemyDamage: 0,
                    cause: RoundEndCause.CardEffectBust,
                    sourceCardKey: sourceCardKey)
                : new RoundResolution(
                    resolutionId,
                    RoundOutcome.EnemyBust,
                    playerDamage: 0,
                    enemyDamage: 1,
                    cause: RoundEndCause.CardEffectBust,
                    sourceCardKey: sourceCardKey);
        }

        public static RoundResolution ResolveContractEffectBust(
            long resolutionId,
            bool playerIsTarget)
        {
            return playerIsTarget
                ? new RoundResolution(
                    resolutionId,
                    RoundOutcome.PlayerBust,
                    playerDamage: 2,
                    enemyDamage: 0,
                    cause: RoundEndCause.ContractEffectBust)
                : new RoundResolution(
                    resolutionId,
                    RoundOutcome.EnemyBust,
                    playerDamage: 0,
                    enemyDamage: 1,
                    cause: RoundEndCause.ContractEffectBust);
        }

        public static RoundResolution Resolve(
            long resolutionId,
            IEnumerable<BlackjackCard> playerCards,
            IEnumerable<BlackjackCard> enemyCards)
        {
            return Resolve(
                resolutionId,
                playerCards,
                enemyCards,
                playerBonus: 0);
        }

        internal static RoundResolution Resolve(
            long resolutionId,
            IEnumerable<BlackjackCard> playerCards,
            IEnumerable<BlackjackCard> enemyCards,
            int playerBonus)
        {
            HandValue player = HandValueCalculator.CalculateWithBonus(
                playerCards,
                playerBonus);
            HandValue enemy = HandValueCalculator.Calculate(enemyCards);

            if (player.IsBust && !enemy.IsBust)
            {
                return new RoundResolution(resolutionId, RoundOutcome.PlayerBust, 2, 0);
            }

            if (enemy.IsBust && !player.IsBust)
            {
                return new RoundResolution(resolutionId, RoundOutcome.EnemyBust, 0, 1);
            }

            if (player.IsBust && enemy.IsBust)
            {
                return player.Total <= enemy.Total
                    ? new RoundResolution(resolutionId, RoundOutcome.PlayerWin, 0, 1)
                    : new RoundResolution(resolutionId, RoundOutcome.EnemyWin, 1, 0);
            }

            if (player.Total >= enemy.Total)
            {
                return player.IsTwentyOne
                    ? new RoundResolution(resolutionId, RoundOutcome.PlayerTwentyOneWin, 0, 2)
                    : new RoundResolution(resolutionId, RoundOutcome.PlayerWin, 0, 1);
            }

            return new RoundResolution(resolutionId, RoundOutcome.EnemyWin, 1, 0);
        }
    }

    public sealed class RoundDamageApplier
    {
        private readonly HashSet<long> _appliedResolutionIds = new HashSet<long>();

        public bool TryApply(RoundResolution resolution, SoulPool playerSoul, SoulPool enemySoul)
        {
            if (playerSoul == null)
            {
                throw new ArgumentNullException(nameof(playerSoul));
            }

            if (enemySoul == null)
            {
                throw new ArgumentNullException(nameof(enemySoul));
            }

            if (!_appliedResolutionIds.Add(resolution.Id))
            {
                return false;
            }

            playerSoul.ApplyDamage(resolution.PlayerDamage);
            enemySoul.ApplyDamage(resolution.EnemyDamage);
            return true;
        }
    }
}
