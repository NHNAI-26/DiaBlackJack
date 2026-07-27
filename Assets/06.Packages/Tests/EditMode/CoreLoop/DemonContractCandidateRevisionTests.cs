using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class DemonContractCandidateRevisionTests
    {
        [Test]
        public void DCR01_U01_EmptyDeckRejectsCandidateRequestWithoutMutation()
        {
            DemonContractDeck deck = CreateDemonDeck(0);

            Assert.That(deck.CanTakeCandidates, Is.False);
            Assert.Throws<System.InvalidOperationException>(() => deck.TakeCandidates());
            Assert.That(deck.DrawCount, Is.Zero);
            Assert.That(deck.DiscardCount, Is.Zero);
            Assert.That(deck.AvailableCardCount, Is.Zero);
            Assert.That(deck.CardsInPlayCount, Is.Zero);
        }

        [Test]
        public void DCR01_U02_OneCardCreatesSingleChoiceAndNoDiscard()
        {
            CoreLoopBattle battle = CreateStartedBattle(1);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(pending.Options.Count, Is.EqualTo(1));
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);
            Assert.That(battle.ActivePlayerDemonContracts.Count, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.DiscardCount, Is.Zero);
            Assert.That(battle.PlayerDemonDeck.CardsInPlayCount, Is.EqualTo(1));
        }

        [Test]
        public void DCR01_U03_TwoOrMoreCardsCreateTwoChoicesAndDiscardOne()
        {
            CoreLoopBattle battle = CreateStartedBattle(4);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(
                pending.Options.Count,
                Is.EqualTo(DemonContractDeck.MaximumCandidateCount));
            Assert.That(
                pending.Options.Select(option => option.ContractCardId).Distinct().Count(),
                Is.EqualTo(2));
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);
            Assert.That(battle.ActivePlayerDemonContracts.Count, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.DiscardCount, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.AvailableCardCount, Is.EqualTo(3));
        }

        [Test]
        public void DCR01_U04_GeneralAndLuciferCandidateCountsStaySeparate()
        {
            DemonContractDeck generalDeck = CreateDemonDeck(6);
            DemonContractDeck luciferDeck = CreateDemonDeck(6);

            Assert.That(DemonContractDeck.MaximumCandidateCount, Is.EqualTo(2));
            Assert.That(DemonContractDeck.LuciferCandidateCount, Is.EqualTo(5));
            Assert.That(
                generalDeck.TakeCandidates().Count,
                Is.EqualTo(DemonContractDeck.MaximumCandidateCount));
            Assert.That(
                luciferDeck.TakeLuciferCandidates().Count,
                Is.EqualTo(DemonContractDeck.LuciferCandidateCount));
            Assert.That(generalDeck.AvailableCardCount, Is.EqualTo(4));
            Assert.That(luciferDeck.AvailableCardCount, Is.EqualTo(1));
        }

        private static CoreLoopBattle CreateStartedBattle(int demonCardCount)
        {
            var resolver = new DemonContractResolver(new NoOpSatanHandler());
            var battle = new CoreLoopBattle(
                CreateBlackjackDeck(0),
                CreateBlackjackDeck(100),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 3,
                new StandPolicy(),
                CardEffectResolver.CreateDefault(),
                CreateDemonDeck(demonCardCount),
                resolver);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static BlackjackDeck CreateBlackjackDeck(int firstId)
        {
            return BlackjackDeck.CreateInDrawOrder(
                Enumerable.Range(firstId, 8)
                    .Select(id => new BlackjackCard(id, rank: 2)));
        }

        private static DemonContractDeck CreateDemonDeck(int count)
        {
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(DemonContractCatalog.SatanKey);
            var cards = new List<DemonContractCard>(count);
            for (int i = 0; i < count; i++)
            {
                cards.Add(new DemonContractCard(i, definition));
            }

            return new DemonContractDeck(cards, seed: 71);
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "dcr01-stand");
            }
        }

        private sealed class NoOpSatanHandler : IDemonContractHandler
        {
            public DemonContractKind Kind => DemonContractKind.Satan;

            public DemonContractRuntimeState Activate(DemonContractContext context)
            {
                return new EmptyDemonContractRuntimeState();
            }
        }
    }
}
