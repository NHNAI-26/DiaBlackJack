using System;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class OpponentSelectionPresentationTests
    {
        [Test]
        public void EUI02_P01_PresenterMapsBothCandidatePreviews()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);

            StageProgressionViewModel model = StageProgressionPresenter.Create(session);
            OpponentSelectionOffer offer = session.PendingOpponentSelection;

            Assert.That(model.OpponentOfferId, Is.EqualTo(offer.OfferId));
            Assert.That(model.OpponentCandidates.Count, Is.EqualTo(2));
            for (int i = 0; i < offer.Candidates.Count; i++)
            {
                EnemyProfilePreview preview = offer.Candidates[i].Preview;
                OpponentCandidateViewModel candidate = model.OpponentCandidates[i];

                Assert.That(candidate.ProfileKey, Is.EqualTo(preview.ProfileKey));
                Assert.That(candidate.DisplayName, Is.EqualTo(preview.DisplayName));
                Assert.That(candidate.Grade, Is.EqualTo(
                    preview.Grade.ToString().ToUpperInvariant()));
                Assert.That(candidate.MaximumSoul, Is.EqualTo(
                    $"SOUL {preview.MaximumSoul}"));
                Assert.That(candidate.Summary, Is.EqualTo(preview.Summary));
                Assert.That(candidate.RewardTier, Is.EqualTo(
                    preview.ExpectedRewardTier == BattleRewardTier.Normal
                        ? "NORMAL REWARD"
                        : "HIGH-GRADE REWARD"));
            }
        }

        [Test]
        public void EUI02_P02_NoFocusDisablesConfirmationAndProgressionInputs()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);

            StageProgressionViewModel model = StageProgressionPresenter.Create(session);

            Assert.That(model.State, Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(model.Message, Is.EqualTo("CHOOSE OPPONENT"));
            Assert.That(model.CanFocusOpponent, Is.True);
            Assert.That(model.CanConfirmOpponent, Is.False);
            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanAdvanceStage, Is.False);
            Assert.That(model.CanRestartRun, Is.False);
            Assert.That(model.CanSelectReward, Is.False);
        }

        [Test]
        public void EUI02_P03_ValidFocusMarksExactlyOneCandidate()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            string profileKey = session.PendingOpponentSelection.Candidates[1].ProfileKey;

            StageProgressionViewModel model = StageProgressionPresenter.Create(
                session,
                profileKey);

            Assert.That(model.FocusedOpponentProfileKey, Is.EqualTo(profileKey));
            Assert.That(model.CanConfirmOpponent, Is.True);
            Assert.That(model.OpponentCandidates.Count(candidate => candidate.IsFocused),
                Is.EqualTo(1));
            Assert.That(
                model.OpponentCandidates.Single(candidate => candidate.IsFocused).ProfileKey,
                Is.EqualTo(profileKey));
        }

        [Test]
        public void EUI02_P04_UnknownOrWrongCaseFocusIsIgnored()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            string existingKey = session.PendingOpponentSelection.Candidates[0].ProfileKey;

            StageProgressionViewModel unknown = StageProgressionPresenter.Create(
                session,
                "unknown-profile");
            StageProgressionViewModel wrongCase = StageProgressionPresenter.Create(
                session,
                existingKey.ToUpperInvariant());

            Assert.That(unknown.FocusedOpponentProfileKey, Is.Null);
            Assert.That(unknown.CanConfirmOpponent, Is.False);
            Assert.That(unknown.OpponentCandidates.All(candidate => !candidate.IsFocused),
                Is.True);
            Assert.That(wrongCase.FocusedOpponentProfileKey, Is.Null);
            Assert.That(wrongCase.CanConfirmOpponent, Is.False);
        }

        [Test]
        public void EUI02_P05_NonSelectionModelDoesNotExposeAnOffer()
        {
            StageProgressionSession session = CreateSelectionSession();

            StageProgressionViewModel model = StageProgressionPresenter.Create(
                session,
                EnemyCombatProfileCatalog.GunslingerKey);

            Assert.That(model.State, Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(model.OpponentOfferId, Is.Null);
            Assert.That(model.OpponentCandidates, Is.Empty);
            Assert.That(model.FocusedOpponentProfileKey, Is.Null);
            Assert.That(model.CanFocusOpponent, Is.False);
            Assert.That(model.CanConfirmOpponent, Is.False);
        }

        [Test]
        public void EUI02_P06_ControllerFocusChangesOnlyPresentationState()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            OpponentSelectionOffer offer = session.PendingOpponentSelection;
            string profileKey = offer.Candidates[0].ProfileKey;
            StageProgressionController controller = CreateController(
                session,
                out GameObject root);
            try
            {
                controller.RequestFocusOpponent(profileKey);

                Assert.That(controller.CurrentViewModel.FocusedOpponentProfileKey,
                    Is.EqualTo(profileKey));
                Assert.That(controller.CurrentViewModel.CanConfirmOpponent, Is.True);
                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.OpponentSelection));
                Assert.That(session.PendingOpponentSelection, Is.SameAs(offer));
                Assert.That(session.Battle, Is.Null);
                Assert.That(session.ActiveStage, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EUI02_P07_ControllerRejectsProfileOutsideCurrentOffer()
        {
            StageProgressionSession session = CreateSelectionSession();
            Assert.That(session.TryStartRun(), Is.True);
            StageProgressionController controller = CreateController(
                session,
                out GameObject root);
            try
            {
                controller.RequestFocusOpponent(EnemyCombatProfileCatalog.FinalBossKey);

                Assert.That(controller.CurrentViewModel.FocusedOpponentProfileKey, Is.Null);
                Assert.That(controller.CurrentViewModel.CanConfirmOpponent, Is.False);
                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.OpponentSelection));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EUI02_I01_StartRunRefreshesSelectionWithoutLoadingBattle()
        {
            StageProgressionSession session = CreateSelectionSession();
            StageProgressionController controller = CreateController(
                session,
                out GameObject root);
            try
            {
                controller.RequestStartRun();

                Assert.That(session.Progress.State,
                    Is.EqualTo(StageProgressionState.OpponentSelection));
                Assert.That(session.Battle, Is.Null);
                Assert.That(controller.CurrentViewModel.State,
                    Is.EqualTo(StageProgressionState.OpponentSelection));
                Assert.That(controller.CurrentViewModel.OpponentCandidates.Count,
                    Is.EqualTo(2));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EUI02_P08_SessionOverloadRejectsNull()
        {
            Assert.Throws<ArgumentNullException>(() =>
                StageProgressionPresenter.Create((StageProgressionSession)null));
        }

        private static StageProgressionSession CreateSelectionSession()
        {
            var progress = new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "normal-1",
                        "Ash Gate",
                        StageKind.NormalCombat,
                        3,
                        10,
                        11),
                    new StageDefinition(
                        "final-boss",
                        "Black Throne",
                        StageKind.FinalBossCombat,
                        7,
                        30,
                        31)
                },
                new PlayerRunState(
                    12,
                    12,
                    new[]
                    {
                        new RunCardDefinition(0, 1),
                        new RunCardDefinition(1, 10)
                    }));
            return new StageProgressionSession(
                progress,
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    EnemyCombatProfileCatalog.Default,
                    20260720));
        }

        private static StageProgressionController CreateController(
            StageProgressionSession session,
            out GameObject root)
        {
            root = new GameObject("EUI02 Controller Test");
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
    }
}
