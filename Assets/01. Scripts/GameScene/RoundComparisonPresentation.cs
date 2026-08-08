using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.GameScene
{
    internal enum RoundComparisonPlaybackMode
    {
        CountTotals,
        SkipForDecisiveHiddenGuess,
        SkipForDirectBust,
    }

    internal sealed class RoundComparisonStep
    {
        public RoundComparisonStep(int cardId, int total, bool isHiddenCard)
        {
            if (cardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardId));
            }

            if (total < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(total));
            }

            CardId = cardId;
            Total = total;
            IsHiddenCard = isHiddenCard;
        }

        public int CardId { get; }

        public bool IsHiddenCard { get; }

        public int Total { get; }
    }

    internal sealed class RoundComparisonSidePlan
    {
        public RoundComparisonSidePlan(
            IReadOnlyList<RoundComparisonStep> publicSteps,
            RoundComparisonStep hiddenStep,
            int cardTotal,
            int bonus)
        {
            PublicSteps = publicSteps ??
                throw new ArgumentNullException(nameof(publicSteps));
            if (cardTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardTotal));
            }

            if (bonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bonus));
            }

            HiddenStep = hiddenStep;
            CardTotal = cardTotal;
            Bonus = bonus;
            FinalTotal = cardTotal + bonus;
        }

        public int Bonus { get; }

        public int CardTotal { get; }

        public int FinalTotal { get; }

        public RoundComparisonStep HiddenStep { get; }

        public IReadOnlyList<RoundComparisonStep> PublicSteps { get; }
    }

    /// <summary>
    /// Immutable snapshot captured while the battle is still resolving. CoreLoop can synchronously
    /// move on to another round after this point, so the presentation must not read the live hand.
    /// </summary>
    internal sealed class RoundComparisonPlan
    {
        public RoundComparisonPlan(
            long resolutionId,
            RoundOutcome outcome,
            RoundEndCause cause,
            int playerDamage,
            int enemyDamage,
            RoundComparisonSidePlan player,
            RoundComparisonSidePlan enemy,
            RoundComparisonPlaybackMode playbackMode =
                RoundComparisonPlaybackMode.CountTotals)
        {
            if (resolutionId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(resolutionId));
            }

            if (playerDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerDamage));
            }

            if (enemyDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyDamage));
            }

            if (!Enum.IsDefined(typeof(RoundComparisonPlaybackMode), playbackMode))
            {
                throw new ArgumentOutOfRangeException(nameof(playbackMode));
            }

            ResolutionId = resolutionId;
            Outcome = outcome;
            Cause = cause;
            PlayerDamage = playerDamage;
            EnemyDamage = enemyDamage;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            Enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            PlaybackMode = playbackMode;
        }

        public RoundEndCause Cause { get; }

        public RoundComparisonSidePlan Enemy { get; }

        public int EnemyDamage { get; }

        public RoundOutcome Outcome { get; }

        public RoundComparisonPlaybackMode PlaybackMode { get; }

        public RoundComparisonSidePlan Player { get; }

        public int PlayerDamage { get; }

        public long ResolutionId { get; }
    }

    /// <summary>
    /// Player-only prefix for Mammon's final choice. It deliberately has no enemy plan or enemy
    /// card projection, so an awaiting-choice snapshot cannot leak the concealed enemy rank.
    /// </summary>
    internal sealed class PlayerMammonComparisonPlan
    {
        public PlayerMammonComparisonPlan(
            int roundNumber,
            int interactionId,
            RoundComparisonSidePlan player,
            IReadOnlyList<GameSceneCardViewModel> revealedPlayerCards)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (interactionId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(interactionId));
            }

            RoundNumber = roundNumber;
            InteractionId = interactionId;
            Player = player ?? throw new ArgumentNullException(nameof(player));
            RevealedPlayerCards = revealedPlayerCards ??
                throw new ArgumentNullException(nameof(revealedPlayerCards));
        }

        public int InteractionId { get; }

        public RoundComparisonSidePlan Player { get; }

        public IReadOnlyList<GameSceneCardViewModel> RevealedPlayerCards { get; }

        public int RoundNumber { get; }
    }

    internal static class RoundComparisonPresenter
    {
        public static RoundComparisonPlan CreateResolved(
            CoreLoopBattle battle,
            IReadOnlyList<GameSceneCardViewModel> playerCards,
            IReadOnlyList<GameSceneCardViewModel> enemyCards,
            GameSceneRevolverAnimationCue revolverCue = null,
            GameSceneSatanNumberGuessAnimationCue satanNumberGuessCue = null,
            GameSceneKnifeAnimationCue knifeCue = null)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (battle.State != CoreLoopState.ResolvingRound ||
                !battle.LastResolution.HasValue)
            {
                return null;
            }

            RoundResolution resolution = battle.LastResolution.Value;
            return new RoundComparisonPlan(
                resolution.Id,
                resolution.Outcome,
                resolution.Cause,
                resolution.PlayerDamage,
                resolution.EnemyDamage,
                CreateSide(
                    battle.Player.Hand,
                    playerCards,
                    battle.LastResolutionPlayerBonus),
                CreateSide(
                    battle.Enemy.Hand,
                    enemyCards,
                    battle.LastResolutionEnemyBonus),
                ResolvePlaybackMode(
                    resolution,
                    revolverCue,
                    satanNumberGuessCue,
                    knifeCue));
        }

        internal static RoundComparisonPlaybackMode ResolvePlaybackMode(
            RoundResolution resolution,
            GameSceneRevolverAnimationCue revolverCue,
            GameSceneSatanNumberGuessAnimationCue satanNumberGuessCue,
            GameSceneKnifeAnimationCue knifeCue = null)
        {
            if (revolverCue != null &&
                revolverCue.Phase == GameSceneRevolverAnimationPhase.Resolved &&
                revolverCue.Succeeded &&
                resolution.Cause == RoundEndCause.CardEffectBust &&
                IsWinningResolutionForActor(resolution, revolverCue.ActorSide))
            {
                return RoundComparisonPlaybackMode.SkipForDecisiveHiddenGuess;
            }

            if (satanNumberGuessCue != null &&
                satanNumberGuessCue.Succeeded &&
                resolution.Cause == RoundEndCause.ContractEffectBust &&
                IsWinningResolutionForActor(
                    resolution,
                    satanNumberGuessCue.ActorSide))
            {
                return RoundComparisonPlaybackMode.SkipForDecisiveHiddenGuess;
            }

            if (knifeCue != null &&
                knifeCue.Phase == GameSceneKnifeAnimationPhase.Resolved &&
                knifeCue.Succeeded &&
                (resolution.Cause == RoundEndCause.CardEffectBust ||
                    resolution.Cause == RoundEndCause.NumericBust) &&
                IsWinningResolutionForActor(resolution, knifeCue.ActorSide))
            {
                return RoundComparisonPlaybackMode.SkipForDirectBust;
            }

            return RoundComparisonPlaybackMode.CountTotals;
        }

        private static bool IsWinningResolutionForActor(
            RoundResolution resolution,
            CombatantSide actorSide)
        {
            return actorSide == CombatantSide.Player
                ? resolution.Outcome == RoundOutcome.EnemyBust &&
                  resolution.PlayerDamage == 0 &&
                  resolution.EnemyDamage > 0
                : resolution.Outcome == RoundOutcome.PlayerBust &&
                  resolution.PlayerDamage > 0 &&
                  resolution.EnemyDamage == 0;
        }

        public static PlayerMammonComparisonPlan CreatePlayerMammonPending(
            CoreLoopBattle battle,
            IReadOnlyList<GameSceneCardViewModel> playerCards)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            if (battle.State != CoreLoopState.PlayerResolvingDemonContract ||
                pending == null ||
                pending.Kind != DemonContractInteractionKind.MammonApplyDie)
            {
                return null;
            }

            return new PlayerMammonComparisonPlan(
                battle.RoundNumber,
                pending.InteractionId,
                CreateSide(battle.Player.Hand, playerCards, bonus: 0),
                RevealHiddenCards(playerCards));
        }

        private static RoundComparisonSidePlan CreateSide(
            BlackjackHand hand,
            IReadOnlyList<GameSceneCardViewModel> displayCards,
            int bonus)
        {
            if (hand == null)
            {
                throw new ArgumentNullException(nameof(hand));
            }

            if (displayCards == null)
            {
                throw new ArgumentNullException(nameof(displayCards));
            }

            var runningCards = new List<BlackjackCard>(displayCards.Count);
            var publicSteps = new List<RoundComparisonStep>(displayCards.Count);
            BlackjackCard hiddenCard = null;
            for (int index = 0; index < displayCards.Count; index++)
            {
                GameSceneCardViewModel displayCard = displayCards[index];
                if (displayCard == null ||
                    !hand.TryGetCard(displayCard.CardId, out BlackjackCard card))
                {
                    continue;
                }

                if (hand.IsHiddenCard(card.Id))
                {
                    hiddenCard = card;
                    continue;
                }

                runningCards.Add(card);
                publicSteps.Add(new RoundComparisonStep(
                    card.Id,
                    HandValueCalculator.Calculate(runningCards).Total,
                    isHiddenCard: false));
            }

            RoundComparisonStep hiddenStep = null;
            if (hiddenCard != null)
            {
                runningCards.Add(hiddenCard);
                hiddenStep = new RoundComparisonStep(
                    hiddenCard.Id,
                    HandValueCalculator.Calculate(runningCards).Total,
                    isHiddenCard: true);
            }

            int cardTotal = HandValueCalculator.Calculate(runningCards).Total;
            return new RoundComparisonSidePlan(
                publicSteps.AsReadOnly(),
                hiddenStep,
                cardTotal,
                bonus);
        }

        private static IReadOnlyList<GameSceneCardViewModel> RevealHiddenCards(
            IReadOnlyList<GameSceneCardViewModel> cards)
        {
            var revealed = new List<GameSceneCardViewModel>(cards.Count);
            for (int index = 0; index < cards.Count; index++)
            {
                GameSceneCardViewModel card = cards[index];
                revealed.Add(new GameSceneCardViewModel(
                    card.CardId,
                    card.Rank,
                    isFaceUp: true,
                    card.RevealRank,
                    card.CanUse,
                    card.DisplayName,
                    card.AbilityDescription,
                    card.Suit,
                    card.ShowHoverBadgeWhenUnavailable,
                    card.DefinitionKey,
                    card.ShowHoverBadgeBelow,
                    card.CardEffectChoiceOptionId,
                    card.IsUsed,
                    card.DirectSelectionCommand,
                    card.IsEffectSource,
                    card.IsSatanBranded,
                    card.IsEffectSourcePersistent));
            }

            return revealed.AsReadOnly();
        }
    }
}
