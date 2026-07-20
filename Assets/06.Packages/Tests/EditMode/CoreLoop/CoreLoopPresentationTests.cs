using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CoreLoopPresentationTests
    {
        [Test]
        public void CL_F04_EnemySoulZeroShowsVictoryAndRestart()
        {
            var session = new CoreLoopSession(CreatePlayerVictoryBattle);
            session.TryPlayerStand();

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanRestart, Is.True);
            Assert.That(model.EnemySoul, Is.EqualTo("0 / 1"));
        }

        [Test]
        public void CL_F05_PlayerSoulZeroShowsDefeatAndRestart()
        {
            var session = new CoreLoopSession(CreatePlayerDefeatBattle);
            session.TryPlayerStand();

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanRestart, Is.True);
            Assert.That(model.PlayerSoul, Is.EqualTo("0 / 1"));
        }

        [Test]
        public void CL_F06_TenRestartsAlwaysCreateCleanInitialState()
        {
            int createdBattleCount = 0;
            var session = new CoreLoopSession(() =>
            {
                createdBattleCount++;
                return CreatePlayerVictoryBattle();
            });

            for (int i = 0; i < 10; i++)
            {
                Assert.That(session.TryPlayerStand(), Is.True, $"End battle {i}");
                Assert.That(session.Battle.State, Is.EqualTo(CoreLoopState.BattleEnded), $"Ended {i}");
                Assert.That(session.TryRestart(), Is.True, $"Restart {i}");
                AssertInitialState(session.Battle, i);
            }

            Assert.That(createdBattleCount, Is.EqualTo(11));
        }

        [Test]
        public void PresentationHidesEnemyPrivateCardWithoutHidingPlayerCard()
        {
            var session = new CoreLoopSession(CreatePlayerVictoryBattle);

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.PlayerCards, Is.EqualTo("10  1"));
            Assert.That(model.EnemyCards, Is.EqualTo("10  ?"));
            Assert.That(model.PlayerTotal, Is.EqualTo(21));
            Assert.That(model.EnemyVisibleTotal, Is.EqualTo(10));
        }

        [Test]
        public void BA04_PlayerTurnShowsFreeChangeAction()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanHit, Is.True);
            Assert.That(model.CanStand, Is.True);
            Assert.That(model.CanChange, Is.True);
            Assert.That(model.IsChoosingChangeCard, Is.False);
            Assert.That(model.ChangeCandidates, Is.Empty);
            Assert.That(model.ChangeActionText, Is.EqualTo("CHANGE (FREE | 12 SOUL LEFT)"));
        }

        [Test]
        public void BA04_ChoosingChangeShowsCandidatesAndDisablesGeneralActions()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerChange();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanChange, Is.False);
            Assert.That(model.IsChoosingChangeCard, Is.True);
            Assert.That(model.ChangeCandidates, Is.EqualTo(new[] { "4", "9" }));
            Assert.That(model.PlayerCards, Is.EqualTo("10"));
            Assert.That(model.EnemyCards, Is.EqualTo("10  ?"));
        }

        [Test]
        public void BA04_CompletedChangeShowsUsedStateAndClearsCandidates()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerChange();
            battle.TrySelectChangedCard(0);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanHit, Is.True);
            Assert.That(model.CanStand, Is.True);
            Assert.That(model.CanChange, Is.True);
            Assert.That(model.IsChoosingChangeCard, Is.False);
            Assert.That(model.ChangeCandidates, Is.Empty);
            Assert.That(model.ChangeActionText, Is.EqualTo("CHANGE (-1 SOUL | 11 LEFT)"));
        }

        [Test]
        public void BA04_LastSoulRuleDisablesPaidChangeAndShowsRequiredSoul()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 1,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerChange();
            battle.TrySelectChangedCard(0);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanChange, Is.False);
            Assert.That(model.ChangeActionText, Is.EqualTo("CHANGE (-1 SOUL | NEED 2+)"));
        }

        [Test]
        public void BA04_ControllerForwardsBothChangeSteps()
        {
            GameObject gameObject = CreateControllerObject(out CoreLoopController controller);
            try
            {
                controller.RequestBeginChange();

                Assert.That(
                    controller.Battle.State,
                    Is.EqualTo(CoreLoopState.PlayerChoosingChangeCard));
                Assert.That(controller.CurrentViewModel.IsChoosingChangeCard, Is.True);
                Assert.That(controller.CurrentViewModel.ChangeCandidates.Count, Is.EqualTo(2));

                controller.RequestSelectChangedCard(0);

                Assert.That(controller.Battle.CompletedPlayerChangeCount, Is.EqualTo(1));
                Assert.That(controller.CurrentViewModel.IsChoosingChangeCard, Is.False);
                Assert.That(
                    controller.CurrentViewModel.ChangeActionText,
                    Is.EqualTo("CHANGE (-1 SOUL | 11 LEFT)"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CU05_PresenterShowsCardUseStateAndDisabledReason()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 5, 7, 8 },
                enemyRanks: new[] { 10, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.PlayerCardActions.Count, Is.EqualTo(2));
            PlayerCardViewModel plainCard = model.PlayerCardActions[0];
            Assert.That(plainCard.Rank, Is.EqualTo(2));
            Assert.That(plainCard.CanUse, Is.False);
            Assert.That(
                plainCard.UnavailableReason,
                Is.EqualTo(CardUseUnavailableReason.CardIsNotManual));
            Assert.That(plainCard.DisabledReason, Is.EqualTo("NO MANUAL EFFECT"));

            PlayerCardViewModel crystalOrb = model.PlayerCardActions[1];
            Assert.That(crystalOrb.Rank, Is.EqualTo(5));
            Assert.That(crystalOrb.DisplayName, Is.EqualTo("수정 구슬"));
            Assert.That(crystalOrb.IsFaceUp, Is.False);
            Assert.That(crystalOrb.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(crystalOrb.CanUse, Is.True);
            Assert.That(crystalOrb.UnavailableReason, Is.EqualTo(CardUseUnavailableReason.None));
            Assert.That(crystalOrb.DisabledReason, Is.Empty);
        }

        [Test]
        public void CU05_PresenterShowsOnlyEffectChoicesWhileSelectionIsPending()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 7 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerCardUse(battle.Player.Hand.Cards[1].Id);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.IsResolvingCardEffect, Is.True);
            Assert.That(model.IsChoosingChangeCard, Is.False);
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanChange, Is.False);
            Assert.That(model.CardEffectPrompt, Is.Not.Empty);
            Assert.That(
                model.CardEffectChoices.Select(choice => choice.OptionId),
                Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(model.PlayerCardActions.All(card => !card.CanUse), Is.True);
            Assert.That(
                model.PlayerCardActions.All(
                    card => card.UnavailableReason ==
                        CardUseUnavailableReason.EffectInProgress),
                Is.True);
        }

        [Test]
        public void CU05_PresenterShowsUsedCardAndSafeRecentEffectResult()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 7 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.TryBeginPlayerCardUse(sourceCard.Id);
            battle.TryResolvePlayerCardChoice(6);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);
            PlayerCardViewModel sourceModel = model.PlayerCardActions.Single(
                card => card.CardId == sourceCard.Id);

            Assert.That(sourceModel.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(sourceModel.CanUse, Is.False);
            Assert.That(sourceModel.DisabledReason, Is.EqualTo("USED"));
            Assert.That(model.LastCardEffect, Is.EqualTo(
                "REVOLVER  |  FAILED  |  ENEMY TURN"));
            Assert.That(model.LastCardEffect, Does.Not.Contain("7"));
            Assert.That(model.EnemyCards, Does.Contain("?"));
        }

        [Test]
        public void CU05_ControllerForwardsStandaloneCardUseAndChoice()
        {
            GameObject gameObject = CreateControllerObject(out CoreLoopController controller);
            try
            {
                var session = new CoreLoopSession(() => CreateBattle(
                    playerRanks: new[] { 2, 5, 7, 8 },
                    enemyRanks: new[] { 10, 7, 5 },
                    playerMaximumSoul: 12,
                    enemyMaximumSoul: 3));
                ReplaceControllerSession(controller, session);
                BlackjackCard sourceCard = controller.Battle.Player.Hand.Cards[1];

                controller.RequestBeginCardUse(sourceCard.Id);

                Assert.That(
                    controller.Battle.State,
                    Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
                Assert.That(controller.CurrentViewModel.IsResolvingCardEffect, Is.True);
                Assert.That(controller.CurrentViewModel.CardEffectChoices.Count, Is.EqualTo(3));

                controller.RequestResolveCardChoice(0);

                Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
                Assert.That(controller.CurrentViewModel.IsResolvingCardEffect, Is.False);
                Assert.That(controller.CurrentViewModel.LastCardEffect, Does.Contain("SUCCESS"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject CreateControllerObject(out CoreLoopController controller)
        {
            var gameObject = new GameObject("BA04 Controller Test");
            gameObject.AddComponent<CoreLoopView>();
            controller = gameObject.AddComponent<CoreLoopController>();
            if (controller.Battle == null)
            {
                MethodInfo awake = typeof(CoreLoopController).GetMethod(
                    "Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                awake.Invoke(controller, null);
            }

            return gameObject;
        }

        private static void ReplaceControllerSession(
            CoreLoopController controller,
            CoreLoopSession session)
        {
            SetPrivateField(controller, "_stageSession", null);
            SetPrivateField(controller, "_session", session);
            MethodInfo refreshView = typeof(CoreLoopController).GetMethod(
                "RefreshView",
                BindingFlags.Instance | BindingFlags.NonPublic);
            refreshView.Invoke(controller, null);
        }

        private static void SetPrivateField(
            CoreLoopController controller,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(CoreLoopController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(controller, value);
        }

        private static void AssertInitialState(CoreLoopBattle battle, int iteration)
        {
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn), $"State {iteration}");
            Assert.That(battle.RoundNumber, Is.EqualTo(1), $"Round {iteration}");
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12), $"Player soul {iteration}");
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(1), $"Enemy soul {iteration}");
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2), $"Player hand {iteration}");
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2), $"Enemy hand {iteration}");
            Assert.That(battle.LastResolution.HasValue, Is.False, $"Last result {iteration}");
        }

        private static CoreLoopBattle CreatePlayerVictoryBattle()
        {
            return CreateBattle(
                playerRanks: new[] { 10, 1 },
                enemyRanks: new[] { 10, 10 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1);
        }

        private static CoreLoopBattle CreatePlayerDefeatBattle()
        {
            return CreateBattle(
                playerRanks: new[] { 10, 8 },
                enemyRanks: new[] { 10, 10 },
                playerMaximumSoul: 1,
                enemyMaximumSoul: 3);
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
