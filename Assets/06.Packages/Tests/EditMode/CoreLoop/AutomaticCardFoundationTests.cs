using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class AutomaticCardFoundationTests
    {
        private static readonly CardDefinition PlainTwo =
            CardDefinitionCatalog.GetByKey("standard-plain-2");
        private static readonly CardDefinition PlainThree =
            CardDefinitionCatalog.GetByKey("standard-plain-3");
        private static readonly CardDefinition PlainFour =
            CardDefinitionCatalog.GetByKey("standard-plain-4");
        private static readonly CardDefinition CrystalOrb =
            CardDefinitionCatalog.GetByKey("crystal-orb-5");
        private static readonly CardDefinition ThreatHammer =
            CardDefinitionCatalog.GetByKey("threat-hammer-6");
        private static readonly CardDefinition MilitaryKnife =
            CardDefinitionCatalog.GetByKey("military-knife-9");
        private static readonly CardDefinition SatanFlame =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.SatanPowerFlameKey);
        private static readonly CardDefinition Poison =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey);

        [TestCase(CardDefinitionCatalog.PoisonKey, 2, CardEffectKind.Poison)]
        [TestCase(CardDefinitionCatalog.ResurrectionHerbKey, 2, CardEffectKind.ResurrectionHerb)]
        [TestCase(CardDefinitionCatalog.LieDetectorKey, 3, CardEffectKind.LieDetector)]
        [TestCase(CardDefinitionCatalog.FlamethrowerKey, 9, CardEffectKind.Flamethrower)]
        [TestCase(CardDefinitionCatalog.PocketWatchKey, 9, CardEffectKind.PocketWatch)]
        public void AC01_U01_CatalogDefinesAutomaticCards(
            string key,
            int rank,
            CardEffectKind effectKind)
        {
            CardDefinition definition = CardDefinitionCatalog.GetByKey(key);

            Assert.That(definition.Rank, Is.EqualTo(rank));
            Assert.That(definition.Activation, Is.EqualTo(CardActivationKind.Automatic));
            Assert.That(definition.Effect, Is.EqualTo(effectKind));
        }

        [Test]
        public void AC01_U02_InitialDealDoesNotTriggerAutomaticCard()
        {
            var handler = new FakeAutomaticCardEffectHandler(waitForChoice: true);
            CoreLoopBattle battle = CreateBattle(
                new[] { Poison, PlainThree, PlainFour },
                new[] { Poison, PlainThree },
                handler);

            Assert.That(battle.Start(), Is.True);

            Assert.That(handler.BeginCount, Is.Zero);
            Assert.That(battle.PendingPlayerAutomaticInteraction, Is.Null);
            Assert.That(battle.Player.Hand.Cards[0].DefinitionKey,
                Is.EqualTo(CardDefinitionCatalog.PoisonKey));
            Assert.That(battle.Player.Hand.Cards[0].IsFaceUp, Is.True);
            Assert.That(battle.Enemy.Hand.Cards[0].DefinitionKey,
                Is.EqualTo(CardDefinitionCatalog.PoisonKey));
            Assert.That(battle.Enemy.Hand.Cards[0].IsFaceUp, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void AC01_U03_PlayerHitLocksInputAndResumesOnceAfterMatchingChoice()
        {
            var handler = new FakeAutomaticCardEffectHandler(waitForChoice: true);
            BlackjackCard automaticCard = new BlackjackCard(2, Poison);
            CoreLoopBattle battle = CreateBattle(
                new[]
                {
                    new BlackjackCard(0, PlainTwo),
                    new BlackjackCard(1, PlainThree),
                    automaticCard,
                    new BlackjackCard(3, PlainFour)
                },
                CreateCards(PlainFour, PlainThree),
                handler);
            Assert.That(battle.Start(), Is.True);
            int totalCardCount = battle.Player.Deck.TotalCardCount;

            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.ResolvingAutomaticCardEffect));
            Assert.That(handler.BeginCount, Is.EqualTo(1));
            Assert.That(battle.TryPlayerHit(), Is.False);
            Assert.That(battle.TryPlayerStand(), Is.False);
            Assert.That(battle.TryBeginPlayerChange(), Is.False);

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId + 1,
                    FakeAutomaticCardEffectHandler.ResolveOptionId),
                Is.False);
            Assert.That(handler.ResolveCount, Is.Zero);
            Assert.That(battle.Player.Hand.Contains(automaticCard.Id), Is.True);

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    FakeAutomaticCardEffectHandler.ResolveOptionId),
                Is.True);

            Assert.That(handler.ResolveCount, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Player.Hand.Cards.Contains(automaticCard), Is.True);
            Assert.That(automaticCard.Id, Is.EqualTo(2));
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(totalCardCount));

            int enemyHandCountAfterResume = battle.Enemy.Hand.Count;
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    FakeAutomaticCardEffectHandler.ResolveOptionId),
                Is.False);
            Assert.That(handler.ResolveCount, Is.EqualTo(1));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(enemyHandCountAfterResume));
        }

        [Test]
        public void AC01_U04_EnemyHitUsesSamePendingBoundaryAndResumesItsAction()
        {
            var handler = new FakeAutomaticCardEffectHandler(waitForChoice: true);
            CoreLoopBattle battle = CreateBattle(
                CreateCards(PlainTwo, PlainThree, PlainTwo),
                new[]
                {
                    new BlackjackCard(0, PlainFour),
                    new BlackjackCard(1, PlainThree),
                    new BlackjackCard(2, Poison)
                },
                handler,
                new HitPolicy());
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.OwnerSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.ResolvingAutomaticCardEffect));

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    FakeAutomaticCardEffectHandler.ResolveOptionId),
                Is.True);

            Assert.That(handler.BeginCount, Is.EqualTo(1));
            Assert.That(handler.ResolveCount, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(3));
        }

        [Test]
        public void AC01_U05_CrystalOrbPublicTakeWaitsForAutomaticEffectBeforeCompleting()
        {
            var handler = new FakeAutomaticCardEffectHandler(waitForChoice: true);
            CoreLoopBattle battle = CreateBattle(
                CreateCards(PlainTwo, CrystalOrb, Poison, PlainFour),
                CreateCards(PlainFour, PlainThree),
                handler);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard crystalOrb = battle.Player.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(crystalOrb.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(1), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(crystalOrb.UseState, Is.EqualTo(CardUseState.Resolving));

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    FakeAutomaticCardEffectHandler.ResolveOptionId),
                Is.True);

            Assert.That(crystalOrb.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.CrystalOrb));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AC01_U06_ForcedPublicDrawResumesKnifeAndSatanAfterAutomaticChoice(
            bool useSatan)
        {
            var handler = new FakeAutomaticCardEffectHandler(waitForChoice: true);
            CardDefinition sourceDefinition = useSatan ? SatanFlame : MilitaryKnife;
            BlackjackCard forcedAutomaticCard = new BlackjackCard(2, Poison);
            CoreLoopBattle battle = CreateBattle(
                CreateCards(PlainTwo, sourceDefinition),
                new[]
                {
                    new BlackjackCard(0, PlainFour),
                    new BlackjackCard(1, PlainThree),
                    forcedAutomaticCard,
                    new BlackjackCard(3, PlainFour)
                },
                handler);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Resolving));

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    FakeAutomaticCardEffectHandler.ResolveOptionId),
                Is.True);

            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Enemy.Hand.Contains(forcedAutomaticCard.Id), Is.False);
            Assert.That(
                battle.Enemy.Deck.GetDiscardedCards().Contains(forcedAutomaticCard),
                Is.True);
            Assert.That(battle.Enemy.Deck.GetDiscardedCards()
                .Count(card => card.Id == forcedAutomaticCard.Id), Is.EqualTo(1));
            Assert.That(battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(useSatan
                    ? CardEffectKind.SatanPower
                    : CardEffectKind.MilitaryKnife));
            if (useSatan)
            {
                Assert.That(sourceCard.DefinitionKey,
                    Is.EqualTo(CardDefinitionCatalog.SatanPowerMightKey));
            }
        }

        [Test]
        public void AC01_U07_HiddenChangeAndHammerReplacementDoNotTriggerAutomaticCard()
        {
            var changeHandler = new FakeAutomaticCardEffectHandler(waitForChoice: true);
            BlackjackCard changedAutomaticCard = new BlackjackCard(2, Poison);
            CoreLoopBattle changeBattle = CreateBattle(
                new[]
                {
                    new BlackjackCard(0, PlainTwo),
                    new BlackjackCard(1, PlainThree),
                    changedAutomaticCard,
                    new BlackjackCard(3, PlainFour)
                },
                CreateCards(PlainFour, PlainThree),
                changeHandler);
            Assert.That(changeBattle.Start(), Is.True);

            Assert.That(changeBattle.TryBeginPlayerChange(), Is.True);
            Assert.That(changeBattle.TrySelectChangedCard(0), Is.True);

            Assert.That(changeHandler.BeginCount, Is.Zero);
            Assert.That(changeBattle.Player.Hand.Contains(changedAutomaticCard.Id), Is.True);
            Assert.That(changedAutomaticCard.IsFaceUp, Is.False);

            var hammerHandler = new FakeAutomaticCardEffectHandler(waitForChoice: true);
            BlackjackCard replacementAutomaticCard = new BlackjackCard(2, Poison);
            CoreLoopBattle hammerBattle = CreateBattle(
                CreateCards(PlainTwo, ThreatHammer),
                new[]
                {
                    new BlackjackCard(0, PlainFour),
                    new BlackjackCard(1, PlainThree),
                    replacementAutomaticCard,
                    new BlackjackCard(3, PlainFour)
                },
                hammerHandler);
            Assert.That(hammerBattle.Start(), Is.True);
            hammerBattle.Enemy.Stand();
            BlackjackCard hammer = hammerBattle.Player.Hand.Cards[1];
            int enemyFaceUpCardId = hammerBattle.Enemy.Hand.Cards[0].Id;

            Assert.That(hammerBattle.TryBeginPlayerCardUse(hammer.Id), Is.True);
            Assert.That(
                hammerBattle.TryResolvePlayerCardChoice(enemyFaceUpCardId),
                Is.True);

            Assert.That(hammerHandler.BeginCount, Is.Zero);
            Assert.That(
                hammerBattle.Enemy.Hand.Contains(replacementAutomaticCard.Id),
                Is.True);
            Assert.That(replacementAutomaticCard.IsFaceUp, Is.False);
        }

        [Test]
        public void AC01_U08_ImmediateAutomaticEffectDiscardsSourceAndContinuesOnce()
        {
            var handler = new FakeAutomaticCardEffectHandler(
                waitForChoice: false,
                disposition: AutomaticCardSourceDisposition.Discard);
            BlackjackCard automaticCard = new BlackjackCard(2, Poison);
            CoreLoopBattle battle = CreateBattle(
                new[]
                {
                    new BlackjackCard(0, PlainTwo),
                    new BlackjackCard(1, PlainThree),
                    automaticCard
                },
                CreateCards(PlainFour, PlainThree),
                handler);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(handler.BeginCount, Is.EqualTo(1));
            Assert.That(handler.ResolveCount, Is.Zero);
            Assert.That(battle.PendingPlayerAutomaticInteraction, Is.Null);
            Assert.That(battle.Player.Hand.Contains(automaticCard.Id), Is.False);
            Assert.That(battle.Player.Deck.GetDiscardedCards().Contains(automaticCard),
                Is.True);
            Assert.That(battle.LastAutomaticCardResult.Value.SourceCardId,
                Is.EqualTo(automaticCard.Id));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AC01_U09_ImmediateForcedAutomaticDiscardIsNotDiscardedTwice(
            bool useSatan)
        {
            var handler = new FakeAutomaticCardEffectHandler(
                waitForChoice: false,
                disposition: AutomaticCardSourceDisposition.Discard);
            BlackjackCard forcedAutomaticCard = new BlackjackCard(2, Poison);
            CoreLoopBattle battle = CreateBattle(
                CreateCards(
                    PlainTwo,
                    useSatan ? SatanFlame : MilitaryKnife),
                new[]
                {
                    new BlackjackCard(0, PlainFour),
                    new BlackjackCard(1, PlainThree),
                    forcedAutomaticCard
                },
                handler);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            Assert.That(handler.BeginCount, Is.EqualTo(1));
            Assert.That(battle.PendingPlayerAutomaticInteraction, Is.Null);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(
                battle.Enemy.Deck.GetDiscardedCards()
                    .Count(card => card.Id == forcedAutomaticCard.Id),
                Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<CardDefinition> playerDefinitions,
            IReadOnlyList<CardDefinition> enemyDefinitions,
            FakeAutomaticCardEffectHandler handler,
            IEnemyBehaviorPolicy enemyPolicy = null)
        {
            return CreateBattle(
                CreateCards(playerDefinitions.ToArray()),
                CreateCards(enemyDefinitions.ToArray()),
                handler,
                enemyPolicy);
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<BlackjackCard> playerCards,
            IReadOnlyList<BlackjackCard> enemyCards,
            FakeAutomaticCardEffectHandler handler,
            IEnemyBehaviorPolicy enemyPolicy = null)
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(playerCards),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 3,
                enemyPolicy ?? new StandPolicy(),
                CardEffectResolver.CreateDefault(),
                automaticCardEffectResolver:
                    new AutomaticCardEffectResolver(handler));
        }

        private static BlackjackCard[] CreateCards(
            params CardDefinition[] definitions)
        {
            var cards = new BlackjackCard[definitions.Length];
            for (int index = 0; index < definitions.Length; index++)
            {
                cards[index] = new BlackjackCard(index, definitions[index]);
            }

            return cards;
        }

        private sealed class FakeAutomaticCardEffectHandler :
            IAutomaticCardEffectHandler
        {
            public const int ResolveOptionId = 7;

            private readonly bool _waitForChoice;
            private readonly AutomaticCardSourceDisposition _disposition;

            public FakeAutomaticCardEffectHandler(
                bool waitForChoice,
                AutomaticCardSourceDisposition disposition =
                    AutomaticCardSourceDisposition.RetainFaceUp)
            {
                _waitForChoice = waitForChoice;
                _disposition = disposition;
            }

            public CardEffectKind EffectKind => CardEffectKind.Poison;

            public int BeginCount { get; private set; }

            public int ResolveCount { get; private set; }

            public AutomaticCardEffectStep Begin(AutomaticCardEffectContext context)
            {
                BeginCount++;
                if (!_waitForChoice)
                {
                    return AutomaticCardEffectStep.Complete(_disposition);
                }

                return AutomaticCardEffectStep.AwaitChoice(
                    CombatantSide.Player,
                    AutomaticCardChoiceKind.PoisonDecision,
                    "테스트 자동 카드 선택",
                    new[]
                    {
                        new AutomaticCardChoiceOption(
                            ResolveOptionId,
                            "해결")
                    });
            }

            public AutomaticCardEffectStep ResolveChoice(
                AutomaticCardEffectContext context,
                PendingAutomaticCardInteraction pendingInteraction,
                AutomaticCardChoiceOption selectedOption)
            {
                ResolveCount++;
                return AutomaticCardEffectStep.Complete(_disposition);
            }
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "test-stand");
            }
        }

        private sealed class HitPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Hit, "test-hit");
            }
        }
    }
}
