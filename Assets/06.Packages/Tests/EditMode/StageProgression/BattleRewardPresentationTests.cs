using System;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class BattleRewardPresentationTests
    {
        [Test]
        public void RW04_P01_PresenterProvidesThreeResolvedRewardOptions()
        {
            RunProgress progress = CreateNormalProgress();
            BattleRewardOffer offer = BeginReward(
                progress,
                BattleRewardTier.Normal,
                BattleRewardCompletionTarget.StageCleared,
                4001);

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);

            Assert.That(model.RewardTier, Is.EqualTo("NORMAL REWARD"));
            Assert.That(model.RewardOptions.Count, Is.EqualTo(3));
            for (int i = 0; i < offer.Options.Count; i++)
            {
                BattleRewardOption source = offer.Options[i];
                CardDefinition definition = CardDefinitionCatalog.GetByKey(
                    source.DefinitionKey);
                BattleRewardOptionViewModel option = model.RewardOptions[i];

                Assert.That(option.OptionId, Is.EqualTo(source.OptionId));
                Assert.That(option.DefinitionKey, Is.EqualTo(source.DefinitionKey));
                Assert.That(option.DisplayName, Is.EqualTo(definition.DisplayName));
                Assert.That(option.Rank, Is.EqualTo(definition.Rank));
                Assert.That(option.EffectSummary, Is.Not.Empty);
            }
        }

        [Test]
        public void RW04_P02_RewardSelectionDisablesUnrelatedProgressionInputs()
        {
            RunProgress progress = CreateNormalProgress();
            BeginReward(
                progress,
                BattleRewardTier.Normal,
                BattleRewardCompletionTarget.StageCleared,
                4002);

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);

            Assert.That(model.CanSelectReward, Is.True);
            Assert.That(model.CanSkipReward, Is.True);
            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanRestartRun, Is.False);
        }

        [Test]
        public void RW04_P03_ControllerForwardsSelectionAndSkipExactlyOnce()
        {
            AssertControllerSelectsExactlyOnce();
            AssertControllerSkipsExactlyOnce();
        }

        [Test]
        public void RW04_P04_BossRewardExplainsRunVictoryDestination()
        {
            RunProgress progress = CreateBossProgress();
            BeginReward(
                progress,
                BattleRewardTier.HighGrade,
                BattleRewardCompletionTarget.RunVictory,
                4004);

            StageProgressionViewModel pendingModel = StageProgressionPresenter.Create(progress);

            Assert.That(pendingModel.RewardTier, Is.EqualTo("HIGH-GRADE REWARD"));
            Assert.That(
                pendingModel.RewardCompletionMessage,
                Is.EqualTo("REWARD COMPLETION WILL END THE RUN"));

            Assert.That(progress.TrySkipBattleReward(), Is.True);
            StageProgressionViewModel completedModel = StageProgressionPresenter.Create(progress);

            Assert.That(completedModel.Message, Is.EqualTo("RUN VICTORY"));
            Assert.That(completedModel.RewardResult, Does.Contain("REWARD SKIPPED"));
            Assert.That(completedModel.DeckCount, Is.EqualTo(2));
        }

        [Test]
        public void RW04_P05_RewardModelDoesNotExposeOpponentInformation()
        {
            RunProgress progress = CreateNormalProgress();
            BeginReward(
                progress,
                BattleRewardTier.Normal,
                BattleRewardCompletionTarget.StageCleared,
                4005);

            StageProgressionViewModel model = StageProgressionPresenter.Create(progress);
            string[] exposedPropertyNames = typeof(StageProgressionViewModel)
                .GetProperties()
                .Concat(typeof(BattleRewardOptionViewModel).GetProperties())
                .Select(property => property.Name)
                .ToArray();

            Assert.That(
                exposedPropertyNames.Any(
                    name => name.IndexOf("Enemy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        name.IndexOf("Hidden", StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False);
            Assert.That(model.RewardOptions.All(option => option.Rank >= 1), Is.True);
        }

        private static void AssertControllerSelectsExactlyOnce()
        {
            RunProgress progress = CreateNormalProgress();
            BattleRewardOffer offer = BeginReward(
                progress,
                BattleRewardTier.Normal,
                BattleRewardCompletionTarget.StageCleared,
                4010);
            var session = new StageProgressionSession(progress);
            StageProgressionController controller = CreateController(session, out GameObject root);
            try
            {
                int initialDeckCount = progress.Player.Deck.Count;
                int optionId = offer.Options[0].OptionId;

                controller.RequestSelectBattleReward(optionId);
                BattleRewardResolution resolution = progress.LastRewardResolution;

                Assert.That(progress.State, Is.EqualTo(StageProgressionState.StageCleared));
                Assert.That(progress.Player.Deck.Count, Is.EqualTo(initialDeckCount + 1));
                Assert.That(resolution.SelectedOptionId, Is.EqualTo(optionId));
                Assert.That(controller.CurrentViewModel.CanAdvanceStage, Is.True);
                Assert.That(controller.CurrentViewModel.RewardResult, Does.Contain("ADDED"));

                controller.RequestSelectBattleReward(optionId);

                Assert.That(progress.Player.Deck.Count, Is.EqualTo(initialDeckCount + 1));
                Assert.That(progress.LastRewardResolution, Is.SameAs(resolution));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void AssertControllerSkipsExactlyOnce()
        {
            RunProgress progress = CreateNormalProgress();
            BeginReward(
                progress,
                BattleRewardTier.Normal,
                BattleRewardCompletionTarget.StageCleared,
                4011);
            var session = new StageProgressionSession(progress);
            StageProgressionController controller = CreateController(session, out GameObject root);
            try
            {
                int initialDeckCount = progress.Player.Deck.Count;

                controller.RequestSkipBattleReward();
                BattleRewardResolution resolution = progress.LastRewardResolution;

                Assert.That(progress.State, Is.EqualTo(StageProgressionState.StageCleared));
                Assert.That(progress.Player.Deck.Count, Is.EqualTo(initialDeckCount));
                Assert.That(resolution.WasSkipped, Is.True);
                Assert.That(controller.CurrentViewModel.RewardResult, Does.Contain("SKIPPED"));

                controller.RequestSkipBattleReward();

                Assert.That(progress.Player.Deck.Count, Is.EqualTo(initialDeckCount));
                Assert.That(progress.LastRewardResolution, Is.SameAs(resolution));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static StageProgressionController CreateController(
            StageProgressionSession session,
            out GameObject root)
        {
            root = new GameObject("RW04 Controller Test");
            StageProgressionView view = root.AddComponent<StageProgressionView>();
            StageProgressionRuntime runtime = root.AddComponent<StageProgressionRuntime>();
            SetAutoProperty(runtime, "Session", session);
            SetAutoProperty(runtime, "Instance", runtime);

            StageProgressionController controller = root.AddComponent<StageProgressionController>();
            if (controller.CurrentViewModel == null)
            {
                SetPrivateField(controller, "_runtime", runtime);
                SetPrivateField(controller, "_view", view);
                InvokePrivate(controller, "RefreshView");
            }

            return controller;
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            Type type = target?.GetType() ?? typeof(StageProgressionRuntime);
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic);
            property.GetSetMethod(true).Invoke(target, new[] { value });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        private static BattleRewardOffer BeginReward(
            RunProgress progress,
            BattleRewardTier tier,
            BattleRewardCompletionTarget target,
            int seed)
        {
            Assert.That(progress.StartRun(), Is.True);
            BattleRewardOffer offer = new BattleRewardGenerator(
                    BattleRewardCatalog.CreateDefault(),
                    seed)
                .Generate(tier);
            Assert.That(progress.TryBeginBattleReward(offer, target), Is.True);
            return offer;
        }

        private static RunProgress CreateNormalProgress()
        {
            return new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "normal-1",
                        "Normal 1",
                        StageKind.NormalCombat,
                        3,
                        10,
                        11),
                    new StageDefinition(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        7,
                        30,
                        31)
                },
                CreatePlayer());
        }

        private static RunProgress CreateBossProgress()
        {
            return new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        7,
                        30,
                        31)
                },
                CreatePlayer());
        }

        private static PlayerRunState CreatePlayer()
        {
            return new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 1),
                    new RunCardDefinition(1, 10)
                });
        }
    }
}
