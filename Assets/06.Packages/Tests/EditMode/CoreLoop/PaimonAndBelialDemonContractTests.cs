using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class PaimonAndBelialDemonContractTests
    {
        [Test]
        public void DCR04_U19_PaimonExilesAChosenOpponentCardBeforeItsCost()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 7, 2, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 10, 2, 10, 2, 7, 8, 9, 6, 5, 4 }, 100),
                new AlwaysHitPolicy(),
                DemonContractKind.Paimon);

            ActivateFirstPlayerContract(battle);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.TryPlayerStand(), Is.True);

            PendingDemonContractInteraction deckChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(deckChoice.Kind,
                Is.EqualTo(DemonContractInteractionKind.PaimonChooseDeck));
            Assert.That(battle.TryResolvePlayerDemonContract(
                deckChoice.InteractionId,
                PaimonDemonContractHandler.OpponentDeckOptionId), Is.True);

            PendingDemonContractInteraction cardChoice =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption rankEight = cardChoice.Options.Single(option =>
                option.NumericValue == 8);
            Assert.That(battle.TryResolvePlayerDemonContract(
                cardChoice.InteractionId,
                rankEight.OptionId), Is.True);

            Assert.That(battle.PaimonExileCount, Is.EqualTo(1));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Rank),
                Is.EqualTo(new[] { 7, 9 }));
        }

        [Test]
        public void DCR04_U20_PaimonSkipRestoresBothPeekedCardsInOrder()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 7, 2, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 10, 2, 10, 2, 7, 8, 9, 6, 5, 4 }, 100),
                new AlwaysHitPolicy(),
                DemonContractKind.Paimon);

            ActivateFirstPlayerContract(battle);
            Assert.That(battle.TryPlayerStand(), Is.True);
            ResolvePlayerPaimonDeckChoice(battle, chooseOpponentDeck: true);
            PendingDemonContractInteraction cardChoice =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(battle.TryResolvePlayerDemonContract(
                cardChoice.InteractionId,
                PaimonDemonContractHandler.SkipExileOptionId), Is.True);

            Assert.That(battle.PaimonExileCount, Is.Zero);
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Rank),
                Is.EqualTo(new[] { 7, 8 }));
        }

        [Test]
        public void DCR04_U21_PaimonCostDoesNotBustAWinningTotalAboveEighteen()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 10, 10, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 10, 2, 10, 2, 7, 8, 9, 6, 5, 4 }, 100),
                new AlwaysHitPolicy(),
                DemonContractKind.Paimon);

            ActivateFirstPlayerContract(battle);
            Assert.That(battle.TryPlayerStand(), Is.True);
            ResolvePlayerPaimonDeckChoice(battle, chooseOpponentDeck: true);
            PendingDemonContractInteraction cardChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                cardChoice.InteractionId,
                PaimonDemonContractHandler.SkipExileOptionId), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.NumericBust));
        }

        [Test]
        public void DCR04_U22_PaimonRestoresExiledOwnershipAtBattleEnd()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 7, 2, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 10, 2, 10, 2, 7, 8, 9, 6, 5, 4 }, 100),
                new AlwaysHitPolicy(),
                DemonContractKind.Paimon,
                playerMaximumSoul: 3,
                playerCurrentSoul: 3);
            int enemyTotalCardCount = battle.Enemy.Deck.TotalCardCount;

            ActivateFirstPlayerContract(battle);
            Assert.That(battle.TryPlayerStand(), Is.True);
            ResolvePlayerPaimonDeckChoice(battle, chooseOpponentDeck: true);
            PendingDemonContractInteraction cardChoice =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption rankEight = cardChoice.Options.Single(option =>
                option.NumericValue == 8);
            Assert.That(battle.TryResolvePlayerDemonContract(
                cardChoice.InteractionId,
                rankEight.OptionId), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.PaimonExileCount, Is.Zero);
            Assert.That(battle.Enemy.Deck.TotalCardCount,
                Is.EqualTo(enemyTotalCardCount));
            Assert.That(battle.Enemy.Deck.AvailableCardCount,
                Is.EqualTo(enemyTotalCardCount));
        }

        [Test]
        public void DCR04_U23_EnemyPaimonUsesOnlyItsScopedPeekAndDoesNotStall()
        {
            CoreLoopBattle battle = CreateEnemyContractBattle(
                PlainDeck(new[] { 10, 2, 10, 2, 9, 8, 7, 6, 5, 4 }),
                PlainDeck(new[] { 10, 10, 2, 3, 4, 5, 6, 7 }, 100),
                DemonContractKind.Paimon,
                new EnemyContractThenStandPolicy());

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Paimon));
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.PaimonExileCount, Is.EqualTo(1));
            Assert.That(battle.Player.Hand.Cards.Select(card => card.Rank),
                Is.EqualTo(new[] { 8, 7 }));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void DCR04_U24_BelialActivationDiscardsOwnerFaceUpCards()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 10, 10, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 2, 2, 3, 4, 5, 6, 7, 8 }, 100),
                new StandPolicy(),
                DemonContractKind.Belial);

            ActivateFirstPlayerContract(battle);

            Assert.That(battle.Player.Hand.GetFaceUpCards(), Is.Empty);
            Assert.That(battle.Player.Hand.HiddenCardCount, Is.EqualTo(1));
            Assert.That(battle.Player.Deck.DiscardCount, Is.EqualTo(1));
        }

        [Test]
        public void DCR04_U25_BelialTransferCancelsStandAndKeepsPlayerAction()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 10, 10, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 2, 2, 3, 4, 5, 6, 7, 8 }, 100),
                new StandPolicy(),
                DemonContractKind.Belial);

            ActivateFirstPlayerContract(battle);
            Assert.That(battle.Enemy.IsStanding, Is.True);
            PendingDemonContractInteraction transfer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption cardOption = transfer.Options.Single(option =>
                option.ContractCardId.HasValue);

            Assert.That(battle.TryResolvePlayerDemonContract(
                transfer.InteractionId,
                cardOption.OptionId), Is.True);

            Assert.That(battle.Enemy.IsStanding, Is.False);
            Assert.That(battle.Enemy.Hand.GetFaceUpCards(), Is.Empty);
            Assert.That(battle.Player.Hand.GetFaceUpCards().Single().Rank,
                Is.EqualTo(2));
            Assert.That(battle.BelialTransferCount, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.CanPlayerAct, Is.True);
        }

        [Test]
        public void DCR04_U26_BelialImmediatelyReusesATransferredUsedManualCard()
        {
            BlackjackDeck enemyDeck = DefinitionDeck(
                CardDefinitionCatalog.GetByKey("crystal-orb-5"),
                new[] { 2, 3, 4, 5, 6, 7, 8 },
                startId: 100);
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 10, 10, 3, 4, 5, 6, 7, 8, 9, 2 }),
                enemyDeck,
                new UseManualCardThenStandPolicy(),
                DemonContractKind.Belial);

            ActivateFirstPlayerContract(battle);
            BlackjackCard usedEnemyCard = battle.Enemy.Hand.GetFaceUpCards()
                .Single();
            Assert.That(usedEnemyCard.UseState, Is.EqualTo(CardUseState.Used));
            PendingDemonContractInteraction transfer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption cardOption = transfer.Options.Single(option =>
                option.ContractCardId == usedEnemyCard.Id);

            Assert.That(battle.TryResolvePlayerDemonContract(
                transfer.InteractionId,
                cardOption.OptionId), Is.True);

            Assert.That(battle.PendingPlayerCardEffect, Is.Not.Null);
            BlackjackCard transferredCard = battle.Player.Hand.GetFaceUpCards()
                .Single();
            Assert.That(transferredCard.Id, Is.Not.EqualTo(usedEnemyCard.Id));
            Assert.That(transferredCard.UseState,
                Is.EqualTo(CardUseState.Resolving));
            Assert.That(battle.TryResolvePlayerCardChoice(optionId: 0), Is.True);
            Assert.That(transferredCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.CanPlayerAct, Is.True);
        }

        [Test]
        public void DCR04_U27_BelialRoundStartCostKillsBeforeTheNextDeal()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 10, 10, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 2, 2, 3, 4, 5, 6, 7, 8 }, 100),
                new StandPolicy(),
                DemonContractKind.Belial,
                playerMaximumSoul: 2,
                playerCurrentSoul: 2);

            ActivateFirstPlayerContract(battle);
            SkipPlayerBelialTransfer(battle);
            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.Player.Hand, Has.Count.EqualTo(0));
            Assert.That(battle.Enemy.Hand, Has.Count.EqualTo(0));
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost,
                Is.EqualTo(BelialDemonContractHandler.RoundStartSoulCost));
        }

        [Test]
        public void DCR04_U28_BelialRestoresOriginalOwnershipAtBattleEnd()
        {
            CoreLoopBattle battle = CreatePlayerContractBattle(
                PlainDeck(new[] { 10, 10, 3, 4, 5, 6, 7, 8 }),
                PlainDeck(new[] { 2, 2, 3, 4, 5, 6, 7, 8 }, 100),
                new StandPolicy(),
                DemonContractKind.Belial,
                enemyMaximumSoul: 1);
            int playerTotal = battle.Player.Deck.TotalCardCount;
            int enemyTotal = battle.Enemy.Deck.TotalCardCount;

            ActivateFirstPlayerContract(battle);
            PendingDemonContractInteraction transfer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption cardOption = transfer.Options.Single(option =>
                option.ContractCardId.HasValue);
            int originalEnemyCardId = cardOption.ContractCardId.Value;
            Assert.That(battle.TryResolvePlayerDemonContract(
                transfer.InteractionId,
                cardOption.OptionId), Is.True);
            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(battle.BelialTransferCount, Is.Zero);
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(playerTotal));
            Assert.That(battle.Enemy.Deck.TotalCardCount, Is.EqualTo(enemyTotal));
            Assert.That(battle.Player.Deck.ContainsKnownCardId(originalEnemyCardId),
                Is.False);
            Assert.That(battle.Enemy.Deck.ContainsKnownCardId(originalEnemyCardId),
                Is.True);
            Assert.That(battle.Enemy.Deck.AvailableCardCount, Is.EqualTo(enemyTotal));
        }

        private static void ResolvePlayerPaimonDeckChoice(
            CoreLoopBattle battle,
            bool chooseOpponentDeck)
        {
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.PaimonChooseDeck));
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                chooseOpponentDeck
                    ? PaimonDemonContractHandler.OpponentDeckOptionId
                    : PaimonDemonContractHandler.OwnerDeckOptionId), Is.True);
        }

        private static void SkipPlayerBelialTransfer(CoreLoopBattle battle)
        {
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.BelialChooseOpponentCard));
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                BelialDemonContractHandler.SkipTransferOptionId), Is.True);
        }

        private static void ActivateFirstPlayerContract(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                pending.Options[0].OptionId), Is.True);
        }

        private static CoreLoopBattle CreatePlayerContractBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractKind kind,
            int playerMaximumSoul = 12,
            int playerCurrentSoul = 12,
            int enemyMaximumSoul = 5)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                CreateDemonDeck(kind),
                DemonContractResolver.CreateDefault());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateEnemyContractBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            DemonContractKind kind,
            IEnemyBehaviorPolicy enemyPolicy)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 5,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                playerDemonDeck: null,
                demonContractResolver: DemonContractResolver.CreateDefault(),
                enemyDemonDeck: CreateDemonDeck(kind));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static DemonContractDeck CreateDemonDeck(DemonContractKind kind)
        {
            string key;
            switch (kind)
            {
                case DemonContractKind.Paimon:
                    key = DemonContractCatalog.PaimonKey;
                    break;
                case DemonContractKind.Belial:
                    key = DemonContractCatalog.BelialKey;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            return new DemonContractDeck(
                new[]
                {
                    new DemonContractCard(
                        0,
                        DemonContractCatalog.Default.GetByKey(key))
                },
                seed: 31);
        }

        private static BlackjackDeck DefinitionDeck(
            CardDefinition firstDefinition,
            IReadOnlyList<int> remainingRanks,
            int startId)
        {
            var cards = new List<BlackjackCard>
            {
                new BlackjackCard(startId, firstDefinition)
            };
            cards.AddRange(remainingRanks.Select((rank, index) =>
                new BlackjackCard(startId + index + 1, rank)));
            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static BlackjackDeck PlainDeck(
            IReadOnlyList<int> ranks,
            int startId = 0)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks.Select(
                (rank, index) => new BlackjackCard(startId + index, rank)));
        }

        private sealed class AlwaysHitPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.Hit)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(candidate, "dcr04-always-hit");
            }
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return EnemyDecision.FromCandidate(
                    observation.ActionCandidates.First(candidate =>
                        candidate.ActionType == EnemyActionType.Stand),
                    "dcr04-stand");
            }
        }

        private sealed class EnemyContractThenStandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(
                    candidate,
                    "dcr04-contract-then-stand");
            }
        }

        private sealed class UseManualCardThenStandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.UseCard &&
                        option.CardEffectOptionId == 0)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.UseCard &&
                        !option.CardEffectOptionId.HasValue)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(
                    candidate,
                    "dcr04-use-manual-then-stand");
            }
        }
    }
}
