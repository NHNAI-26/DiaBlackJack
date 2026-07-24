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
        private HiddenCardComparisonKnowledge? _playerKnowledge;
        private HiddenCardComparisonKnowledge? _enemyKnowledge;

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

        public HiddenCardComparisonKnowledge? GetHiddenCardKnowledge(
            CombatantSide observerSide)
        {
            switch (observerSide)
            {
                case CombatantSide.Player:
                    return _playerKnowledge;
                case CombatantSide.Enemy:
                    return _enemyKnowledge;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(observerSide));
            }
        }

        public void SetHiddenCardKnowledge(
            CombatantSide observerSide,
            CombatantSide subjectSide,
            int subjectHiddenCardId,
            int declaredNumber,
            bool isAtLeastDeclaredNumber,
            int roundNumber)
        {
            var knowledge = new HiddenCardComparisonKnowledge(
                observerSide,
                subjectSide,
                subjectHiddenCardId,
                declaredNumber,
                isAtLeastDeclaredNumber,
                roundNumber);
            switch (observerSide)
            {
                case CombatantSide.Player:
                    _playerKnowledge = knowledge;
                    break;
                case CombatantSide.Enemy:
                    _enemyKnowledge = knowledge;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(observerSide));
            }
        }

        public void ClearHiddenCardKnowledgeForObserver(
            CombatantSide observerSide)
        {
            switch (observerSide)
            {
                case CombatantSide.Player:
                    _playerKnowledge = null;
                    break;
                case CombatantSide.Enemy:
                    _enemyKnowledge = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(observerSide));
            }
        }

        public void InvalidateKnowledgeAboutHiddenCard(
            CombatantSide subjectSide,
            int subjectHiddenCardId)
        {
            if (_playerKnowledge.HasValue &&
                RefersTo(
                    _playerKnowledge.Value,
                    subjectSide,
                    subjectHiddenCardId))
            {
                _playerKnowledge = null;
            }

            if (_enemyKnowledge.HasValue &&
                RefersTo(
                    _enemyKnowledge.Value,
                    subjectSide,
                    subjectHiddenCardId))
            {
                _enemyKnowledge = null;
            }
        }

        public void ClearRoundState()
        {
            _poisonWinRewards.Clear();
            _playerKnowledge = null;
            _enemyKnowledge = null;
        }

        private static bool RefersTo(
            HiddenCardComparisonKnowledge knowledge,
            CombatantSide subjectSide,
            int subjectHiddenCardId)
        {
            return knowledge.SubjectSide == subjectSide &&
                knowledge.SubjectHiddenCardId == subjectHiddenCardId;
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
