using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class OpponentSelectionSystemValidationTests
    {
        private const int RepeatCount = 10;

        [Test]
        public void EUI05_V01_NormalPairSelectionBattleAndRewardRepeatTenTimes()
        {
            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                StageProgressionSession session = CreateSelectionSession(
                    includeSecondNormalStage: false,
                    eliteChancePercent: 0);
                Assert.That(session.TryStartRun(), Is.True, IterationLabel(iteration));
                OpponentSelectionOffer offer = session.PendingOpponentSelection;

                Assert.That(offer.Candidates, Has.Count.EqualTo(2), IterationLabel(iteration));
                Assert.That(offer.Candidates.All(candidate =>
                    candidate.Preview.Grade == EnemyGrade.Normal), Is.True,
                    IterationLabel(iteration));

                OpponentSelectionCandidate selected =
                    offer.Candidates[iteration % offer.Candidates.Count];
                Assert.That(
                    session.TrySelectOpponent(offer.OfferId, selected.ProfileKey),
                    Is.True,
                    IterationLabel(iteration));

                WinCurrentBattle(session, iteration);
                Assert.That(session.Progress.PendingReward.Offer.Tier,
                    Is.EqualTo(BattleRewardTier.Normal), IterationLabel(iteration));
                Assert.That(session.TrySkipBattleReward(), Is.True,
                    IterationLabel(iteration));
                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.StageCleared),
                    IterationLabel(iteration));
            }
        }

        [Test]
        public void EUI05_V02_NormalAndEliteCandidatesEachCompleteTenTimes()
        {
            foreach (EnemyGrade targetGrade in
                new[] { EnemyGrade.Normal, EnemyGrade.Elite })
            {
                for (int iteration = 0; iteration < RepeatCount; iteration++)
                {
                    StageProgressionSession session = CreateSelectionSession(
                        includeSecondNormalStage: false,
                        eliteChancePercent: 100);
                    Assert.That(session.TryStartRun(), Is.True,
                        GradeIterationLabel(targetGrade, iteration));
                    OpponentSelectionOffer offer = session.PendingOpponentSelection;
                    OpponentSelectionCandidate selected = offer.Candidates.Single(
                        candidate => candidate.Preview.Grade == targetGrade);

                    Assert.That(
                        session.TrySelectOpponent(offer.OfferId, selected.ProfileKey),
                        Is.True,
                        GradeIterationLabel(targetGrade, iteration));
                    Assert.That(session.ActiveStage.BattleProfileKey,
                        Is.EqualTo(selected.ProfileKey),
                        GradeIterationLabel(targetGrade, iteration));

                    CoreLoopViewModel battleModel = CoreLoopPresenter.Create(
                        session.Battle,
                        session.ActiveStage.BattleProfileKey);
                    Assert.That(battleModel.EnemyGrade,
                        Is.EqualTo(targetGrade.ToString().ToUpperInvariant()),
                        GradeIterationLabel(targetGrade, iteration));

                    WinCurrentBattle(session, iteration);
                    BattleRewardTier expectedTier = targetGrade == EnemyGrade.Elite
                        ? BattleRewardTier.HighGrade
                        : BattleRewardTier.Normal;
                    Assert.That(session.Progress.PendingReward.Offer.Tier,
                        Is.EqualTo(expectedTier),
                        GradeIterationLabel(targetGrade, iteration));
                    Assert.That(session.TrySkipBattleReward(), Is.True,
                        GradeIterationLabel(targetGrade, iteration));
                }
            }
        }

        [Test]
        public void EUI05_V03_TwoSelectionsFixedBossRewardAndRestartRepeatTenTimes()
        {
            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                StageProgressionSession session = CreateSelectionSession(
                    includeSecondNormalStage: true,
                    eliteChancePercent: 100);
                int initialDeckCount = session.Progress.Player.Deck.Count;
                Assert.That(session.TryStartRun(), Is.True, IterationLabel(iteration));

                CompleteSelectedStage(
                    session,
                    selectElite: iteration % 2 == 0,
                    selectReward: true,
                    iteration);
                Assert.That(session.TryAdvanceToNextStage(), Is.True,
                    IterationLabel(iteration));

                CompleteSelectedStage(
                    session,
                    selectElite: iteration % 2 != 0,
                    selectReward: false,
                    iteration);
                Assert.That(session.TryAdvanceToNextStage(), Is.True,
                    IterationLabel(iteration));

                Assert.That(session.PendingOpponentSelection, Is.Null,
                    IterationLabel(iteration));
                Assert.That(session.ActiveStage.BattleProfileKey,
                    Is.EqualTo(EnemyCombatProfileCatalog.FinalBossKey),
                    IterationLabel(iteration));
                CoreLoopViewModel bossModel = CoreLoopPresenter.Create(
                    session.Battle,
                    session.ActiveStage.BattleProfileKey);
                Assert.That(bossModel.EnemyGrade, Is.EqualTo("BOSS"),
                    IterationLabel(iteration));
                Assert.That(bossModel.EnemyInformationTitle,
                    Is.EqualTo("BOSS PATTERN"), IterationLabel(iteration));

                WinCurrentBattle(session, iteration);
                Assert.That(session.Progress.PendingReward.CompletionTarget,
                    Is.EqualTo(BattleRewardCompletionTarget.RunVictory),
                    IterationLabel(iteration));
                Assert.That(session.TrySkipBattleReward(), Is.True,
                    IterationLabel(iteration));
                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.RunVictory),
                    IterationLabel(iteration));

                Assert.That(session.TryRestartRun(), Is.True,
                    IterationLabel(iteration));
                AssertFreshFirstOffer(session, initialDeckCount, iteration);
            }
        }

        [Test]
        public void EUI05_V04_StaleDuplicateFocusAndBossWarningStateDoNotLeak()
        {
            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                StageProgressionSession session = CreateSelectionSession(
                    includeSecondNormalStage: true,
                    eliteChancePercent: 100);
                Assert.That(session.TryStartRun(), Is.True, IterationLabel(iteration));
                OpponentSelectionOffer firstOffer = session.PendingOpponentSelection;
                string selectedKey = firstOffer.Candidates[0].ProfileKey;
                StageProgressionController controller = CreateController(
                    session,
                    out GameObject root);
                try
                {
                    controller.RequestFocusOpponent(selectedKey);
                    Assert.That(controller.CurrentViewModel.CanConfirmOpponent,
                        Is.True, IterationLabel(iteration));

                    Assert.That(session.TrySelectOpponent(
                        firstOffer.OfferId + 1,
                        selectedKey), Is.False, IterationLabel(iteration));
                    Assert.That(session.PendingOpponentSelection,
                        Is.SameAs(firstOffer), IterationLabel(iteration));

                    Assert.That(session.TrySelectOpponent(
                        firstOffer.OfferId,
                        selectedKey), Is.True, IterationLabel(iteration));
                    Assert.That(session.TrySelectOpponent(
                        firstOffer.OfferId,
                        selectedKey), Is.False, IterationLabel(iteration));
                    InvokePrivate(controller, "RefreshView");

                    Assert.That(controller.CurrentViewModel.FocusedOpponentProfileKey,
                        Is.Null, IterationLabel(iteration));
                    Assert.That(controller.CurrentViewModel.OpponentCandidates,
                        Is.Empty, IterationLabel(iteration));
                    CoreLoopViewModel normalOrEliteModel = CoreLoopPresenter.Create(
                        session.Battle,
                        session.ActiveStage.BattleProfileKey);
                    Assert.That(normalOrEliteModel.EnemyInformationTitle,
                        Is.Not.EqualTo("BOSS PATTERN"), IterationLabel(iteration));
                    Assert.That(normalOrEliteModel.EnemyWarning, Is.Empty,
                        IterationLabel(iteration));

                    WinCurrentBattle(session, iteration);
                    Assert.That(session.TrySkipBattleReward(), Is.True,
                        IterationLabel(iteration));
                    Assert.That(session.TryAdvanceToNextStage(), Is.True,
                        IterationLabel(iteration));
                    InvokePrivate(controller, "RefreshView");

                    OpponentSelectionOffer secondOffer =
                        session.PendingOpponentSelection;
                    Assert.That(secondOffer.OfferId,
                        Is.Not.EqualTo(firstOffer.OfferId), IterationLabel(iteration));
                    Assert.That(controller.CurrentViewModel.FocusedOpponentProfileKey,
                        Is.Null, IterationLabel(iteration));
                    Assert.That(controller.CurrentViewModel.CanConfirmOpponent,
                        Is.False, IterationLabel(iteration));
                    Assert.That(session.TrySelectOpponent(
                        firstOffer.OfferId,
                        secondOffer.Candidates[0].ProfileKey),
                        Is.False,
                        IterationLabel(iteration));
                    Assert.That(session.PendingOpponentSelection,
                        Is.SameAs(secondOffer), IterationLabel(iteration));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        [Test]
        public void EUI05_V05_LegacyFixedRunAndStandaloneBattleRepeatTenTimes()
        {
            StageProgressionSession session = CreateFixedSession();
            int initialDeckCount = session.Progress.Player.Deck.Count;
            Assert.That(session.TryStartRun(), Is.True);
            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                CoreLoopBattle standaloneBattle = CreateDeterministicVictoryBattle(
                    session.Progress.CurrentStage,
                    session.Progress.Player);
                CoreLoopViewModel standaloneModel =
                    CoreLoopPresenter.Create(standaloneBattle);
                Assert.That(standaloneModel.EnemyGrade, Is.EqualTo("UNPROFILED"),
                    IterationLabel(iteration));
                Assert.That(standaloneModel.EnemyWarning, Is.Empty,
                    IterationLabel(iteration));

                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.InBattle),
                    IterationLabel(iteration));
                Assert.That(session.PendingOpponentSelection, Is.Null,
                    IterationLabel(iteration));

                WinCurrentBattle(session, iteration);
                Assert.That(session.TrySkipBattleReward(), Is.True,
                    IterationLabel(iteration));
                Assert.That(session.TryAdvanceToNextStage(), Is.True,
                    IterationLabel(iteration));
                Assert.That(session.ActiveStage.BattleProfileKey,
                    Is.EqualTo(EnemyCombatProfileCatalog.FinalBossKey),
                    IterationLabel(iteration));

                WinCurrentBattle(session, iteration);
                Assert.That(session.TrySkipBattleReward(), Is.True,
                    IterationLabel(iteration));
                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.RunVictory),
                    IterationLabel(iteration));
                Assert.That(session.TryRestartRun(), Is.True,
                    IterationLabel(iteration));
                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.InBattle),
                    IterationLabel(iteration));
                Assert.That(session.PendingOpponentSelection, Is.Null,
                    IterationLabel(iteration));
                Assert.That(session.Progress.Player.Deck.Count,
                    Is.EqualTo(initialDeckCount), IterationLabel(iteration));
            }
        }

        private static StageProgressionSession CreateSelectionSession(
            bool includeSecondNormalStage,
            int eliteChancePercent)
        {
            EnemyCombatProfileCatalog defaultCatalog =
                EnemyCombatProfileCatalog.Default;
            var catalog = new EnemyCombatProfileCatalog(
                new[]
                {
                    defaultCatalog.GetByKey(EnemyCombatProfileCatalog.GunslingerKey),
                    defaultCatalog.GetByKey(EnemyCombatProfileCatalog.CultistKey),
                    defaultCatalog.GetByKey(EnemyCombatProfileCatalog.EnforcerKey)
                });
            return new StageProgressionSession(
                CreateProgress(includeSecondNormalStage, fixedNormalProfiles: false),
                CreateDeterministicVictoryBattle,
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    catalog,
                    20260720,
                    eliteChancePercent));
        }

        private static StageProgressionSession CreateFixedSession()
        {
            return new StageProgressionSession(
                CreateProgress(includeSecondNormalStage: false,
                    fixedNormalProfiles: true),
                CreateDeterministicVictoryBattle);
        }

        private static RunProgress CreateProgress(
            bool includeSecondNormalStage,
            bool fixedNormalProfiles)
        {
            var stages = new List<StageDefinition>();
            stages.Add(fixedNormalProfiles
                ? StageDefinition.CreateForEnemyProfile(
                    "normal-1",
                    "Ash Gate",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.GunslingerKey,
                    10,
                    11)
                : new StageDefinition(
                    "normal-1",
                    "Ash Gate",
                    StageKind.NormalCombat,
                    3,
                    10,
                    11));
            if (includeSecondNormalStage)
            {
                stages.Add(new StageDefinition(
                    "normal-2",
                    "Blood Hall",
                    StageKind.NormalCombat,
                    4,
                    20,
                    21));
            }

            stages.Add(StageDefinition.CreateForEnemyProfile(
                "final-boss",
                "Black Throne",
                StageKind.FinalBossCombat,
                EnemyCombatProfileCatalog.FinalBossKey,
                30,
                31));
            return new RunProgress(stages, CreatePlayer());
        }

        private static PlayerRunState CreatePlayer()
        {
            var cards = new List<RunCardDefinition>(20);
            for (int index = 0; index < 20; index++)
            {
                cards.Add(new RunCardDefinition(index, index % 10 + 1));
            }

            return new PlayerRunState(12, 12, cards);
        }

        private static CoreLoopBattle CreateDeterministicVictoryBattle(
            StageDefinition stage,
            PlayerRunState player)
        {
            return new CoreLoopBattle(
                CreateRepeatedDeck(20, 10, 1),
                CreateRepeatedDeck(20, 10, 10),
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul,
                new SimpleEnemyPolicy());
        }

        private static BlackjackDeck CreateRepeatedDeck(
            int cardCount,
            int firstRank,
            int secondRank)
        {
            var cards = new List<BlackjackCard>(cardCount);
            for (int index = 0; index < cardCount; index++)
            {
                cards.Add(new BlackjackCard(
                    index,
                    index % 2 == 0 ? firstRank : secondRank));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static void CompleteSelectedStage(
            StageProgressionSession session,
            bool selectElite,
            bool selectReward,
            int iteration)
        {
            OpponentSelectionOffer offer = session.PendingOpponentSelection;
            EnemyGrade targetGrade = selectElite
                ? EnemyGrade.Elite
                : EnemyGrade.Normal;
            OpponentSelectionCandidate candidate = offer.Candidates.Single(
                item => item.Preview.Grade == targetGrade);
            Assert.That(session.TrySelectOpponent(
                offer.OfferId,
                candidate.ProfileKey), Is.True, IterationLabel(iteration));
            WinCurrentBattle(session, iteration);

            bool resolved = selectReward
                ? session.TrySelectBattleReward(
                    session.Progress.PendingReward.Offer.Options[0].OptionId)
                : session.TrySkipBattleReward();
            Assert.That(resolved, Is.True, IterationLabel(iteration));
            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared),
                IterationLabel(iteration));
        }

        private static void WinCurrentBattle(
            StageProgressionSession session,
            int iteration)
        {
            for (int action = 0;
                action < 12 &&
                    session.Progress.State == StageProgressionState.InBattle;
                action++)
            {
                Assert.That(session.TryPlayerStand(), Is.True,
                    $"{IterationLabel(iteration)}, stand {action + 1}");
            }

            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.RewardSelection),
                IterationLabel(iteration));
        }

        private static void AssertFreshFirstOffer(
            StageProgressionSession session,
            int initialDeckCount,
            int iteration)
        {
            Assert.That(session.Progress.CurrentStageIndex, Is.Zero,
                IterationLabel(iteration));
            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection),
                IterationLabel(iteration));
            Assert.That(session.Progress.Player.CurrentSoul,
                Is.EqualTo(session.Progress.Player.MaximumSoul),
                IterationLabel(iteration));
            Assert.That(session.Progress.Player.Deck.Count,
                Is.EqualTo(initialDeckCount), IterationLabel(iteration));
            Assert.That(session.PendingOpponentSelection, Is.Not.Null,
                IterationLabel(iteration));
            Assert.That(session.PendingOpponentSelection.OfferId, Is.Zero,
                IterationLabel(iteration));
            Assert.That(session.ActiveStage, Is.Null, IterationLabel(iteration));
            Assert.That(session.Battle, Is.Null, IterationLabel(iteration));
        }

        private static StageProgressionController CreateController(
            StageProgressionSession session,
            out GameObject root)
        {
            root = new GameObject("EUI05 Controller Validation");
            StageProgressionView view = root.AddComponent<StageProgressionView>();
            StageProgressionRuntime runtime = root.AddComponent<StageProgressionRuntime>();
            SetAutoProperty(runtime, "Session", session);
            SetAutoProperty(runtime, "Instance", runtime);

            StageProgressionController controller =
                root.AddComponent<StageProgressionController>();
            if (controller.CurrentViewModel == null)
            {
                SetPrivateField(controller, "_runtime", runtime);
                SetPrivateField(controller, "_view", view);
                InvokePrivate(controller, "RefreshView");
            }

            return controller;
        }

        private static void SetAutoProperty(
            object target,
            string propertyName,
            object value)
        {
            Type type = target?.GetType() ?? typeof(StageProgressionRuntime);
            PropertyInfo property = type.GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.Public | BindingFlags.NonPublic);
            property.GetSetMethod(true).Invoke(target, new[] { value });
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
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

        private static string IterationLabel(int iteration)
        {
            return $"iteration {iteration + 1}/{RepeatCount}";
        }

        private static string GradeIterationLabel(
            EnemyGrade grade,
            int iteration)
        {
            return $"{grade}, {IterationLabel(iteration)}";
        }
    }
}
