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
        EnemyWin,
        PlayerFold
    }

    public readonly struct RoundResolution
    {
        public RoundResolution(long id, RoundOutcome outcome, int playerDamage, int enemyDamage)
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

            Id = id;
            Outcome = outcome;
            PlayerDamage = playerDamage;
            EnemyDamage = enemyDamage;
        }

        public long Id { get; }

        public RoundOutcome Outcome { get; }

        public int PlayerDamage { get; }

        public int EnemyDamage { get; }
    }

    public static class RoundResolver
    {
        public static RoundResolution ResolvePlayerFold(long resolutionId)
        {
            return new RoundResolution(
                resolutionId,
                RoundOutcome.PlayerFold,
                playerDamage: 1,
                enemyDamage: 0);
        }

        public static RoundResolution Resolve(
            long resolutionId,
            IEnumerable<BlackjackCard> playerCards,
            IEnumerable<BlackjackCard> enemyCards)
        {
            HandValue player = HandValueCalculator.Calculate(playerCards);
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
