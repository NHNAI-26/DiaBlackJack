using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class FlamethrowerAndPocketWatchTests
    {
        private static readonly CardDefinition Flamethrower =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.FlamethrowerKey);
        private static readonly CardDefinition PocketWatch =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PocketWatchKey);
        private static readonly CardDefinition Poison =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey);
        private static readonly CardDefinition CrystalOrb =
            CardDefinitionCatalog.GetByKey("crystal-orb-5");
        private static readonly CardDefinition ThreatHammer =
            CardDefinitionCatalog.GetByKey("threat-hammer-6");

        [Test]
        public void AC04_U01_FlamethrowerChoosesOwnerThenOpponent()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, Flamethrower),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction ownerChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(ownerChoice.ChoiceKind,
                Is.EqualTo(AutomaticCardChoiceKind.FlamethrowerOwnerDiscard));
            Assert.That(ownerChoice.DecisionSide,
                Is.EqualTo(CombatantSide.Player));
            Assert.That(
                ResolveCardOptionAsPlayer(battle, ownerChoice, cardId: 0),
                Is.True);

            PendingAutomaticCardInteraction opponentChoice =
                battle.PendingAutomaticInteraction;
            Assert.That(opponentChoice.ChoiceKind,
                Is.EqualTo(AutomaticCardChoiceKind.FlamethrowerOpponentDiscard));
            Assert.That(opponentChoice.DecisionSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(battle.Player.Hand.Contains(0), Is.False);
            Assert.That(
                ResolveCardOption(
                    battle,
                    CombatantSide.Enemy,
                    opponentChoice,
                    cardId: 100),
                Is.True);

            Assert.That(battle.Player.Deck.GetDiscardedCards()
                .Select(card => card.Id),
                Does.Contain(0));
            Assert.That(battle.Player.Deck.GetDiscardedCards()
                .Select(card => card.Id),
                Does.Contain(ownerChoice.SourceCardId));
            Assert.That(battle.Enemy.Deck.GetDiscardedCards()
                .Select(card => card.Id),
                Does.Contain(100));
        }

        [Test]
        public void AC04_U02_FlamethrowerSkipsOwnerWithoutCandidates()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, Flamethrower),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.Player.TryDiscardCard(cardId: 0), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingAutomaticInteraction;
            Assert.That(pending.ChoiceKind,
                Is.EqualTo(AutomaticCardChoiceKind.FlamethrowerOpponentDiscard));
            Assert.That(pending.DecisionSide,
                Is.EqualTo(CombatantSide.Enemy));
        }

        [Test]
        public void AC04_U02_FlamethrowerSkipsStandingOpponent()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, Flamethrower),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            battle.Enemy.Stand();
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction ownerChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    ownerChoice.InteractionId,
                    FlamethrowerEffectHandler.SkipOptionId),
                Is.True);

            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Player.Deck.GetDiscardedCards()
                .Select(card => card.Id),
                Does.Contain(ownerChoice.SourceCardId));
        }

        [Test]
        public void AC04_U03_FlamethrowerSourceIsNotAnOwnerDiscardCandidate()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, Flamethrower),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending.Options.Select(option => option.CardId),
                Does.Contain(0));
            Assert.That(
                pending.Options.Any(
                    option => option.CardId == pending.SourceCardId),
                Is.False);
        }

        [Test]
        public void AC04_U04_FlamethrowerDiscardsSourceBeforeVisibleBustCheck()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(10, 2, 8, Flamethrower),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.Enemy.IsStanding, Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(battle.Player.VisibleHandValue.IsBust, Is.True);

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    FlamethrowerEffectHandler.SkipOptionId),
                Is.True);

            Assert.That(battle.Player.VisibleHandValue.Total, Is.EqualTo(18));
            Assert.That(battle.Player.VisibleHandValue.IsBust, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void AC04_U05_PocketWatchReactivatesOnlyTheSelectedUsedManualCard()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, ThreatHammer, PocketWatch),
                EnemyCards(4, 7, 3),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            BlackjackCard usedHammer =
                MarkInitialHiddenManualCardUsed(battle.Player);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction targetChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(targetChoice.ChoiceKind,
                Is.EqualTo(AutomaticCardChoiceKind.PocketWatchManualCard));
            Assert.That(
                targetChoice.Options
                    .Where(option => option.CardId.HasValue)
                    .Select(option => option.CardId.Value),
                Is.EqualTo(new[] { usedHammer.Id }));
            Assert.That(
                ResolveCardOptionAsPlayer(
                    battle,
                    targetChoice,
                    usedHammer.Id),
                Is.True);

            Assert.That(usedHammer.UseState,
                Is.EqualTo(CardUseState.Available));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);

            PendingAutomaticCardInteraction dispositionChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    dispositionChoice.InteractionId,
                    PocketWatchEffectHandler.DiscardSourceOptionId),
                Is.True);
            Assert.That(battle.CanUsePlayerCard(usedHammer.Id), Is.True);
            Assert.That(
                battle.TryBeginPlayerCardUse(usedHammer.Id),
                Is.True);
        }

        [Test]
        public void AC04_U06_PocketWatchExcludesAutomaticSourceAndAvailableManualCards()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(
                    2,
                    ThreatHammer,
                    CrystalOrb,
                    Poison,
                    PocketWatch),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            BlackjackCard usedHammer =
                MarkInitialHiddenManualCardUsed(battle.Player);
            BlackjackCard availableManual = battle.Player.Draw(faceUp: true);
            BlackjackCard otherAutomatic = battle.Player.Draw(faceUp: true);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            int sourceCardId = pending.SourceCardId;
            IReadOnlyList<int> targetIds = pending.Options
                .Where(option => option.CardId.HasValue)
                .Select(option => option.CardId.Value)
                .ToArray();

            Assert.That(targetIds, Is.EqualTo(new[] { usedHammer.Id }));
            Assert.That(targetIds.Contains(availableManual.Id), Is.False);
            Assert.That(targetIds.Contains(otherAutomatic.Id), Is.False);
            Assert.That(targetIds.Contains(sourceCardId), Is.False);
            Assert.That(availableManual.UseState,
                Is.EqualTo(CardUseState.Available));
            Assert.That(otherAutomatic.UseState,
                Is.EqualTo(CardUseState.Unavailable));
        }

        [TestCase(false, 2)]
        [TestCase(true, 11)]
        public void AC04_U07_PocketWatchDispositionControlsSourceLocationAndVisibleTotal(
            bool retainSource,
            int expectedVisibleTotal)
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, PocketWatch),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction dispositionChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(dispositionChoice.ChoiceKind,
                Is.EqualTo(
                    AutomaticCardChoiceKind.PocketWatchSourceDisposition));
            int sourceCardId = dispositionChoice.SourceCardId;
            int optionId = retainSource
                ? PocketWatchEffectHandler.RetainSourceOptionId
                : PocketWatchEffectHandler.DiscardSourceOptionId;

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    dispositionChoice.InteractionId,
                    optionId),
                Is.True);

            Assert.That(battle.Player.VisibleHandValue.Total,
                Is.EqualTo(expectedVisibleTotal));
            Assert.That(battle.Player.Hand.Contains(sourceCardId),
                Is.EqualTo(retainSource));
            Assert.That(battle.Player.Deck.GetDiscardedCards()
                .Any(card => card.Id == sourceCardId),
                Is.EqualTo(!retainSource));
            Assert.That(
                battle.LastAutomaticCardResult.Value.SourceDisposition,
                Is.EqualTo(
                    retainSource
                        ? AutomaticCardSourceDisposition.RetainFaceUp
                        : AutomaticCardSourceDisposition.Discard));
        }

        [Test]
        public void AC04_U08_RetainedPocketWatchDoesNotTriggerAgainInSameHand()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, PocketWatch, 1),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction dispositionChoice =
                battle.PendingPlayerAutomaticInteraction;
            int sourceCardId = dispositionChoice.SourceCardId;
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    dispositionChoice.InteractionId,
                    PocketWatchEffectHandler.RetainSourceOptionId),
                Is.True);
            Assert.That(battle.Enemy.IsStanding, Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
            Assert.That(battle.Player.Hand.Cards
                .Count(card => card.Id == sourceCardId), Is.EqualTo(1));
            Assert.That(
                battle.LastAutomaticCardResult.Value.SourceCardId,
                Is.EqualTo(sourceCardId));
        }

        [Test]
        public void AC04_U05_EnemyPocketWatchUsesTheSameOwnerBoundaries()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, 2),
                EnemyCards(4, ThreatHammer, PocketWatch),
                new SequencePolicy(EnemyActionType.Hit));
            Assert.That(battle.Start(), Is.True);
            BlackjackCard usedHammer =
                MarkInitialHiddenManualCardUsed(battle.Enemy);

            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction targetChoice =
                battle.PendingAutomaticInteraction;
            Assert.That(targetChoice.OwnerSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(targetChoice.DecisionSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                ResolveCardOption(
                    battle,
                    CombatantSide.Enemy,
                    targetChoice,
                    usedHammer.Id),
                Is.True);

            PendingAutomaticCardInteraction dispositionChoice =
                battle.PendingAutomaticInteraction;
            int sourceCardId = dispositionChoice.SourceCardId;
            Assert.That(
                battle.TryResolveAutomaticCardChoice(
                    CombatantSide.Enemy,
                    dispositionChoice.InteractionId,
                    PocketWatchEffectHandler.DiscardSourceOptionId),
                Is.True);

            Assert.That(usedHammer.UseState,
                Is.EqualTo(CardUseState.Available));
            Assert.That(battle.Enemy.Deck.GetDiscardedCards()
                .Select(card => card.Id),
                Does.Contain(sourceCardId));
        }

        private static BlackjackCard MarkInitialHiddenManualCardUsed(
            BattleParticipant participant)
        {
            BlackjackCard card = participant.Hand.Cards
                .Single(candidate =>
                    !candidate.IsFaceUp &&
                    candidate.Definition.Activation ==
                        CardActivationKind.Manual);
            card.Reveal();
            Assert.That(card.TryBeginUse(), Is.True);
            Assert.That(card.TryCompleteUse(), Is.True);
            return card;
        }

        private static bool ResolveCardOptionAsPlayer(
            CoreLoopBattle battle,
            PendingAutomaticCardInteraction pending,
            int cardId)
        {
            AutomaticCardChoiceOption option = pending.Options
                .Single(candidate => candidate.CardId == cardId);
            return battle.TryResolvePlayerAutomaticCardChoice(
                pending.InteractionId,
                option.OptionId);
        }

        private static bool ResolveCardOption(
            CoreLoopBattle battle,
            CombatantSide decisionSide,
            PendingAutomaticCardInteraction pending,
            int cardId)
        {
            AutomaticCardChoiceOption option = pending.Options
                .Single(candidate => candidate.CardId == cardId);
            return battle.TryResolveAutomaticCardChoice(
                decisionSide,
                pending.InteractionId,
                option.OptionId);
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<BlackjackCard> playerCards,
            IReadOnlyList<BlackjackCard> enemyCards,
            IEnemyBehaviorPolicy enemyPolicy)
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(playerCards),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 12,
                enemyPolicy);
        }

        private static IReadOnlyList<BlackjackCard> PlayerCards(
            object first,
            object second,
            params object[] remaining)
        {
            return CreateCards(0, first, second, remaining);
        }

        private static IReadOnlyList<BlackjackCard> EnemyCards(
            object first,
            object second,
            params object[] remaining)
        {
            return CreateCards(100, first, second, remaining);
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(
            int startId,
            object first,
            object second,
            IReadOnlyList<object> remaining)
        {
            var values = new List<object> { first, second };
            values.AddRange(remaining);
            return values.Select((value, index) =>
            {
                CardDefinition definition =
                    value is CardDefinition cardDefinition
                        ? cardDefinition
                        : CardDefinitionCatalog.GetDefaultForRank((int)value);
                return new BlackjackCard(startId + index, definition);
            }).ToArray();
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "ac04-test-stand");
            }
        }

        private sealed class SequencePolicy : IEnemyBehaviorPolicy
        {
            private readonly Queue<EnemyActionType> _actions;

            public SequencePolicy(params EnemyActionType[] actions)
            {
                _actions = new Queue<EnemyActionType>(actions);
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionType action = _actions.Count > 0
                    ? _actions.Dequeue()
                    : EnemyActionType.Stand;
                return new EnemyDecision(action, "ac04-test-sequence");
            }
        }
    }
}
