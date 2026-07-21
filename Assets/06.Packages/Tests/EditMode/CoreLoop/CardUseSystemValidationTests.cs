using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CardUseSystemValidationTests
    {
        [Test]
        public void CU06_U01_TenStandaloneRestartsResetCardStateAndOwnership()
        {
            var session = new CoreLoopSession(CreateAutoPistolVictoryBattle);

            for (int iteration = 0; iteration < 10; iteration++)
            {
                CoreLoopBattle battle = session.Battle;
                AssertPlayerDeckOwnership(battle, expectedHandCount: 2, iteration);

                BlackjackCard sourceCard = battle.Player.Hand.Cards.Single(
                    card => card.Rank == 7);
                Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
                Assert.That(session.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
                Assert.That(session.TryResolvePlayerCardChoice(7), Is.True);

                Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
                Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
                Assert.That(
                    battle.Player.Deck.AvailableCardCount,
                    Is.EqualTo(battle.Player.Deck.TotalCardCount));
                Assert.That(battle.Player.Deck.CardsInPlayCount, Is.Zero);

                Assert.That(session.TryRestart(), Is.True, $"Restart {iteration}");
            }
        }

        [Test]
        public void CU06_U02_AllHiddenRanksStayMaskedInPresenterAndEffectResult()
        {
            for (int hiddenRank = 1; hiddenRank <= 10; hiddenRank++)
            {
                CoreLoopBattle battle = CreateBattle(
                    new[] { 2, 7 },
                    new[] { 5, hiddenRank, 5 },
                    playerMaximumSoul: 12,
                    enemyMaximumSoul: 3);
                Assert.That(battle.Start(), Is.True);

                BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
                Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

                CoreLoopViewModel pendingModel = CoreLoopPresenter.Create(battle);
                Assert.That(pendingModel.EnemyCards, Does.Contain("?"));
                Assert.That(pendingModel.CardEffectChoices.Count, Is.EqualTo(10));

                int incorrectGuess = hiddenRank == 1 ? 2 : 1;
                Assert.That(
                    battle.TryResolvePlayerCardChoice(incorrectGuess),
                    Is.True,
                    $"Hidden rank {hiddenRank}");

                CoreLoopViewModel resolvedModel = CoreLoopPresenter.Create(battle);
                Assert.That(resolvedModel.EnemyCards, Does.Contain("?"));
                Assert.That(
                    resolvedModel.LastCardEffect,
                    Is.EqualTo("REVOLVER  |  FAILED  |  ENEMY TURN"));
                Assert.That(battle.LastCardEffectResult.Value.SourceCardId, Is.EqualTo(sourceCard.Id));
                Assert.That(
                    battle.LastCardEffectResult.Value.EffectKind,
                    Is.EqualTo(CardEffectKind.AutoPistol));
                Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.False);
                Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.False);
            }
        }

        [Test]
        public void CU06_U03_TenCrystalOrbChoicesPreserveCardOwnership()
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                CoreLoopBattle battle = CreateBattle(
                    new[] { 2, 5, 7, 8, 3, 4 },
                    new[] { 10, 7, 5 },
                    playerMaximumSoul: 12,
                    enemyMaximumSoul: 3);
                Assert.That(battle.Start(), Is.True);

                BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
                Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
                Assert.That(battle.PendingPlayerCardEffect.Options.Count, Is.EqualTo(3));
                AssertDeckTotalIsConserved(battle.Player.Deck, iteration);

                int optionId = iteration % 3;
                Assert.That(battle.TryResolvePlayerCardChoice(optionId), Is.True);

                int expectedHandCount = optionId == 0 ? 2 : 3;
                Assert.That(battle.PendingPlayerCardEffect, Is.Null);
                Assert.That(battle.Player.Hand.Count, Is.EqualTo(expectedHandCount));
                Assert.That(
                    battle.Player.Hand.Cards.Select(card => card.Id).Distinct().Count(),
                    Is.EqualTo(expectedHandCount));
                Assert.That(
                    battle.Player.Deck.CardsInPlayCount,
                    Is.EqualTo(expectedHandCount));
                AssertDeckTotalIsConserved(battle.Player.Deck, iteration);
                Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            }
        }

        [Test]
        public void CU06_U04_TenRunVictoriesRestartWithFreshCardStateAndSoul()
        {
            StageProgressionSession session = CreateRepeatingStageSession(
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1,
                playerRanks: new[] { 2, 9 },
                enemyRanks: new[] { 10, 2, 6, 10 });
            Assert.That(session.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < 10; iteration++)
            {
                CoreLoopBattle battle = session.Battle;
                BlackjackCard sourceCard = battle.Player.Hand.Cards.Single(
                    card => card.Rank == 9);
                battle.Enemy.Draw(faceUp: true);
                Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
                Assert.That(battle.Player.Soul.Current, Is.EqualTo(12));

                Assert.That(session.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
                Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
                Assert.That(
                    session.Progress.State,
                    Is.EqualTo(StageProgressionState.RewardSelection));
                Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(12));
                Assert.That(session.TryBeginPlayerCardUse(sourceCard.Id), Is.False);
                Assert.That(session.TrySkipBattleReward(), Is.True);
                Assert.That(
                    session.Progress.State,
                    Is.EqualTo(StageProgressionState.RunVictory));

                AssertRestartCreatesFreshBattle(
                    session,
                    battle,
                    expectedSoul: 12,
                    iteration);
            }
        }

        [Test]
        public void CU06_U05_TenRunDefeatsRestartWithFreshCardStateAndSoul()
        {
            StageProgressionSession session = CreateRepeatingStageSession(
                playerMaximumSoul: 2,
                enemyMaximumSoul: 3,
                playerRanks: new[] { 10, 5, 10, 2 },
                enemyRanks: new[] { 2, 3, 4, 5 });
            Assert.That(session.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < 10; iteration++)
            {
                CoreLoopBattle battle = session.Battle;
                BlackjackCard sourceCard = battle.Player.Hand.Cards.Single(
                    card => card.Rank == 5);
                Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
                Assert.That(battle.Player.Soul.Current, Is.EqualTo(2));

                Assert.That(session.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
                Assert.That(session.TryResolvePlayerCardChoice(1), Is.True);
                Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
                Assert.That(
                    session.Progress.State,
                    Is.EqualTo(StageProgressionState.RunDefeat));
                Assert.That(session.Progress.Player.CurrentSoul, Is.Zero);
                Assert.That(session.TryResolvePlayerCardChoice(1), Is.False);

                AssertRestartCreatesFreshBattle(
                    session,
                    battle,
                    expectedSoul: 2,
                    iteration);
            }
        }

        private static StageProgressionSession CreateRepeatingStageSession(
            int playerMaximumSoul,
            int enemyMaximumSoul,
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks)
        {
            var progress = new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        enemyMaximumSoul,
                        10,
                        11)
                },
                new PlayerRunState(
                    playerMaximumSoul,
                    playerMaximumSoul,
                    new[]
                    {
                        new RunCardDefinition(0, 2),
                        new RunCardDefinition(1, 5)
                    }));

            return new StageProgressionSession(
                progress,
                (stage, player) => new CoreLoopBattle(
                    CreateDeck(playerRanks),
                    CreateDeck(enemyRanks),
                    player.MaximumSoul,
                    player.CurrentSoul,
                    stage.EnemyMaximumSoul));
        }

        private static void AssertRestartCreatesFreshBattle(
            StageProgressionSession session,
            CoreLoopBattle previousBattle,
            int expectedSoul,
            int iteration)
        {
            Assert.That(session.TryRestartRun(), Is.True, $"Restart {iteration}");
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(expectedSoul));
            Assert.That(session.Battle, Is.Not.SameAs(previousBattle));
            Assert.That(session.Battle.Player.Soul.Current, Is.EqualTo(expectedSoul));
            Assert.That(
                session.Battle.Player.Hand.Cards[1].UseState,
                Is.EqualTo(CardUseState.Available));
        }

        private static void AssertPlayerDeckOwnership(
            CoreLoopBattle battle,
            int expectedHandCount,
            int iteration)
        {
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(expectedHandCount));
            Assert.That(
                battle.Player.Hand.Cards.Select(card => card.Id).Distinct().Count(),
                Is.EqualTo(expectedHandCount));
            Assert.That(
                battle.Player.Deck.CardsInPlayCount,
                Is.EqualTo(expectedHandCount));
            AssertDeckTotalIsConserved(battle.Player.Deck, iteration);
        }

        private static void AssertDeckTotalIsConserved(
            BlackjackDeck deck,
            int iteration)
        {
            Assert.That(
                deck.AvailableCardCount + deck.CardsInPlayCount,
                Is.EqualTo(deck.TotalCardCount),
                $"Total cards {iteration}");
        }

        private static CoreLoopBattle CreateAutoPistolVictoryBattle()
        {
            return CreateBattle(
                new[] { 2, 7 },
                new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1);
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int playerMaximumSoul,
            int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul,
                enemyMaximumSoul);
        }

        private static BlackjackDeck CreateDeck(IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Count);
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }
    }
}
