using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class SatanDemonContractTests
    {
        [Test]
        public void DC06_U01_SatanCandidateIsSelectableAndActivationCreatesFirePower()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: new[] { 2, 2, 2, 2, 2, 2, 2, 2 },
                enemyRanks: new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            DemonContractPanelViewModel choosing = DemonContractPresenter.Create(battle);
            DemonContractChoiceViewModel satan = choosing.Choices.First(choice =>
                choice.Title == "사탄");
            Assert.That(satan.CanSelect, Is.True);
            Assert.That(satan.DisabledReason, Is.Empty);

            Assert.That(battle.TryResolvePlayerDemonContract(
                choosing.InteractionId.Value,
                satan.OptionId), Is.True);

            SatanRuntimeState state = GetSatanState(battle);
            BlackjackCard power = GetPowerCard(battle, state.PowerCardId);
            Assert.That(state.RemainingNormalTurns, Is.EqualTo(4));
            Assert.That(power.DefinitionKey,
                Is.EqualTo(CardDefinitionCatalog.SatanPowerFlameKey));
            Assert.That(power.Rank, Is.EqualTo(10));
            Assert.That(power.IsFaceUp, Is.True);
            Assert.That(power.CanUse, Is.True);
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(9));
        }

        [Test]
        public void DC06_U02_ActiveSatanRejectsStandAndPreventsVisibleNumericBust()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: new[] { 10, 2, 5, 2, 2, 2, 2, 2 },
                enemyRanks: new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);

            Assert.That(battle.CanPlayerStand, Is.False);
            Assert.That(CoreLoopPresenter.Create(battle).CanStand, Is.False);
            Assert.That(battle.TryPlayerStand(), Is.False);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.Player.VisibleHandValue.IsBust, Is.True);
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.ActivePlayerDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Satan));
        }

        [Test]
        public void DC06_U03_CountdownExpiresOncePaysTwoAndRemovesPower()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: new[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 },
                enemyRanks: new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);
            int powerCardId = GetSatanState(battle).PowerCardId;

            for (int i = 0; i < 4; i++)
            {
                Assert.That(battle.TryPlayerHit(), Is.True, $"hit {i + 1}");
            }

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(9));
            Assert.That(battle.ActivePlayerDemonContracts, Is.Empty);
            Assert.That(battle.CanPlayerStand, Is.True);
            Assert.That(battle.Player.Hand.Contains(powerCardId), Is.False);
            Assert.That(battle.Player.Deck.ContainsKnownCardId(powerCardId), Is.False);
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(10));
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost, Is.EqualTo(2));
        }

        [Test]
        public void DC06_U04_CountdownSoulCostAtZeroEndsBattleBeforeAnotherAction()
        {
            var enemyPolicy = new StandPolicy();
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: new[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 },
                enemyRanks: new[] { 10, 7, 2, 2, 2, 2 },
                enemyPolicy,
                playerCurrentSoul: 3);
            ActivateSatan(battle);

            for (int i = 0; i < 4; i++)
            {
                Assert.That(battle.TryPlayerHit(), Is.True, $"hit {i + 1}");
            }

            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.ActivePlayerDemonContracts, Is.Empty);
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
        }

        [Test]
        public void DC06_U05_FireForcesAndDiscardsSafeDrawThenFlipsSameUsedCard()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: new[] { 2, 2, 2, 2, 2, 2, 2, 2 },
                enemyRanks: new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);
            SatanRuntimeState state = GetSatanState(battle);
            BlackjackCard power = GetPowerCard(battle, state.PowerCardId);
            int opponentHandCount = battle.Enemy.Hand.Count;

            Assert.That(battle.TryBeginPlayerCardUse(power.Id), Is.True);

            Assert.That(power.Id, Is.EqualTo(state.PowerCardId));
            Assert.That(power.DefinitionKey,
                Is.EqualTo(CardDefinitionCatalog.SatanPowerMightKey));
            Assert.That(power.Rank, Is.EqualTo(8));
            Assert.That(power.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(opponentHandCount));
            Assert.That(battle.Enemy.Deck.GetDiscardedCards().Select(card => card.Rank),
                Does.Contain(2));
            Assert.That(battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.SatanPower));
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.False);
        }

        [Test]
        public void DC06_U06_MightDeclaresTwoDistinctNumbersAndFlipsToFire()
        {
            CardDefinition might =
                CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.SatanPowerMightKey);
            BlackjackCard power = new BlackjackCard(0, might);
            CoreLoopBattle battle = CreateStartedBattleWithoutContract(
                BlackjackDeck.CreateInDrawOrder(new[]
                {
                    power,
                    new BlackjackCard(1, rank: 2),
                    new BlackjackCard(2, rank: 2),
                    new BlackjackCard(3, rank: 2)
                }),
                CreatePlainDeck(new[] { 10, 7, 2, 2, 2, 2 }),
                new StandPolicy());
            IReadOnlyList<int> beforeCounts = battle.Player.Deck.GetKnownRankCounts();

            Assert.That(battle.TryBeginPlayerCardUse(power.Id), Is.True);
            PendingCardEffect first = battle.PendingPlayerCardEffect;
            Assert.That(first.ChoiceKind,
                Is.EqualTo(CardEffectChoiceKind.DeclareFirstOfTwoNumbers));
            int firstOption = first.Options.Single(option =>
                option.NumericValue == 3).Id;
            Assert.That(battle.TryResolvePlayerCardChoice(firstOption), Is.True);

            PendingCardEffect second = battle.PendingPlayerCardEffect;
            Assert.That(second.ChoiceKind,
                Is.EqualTo(CardEffectChoiceKind.DeclareSecondOfTwoNumbers));
            Assert.That(second.ContextNumericValue, Is.EqualTo(3));
            Assert.That(second.Options.Any(option => option.NumericValue == 3), Is.False);
            int secondOption = second.Options.Single(option =>
                option.NumericValue == 7).Id;
            Assert.That(battle.TryResolvePlayerCardChoice(secondOption), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(power.DefinitionKey,
                Is.EqualTo(CardDefinitionCatalog.SatanPowerFlameKey));
            Assert.That(power.Id, Is.Zero);
            Assert.That(power.UseState, Is.EqualTo(CardUseState.Used));
            IReadOnlyList<int> afterCounts = battle.Player.Deck.GetKnownRankCounts();
            Assert.That(afterCounts[8], Is.EqualTo(beforeCounts[8] - 1));
            Assert.That(afterCounts[10], Is.EqualTo(beforeCounts[10] + 1));
        }

        [Test]
        public void DC06_U07_BattleEndRemovesPowerFromAllPlayerLocations()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: new[] { 2, 2, 2, 2, 2, 2, 2, 2 },
                enemyRanks: new[] { 10, 7, 2, 10, 2, 2 },
                new SequencePolicy(EnemyActionType.Hit),
                enemyMaximumSoul: 1);
            ActivateSatan(battle);
            SatanRuntimeState state = GetSatanState(battle);
            BlackjackCard power = GetPowerCard(battle, state.PowerCardId);

            Assert.That(battle.TryBeginPlayerCardUse(power.Id), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(battle.ActivePlayerDemonContracts, Is.Empty);
            Assert.That(battle.Player.Hand.Contains(power.Id), Is.False);
            Assert.That(battle.Player.Deck.ContainsKnownCardId(power.Id), Is.False);
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(8));
        }

        [Test]
        public void DC06_U08_PresentationShowsCountdownAndCurrentPowerFace()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: new[] { 2, 2, 2, 2, 2, 2, 2, 2 },
                enemyRanks: new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);

            DemonContractPanelViewModel model = DemonContractPresenter.Create(battle);

            Assert.That(model.ActiveContracts.Single(), Does.Contain("남은 정상 차례 4"));
            Assert.That(model.ActiveContracts.Single(), Does.Contain("권능 화염(10)"));
        }

        private static CoreLoopBattle CreateSatanBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            IEnemyBehaviorPolicy enemyPolicy,
            int playerCurrentSoul = 12,
            int enemyMaximumSoul = 3)
        {
            return CreateStartedBattle(
                CreatePlainDeck(playerRanks),
                CreatePlainDeck(enemyRanks, startId: 100),
                enemyPolicy,
                CreateRepeatedSatanDeck(),
                playerCurrentSoul,
                enemyMaximumSoul);
        }

        private static CoreLoopBattle CreateStartedBattleWithoutContract(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            IEnemyBehaviorPolicy enemyPolicy)
        {
            return CreateStartedBattle(
                playerDeck,
                enemyDeck,
                enemyPolicy,
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0),
                playerCurrentSoul: 12,
                enemyMaximumSoul: 3);
        }

        private static CoreLoopBattle CreateStartedBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractDeck demonDeck,
            int playerCurrentSoul,
            int enemyMaximumSoul)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                demonDeck,
                DemonContractResolver.CreateDefault());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static void ActivateSatan(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                pending.Options[0].OptionId), Is.True);
            Assert.That(battle.ActivePlayerDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Satan));
        }

        private static SatanRuntimeState GetSatanState(CoreLoopBattle battle)
        {
            return (SatanRuntimeState)battle.ActivePlayerDemonContracts
                .Single(contract => contract.Kind == DemonContractKind.Satan)
                .RuntimeState;
        }

        private static BlackjackCard GetPowerCard(CoreLoopBattle battle, int cardId)
        {
            Assert.That(battle.Player.Hand.TryGetCard(cardId, out BlackjackCard card),
                Is.True);
            return card;
        }

        private static DemonContractDeck CreateRepeatedSatanDeck()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default.GetByKey(
                DemonContractCatalog.SatanKey);
            return new DemonContractDeck(
                Enumerable.Range(0, 4).Select(id =>
                    new DemonContractCard(id, definition)),
                seed: 73);
        }

        private static BlackjackDeck CreatePlainDeck(
            IReadOnlyList<int> ranks,
            int startId = 0)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks.Select(
                (rank, index) => new BlackjackCard(startId + index, rank)));
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public int DecisionCount { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                DecisionCount++;
                return new EnemyDecision(EnemyActionType.Stand, "dc06-stand");
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
                return new EnemyDecision(action, "dc06-sequence");
            }
        }
    }
}
