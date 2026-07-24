using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class LieDetectorAutomaticCardTests
    {
        private static readonly CardDefinition LieDetector =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.LieDetectorKey);
        private static readonly CardDefinition ThreatHammer =
            CardDefinitionCatalog.GetByKey("threat-hammer-6");

        [Test]
        public void AC03_U01_OnlyDeclarationsFromOneToTenAreAccepted()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, LieDetector),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.ChoiceKind,
                Is.EqualTo(AutomaticCardChoiceKind.LieDetectorNumber));
            Assert.That(
                pending.Options.Select(option => option.NumericValue),
                Is.EqualTo(Enumerable.Range(1, 10).Select(value => (int?)value)));
            Assert.That(
                pending.Options.Select(option => option.OptionId),
                Is.EqualTo(Enumerable.Range(1, 10)));

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    optionId: 0),
                Is.False);
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId + 1,
                    optionId: 10),
                Is.False);
            Assert.That(battle.PendingPlayerAutomaticInteraction,
                Is.SameAs(pending));
        }

        [TestCase(7, true)]
        [TestCase(8, false)]
        public void AC03_U02_OwnerReceivesCorrectAtLeastOrBelowResult(
            int declaredNumber,
            bool expectedAtLeast)
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, LieDetector),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            int sourceCardId =
                battle.PendingPlayerAutomaticInteraction.SourceCardId;

            Assert.That(
                ResolvePlayerDeclaration(battle, declaredNumber),
                Is.True);

            HiddenCardComparisonKnowledge? knowledge =
                battle.PlayerHiddenCardComparisonKnowledge;
            Assert.That(knowledge.HasValue, Is.True);
            Assert.That(knowledge.Value.ObserverSide,
                Is.EqualTo(CombatantSide.Player));
            Assert.That(knowledge.Value.SubjectSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(knowledge.Value.DeclaredNumber,
                Is.EqualTo(declaredNumber));
            Assert.That(knowledge.Value.IsAtLeastDeclaredNumber,
                Is.EqualTo(expectedAtLeast));
            Assert.That(knowledge.Value.RoundNumber,
                Is.EqualTo(battle.RoundNumber));
            Assert.That(
                battle.Player.Deck.GetDiscardedCards()
                    .Single(card => card.Id == sourceCardId)
                    .DefinitionKey,
                Is.EqualTo(CardDefinitionCatalog.LieDetectorKey));
        }

        [Test]
        public void AC03_U03_PublicResultContainsDeclarationButNoComparisonOrRank()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, LieDetector),
                EnemyCards(4, 7),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(ResolvePlayerDeclaration(battle, 6), Is.True);

            LieDetectorPublicResult publicResult =
                battle.LastLieDetectorPublicResult.Value;
            Assert.That(publicResult.OwnerSide,
                Is.EqualTo(CombatantSide.Player));
            Assert.That(publicResult.DeclaredNumber, Is.EqualTo(6));
            Assert.That(publicResult.WasComparable, Is.True);
            Assert.That(
                typeof(LieDetectorPublicResult)
                    .GetProperties()
                    .Select(property => property.Name),
                Is.EquivalentTo(new[]
                {
                    nameof(LieDetectorPublicResult.SourceCardId),
                    nameof(LieDetectorPublicResult.OwnerSide),
                    nameof(LieDetectorPublicResult.DeclaredNumber),
                    nameof(LieDetectorPublicResult.WasComparable)
                }));
            Assert.That(
                typeof(HiddenCardComparisonKnowledge)
                    .GetProperties()
                    .Select(property => property.Name),
                Does.Not.Contain("Rank"));
            Assert.That(
                typeof(HiddenCardComparisonKnowledge)
                    .GetProperties()
                    .Select(property => property.Name),
                Does.Not.Contain("SubjectHiddenCardId"));
        }

        [Test]
        public void AC03_U04_EnemyObservationReceivesOnlyItsLegalComparison()
        {
            var enemyPolicy = new RecordingSequencePolicy(
                EnemyActionType.Hit,
                EnemyActionType.Stand);
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 7, 2),
                EnemyCards(4, 3, LieDetector),
                enemyPolicy);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.OwnerSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                battle.TryResolveAutomaticCardChoice(
                    CombatantSide.Enemy,
                    pending.InteractionId,
                    optionId: 7),
                Is.True);

            Assert.That(battle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.False);
            EnemyObservation observation =
                EnemyObservationFactory.Create(battle, decisionSeed: 1);
            HiddenCardComparisonKnowledge? observationKnowledge =
                observation.LieDetectorComparisonKnowledge;
            Assert.That(observationKnowledge.HasValue, Is.True);
            Assert.That(observationKnowledge.Value.ObserverSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(observationKnowledge.Value.SubjectSide,
                Is.EqualTo(CombatantSide.Player));
            Assert.That(observationKnowledge.Value.DeclaredNumber,
                Is.EqualTo(7));
            Assert.That(observationKnowledge.Value.IsAtLeastDeclaredNumber,
                Is.True);
            Assert.That(
                battle.LastLieDetectorPublicResult.Value.DeclaredNumber,
                Is.EqualTo(7));
        }

        [Test]
        public void AC03_U05_PlayerChangeInvalidatesEnemyKnowledge()
        {
            var enemyPolicy = new RecordingSequencePolicy(
                EnemyActionType.Hit,
                EnemyActionType.Stand);
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(4, 7, 2, 5, 6),
                EnemyCards(4, 3, LieDetector),
                enemyPolicy);
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingAutomaticInteraction;
            Assert.That(
                battle.TryResolveAutomaticCardChoice(
                    CombatantSide.Enemy,
                    pending.InteractionId,
                    optionId: 7),
                Is.True);
            Assert.That(
                EnemyObservationFactory.Create(battle, decisionSeed: 1)
                    .LieDetectorComparisonKnowledge.HasValue,
                Is.True);

            Assert.That(battle.TryBeginPlayerChange(), Is.True);

            Assert.That(
                EnemyObservationFactory.Create(battle, decisionSeed: 2)
                    .LieDetectorComparisonKnowledge.HasValue,
                Is.False);
        }

        [Test]
        public void AC03_U05_ThreatHammerReplacementInvalidatesPlayerKnowledge()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, ThreatHammer, LieDetector),
                EnemyCards(4, 7, 3),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(ResolvePlayerDeclaration(battle, 7), Is.True);
            Assert.That(battle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.True);
            Assert.That(battle.Enemy.IsStanding, Is.True);
            BlackjackCard hammer = battle.Player.Hand.Cards
                .Single(card => card.DefinitionKey == "threat-hammer-6");

            Assert.That(battle.TryBeginPlayerCardUse(hammer.Id), Is.True);
            int discardOptionId =
                battle.PendingPlayerCardEffect.Options[0].Id;
            Assert.That(
                battle.TryResolvePlayerCardChoice(discardOptionId),
                Is.True);

            Assert.That(battle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.False);
        }

        [Test]
        public void AC03_U05_NewRoundAndBattleEndClearComparisonKnowledge()
        {
            CoreLoopBattle continuingBattle = CreateBattle(
                PlayerCards(4, 7, LieDetector, 2, 3),
                EnemyCards(3, 3, 4, 4),
                new StandPolicy(),
                enemyMaximumSoul: 3);
            Assert.That(continuingBattle.Start(), Is.True);
            Assert.That(continuingBattle.TryPlayerHit(), Is.True);
            Assert.That(
                ResolvePlayerDeclaration(continuingBattle, 3),
                Is.True);
            Assert.That(continuingBattle.TryPlayerStand(), Is.True);

            Assert.That(continuingBattle.State,
                Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(continuingBattle.RoundNumber, Is.EqualTo(2));
            Assert.That(
                continuingBattle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.False);

            CoreLoopBattle endingBattle = CreateBattle(
                PlayerCards(4, 7, LieDetector),
                EnemyCards(3, 3),
                new StandPolicy(),
                enemyMaximumSoul: 1);
            Assert.That(endingBattle.Start(), Is.True);
            Assert.That(endingBattle.TryPlayerHit(), Is.True);
            Assert.That(ResolvePlayerDeclaration(endingBattle, 3), Is.True);
            Assert.That(endingBattle.TryPlayerStand(), Is.True);

            Assert.That(endingBattle.State,
                Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(
                endingBattle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.False);
        }

        [TestCase(0)]
        [TestCase(2)]
        public void AC03_U06_NonSingleHiddenCardCompletesAsUnresolvable(
            int hiddenCardCount)
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, LieDetector),
                EnemyCards(4, 7, 5),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            if (hiddenCardCount == 0)
            {
                battle.Enemy.Hand.Cards
                    .Single(card => !card.IsFaceUp)
                    .Reveal();
            }
            else
            {
                BlackjackCard extraHiddenCard = battle.Enemy.Deck.Draw();
                extraHiddenCard.Conceal();
                battle.Enemy.Hand.Add(extraHiddenCard);
            }

            Assert.That(battle.Enemy.Hand.HiddenCardCount,
                Is.EqualTo(hiddenCardCount));
            Assert.That(battle.TryPlayerHit(), Is.True);
            int sourceCardId =
                battle.PendingPlayerAutomaticInteraction.SourceCardId;

            Assert.That(ResolvePlayerDeclaration(battle, 5), Is.True);

            Assert.That(battle.PlayerHiddenCardComparisonKnowledge.HasValue,
                Is.False);
            Assert.That(
                battle.LastLieDetectorPublicResult.Value.WasComparable,
                Is.False);
            Assert.That(
                battle.Player.Deck.GetDiscardedCards()
                    .Count(card => card.Id == sourceCardId),
                Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        private static bool ResolvePlayerDeclaration(
            CoreLoopBattle battle,
            int declaredNumber)
        {
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            return battle.TryResolvePlayerAutomaticCardChoice(
                pending.InteractionId,
                declaredNumber);
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<BlackjackCard> playerCards,
            IReadOnlyList<BlackjackCard> enemyCards,
            IEnemyBehaviorPolicy enemyPolicy,
            int enemyMaximumSoul = 3)
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(playerCards),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                playerMaximumSoul: 12,
                enemyMaximumSoul,
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
                CardDefinition definition = value is CardDefinition cardDefinition
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
                    "ac03-test-stand");
            }
        }

        private sealed class RecordingSequencePolicy : IEnemyBehaviorPolicy
        {
            private readonly Queue<EnemyActionType> _actions;

            public RecordingSequencePolicy(params EnemyActionType[] actions)
            {
                _actions = new Queue<EnemyActionType>(actions);
            }

            public EnemyObservation LastObservation { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                LastObservation = observation;
                EnemyActionType action = _actions.Count > 0
                    ? _actions.Dequeue()
                    : EnemyActionType.Stand;
                return new EnemyDecision(action, "ac03-test-sequence");
            }
        }
    }
}
