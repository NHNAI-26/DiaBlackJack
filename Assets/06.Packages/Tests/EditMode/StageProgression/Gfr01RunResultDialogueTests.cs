using System.Collections;
using System.Collections.Generic;
using Border.SaveLoad;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace DiaBlackJack.StageProgression.Tests
{
    [Category("GFR01")]
    public sealed class Gfr01RunResultDialogueTests
    {
        private const string DialogueAssetPath =
            "Assets/02. ScriptableObjects/Dialogue/run_result_dialogue.asset";
        private const string SpeechBubblePrefabPath =
            "Assets/03. Prefabs/UI/SpeechBubble.prefab";
        private const string SavedAtUtc =
            "2026-08-09T00:00:00.0000000+00:00";

        [Test]
        public void GFR01_U01_ContractHistoryPersistsAcrossStageAndResetsForNewRun()
        {
            PlayerRunState player = CreatePlayer();
            StageProgressionSession session = new StageProgressionSession(
                new RunProgress(CreateStages(), player),
                CreateContractVictoryBattle,
                usesBattleRewards: false);

            Assert.That(player.HasMadeDemonContract, Is.False);
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                session.Battle.PendingPlayerDemonContractInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(
                session.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);
            Assert.That(
                session.Battle.ActivePlayerDemonContracts,
                Has.Count.EqualTo(1));
            Assert.That(session.TryPlayerStand(), Is.True);

            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(
                player.HasMadeDemonContract,
                Is.True,
                "Finished contracted battle did not mark the run.");
            Assert.That(
                session.TryAdvanceToNextStage(),
                Is.True,
                $"State before advance: {session.Progress.State}");
            Assert.That(
                player.HasMadeDemonContract,
                Is.True,
                "Contract history did not survive stage advance.");

            player.ResetForNewRun();
            Assert.That(player.HasMadeDemonContract, Is.False);
        }

        [Test]
        public void GFR01_U02_CaptureJsonAndRestorePreserveContractHistory()
        {
            PlayerRunState player = CreatePlayer();
            RunProgress progress = new RunProgress(CreateStages(), player);
            Assert.That(progress.StartRun(), Is.True);
            player.MarkDemonContractMade();
            Assert.That(CompleteCurrentStage(progress), Is.True);

            Assert.That(
                RunSaveCapture.TryCapture(
                    progress,
                    new RunSaveCaptureContext(
                        1,
                        "gfr01-run",
                        SavedAtUtc,
                        RunCheckpointKind.CombatSettlementCompleted,
                        20260809,
                        RunNextContentKind.Shop),
                    out RunSaveSnapshot captured,
                    out RunSaveValidationResult captureValidation),
                Is.True,
                $"Capture error: {captureValidation.Error}");
            Assert.That(
                captureValidation.IsValid,
                Is.True,
                $"Capture validation: {captureValidation.Error}");
            Assert.That(
                captured.Player.HasMadeDemonContract,
                Is.True,
                "Capture lost contract history.");
            Assert.That(
                RunSaveSerializer.TrySerialize(captured, out string json),
                Is.True,
                "Serialize failed");
            Assert.That(json, Does.Contain("\"hasMadeDemonContract\":true"));
            Assert.That(
                RunSaveSerializer.TryDeserialize(
                    json,
                    out RunSaveSnapshot deserialized,
                    out RunSaveSerializationStatus status),
                Is.True,
                $"Deserialize status: {status}");
            Assert.That(status, Is.EqualTo(RunSaveSerializationStatus.Success));
            Assert.That(
                deserialized.Player.HasMadeDemonContract,
                Is.True,
                "JSON round trip lost contract history.");

            RunRestoreFactory restoreFactory = new RunRestoreFactory(
                _ => CreateStages());
            Assert.That(
                restoreFactory.TryRestore(
                    deserialized,
                    out RunRestoreResult restored,
                    out RunSaveValidationResult restoreValidation),
                Is.True,
                $"Restore error: {restoreValidation.Error}");
            Assert.That(
                restoreValidation.IsValid,
                Is.True,
                $"Restore validation: {restoreValidation.Error}");
            Assert.That(
                restored.Session.Progress.Player.HasMadeDemonContract,
                Is.True,
                "Restore factory lost contract history.");
        }

        [Test]
        public void GFR01_U03_MissingV2JsonFieldRestoresContractHistoryAsFalse()
        {
            RunSaveSnapshot snapshot = CreateSaveSnapshot(
                hasMadeDemonContract: true);
            Assert.That(
                RunSaveSerializer.TrySerialize(snapshot, out string json),
                Is.True);
            string legacyV2Json = json.Replace(
                "\"hasMadeDemonContract\":true,",
                string.Empty);
            Assert.That(legacyV2Json, Does.Not.Contain("hasMadeDemonContract"));

            Assert.That(
                RunSaveSerializer.TryDeserialize(
                    legacyV2Json,
                    out RunSaveSnapshot restored,
                    out RunSaveSerializationStatus status),
                Is.True);
            Assert.That(status, Is.EqualTo(RunSaveSerializationStatus.Success));
            Assert.That(restored.SchemaVersion, Is.EqualTo(2));
            Assert.That(restored.Player.HasMadeDemonContract, Is.False);
        }

        [Test]
        public void GFR01_U04_VictoryDialogueAddsContractLinesOnlyWhenContracted()
        {
            RunResultDialogueSO dialogue = LoadDialogue();
            PlayerRunState player = CreatePlayer();
            StageDefinition stage = CreateStages()[1];

            RunResultDialogueViewModel normal =
                RunResultDialoguePresenter.Create(
                    GameFlowScreen.RunVictory,
                    player,
                    stage,
                    dialogue);
            Assert.That(normal.CharactersPerSecond, Is.EqualTo(40f));
            Assert.That(normal.Lines, Is.EqualTo(new[]
            {
                "생존하셨습니다. 아마도요.",
                "탈출을 축하드립니다. 고지서는 나중에 따로 보내드리겠습니다."
            }));

            player.MarkDemonContractMade();
            RunResultDialogueViewModel contracted =
                RunResultDialoguePresenter.Create(
                    GameFlowScreen.RunVictory,
                    player,
                    stage,
                    dialogue);
            Assert.That(
                contracted.Lines.Count,
                Is.EqualTo(
                    dialogue.VictoryLines.Count +
                    dialogue.ContractedVictoryLines.Count));
            for (int index = 0;
                 index < dialogue.ContractedVictoryLines.Count;
                 index++)
            {
                Assert.That(
                    contracted.Lines[dialogue.VictoryLines.Count + index],
                    Is.EqualTo(dialogue.ContractedVictoryLines[index]));
            }
        }

        [Test]
        public void GFR01_U05_DefeatDialogueUsesCommonOpponentAndClosingOrder()
        {
            RunResultDialogueSO dialogue = LoadDialogue();
            RunResultDialogueViewModel model =
                RunResultDialoguePresenter.Create(
                    GameFlowScreen.RunDefeat,
                    CreatePlayer(),
                    StageDefinition.CreateForEnemyProfile(
                        "gunslinger-stage",
                        "총잡이",
                        StageKind.NormalCombat,
                        EnemyCombatProfileCatalog.GunslingerKey,
                        1,
                        2),
                    dialogue);
            Assert.That(
                dialogue.TryGetOpponentDefeatLines(
                    EnemyCombatProfileCatalog.GunslingerKey,
                    out IReadOnlyList<string> opponentLines),
                Is.True);

            Assert.That(
                model.Lines.Count,
                Is.EqualTo(
                    dialogue.DefeatOpeningLines.Count +
                    opponentLines.Count +
                    dialogue.DefeatClosingLines.Count));
            int lineIndex = 0;
            foreach (string line in dialogue.DefeatOpeningLines)
            {
                Assert.That(model.Lines[lineIndex++], Is.EqualTo(line));
            }

            foreach (string line in opponentLines)
            {
                Assert.That(model.Lines[lineIndex++], Is.EqualTo(line));
            }

            foreach (string line in dialogue.DefeatClosingLines)
            {
                Assert.That(model.Lines[lineIndex++], Is.EqualTo(line));
            }
        }

        [Test]
        public void GFR01_U06_DialogueAssetMapsAllSixOpponentProfiles()
        {
            RunResultDialogueSO dialogue = LoadDialogue();
            dialogue.ValidateOrThrow();
            string[] profileKeys =
            {
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                EnemyCombatProfileCatalog.GunslingerKey,
                EnemyCombatProfileCatalog.CultistKey,
                EnemyCombatProfileCatalog.TricksterKey,
                EnemyCombatProfileCatalog.EnforcerKey,
                EnemyCombatProfileCatalog.FinalBossKey
            };

            foreach (string profileKey in profileKeys)
            {
                Assert.That(
                    dialogue.TryGetOpponentDefeatLines(
                        profileKey,
                        out IReadOnlyList<string> lines),
                    Is.True,
                    profileKey);
                Assert.That(lines, Has.Count.EqualTo(1), profileKey);
            }
        }

        [Test]
        public void GFR01_U07_MissingOpponentUsesDisplayNameFallback()
        {
            LogAssert.Expect(
                LogType.Warning,
                "Run result dialogue is missing opponent ''.");

            RunResultDialogueViewModel model =
                RunResultDialoguePresenter.Create(
                    GameFlowScreen.RunDefeat,
                    CreatePlayer(),
                    new StageDefinition(
                        "missing",
                        "누락 상대",
                        StageKind.NormalCombat,
                        3,
                        1,
                        2),
                    LoadDialogue());

            Assert.That(
                model.Lines[1],
                Is.EqualTo("누락 상대는 아무래도 쉽지 않은 상대죠."));
        }

        [UnityTest]
        public IEnumerator GFR01_U08_TypewriterCanCompleteImmediately()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                SpeechBubblePrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                SpeechBubbleView bubble =
                    instance.GetComponent<SpeechBubbleView>();
                bubble.Play("한 글자씩 출력", 1f);
                yield return null;

                Assert.That(bubble.IsComplete, Is.False);
                bubble.CompleteImmediately();
                Assert.That(bubble.IsComplete, Is.True);
                Assert.That(bubble.DisplayedText, Is.EqualTo("한 글자씩 출력"));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GFR01_U09_ClickSequenceCompletesAdvancesAndFiresOnce()
        {
            RunResultDialogueSequence sequence =
                new RunResultDialogueSequence(new[] { "첫 줄", "둘째 줄" });
            int completedCount = 0;
            sequence.Completed += () => completedCount++;

            Assert.That(
                sequence.HandleClick(currentLineComplete: false),
                Is.EqualTo(
                    RunResultDialogueClickResult.CompleteCurrentLine));
            Assert.That(sequence.CurrentLine, Is.EqualTo("첫 줄"));
            Assert.That(
                sequence.HandleClick(currentLineComplete: true),
                Is.EqualTo(RunResultDialogueClickResult.ShowNextLine));
            Assert.That(sequence.CurrentLine, Is.EqualTo("둘째 줄"));
            Assert.That(
                sequence.HandleClick(currentLineComplete: true),
                Is.EqualTo(RunResultDialogueClickResult.CompleteDialogue));
            Assert.That(
                sequence.HandleClick(currentLineComplete: true),
                Is.EqualTo(RunResultDialogueClickResult.CompleteDialogue));
            Assert.That(sequence.IsCompleted, Is.True);
            Assert.That(completedCount, Is.EqualTo(1));
        }

        [Test]
        public void GFR01_U10_ResultScreensUseReadyMoodAndHideNormalUi()
        {
            Assert.That(
                GameSceneMoodResolver.Resolve(
                    GameFlowScreen.RunVictory,
                    EnemyCombatProfileCatalog.FinalBossKey),
                Is.EqualTo("readyStage"));
            Assert.That(
                GameSceneMoodResolver.Resolve(
                    GameFlowScreen.RunDefeat,
                    EnemyCombatProfileCatalog.GunslingerKey),
                Is.EqualTo("readyStage"));
            Assert.That(
                GameFlowController.ShouldShowHudRoot(
                    GameFlowScreen.RunVictory),
                Is.False);
            Assert.That(
                GameFlowController.ShouldShowHudRoot(
                    GameFlowScreen.RunDefeat),
                Is.False);
            Assert.That(
                GameFlowController.ShouldShowResultPanel(
                    isResult: true,
                    saveBlocksProgression: false),
                Is.False);
            Assert.That(
                GameFlowController.ShouldShowResultPanel(
                    isResult: true,
                    saveBlocksProgression: true),
                Is.True);
            Assert.That(
                GameFlowController.ShouldShowResultPanel(
                    isResult: false,
                    saveBlocksProgression: true),
                Is.False);
        }

        [Test]
        public void GFR01_U11_QuickPreviewUsesProductionDialogueComposition()
        {
            RunResultDialogueSO dialogue = LoadDialogue();
            StageDefinition stage = CreateStages()[1];
            PlayerRunState player = CreatePlayer();
            RunResultDialogueViewModel production =
                RunResultDialoguePresenter.Create(
                    GameFlowScreen.RunVictory,
                    player,
                    stage,
                    dialogue);
            RunResultDialogueViewModel preview =
                RunResultDialoguePresenter.CreateForPreview(
                    GameFlowScreen.RunVictory,
                    hasMadeDemonContract: false,
                    EnemyCombatProfileCatalog.FinalBossKey,
                    "Final Boss",
                    dialogue);

            Assert.That(preview.Lines, Is.EqualTo(production.Lines));
            Assert.That(
                RunResultDialoguePresenter.CreateForPreview(
                    GameFlowScreen.RunVictory,
                    hasMadeDemonContract: true,
                    EnemyCombatProfileCatalog.FinalBossKey,
                    "Final Boss",
                    dialogue).Lines,
                Has.Count.EqualTo(5));

            foreach (EnemyProfilePreview opponent in
                EnemyCombatProfileCatalog.Default.Previews)
            {
                RunResultDialogueViewModel defeat =
                    RunResultDialoguePresenter.CreateForPreview(
                        GameFlowScreen.RunDefeat,
                        hasMadeDemonContract: false,
                        opponent.ProfileKey,
                        opponent.DisplayName,
                        dialogue);
                Assert.That(
                    defeat.Lines,
                    Has.Count.EqualTo(4),
                    opponent.ProfileKey);
            }
        }

        private static RunResultDialogueSO LoadDialogue()
        {
            RunResultDialogueSO dialogue =
                AssetDatabase.LoadAssetAtPath<RunResultDialogueSO>(
                    DialogueAssetPath);
            Assert.That(dialogue, Is.Not.Null);
            return dialogue;
        }

        private static PlayerRunState CreatePlayer()
        {
            return new PlayerRunState(
                12,
                12,
                new[]
                {
                    new RunCardDefinition(0, 10),
                    new RunCardDefinition(1, 10),
                    new RunCardDefinition(2, 10),
                    new RunCardDefinition(3, 10)
                });
        }

        private static IReadOnlyList<StageDefinition> CreateStages()
        {
            return new[]
            {
                new StageDefinition(
                    "normal",
                    "일반 상대",
                    StageKind.NormalCombat,
                    1,
                    1,
                    2),
                new StageDefinition(
                    "boss",
                    "보스",
                    StageKind.FinalBossCombat,
                    1,
                    3,
                    4)
            };
        }

        private static CoreLoopBattle CreateContractVictoryBattle(
            StageDefinition stage,
            PlayerRunState player)
        {
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(
                new[]
                {
                    new BlackjackCard(0, 10),
                    new BlackjackCard(1, 10),
                    new BlackjackCard(2, 10),
                    new BlackjackCard(3, 10)
                });
            BlackjackDeck enemyDeck = BlackjackDeck.CreateInDrawOrder(
                new[]
                {
                    new BlackjackCard(100, 10),
                    new BlackjackCard(101, 2),
                    new BlackjackCard(102, 1),
                    new BlackjackCard(103, 1)
                });
            DemonContractDefinition asmodeus =
                DemonContractCatalog.Default.GetByKey(
                    DemonContractCatalog.AsmodeusKey);
            DemonContractDeck demonDeck = new DemonContractDeck(
                new[] { new DemonContractCard(0, asmodeus) },
                seed: 7);

            return new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul,
                new StandPolicy(),
                CardEffectResolver.CreateDefault(),
                demonDeck,
                DemonContractResolver.CreateDefault());
        }

        private static RunSaveSnapshot CreateSaveSnapshot(
            bool hasMadeDemonContract)
        {
            PlayerRunSaveSnapshot player = new PlayerRunSaveSnapshot(
                12,
                8,
                0,
                3,
                0,
                true,
                new[]
                {
                    new RunSaveCardSnapshot(
                        0,
                        "military-knife-10",
                        CardSuit.Spade),
                    new RunSaveCardSnapshot(
                        3,
                        "military-knife-10",
                        CardSuit.Clover)
                },
                new[]
                {
                    new RunSaveDemonSnapshot(
                        0,
                        DemonContractCatalog.AsmodeusKey)
                },
                hasMadeDemonContract);
            return new RunSaveSnapshot(
                RunSaveSnapshot.CurrentSchemaVersion,
                RunSaveSnapshot.CurrentContentRevision,
                1,
                "gfr01-save",
                SavedAtUtc,
                RunCheckpointKind.CombatSettlementCompleted,
                RunSaveStatus.InProgress,
                20260809,
                0,
                "normal",
                RunNextContentKind.Shop,
                player,
                new RunRandomSaveSnapshot(0, 0, 0, 0, string.Empty),
                new string[0],
                new string[0]);
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "gfr01-stand");
            }
        }

        private static bool CompleteCurrentStage(RunProgress progress)
        {
            BattleRewardOffer offer = new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                20260809).Generate(BattleRewardTier.Normal);
            return progress.TryBeginBattleReward(
                    offer,
                    BattleRewardCompletionTarget.StageCleared) &&
                progress.TrySkipBattleReward();
        }
    }
}
