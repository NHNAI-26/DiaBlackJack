using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class DemonContractPresentationTests
    {
        [Test]
        public void DC05_U01_AvailabilityPreviewDoesNotSpendSoulOrMoveCards()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon,
                DemonContractKind.Leviathan);
            int soulBefore = battle.Player.Soul.Current;
            int drawBefore = battle.PlayerDemonDeck.DrawCount;

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.DemonContract.CanBegin, Is.True);
            Assert.That(model.DemonContract.SoulCost, Is.EqualTo(1));
            Assert.That(model.DemonContract.SoulAfterCost, Is.EqualTo(soulBefore - 1));
            Assert.That(model.DemonContract.ActionText, Does.Contain("-1 SOUL"));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(soulBefore));
            Assert.That(battle.PlayerDemonDeck.DrawCount, Is.EqualTo(drawBefore));
        }

        [Test]
        public void DC05_U02_ContractCandidatesShowAbilityCostAndUnimplementedSatan()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                DemonContractKind.Satan,
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.DemonContract.IsResolving, Is.True);
            Assert.That(model.DemonContract.InteractionKind,
                Is.EqualTo(DemonContractInteractionKind.ChooseContract));
            Assert.That(model.DemonContract.Choices.Count, Is.EqualTo(3));
            Assert.That(model.DemonContract.Choices.All(
                choice => !string.IsNullOrEmpty(choice.Ability)), Is.True);
            Assert.That(model.DemonContract.Choices.All(
                choice => !string.IsNullOrEmpty(choice.Cost)), Is.True);

            DemonContractChoiceViewModel satan = model.DemonContract.Choices.Single(
                choice => choice.Title == "사탄");
            Assert.That(satan.CanSelect, Is.False);
            Assert.That(satan.DisabledReason, Does.Contain("DC-06"));
        }

        [Test]
        public void DC05_U03_BelphegorPreviewIsMarkedPlayerOnly()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon,
                DemonContractKind.Leviathan);
            SelectContract(battle, DemonContractKind.Belphegor);

            Assert.That(battle.TryPlayerHit(), Is.True);
            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.DemonContract.IsResolving, Is.True);
            Assert.That(model.DemonContract.InteractionKind,
                Is.EqualTo(DemonContractInteractionKind.BelphegorTopCard));
            Assert.That(model.DemonContract.OwnerPreview, Does.StartWith("PLAYER ONLY"));
            Assert.That(model.DemonContract.OwnerPreview, Does.Not.Contain("ENEMY"));
            Assert.That(model.EnemyCards, Does.Contain("?"));
        }

        [Test]
        public void DC05_U04_ActiveAndRecentContractResultsRemainVisible()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon,
                DemonContractKind.Leviathan);
            SelectContract(battle, DemonContractKind.Belphegor);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.DemonContract.ActiveContracts.Count, Is.EqualTo(1));
            Assert.That(model.DemonContract.ActiveContracts[0], Does.Contain("벨페고르"));
            Assert.That(model.DemonContract.LastContractResult, Does.Contain("영혼 -1"));
            Assert.That(model.DemonContract.LastContractResult,
                Does.Contain(battle.Player.Soul.Current.ToString()));
            Assert.That(model.DemonContract.CanBegin, Is.False);
            Assert.That(model.DemonContract.FailureReason,
                Is.EqualTo(DemonContractFailureReason.BaseUseLimitReached));
        }

        [Test]
        public void DC05_U05_GameSceneUsesTheSameSafeContractProjection()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon,
                DemonContractKind.Leviathan);
            SelectContract(battle, DemonContractKind.Leviathan);

            GameSceneViewModel model = GameScenePresenter.Create(battle);

            Assert.That(model.Core.DemonContract.ActiveContracts.Single(),
                Does.Contain("레비아탄"));
            Assert.That(model.Core.DemonContract.LastContractResult,
                Does.Not.Contain("?"));
            Assert.That(model.EnemyCards.Count(card => !card.RevealRank), Is.EqualTo(1));
            Assert.That(model.EnemyCards.Single(card => !card.RevealRank).Rank, Is.Zero);
        }

        [Test]
        public void DC05_U06_ControllerForwardsStandaloneContractInteraction()
        {
            GameObject gameObject = new GameObject("DC05 Controller Test");
            gameObject.AddComponent<CoreLoopView>();
            CoreLoopController controller = gameObject.AddComponent<CoreLoopController>();
            try
            {
                if (controller.Battle == null)
                {
                    MethodInfo awake = typeof(CoreLoopController).GetMethod(
                        "Awake",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    awake.Invoke(controller, null);
                }

                var session = new CoreLoopSession(() => CreateUnstartedBattle(
                    DemonContractKind.Belphegor,
                    DemonContractKind.Mammon,
                    DemonContractKind.Leviathan));
                ReplaceControllerSession(controller, session);

                controller.RequestBeginDemonContract();
                PendingDemonContractInteraction pending =
                    controller.Battle.PendingPlayerDemonContractInteraction;
                DemonContractOption belphegor = pending.Options.Single(
                    option => option.ContractDefinitionKey ==
                        DemonContractCatalog.BelphegorKey);

                Assert.That(controller.CurrentViewModel.DemonContract.IsResolving, Is.True);
                controller.RequestResolveDemonContract(
                    pending.InteractionId,
                    belphegor.OptionId);

                Assert.That(controller.Battle.ActivePlayerDemonContracts.Single().Kind,
                    Is.EqualTo(DemonContractKind.Belphegor));
                Assert.That(controller.CurrentViewModel.DemonContract.LastContractResult,
                    Does.Contain("벨페고르"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DC05_U07_PrototypeDeckContainsOneOfEachInitialContract()
        {
            DemonContractDeck deck = DemonContractDeck.CreatePrototype(seed: 20260723);

            Assert.That(deck.TotalCardCount, Is.EqualTo(4));
            Assert.That(deck.AvailableCardCount, Is.EqualTo(4));
            Assert.That(deck.CanTakeCandidates, Is.True);
        }

        private static void SelectContract(
            CoreLoopBattle battle,
            DemonContractKind kind)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            string key = GetDefinitionKey(kind);
            DemonContractOption option = pending.Options.Single(
                candidate => candidate.ContractDefinitionKey == key);
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                option.OptionId), Is.True);
        }

        private static CoreLoopBattle CreateStartedBattle(
            params DemonContractKind[] contractKinds)
        {
            CoreLoopBattle battle = CreateUnstartedBattle(contractKinds);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateUnstartedBattle(
            params DemonContractKind[] contractKinds)
        {
            return new CoreLoopBattle(
                CreateDeck(5, 2, 3, 4, 6, 7, 8),
                CreateDeck(10, 7, 6, 5, 4),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3,
                enemyPolicy: new StandPolicy(),
                playerDemonDeck: CreateDemonDeck(contractKinds));
        }

        private static DemonContractDeck CreateDemonDeck(
            IReadOnlyList<DemonContractKind> kinds)
        {
            var cards = new List<DemonContractCard>(kinds.Count);
            for (int i = 0; i < kinds.Count; i++)
            {
                DemonContractDefinition definition = DemonContractCatalog.Default.GetByKey(
                    GetDefinitionKey(kinds[i]));
                cards.Add(new DemonContractCard(i, definition));
            }

            return new DemonContractDeck(cards, seed: 73);
        }

        private static BlackjackDeck CreateDeck(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static string GetDefinitionKey(DemonContractKind kind)
        {
            switch (kind)
            {
                case DemonContractKind.Satan:
                    return DemonContractCatalog.SatanKey;
                case DemonContractKind.Belphegor:
                    return DemonContractCatalog.BelphegorKey;
                case DemonContractKind.Mammon:
                    return DemonContractCatalog.MammonKey;
                case DemonContractKind.Leviathan:
                    return DemonContractCatalog.LeviathanKey;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
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

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "dc05-stand");
            }
        }
    }
}
