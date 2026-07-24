using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class ResurrectionHerbAutomaticCardTests
    {
        private static readonly CardDefinition ResurrectionHerb =
            CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.ResurrectionHerbKey);
        private static readonly CardDefinition Poison =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey);
        private static readonly CardDefinition CrystalOrb =
            CardDefinitionCatalog.GetByKey("crystal-orb-5");
        private static readonly CardDefinition MilitaryKnife =
            CardDefinitionCatalog.GetByKey("military-knife-9");

        [TestCase(1, 2, false)]
        [TestCase(2, 1, false)]
        [TestCase(2, 2, true)]
        public void AC05_U01_RestartRequiresBothParticipantsToHaveTwoSoul(
            int playerSoul,
            int enemySoul,
            bool expectsRestart)
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, ResurrectionHerb, 4, 5),
                EnemyCards(4, 7, 2, 3),
                playerSoul,
                enemySoul,
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending.ChoiceKind,
                Is.EqualTo(
                    AutomaticCardChoiceKind.ResurrectionHerbDecision));
            Assert.That(
                pending.Options.Any(option =>
                    option.OptionId ==
                        ResurrectionHerbEffectHandler.DeclineOptionId),
                Is.True);
            Assert.That(
                pending.Options.Any(option =>
                    option.OptionId ==
                        ResurrectionHerbEffectHandler.RestartRoundOptionId),
                Is.EqualTo(expectsRestart));
        }

        [Test]
        public void AC05_U02_RestartChangesSoulHandsAndRoundExactlyOnce()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, ResurrectionHerb, 4, 5),
                EnemyCards(6, 7, 2, 3),
                playerSoul: 5,
                enemySoul: 6,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            IReadOnlyList<int> previousPlayerCardIds =
                battle.Player.Hand.Cards.Select(card => card.Id).ToArray();
            IReadOnlyList<int> previousEnemyCardIds =
                battle.Enemy.Hand.Cards.Select(card => card.Id).ToArray();
            Assert.That(battle.TryPlayerHit(), Is.True);
            int sourceCardId =
                battle.PendingPlayerAutomaticInteraction.SourceCardId;

            Assert.That(RestartAsPlayer(battle), Is.True);

            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(4));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Player.IsStanding, Is.False);
            Assert.That(battle.Enemy.IsStanding, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.LastResolution.HasValue, Is.False);

            RoundTransition transition =
                battle.LastRoundTransition.Value;
            Assert.That(transition.Cause,
                Is.EqualTo(RoundTransitionCause.ResurrectionHerb));
            Assert.That(transition.PreviousRoundNumber, Is.EqualTo(1));
            Assert.That(transition.NewRoundNumber, Is.EqualTo(2));
            Assert.That(transition.SourceCardId, Is.EqualTo(sourceCardId));
            Assert.That(transition.OwnerSide,
                Is.EqualTo(CombatantSide.Player));
            Assert.That(transition.HasWinner, Is.False);
            Assert.That(transition.AppliesDamage, Is.False);
            Assert.That(transition.CancelsContinuation, Is.True);

            IReadOnlyList<int> discardedPlayerIds =
                battle.Player.Deck.GetDiscardedCards()
                    .Select(card => card.Id)
                    .ToArray();
            IReadOnlyList<int> discardedEnemyIds =
                battle.Enemy.Deck.GetDiscardedCards()
                    .Select(card => card.Id)
                    .ToArray();
            Assert.That(discardedPlayerIds,
                Does.Contain(sourceCardId));
            Assert.That(previousPlayerCardIds.All(
                cardId => discardedPlayerIds.Contains(cardId)), Is.True);
            Assert.That(previousEnemyCardIds.All(
                cardId => discardedEnemyIds.Contains(cardId)), Is.True);
        }

        [Test]
        public void AC05_U02_RepeatedRestartsRemainIsolated()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(
                    2,
                    3,
                    ResurrectionHerb,
                    4,
                    5,
                    ResurrectionHerb,
                    6,
                    7),
                EnemyCards(4, 7, 2, 3, 4, 5),
                playerSoul: 6,
                enemySoul: 6,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(RestartAsPlayer(battle), Is.True);
            int firstSourceId =
                battle.LastAutomaticCardResult.Value.SourceCardId;

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(RestartAsPlayer(battle), Is.True);

            Assert.That(battle.RoundNumber, Is.EqualTo(3));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(4));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(4));
            Assert.That(
                battle.LastAutomaticCardResult.Value.SourceCardId,
                Is.Not.EqualTo(firstSourceId));
            Assert.That(
                battle.LastRoundTransition.Value.PreviousRoundNumber,
                Is.EqualTo(2));
            Assert.That(
                battle.LastRoundTransition.Value.NewRoundNumber,
                Is.EqualTo(3));
            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
        }

        [Test]
        public void AC05_U03_RestartClearsRoundKnowledgeAndPoisonRewards()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, ResurrectionHerb, 4, 5),
                EnemyCards(4, 7, 2, 3),
                playerSoul: 6,
                enemySoul: 6,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            BlackjackCard enemyHidden = battle.Enemy.Hand.Cards
                .Single(card => !card.IsFaceUp);
            battle.RegisterPoisonWinReward(90, CombatantSide.Player, 5);
            battle.RecordLieDetectorResult(
                91,
                CombatantSide.Player,
                5,
                enemyHidden.Id,
                true);
            Assert.That(battle.PendingPoisonWinRewardCount, Is.EqualTo(1));
            Assert.That(
                battle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(RestartAsPlayer(battle), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.PendingPoisonWinRewardCount, Is.EqualTo(0));
            Assert.That(
                battle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.False);
            Assert.That(battle.LastResolution.HasValue, Is.False);
        }

        [Test]
        public void AC05_U04_RestartNotifiesActiveContractOnce()
        {
            var handler = new RoundEndTrackingContractHandler();
            var enemyPolicy = new CountingStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, ResurrectionHerb, 4, 5),
                EnemyCards(4, 7, 2, 3),
                playerSoul: 5,
                enemySoul: 5,
                enemyPolicy: enemyPolicy,
                CreateRepeatedDemonDeck(DemonContractKind.Belphegor),
                new DemonContractResolver(handler));
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction contractChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    contractChoice.InteractionId,
                    contractChoice.Options[0].OptionId),
                Is.True);
            Assert.That(handler.RoundEndCount, Is.EqualTo(0));

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(RestartAsPlayer(battle), Is.True);

            Assert.That(handler.RoundEndCount, Is.EqualTo(1));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.ActivePlayerDemonContracts.Count,
                Is.EqualTo(1));
        }

        [Test]
        public void AC05_U05_CrystalOrbParentEffectDoesNotResume()
        {
            var enemyPolicy = new CountingStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(
                    2,
                    CrystalOrb,
                    ResurrectionHerb,
                    4,
                    5,
                    6),
                EnemyCards(4, 7, 2, 3),
                playerSoul: 5,
                enemySoul: 5,
                enemyPolicy: enemyPolicy);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard orb = battle.Player.Hand.Cards
                .Single(card =>
                    card.Definition.Effect == CardEffectKind.CrystalOrb);
            Assert.That(battle.TryBeginPlayerCardUse(orb.Id), Is.True);
            PendingCardEffect orbChoice = battle.PendingPlayerCardEffect;
            CardEffectChoiceOption herbOption = orbChoice.Options
                .Single(option =>
                    option.CardId.HasValue &&
                    orbChoice.TemporaryCards.Any(card =>
                        card.Id == option.CardId.Value &&
                        card.Definition.Effect ==
                            CardEffectKind.ResurrectionHerb));
            Assert.That(
                battle.TryResolvePlayerCardChoice(herbOption.Id),
                Is.True);

            Assert.That(RestartAsPlayer(battle), Is.True);

            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.LastCardEffectResult.HasValue, Is.False);
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(orb.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(
                battle.Player.Deck.GetDiscardedCards()
                    .Any(card => card.Id == orb.Id),
                Is.True);
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(0));
            AssertCardConservation(battle.Player);
            AssertCardConservation(battle.Enemy);
        }

        [Test]
        public void AC05_U05_ForcedEnemyHerbCancelsMilitaryKnifeAndTurnResume()
        {
            var enemyPolicy = new CountingStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, MilitaryKnife, 4, 5),
                EnemyCards(4, 7, ResurrectionHerb, 2, 3),
                playerSoul: 5,
                enemySoul: 5,
                enemyPolicy: enemyPolicy);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard knife = battle.Player.Hand.Cards
                .Single(card =>
                    card.Definition.Effect == CardEffectKind.MilitaryKnife);

            Assert.That(battle.TryBeginPlayerCardUse(knife.Id), Is.True);
            PendingAutomaticCardInteraction herbChoice =
                battle.PendingAutomaticInteraction;
            Assert.That(herbChoice.OwnerSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(herbChoice.DecisionSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                battle.TryResolveAutomaticCardChoice(
                    CombatantSide.Enemy,
                    herbChoice.InteractionId,
                    ResurrectionHerbEffectHandler.RestartRoundOptionId),
                Is.True);

            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.LastCardEffectResult.HasValue, Is.False);
            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(
                battle.LastRoundTransition.Value.OwnerSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(0));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            AssertCardConservation(battle.Player);
            AssertCardConservation(battle.Enemy);
        }

        [Test]
        public void AC05_U06_AutomaticCardInRestartDealDoesNotTrigger()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(
                    2,
                    3,
                    ResurrectionHerb,
                    Poison,
                    4),
                EnemyCards(4, 7, 2, 3),
                playerSoul: 5,
                enemySoul: 5,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            int herbSourceId =
                battle.PendingPlayerAutomaticInteraction.SourceCardId;

            Assert.That(RestartAsPlayer(battle), Is.True);

            BlackjackCard dealtPoison = battle.Player.Hand.Cards
                .Single(card =>
                    card.Definition.Effect == CardEffectKind.Poison);
            Assert.That(dealtPoison.IsFaceUp, Is.True);
            Assert.That(dealtPoison.UseState,
                Is.EqualTo(CardUseState.Unavailable));
            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
            Assert.That(
                battle.LastAutomaticCardResult.Value.SourceCardId,
                Is.EqualTo(herbSourceId));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(4));
        }

        [Test]
        public void AC05_U07_DeclineOnlyDiscardsSourceAndResumesAction()
        {
            var enemyPolicy = new CountingStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, ResurrectionHerb, 4, 5),
                EnemyCards(4, 7, 2, 3),
                playerSoul: 5,
                enemySoul: 5,
                enemyPolicy: enemyPolicy);
            Assert.That(battle.Start(), Is.True);
            IReadOnlyList<int> originalPlayerIds =
                battle.Player.Hand.Cards.Select(card => card.Id).ToArray();
            IReadOnlyList<int> originalEnemyIds =
                battle.Enemy.Hand.Cards.Select(card => card.Id).ToArray();
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            int sourceCardId = pending.SourceCardId;

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    ResurrectionHerbEffectHandler.DeclineOptionId),
                Is.True);

            Assert.That(battle.RoundNumber, Is.EqualTo(1));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.LastRoundTransition.HasValue, Is.False);
            Assert.That(originalPlayerIds.All(
                cardId => battle.Player.Hand.Contains(cardId)), Is.True);
            Assert.That(originalEnemyIds.All(
                cardId => battle.Enemy.Hand.Contains(cardId)), Is.True);
            Assert.That(
                battle.Player.Deck.GetDiscardedCards()
                    .Any(card => card.Id == sourceCardId),
                Is.True);
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
            Assert.That(battle.Enemy.IsStanding, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        private static bool RestartAsPlayer(CoreLoopBattle battle)
        {
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            return battle.TryResolvePlayerAutomaticCardChoice(
                pending.InteractionId,
                ResurrectionHerbEffectHandler.RestartRoundOptionId);
        }

        private static void AssertCardConservation(
            BattleParticipant participant)
        {
            Assert.That(
                participant.Hand.Count +
                participant.Deck.DrawCount +
                participant.Deck.DiscardCount,
                Is.EqualTo(participant.Deck.TotalCardCount));
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<BlackjackCard> playerCards,
            IReadOnlyList<BlackjackCard> enemyCards,
            int playerSoul,
            int enemySoul,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractDeck playerDemonDeck = null,
            DemonContractResolver demonContractResolver = null)
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(playerCards),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                playerMaximumSoul: playerSoul,
                playerCurrentSoul: playerSoul,
                enemyMaximumSoul: enemySoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                playerDemonDeck,
                demonContractResolver);
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

        private static DemonContractDeck CreateRepeatedDemonDeck(
            DemonContractKind kind)
        {
            string definitionKey;
            switch (kind)
            {
                case DemonContractKind.Belphegor:
                    definitionKey = DemonContractCatalog.BelphegorKey;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(definitionKey);
            return new DemonContractDeck(
                Enumerable.Range(0, 3)
                    .Select(id => new DemonContractCard(id, definition)),
                seed: 73);
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "ac05-test-stand");
            }
        }

        private sealed class CountingStandPolicy : IEnemyBehaviorPolicy
        {
            public int DecisionCount { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                DecisionCount++;
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "ac05-test-counting-stand");
            }
        }

        private sealed class RoundEndTrackingContractHandler :
            IDemonContractHandler,
            IDemonContractOwnerTurnHandler
        {
            public DemonContractKind Kind => DemonContractKind.Belphegor;

            public int RoundEndCount { get; private set; }

            public DemonContractRuntimeState Activate(
                DemonContractContext context)
            {
                return new TrackingContractRuntimeState();
            }

            public void OnOwnerTurnStarted(DemonContractContext context)
            {
            }

            public bool TryConsumeAutoStandAfterOwnerAction(
                DemonContractContext context)
            {
                return false;
            }

            public void OnRoundEnded(DemonContractContext context)
            {
                RoundEndCount++;
            }
        }

        private sealed class TrackingContractRuntimeState :
            DemonContractRuntimeState
        {
        }
    }
}
