using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    internal enum CombatActionSkullTargetKind
    {
        Hit,
        Stand,
        Change,
        NormalCard,
        DemonCard,
    }

    internal readonly struct CombatActionSkullRequest
    {
        public CombatActionSkullRequest(
            CombatantSide side,
            CombatActionSkullTargetKind targetKind,
            int? cardId = null)
        {
            bool cardTarget = targetKind == CombatActionSkullTargetKind.NormalCard ||
                targetKind == CombatActionSkullTargetKind.DemonCard;
            if (cardTarget != cardId.HasValue || cardId < 0)
            {
                throw new ArgumentException(
                    "Card skull targets require one non-negative card id.",
                    nameof(cardId));
            }

            Side = side;
            TargetKind = targetKind;
            CardId = cardId;
        }

        public CombatantSide Side { get; }

        public CombatActionSkullTargetKind TargetKind { get; }

        public int? CardId { get; }
    }

    internal readonly struct CombatActionSkullCueKey :
        IEquatable<CombatActionSkullCueKey>
    {
        public CombatActionSkullCueKey(int roundNumber, int actionOrdinal)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (actionOrdinal < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(actionOrdinal));
            }

            RoundNumber = roundNumber;
            ActionOrdinal = actionOrdinal;
        }

        public int RoundNumber { get; }

        public int ActionOrdinal { get; }

        public bool Equals(CombatActionSkullCueKey other)
        {
            return RoundNumber == other.RoundNumber &&
                ActionOrdinal == other.ActionOrdinal;
        }

        public override bool Equals(object obj)
        {
            return obj is CombatActionSkullCueKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(RoundNumber, ActionOrdinal);
        }
    }

    internal static class CombatActionSkullPresenter
    {
        public static bool ShouldStartTerminalDissolve(
            BattleOutcome outcome,
            bool isRunning,
            bool isCompleted)
        {
            return outcome != BattleOutcome.InProgress &&
                !isRunning &&
                !isCompleted;
        }

        public static bool TryResolveLosingSide(
            BattleOutcome outcome,
            out CombatantSide side)
        {
            side = CombatantSide.Player;
            if (outcome == BattleOutcome.PlayerDefeat)
            {
                return true;
            }

            if (outcome == BattleOutcome.PlayerVictory)
            {
                side = CombatantSide.Enemy;
                return true;
            }

            return false;
        }

        public static bool TryCreateRequest(
            CombatantSide side,
            PublicCombatAction action,
            int? sourceCardId,
            out CombatActionSkullRequest request)
        {
            request = default;
            if (action == null || action.ActorSide != side)
            {
                return false;
            }

            switch (action.ActionType)
            {
                case PublicCombatActionType.Hit:
                    request = new CombatActionSkullRequest(
                        side,
                        CombatActionSkullTargetKind.Hit);
                    return true;
                case PublicCombatActionType.Stand:
                    request = new CombatActionSkullRequest(
                        side,
                        CombatActionSkullTargetKind.Stand);
                    return true;
                case PublicCombatActionType.Change:
                    request = new CombatActionSkullRequest(
                        side,
                        CombatActionSkullTargetKind.Change);
                    return true;
                case PublicCombatActionType.UseCard when sourceCardId.HasValue:
                    request = new CombatActionSkullRequest(
                        side,
                        CombatActionSkullTargetKind.NormalCard,
                        sourceCardId.Value);
                    return true;
                case PublicCombatActionType.DemonContract when sourceCardId.HasValue:
                    request = new CombatActionSkullRequest(
                        side,
                        CombatActionSkullTargetKind.DemonCard,
                        sourceCardId.Value);
                    return true;
                default:
                    return false;
            }
        }

        public static bool TryCreateEnemyRequest(
            int roundNumber,
            int actionOrdinal,
            CoreLoopState state,
            PublicCombatAction action,
            int? sourceCardId,
            EnemyDecision decision,
            CombatActionSkullCueKey? lastCue,
            out CombatActionSkullRequest request,
            out CombatActionSkullCueKey cue)
        {
            request = default;
            cue = default;
            if (state == CoreLoopState.Initializing ||
                state == CoreLoopState.StartingRound ||
                decision == null ||
                roundNumber < 1 ||
                actionOrdinal < 1 ||
                action == null ||
                action.ActorSide != CombatantSide.Enemy)
            {
                return false;
            }

            cue = new CombatActionSkullCueKey(roundNumber, actionOrdinal);
            if (lastCue.HasValue && lastCue.Value.Equals(cue))
            {
                return false;
            }

            if (!MatchesDecision(action.ActionType, decision.ActionType))
            {
                return false;
            }

            int? selectedCardId = sourceCardId;
            if (decision.ActionType == EnemyActionType.UseCard)
            {
                selectedCardId = decision.CardId;
                if (!selectedCardId.HasValue ||
                    selectedCardId != sourceCardId)
                {
                    return false;
                }
            }
            else if (decision.ActionType == EnemyActionType.DemonContract)
            {
                selectedCardId = decision.DemonContractSourceCardId ?? sourceCardId;
            }

            return TryCreateRequest(
                CombatantSide.Enemy,
                action,
                selectedCardId,
                out request);
        }

        public static UnityEngine.Vector3 ResolveSharedButtonOffset(
            CombatantSide side)
        {
            return new UnityEngine.Vector3(
                side == CombatantSide.Player ? -0.16f : 0.16f,
                0.06f,
                0.25f);
        }

        private static bool MatchesDecision(
            PublicCombatActionType action,
            EnemyActionType decision)
        {
            return (action == PublicCombatActionType.Hit &&
                    decision == EnemyActionType.Hit) ||
                (action == PublicCombatActionType.Stand &&
                 decision == EnemyActionType.Stand) ||
                (action == PublicCombatActionType.Change &&
                 decision == EnemyActionType.Change) ||
                (action == PublicCombatActionType.UseCard &&
                 decision == EnemyActionType.UseCard) ||
                (action == PublicCombatActionType.DemonContract &&
                 decision == EnemyActionType.DemonContract);
        }
    }
}
