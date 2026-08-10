using System.Reflection;
using System.Collections.Generic;
using Border.SaveLoad.UI;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class GameSceneFullFlowPresentationTests
    {
        [Test]
        public void GSV07_U03_StartingDemonRevealKeepsHudRootVisibleForDetailPanel()
        {
            Assert.That(
                GameFlowController.ShouldShowHudRoot(
                    GameFlowScreen.StartingDemonReveal),
                Is.True);
            Assert.That(
                GameFlowController.ShouldShowHudRoot(
                    GameFlowScreen.OpponentSelection),
                Is.False);
        }

        [Test]
        public void GFH01_U03_TutorialCombatWaitsEvenBeforeCharacterReferencesResolve()
        {
            Assert.That(
                GameFlowController.ShouldDelayCombatForTutorialIntro(
                    isEnteringCombat: true,
                    isTutorialRun: true),
                Is.True);
            Assert.That(
                GameFlowController.ShouldDelayCombatForTutorialIntro(
                    isEnteringCombat: true,
                    isTutorialRun: false),
                Is.False);
            Assert.That(
                GameFlowController.ShouldDelayCombatForTutorialIntro(
                    isEnteringCombat: false,
                    isTutorialRun: true),
                Is.False);
        }

        [Test]
        public void GFH03_U01_FinalBossRevealToCombatForcesFreshEntrance()
        {
            Assert.That(
                GameFlowController.IsFinalBossCombatEntrance(
                    GameFlowScreen.FinalBossReveal,
                    GameFlowScreen.Combat),
                Is.True);
            Assert.That(
                GameFlowController.IsFinalBossCombatEntrance(
                    GameFlowScreen.Shop,
                    GameFlowScreen.Combat),
                Is.False);
            Assert.That(
                GameFlowController.IsFinalBossCombatEntrance(
                    GameFlowScreen.FinalBossReveal,
                    GameFlowScreen.FinalBossReveal),
                Is.False);
        }

        [Test]
        public void GFH03_U02_FinalBossEntranceClearsVisibleCharacterState()
        {
            GameObject controllerObject = new GameObject("FlowControllerTest");
            GameObject characters = new GameObject("Characters");
            GameObject enemy = new GameObject("EnemyCharacter");
            enemy.transform.SetParent(characters.transform);
            CharacterView character = enemy.AddComponent<CharacterView>();
            controllerObject.SetActive(false);
            MoodController mood = controllerObject.AddComponent<MoodController>();
            GameFlowController controller =
                controllerObject.AddComponent<GameFlowController>();
            MoodProfileSO profile =
                ScriptableObject.CreateInstance<MoodProfileSO>();
            try
            {
                SetPrivateField(profile, "id", "bossStage");
                SetPrivateField(
                    profile,
                    "bgmIds",
                    new List<string> { "bossStage" });
                SetPrivateField(
                    mood,
                    "moodProfiles",
                    new List<MoodProfileSO> { profile });
                SetPrivateField(controller, "charactersRoot", characters);
                SetPrivateField(controller, "enemyCharacter", character);
                SetPrivateField(controller, "moodController", mood);
                SetPrivateField(controller, "_hasPresentedCharacters", true);
                SetPrivateField(controller, "_characterEntranceInProgress", true);

                MethodInfo prepare = typeof(GameFlowController).GetMethod(
                    "PrepareFinalBossCharacterEntrance",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(prepare, Is.Not.Null);
                prepare.Invoke(controller, null);

                Assert.That(characters.activeSelf, Is.False);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "_hasPresentedCharacters"),
                    Is.False);
                Assert.That(
                    GetPrivateField<bool>(
                        controller,
                        "_characterEntranceInProgress"),
                    Is.False);

                Assert.That(mood.TryQueuePendingBgm("bossStage"), Is.True);
                MethodInfo show = typeof(GameFlowController).GetMethod(
                    "ShowCharactersWithEntrance",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(show, Is.Not.Null);
                show.Invoke(controller, null);

                Assert.That(characters.activeSelf, Is.True);
                Assert.That(
                    GetPrivateField<object>(character, "_appearanceTween"),
                    Is.Not.Null);
                Assert.That(
                    GetPrivateField<object>(mood, "_pendingBgmProfile"),
                    Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(characters);
                Object.DestroyImmediate(profile);
            }
        }

        [Test]
        public void GFH02_U01_TutorialDialogueAndOpeningClickBlockCombatInput()
        {
            Assert.That(
                TutorialDirector.ShouldBlockCombatInput(
                    begun: true,
                    finished: false,
                    gateActive: false,
                    gateActivatedFrame: -1,
                    currentFrame: 10),
                Is.True);
            Assert.That(
                TutorialDirector.ShouldBlockCombatInput(
                    begun: true,
                    finished: false,
                    gateActive: true,
                    gateActivatedFrame: 10,
                    currentFrame: 10),
                Is.True);
            Assert.That(
                TutorialDirector.ShouldBlockCombatInput(
                    begun: true,
                    finished: false,
                    gateActive: true,
                    gateActivatedFrame: 10,
                    currentFrame: 11),
                Is.False);
        }

        [Test]
        public void GFH02_U02_InactiveCharactersHierarchyStillResolvesEnemy()
        {
            GameObject controllerObject = new GameObject("FlowControllerTest");
            GameObject characters = new GameObject("Characters");
            GameObject enemy = new GameObject("EnemyCharacter");
            enemy.transform.SetParent(characters.transform);
            characters.SetActive(false);
            CharacterView expected = enemy.AddComponent<CharacterView>();
            controllerObject.SetActive(false);
            GameFlowController controller =
                controllerObject.AddComponent<GameFlowController>();

            try
            {
                MethodInfo resolve = typeof(GameFlowController).GetMethod(
                    "ResolveSceneReferences",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(resolve, Is.Not.Null);
                resolve.Invoke(controller, null);

                FieldInfo rootField = typeof(GameFlowController).GetField(
                    "charactersRoot",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo enemyField = typeof(GameFlowController).GetField(
                    "enemyCharacter",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(rootField?.GetValue(controller), Is.EqualTo(characters));
                Assert.That(enemyField?.GetValue(controller), Is.EqualTo(expected));
            }
            finally
            {
                Object.DestroyImmediate(controllerObject);
                Object.DestroyImmediate(characters);
            }
        }

        [Test]
        public void GF01_U01_NewRunPresentsTwoGrantedDemonsWithoutSelection()
        {
            FormalRunSession run = CreateRun();

            Assert.That(run.TryStartRun(), Is.True);

            StageProgressionViewModel model =
                StageProgressionPresenter.Create(run);
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.NotStarted));
            Assert.That(model.IsStartingDemonReveal, Is.True);
            Assert.That(model.StartingDemonGrantId, Is.Not.Null);
            Assert.That(model.StartingDemonGrantCards.Count, Is.EqualTo(2));
            Assert.That(
                model.StartingDemonGrantCards[0].DefinitionKey,
                Is.Not.EqualTo(
                    model.StartingDemonGrantCards[1].DefinitionKey));
            Assert.That(model.CanStartRun, Is.False);
            Assert.That(model.CanFocusOpponent, Is.False);
            Assert.That(model.CanConfirmOpponent, Is.False);
            Assert.That(
                run.CombatSession.Progress.Player.DemonDeck.Count,
                Is.EqualTo(2));
            Assert.That(
                run.CombatSession.Progress.Player.StartingDemonGrantCompleted,
                Is.True);
        }

        [Test]
        public void GF01_U02_RevealConfirmationOpensOpponentSelection()
        {
            FormalRunSession run = CreateRun();
            Assert.That(run.TryStartRun(), Is.True);
            Assert.That(
                run.CombatSession.TryCompleteStartingDemonReveal(),
                Is.True);

            Assert.That(run.TryStartRun(), Is.True);

            StageProgressionViewModel model =
                StageProgressionPresenter.Create(run);
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Combat));
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(model.IsStartingDemonReveal, Is.False);
            Assert.That(model.OpponentOfferId, Is.Not.Null);
            Assert.That(model.OpponentCandidates.Count, Is.EqualTo(2));
            GoldRewardCatalog goldRewards =
                GoldRewardCatalog.CreatePrototype();
            foreach (OpponentCandidateViewModel candidate in
                model.OpponentCandidates)
            {
                Assert.That(candidate.SoulAmountText,
                    Is.EqualTo($"×{candidate.MaximumSoul.Substring(5)}"),
                    candidate.ProfileKey);
                Assert.That(candidate.DefeatGoldAmountText,
                    Is.EqualTo($"×{goldRewards.GetAmount(candidate.ProfileKey)}"),
                    candidate.ProfileKey);
            }
            Assert.That(model.CanFocusOpponent, Is.True);
            Assert.That(model.CanConfirmOpponent, Is.False);

            OpponentSelectionOffer offer =
                run.CombatSession.PendingOpponentSelection;
            Assert.That(
                run.TrySelectOpponent(
                    offer.OfferId,
                    offer.Candidates[0].ProfileKey),
                Is.True);
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
        }

        [Test]
        public void GF01_U03_FormalRunScreenSequenceEndsAtBossVictory()
        {
            FormalRunSession run = CreateRun();
            BeginSelectedBattle(run);

            WinCurrentBattle(run.CombatSession);
            AssertShopScreen(run, expectedCompletedShopCount: 0);

            int firstShopOfferId = run.ActiveShop.Offer.OfferId;
            Assert.That(run.TryLeaveShop(firstShopOfferId), Is.True);
            AssertOpponentSelectionScreen(run, expectedStageIndex: 1);
            SelectFirstOpponent(run);

            WinCurrentBattle(run.CombatSession);
            AssertShopScreen(run, expectedCompletedShopCount: 1);

            int secondShopOfferId = run.ActiveShop.Offer.OfferId;
            Assert.That(run.TryLeaveShop(secondShopOfferId), Is.True);
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Combat));
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(
                run.CombatSession.ActiveStage.Kind,
                Is.EqualTo(StageKind.FinalBossCombat));
            Assert.That(
                run.CombatSession.PendingOpponentSelection,
                Is.Null);

            WinCurrentBattle(
                run.CombatSession,
                StageProgressionState.RunVictory);

            StageProgressionViewModel victory =
                StageProgressionPresenter.Create(run);
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.RunVictory));
            Assert.That(run.ActiveShop, Is.Null);
            Assert.That(victory.IsShop, Is.False);
            Assert.That(victory.CanRestartRun, Is.True);
        }

        [Test]
        public void GF01_U04_StaleAndDuplicateInputsDoNotChangeScreenState()
        {
            FormalRunSession run = CreateRun();
            BeginOpponentSelection(run);
            OpponentSelectionOffer opponent =
                run.CombatSession.PendingOpponentSelection;
            OpponentSelectionCandidate candidate = opponent.Candidates[0];

            Assert.That(
                run.TrySelectOpponent(
                    opponent.OfferId + 1,
                    candidate.ProfileKey),
                Is.False);
            AssertOpponentSelectionScreen(run, expectedStageIndex: 0);
            Assert.That(
                run.TrySelectOpponent(
                    opponent.OfferId,
                    candidate.ProfileKey),
                Is.True);
            Assert.That(
                run.TrySelectOpponent(
                    opponent.OfferId,
                    candidate.ProfileKey),
                Is.False);
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));

            WinCurrentBattle(run.CombatSession);
            ShopVisit shop = run.ActiveShop;
            Assert.That(
                run.TryLeaveShop(shop.Offer.OfferId + 1),
                Is.False);
            Assert.That(run.ActiveShop, Is.SameAs(shop));
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Shop));
            Assert.That(
                run.TryLeaveShop(shop.Offer.OfferId),
                Is.True);
            Assert.That(
                run.TryLeaveShop(shop.Offer.OfferId),
                Is.False);
            AssertOpponentSelectionScreen(run, expectedStageIndex: 1);
        }

        [Test]
        public void GF01_U05_RestartPreservesGrantedPairWithoutRegranting()
        {
            FormalRunSession run = CreateRun();
            BeginSelectedBattle(run);
            string firstDemonKey =
                run.CombatSession.Progress.Player.DemonDeck[0].DefinitionKey;
            string secondDemonKey =
                run.CombatSession.Progress.Player.DemonDeck[1].DefinitionKey;

            CompleteVictoryRun(run);
            Assert.That(run.TryRestartRun(), Is.True);

            PlayerRunState player = run.CombatSession.Progress.Player;
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Combat));
            Assert.That(run.CompletedShopCount, Is.Zero);
            Assert.That(run.UtilityPriceLevel, Is.Zero);
            Assert.That(player.CurrentGold, Is.Zero);
            Assert.That(player.DemonDeck.Count, Is.EqualTo(2));
            Assert.That(
                player.DemonDeck[0].DefinitionKey,
                Is.EqualTo(firstDemonKey));
            Assert.That(
                player.DemonDeck[1].DefinitionKey,
                Is.EqualTo(secondDemonKey));
            Assert.That(
                player.StartingDemonGrantCompleted,
                Is.True);
            Assert.That(
                run.CombatSession.PendingStartingDemonGrant,
                Is.Null);
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
        }

        [Test]
        public void GF02_U01_ScreenResolverFollowsFormalRunState()
        {
            FormalRunSession run = CreateRun();
            Assert.That(run.TryStartRun(), Is.True);
            Assert.That(
                GameFlowScreenResolver.Resolve(run),
                Is.EqualTo(GameFlowScreen.StartingDemonReveal));

            Assert.That(
                run.CombatSession.TryCompleteStartingDemonReveal(),
                Is.True);
            Assert.That(run.TryStartRun(), Is.True);
            Assert.That(
                GameFlowScreenResolver.Resolve(run),
                Is.EqualTo(GameFlowScreen.OpponentSelection));

            SelectFirstOpponent(run);
            Assert.That(
                GameFlowScreenResolver.Resolve(run),
                Is.EqualTo(GameFlowScreen.Combat));

            WinCurrentBattle(run.CombatSession);
            Assert.That(
                GameFlowScreenResolver.Resolve(run),
                Is.EqualTo(GameFlowScreen.Shop));
        }

        [Test]
        public void GF02_U02_NullFormalRunHasNoIntegratedScreen()
        {
            Assert.That(
                GameFlowScreenResolver.Resolve(null),
                Is.EqualTo(GameFlowScreen.Unavailable));
        }

        [Test]
        public void GF05_U01_VictoryResultPresentsSavedTotalsAndExitActions()
        {
            FormalRunSession run = CreateRun();
            BeginSelectedBattle(run);
            CompleteVictoryRun(run);
            StageProgressionViewModel progression =
                StageProgressionPresenter.Create(run);
            RunSaveViewModel save = CreateSaveViewModel(
                canRetrySave: false,
                blocksProgressionInput: false,
                saveIndicator: "SAVED");

            RunResultViewModel result = RunResultPresenter.Create(
                GameFlowScreen.RunVictory,
                progression,
                save);

            Assert.That(result.IsVisible, Is.True);
            Assert.That(result.IsVictory, Is.True);
            Assert.That(result.Title, Is.EqualTo("RUN VICTORY"));
            Assert.That(result.PlayerSoul, Is.EqualTo(progression.PlayerSoul));
            Assert.That(result.PlayerGold, Is.EqualTo(progression.PlayerGold));
            Assert.That(result.SaveStatus, Is.EqualTo("SAVED"));
            Assert.That(result.CanRestart, Is.True);
            Assert.That(result.CanReturnToMainMenu, Is.True);
            Assert.That(result.CanRetrySave, Is.False);
        }

        [Test]
        public void GF05_U02_PendingTerminalSaveLocksExitAndOffersRetry()
        {
            FormalRunSession run = CreateRun();
            BeginSelectedBattle(run);
            CompleteVictoryRun(run);
            StageProgressionViewModel progression =
                StageProgressionPresenter.Create(run);
            RunSaveViewModel save = CreateSaveViewModel(
                canRetrySave: true,
                blocksProgressionInput: true,
                saveIndicator: "SAVE FAILED");

            RunResultViewModel result = RunResultPresenter.Create(
                GameFlowScreen.RunVictory,
                progression,
                save);

            Assert.That(result.IsVisible, Is.True);
            Assert.That(result.CanRestart, Is.False);
            Assert.That(result.CanReturnToMainMenu, Is.False);
            Assert.That(result.CanRetrySave, Is.True);
            Assert.That(result.SaveStatus, Is.EqualTo("SAVE FAILED"));
        }

        [Test]
        public void GF05_U03_NonTerminalScreenHidesResult()
        {
            FormalRunSession run = CreateRun();
            BeginSelectedBattle(run);
            StageProgressionViewModel progression =
                StageProgressionPresenter.Create(run);
            RunSaveViewModel save = CreateSaveViewModel(
                canRetrySave: false,
                blocksProgressionInput: false,
                saveIndicator: string.Empty);

            RunResultViewModel result = RunResultPresenter.Create(
                GameFlowScreen.Combat,
                progression,
                save);

            Assert.That(result.IsVisible, Is.False);
            Assert.That(result.CanRestart, Is.False);
            Assert.That(result.CanReturnToMainMenu, Is.False);
            Assert.That(result.CanRetrySave, Is.False);
        }

        private static RunSaveViewModel CreateSaveViewModel(
            bool canRetrySave,
            bool blocksProgressionInput,
            string saveIndicator)
        {
            return new RunSaveViewModel(
                false,
                false,
                false,
                false,
                false,
                canRetrySave,
                blocksProgressionInput,
                string.Empty,
                saveIndicator);
        }

        private static FormalRunSession CreateRun()
        {
            PlayerRunState player = new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 10),
                    new RunCardDefinition(1, 1),
                    new RunCardDefinition(2, 10),
                    new RunCardDefinition(3, 1)
                },
                demonDeck: new List<RunDemonDefinition>());
            EnemyCombatProfileCatalog source =
                EnemyCombatProfileCatalog.Default;
            EnemyCombatProfileCatalog opponents =
                new EnemyCombatProfileCatalog(new[]
                {
                    source.GetByKey(
                        EnemyCombatProfileCatalog.GunslingerKey),
                    source.GetByKey(
                        EnemyCombatProfileCatalog.CultistKey)
                });
            StageProgressionSession combat =
                new StageProgressionSession(
                    new RunProgress(
                        new[]
                        {
                            StageDefinition.CreateForEnemyProfile(
                                "normal-1",
                                "Ash Gate",
                                StageKind.NormalCombat,
                                EnemyCombatProfileCatalog.GunslingerKey,
                                10,
                                11),
                            StageDefinition.CreateForEnemyProfile(
                                "normal-2",
                                "Blood Hall",
                                StageKind.NormalCombat,
                                EnemyCombatProfileCatalog.EnforcerKey,
                                20,
                                21),
                            StageDefinition.CreateForEnemyProfile(
                                "boss",
                                "Black Throne",
                                StageKind.FinalBossCombat,
                                EnemyCombatProfileCatalog.FinalBossKey,
                                30,
                                31)
                        },
                        player),
                    CreateVictoryBattle,
                    opponentSelectionGenerator:
                        new OpponentSelectionGenerator(opponents, 20260731, 0),
                    startingDemonGrantGenerator:
                        new StartingDemonGrantGenerator(
                            DemonContractCatalog.Default,
                            20260734),
                    usesBattleRewards: false);
            return new FormalRunSession(
                combat,
                new ShopOfferGenerator(20260735));
        }

        private static CoreLoopBattle CreateVictoryBattle(
            StageDefinition stage,
            PlayerRunState player)
        {
            return new CoreLoopBattle(
                CreateRepeatedDeck(40, 10, 1),
                CreateRepeatedDeck(40, 10, 10),
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul,
                new SimpleEnemyPolicy());
        }

        private static BlackjackDeck CreateRepeatedDeck(
            int count,
            int firstRank,
            int secondRank)
        {
            List<BlackjackCard> cards =
                new List<BlackjackCard>(count);
            for (int index = 0; index < count; index++)
            {
                cards.Add(new BlackjackCard(
                    index,
                    index % 2 == 0 ? firstRank : secondRank));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static void BeginOpponentSelection(FormalRunSession run)
        {
            Assert.That(run.TryStartRun(), Is.True);
            Assert.That(
                run.CombatSession.TryCompleteStartingDemonReveal(),
                Is.True);
            Assert.That(run.TryStartRun(), Is.True);
            AssertOpponentSelectionScreen(run, expectedStageIndex: 0);
        }

        private static void BeginSelectedBattle(FormalRunSession run)
        {
            BeginOpponentSelection(run);
            SelectFirstOpponent(run);
        }

        private static void SelectFirstOpponent(FormalRunSession run)
        {
            OpponentSelectionOffer offer =
                run.CombatSession.PendingOpponentSelection;
            Assert.That(offer, Is.Not.Null);
            Assert.That(
                run.TrySelectOpponent(
                    offer.OfferId,
                    offer.Candidates[0].ProfileKey),
                Is.True);
        }

        private static void CompleteVictoryRun(FormalRunSession run)
        {
            WinCurrentBattle(run.CombatSession);
            Assert.That(
                run.TryLeaveShop(run.ActiveShop.Offer.OfferId),
                Is.True);
            SelectFirstOpponent(run);
            WinCurrentBattle(run.CombatSession);
            Assert.That(
                run.TryLeaveShop(run.ActiveShop.Offer.OfferId),
                Is.True);
            WinCurrentBattle(
                run.CombatSession,
                StageProgressionState.RunVictory);
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.RunVictory));
        }

        private static void AssertOpponentSelectionScreen(
            FormalRunSession run,
            int expectedStageIndex)
        {
            StageProgressionViewModel model =
                StageProgressionPresenter.Create(run);
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Combat));
            Assert.That(
                run.CombatSession.Progress.CurrentStageIndex,
                Is.EqualTo(expectedStageIndex));
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(model.OpponentOfferId, Is.Not.Null);
            Assert.That(model.OpponentCandidates.Count, Is.EqualTo(2));
            Assert.That(model.IsShop, Is.False);
        }

        private static void AssertShopScreen(
            FormalRunSession run,
            int expectedCompletedShopCount)
        {
            StageProgressionViewModel model =
                StageProgressionPresenter.Create(run);
            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Shop));
            Assert.That(
                run.CompletedShopCount,
                Is.EqualTo(expectedCompletedShopCount));
            Assert.That(run.ActiveShop, Is.Not.Null);
            Assert.That(model.IsShop, Is.True);
            Assert.That(model.ShopOfferId, Is.Not.Null);
            Assert.That(model.ShopCardOptions.Count, Is.EqualTo(5));
            Assert.That(model.CanLeaveShop, Is.True);
        }

        private static void WinCurrentBattle(
            StageProgressionSession session,
            StageProgressionState expectedState =
                StageProgressionState.StageCleared)
        {
            for (int action = 0;
                action < 40 &&
                session.Progress.State == StageProgressionState.InBattle;
                action++)
            {
                Assert.That(
                    session.TryPlayerStand(),
                    Is.True,
                    $"Stand action {action}");
            }

            Assert.That(
                session.Progress.State,
                Is.EqualTo(expectedState));
        }

        private static void SetPrivateField<T>(
            object target,
            string name,
            T value)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string name)
        {
            FieldInfo field = target.GetType().GetField(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            return (T)field.GetValue(target);
        }
    }
}
