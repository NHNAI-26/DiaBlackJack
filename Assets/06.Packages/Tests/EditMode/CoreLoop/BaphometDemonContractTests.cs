using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class BaphometDemonContractTests
    {
        [Test]
        public void DCR04_U29_ActivationInsertsThreeOwnerAndFiveOpponentPentagrams()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(Enumerable.Repeat(2, 10), startId: 0),
                PlainDeck(Enumerable.Repeat(3, 10), startId: 100));
            int playerTotal = battle.Player.Deck.TotalCardCount;
            int enemyTotal = battle.Enemy.Deck.TotalCardCount;
            IReadOnlyList<int> playerRanksBefore =
                battle.Player.Deck.GetKnownRankCounts();
            IReadOnlyList<int> enemyRanksBefore =
                battle.Enemy.Deck.GetKnownRankCounts();

            ActivateBaphomet(battle);

            Assert.That(battle.ActivePlayerDemonContracts.Single().RuntimeState,
                Is.TypeOf<BaphometRuntimeState>());
            Assert.That(battle.BaphometWaveCount, Is.EqualTo(2));
            Assert.That(battle.BaphometPentagramCount, Is.EqualTo(8));
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(playerTotal + 3));
            Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(enemyTotal + 5));
            AssertRankIncrease(
                playerRanksBefore,
                battle.Player.Deck.GetKnownRankCounts(),
                expectedMaximumPentagramRank: 3);
            AssertRankIncrease(
                enemyRanksBefore,
                battle.Enemy.Deck.GetKnownRankCounts(),
                expectedMaximumPentagramRank: 5);
        }

        [Test]
        public void DCR04_U30_OwnerDrawPileExhaustionBustsOwnerAndCleansBattleCards()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(Enumerable.Repeat(1, 5), startId: 0),
                PlainDeck(Enumerable.Repeat(2, 8), startId: 100),
                playerMaximumSoul: 2,
                playerCurrentSoul: 2);
            int playerTotal = battle.Player.Deck.TotalCardCount;
            int enemyTotal = battle.Enemy.Deck.TotalCardCount;
            ActivateBaphomet(battle);
            DrawRemainingPile(battle.Player);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.BaphometWaveCount, Is.Zero);
            Assert.That(battle.BaphometPentagramCount, Is.Zero);
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(playerTotal));
            Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(enemyTotal));
        }

        [Test]
        public void DCR04_U31_OpponentDrawPileExhaustionBustsOpponent()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(Enumerable.Repeat(1, 8), startId: 0),
                PlainDeck(Enumerable.Repeat(1, 5), startId: 100),
                enemyMaximumSoul: 1);
            ActivateBaphomet(battle);
            DrawRemainingPile(battle.Enemy);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Enemy.Soul.Current, Is.Zero);
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
        }

        [Test]
        public void DCR04_U32_ExhaustedWaveResetsAvailableDeckAndReinsertsSameRanks()
        {
            BattleParticipant owner = new BattleParticipant(
                PlainDeck(Enumerable.Repeat(6, 4), startId: 0),
                maximumSoul: 12);
            BattleParticipant opponent = new BattleParticipant(
                PlainDeck(Enumerable.Repeat(7, 4), startId: 100),
                maximumSoul: 3);
            var state = new DemonContractCardState();
            state.CreateBaphometWaves(
                owner,
                CombatantSide.Player,
                opponent,
                sourceContractCardId: 9);
            DrawRemainingPile(owner);

            Assert.That(state.TryResetNextExhaustedBaphometWave(
                out BaphometExhaustion exhaustion), Is.True);

            Assert.That(exhaustion.TargetSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(exhaustion.SourceContractCardId, Is.EqualTo(9));
            Assert.That(owner.Deck.AvailableCardCount, Is.EqualTo(3));
            Assert.That(owner.Deck.GetDrawPileRankCounts()[1], Is.EqualTo(1));
            Assert.That(owner.Deck.GetDrawPileRankCounts()[2], Is.EqualTo(1));
            Assert.That(owner.Deck.GetDrawPileRankCounts()[3], Is.EqualTo(1));
            Assert.That(state.BaphometPentagramCount, Is.EqualTo(8));
        }

        [Test]
        public void DCR04_U33_PaimonExiledPentagramsAreConsumedBeforeWaveReset()
        {
            BattleParticipant owner = new BattleParticipant(
                PlainDeck(Enumerable.Repeat(6, 4), startId: 0),
                maximumSoul: 12);
            BattleParticipant opponent = new BattleParticipant(
                PlainDeck(Enumerable.Repeat(7, 4), startId: 100),
                maximumSoul: 3);
            var state = new DemonContractCardState();
            state.CreateBaphometWaves(
                owner,
                CombatantSide.Player,
                opponent,
                sourceContractCardId: 10);
            IReadOnlyList<BlackjackCard> removed =
                owner.Deck.TakeTop(owner.Deck.AvailableCardCount);
            var returning = new List<BlackjackCard>();
            foreach (BlackjackCard card in removed)
            {
                if (card.DefinitionKey.StartsWith(
                    BaphometPentagramCatalog.KeyPrefix,
                    StringComparison.Ordinal))
                {
                    state.TrackPaimonExile(owner, card);
                }
                else
                {
                    returning.Add(card);
                }
            }

            owner.Deck.ReturnToTop(returning);
            Assert.That(state.PaimonExileCount, Is.EqualTo(3));

            Assert.That(state.TryResetNextExhaustedBaphometWave(out _), Is.True);

            Assert.That(state.PaimonExileCount, Is.Zero);
            Assert.That(owner.Deck.AvailableCardCount, Is.EqualTo(7));
            Assert.That(owner.Deck.TotalCardCount, Is.EqualTo(7));
        }

        [Test]
        public void DCR04_U34_BelialTransferredPentagramReturnsToItsWaveBeforeReset()
        {
            BattleParticipant owner = new BattleParticipant(
                PlainDeck(Enumerable.Repeat(6, 4), startId: 0),
                maximumSoul: 12);
            BattleParticipant opponent = new BattleParticipant(
                PlainDeck(Enumerable.Repeat(7, 4), startId: 100),
                maximumSoul: 3);
            var state = new DemonContractCardState();
            state.CreateBaphometWaves(
                owner,
                CombatantSide.Player,
                opponent,
                sourceContractCardId: 11);
            DrawRemainingPile(owner);
            BlackjackCard pentagram = owner.Hand.Cards.First(card =>
                card.DefinitionKey.StartsWith(
                    BaphometPentagramCatalog.KeyPrefix,
                    StringComparison.Ordinal));
            Assert.That(state.TryTransferFaceUpCard(
                owner,
                opponent,
                pentagram.Id,
                sourceContractCardId: 12,
                out BlackjackCard proxy), Is.True);
            Assert.That(proxy.DefinitionKey, Is.EqualTo(pentagram.DefinitionKey));

            Assert.That(state.TryResetNextExhaustedBaphometWave(out _), Is.True);

            Assert.That(state.BelialTransferCount, Is.Zero);
            Assert.That(owner.Hand.Cards.Any(IsPentagram), Is.False);
            Assert.That(opponent.Hand.Cards.Any(IsPentagram), Is.False);
            Assert.That(owner.Deck.AvailableCardCount, Is.EqualTo(3));
        }

        [Test]
        public void DCR04_U35_PentagramNumbersContributeToHandValue()
        {
            BlackjackCard[] cards = BaphometPentagramCatalog.OwnerRanks
                .Select((rank, index) => new BlackjackCard(
                    index,
                    BaphometPentagramCatalog.GetByRank(rank),
                    isFaceUp: true))
                .ToArray();

            HandValue value = HandValueCalculator.Calculate(cards);

            Assert.That(value.Total, Is.EqualTo(16));
            Assert.That(value.IsBust, Is.False);
        }

        private static bool IsPentagram(BlackjackCard card)
        {
            return card.DefinitionKey.StartsWith(
                BaphometPentagramCatalog.KeyPrefix,
                StringComparison.Ordinal);
        }

        private static void AssertRankIncrease(
            IReadOnlyList<int> before,
            IReadOnlyList<int> after,
            int expectedMaximumPentagramRank)
        {
            for (int rank = 1; rank <= 10; rank++)
            {
                Assert.That(
                    after[rank] - before[rank],
                    Is.EqualTo(rank <= expectedMaximumPentagramRank ? 1 : 0),
                    $"rank {rank}");
            }
        }

        private static void DrawRemainingPile(BattleParticipant participant)
        {
            while (participant.Deck.DrawCount > 0)
            {
                participant.Draw(faceUp: true);
            }
        }

        private static void ActivateBaphomet(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                pending.Options.Single().OptionId), Is.True);
            Assert.That(battle.ActivePlayerDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Baphomet));
        }

        private static CoreLoopBattle CreateBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerMaximumSoul = 12,
            int playerCurrentSoul = 12,
            int enemyMaximumSoul = 5)
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .GetByKey(DemonContractCatalog.BaphometKey);
            var demonDeck = new DemonContractDeck(
                new[] { new DemonContractCard(0, definition) },
                seed: 41);
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                new StandPolicy(),
                CardEffectResolver.CreateDefault(),
                demonDeck,
                DemonContractResolver.CreateDefault());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static BlackjackDeck PlainDeck(
            IEnumerable<int> ranks,
            int startId)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks.Select(
                (rank, index) => new BlackjackCard(startId + index, rank)));
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate stand = observation.ActionCandidates
                    .First(candidate =>
                        candidate.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(stand, "test-stand");
            }
        }
    }
}
