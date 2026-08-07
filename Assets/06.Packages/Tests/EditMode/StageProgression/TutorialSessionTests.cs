using System;
using System.Collections.Generic;
using System.Linq;
using Border.SaveLoad;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class TutorialSessionTests
    {
        private const int RootSeed = 20260807;

        [Test]
        public void TU_U01_ForcedGrantReturnsBeelzebubAndAsmodeusFirstThenFallsBackToRandom()
        {
            var generator = new StartingDemonGrantGenerator(
                DemonContractCatalog.Default,
                RootSeed,
                DemonContractCatalog.PlayerDefaultDemonDeckKeys,
                forcedFirstGrantDefinitionKeys: new[]
                {
                    DemonContractCatalog.BeelzebubKey,
                    DemonContractCatalog.AsmodeusKey
                });

            StartingDemonGrant firstGrant = generator.Generate();
            StartingDemonGrant secondGrant = generator.Generate();

            Assert.That(
                firstGrant.Cards.Select(card => card.DefinitionKey),
                Is.EquivalentTo(new[]
                {
                    DemonContractCatalog.BeelzebubKey,
                    DemonContractCatalog.AsmodeusKey
                }));
            Assert.That(
                secondGrant.Cards.Select(card => card.DefinitionKey)
                    .Distinct()
                    .Count(),
                Is.EqualTo(2));
        }

        [Test]
        public void TU_U02_ForcedFirstStageSkipsOpponentSelectionAndUsesCowardlyGambler()
        {
            StageProgressionSession session = CreateForcedFirstStageSession();

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryCompleteStartingDemonReveal(), Is.True);
            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.PendingOpponentSelection, Is.Null);
            Assert.That(
                session.ActiveStage.BattleProfileKey,
                Is.EqualTo(EnemyCombatProfileCatalog.CowardlyGamblerKey));
        }

        [Test]
        public void TU_U03_SecondStageOfTutorialRunStillOffersNormalOpponentSelection()
        {
            StageProgressionSession session = CreateForcedFirstStageSession();
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryCompleteStartingDemonReveal(), Is.True);
            Assert.That(session.TryStartRun(), Is.True);

            while (session.Progress.State == StageProgressionState.InBattle)
            {
                Assert.That(session.TryPlayerStand(), Is.True);
            }

            // usesBattleRewards: false (matching CreatePrototypeSession/CreateTutorialSession)
            // skips RewardSelection entirely and lands directly on StageCleared once
            // the battle ends.
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(session.TryAdvanceToNextStage(), Is.True);
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.PendingOpponentSelection, Is.Not.Null);
            Assert.That(
                session.PendingOpponentSelection.Candidates.Count,
                Is.GreaterThanOrEqualTo(2));
        }

        [Test]
        public void TU_U04_IsTutorialRunReflectsConstructorParameter()
        {
            StageProgressionSession tutorialSession = CreateForcedFirstStageSession();
            StageProgressionSession normalSession = new StageProgressionSession(
                new RunProgress(CreateStages(RootSeed), CreateEmptyPlayer()));

            Assert.That(tutorialSession.IsTutorialRun, Is.True);
            Assert.That(normalSession.IsTutorialRun, Is.False);
        }

        [Test]
        public void TU_U05_TutorialBattleFactoryUsesScriptedDeckOnlyForTutorialStage()
        {
            PlayerRunState player = CreateEmptyPlayer();
            StageDefinition tutorialStage = StageDefinition.CreateForEnemyProfile(
                TutorialBattleFactory.TutorialStageId,
                "Tutorial",
                StageKind.NormalCombat,
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                RootSeed,
                unchecked(RootSeed + 1));
            StageDefinition laterStage = StageDefinition.CreateForEnemyProfile(
                "normal-2",
                "Later",
                StageKind.NormalCombat,
                EnemyCombatProfileCatalog.GunslingerKey,
                unchecked(RootSeed + 2),
                unchecked(RootSeed + 3));

            CoreLoopBattle tutorialBattle = TutorialBattleFactory.Create(tutorialStage, player);
            CoreLoopBattle laterBattle = TutorialBattleFactory.Create(laterStage, player);
            Assert.That(tutorialBattle.Start(), Is.True);
            Assert.That(laterBattle.Start(), Is.True);

            Assert.That(
                tutorialBattle.Player.Hand.Cards.Select(card => card.Rank),
                Is.EqualTo(TutorialBattleFactory.PlayerDeckRanksForTest.Take(2)));
            Assert.That(laterBattle, Is.Not.Null);
        }

        [Test]
        public void TU_U06_CreateTutorialSessionForcesFixedDemonsAndOpponent()
        {
            StageProgressionSession session =
                StageProgressionRuntime.CreateTutorialSession(RootSeed);

            Assert.That(session.IsTutorialRun, Is.True);
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryCompleteStartingDemonReveal(), Is.True);
            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(
                session.Progress.Player.DemonDeck
                    .Select(card => card.DefinitionKey),
                Is.EquivalentTo(new[]
                {
                    DemonContractCatalog.BeelzebubKey,
                    DemonContractCatalog.AsmodeusKey
                }));
            Assert.That(
                session.ActiveStage.BattleProfileKey,
                Is.EqualTo(EnemyCombatProfileCatalog.CowardlyGamblerKey));
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
        }

        [Test]
        public void TU_U07_CreatePrototypeSessionStaysOnNormalRandomPath()
        {
            StageProgressionSession session =
                StageProgressionRuntime.CreatePrototypeSession(RootSeed);

            Assert.That(session.IsTutorialRun, Is.False);
        }

        [Test]
        public void TU_U08_TutorialProgressStoreTracksSeenFlagAcrossInstances()
        {
            var fileStore = new MemoryRunFileStore();
            var firstHandle = new TutorialProgressStore(fileStore);

            Assert.That(firstHandle.HasSeenTutorial, Is.False);
            Assert.That(firstHandle.TryMarkSeen(), Is.True);
            Assert.That(firstHandle.HasSeenTutorial, Is.True);

            var secondHandle = new TutorialProgressStore(fileStore);
            Assert.That(secondHandle.HasSeenTutorial, Is.True);
        }

        private static StageProgressionSession CreateForcedFirstStageSession()
        {
            return new StageProgressionSession(
                new RunProgress(CreateStages(RootSeed), CreateEmptyPlayer()),
                battleFactory: TutorialBattleFactory.Create,
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    EnemyCombatProfileCatalog.Default,
                    RootSeed),
                startingDemonGrantGenerator: new StartingDemonGrantGenerator(
                    DemonContractCatalog.Default,
                    unchecked(RootSeed + 3),
                    DemonContractCatalog.PlayerDefaultDemonDeckKeys,
                    forcedFirstGrantDefinitionKeys: new[]
                    {
                        DemonContractCatalog.BeelzebubKey,
                        DemonContractCatalog.AsmodeusKey
                    }),
                usesBattleRewards: false,
                forcedFirstStageOpponentProfileKey:
                    EnemyCombatProfileCatalog.CowardlyGamblerKey);
        }

        private static PlayerRunState CreateEmptyPlayer()
        {
            List<RunCardDefinition> cards = new List<RunCardDefinition>(20);
            int cardId = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                cards.Add(new RunCardDefinition(cardId++, rank, CardSuit.Spade));
                cards.Add(new RunCardDefinition(cardId++, rank, CardSuit.Clover));
            }

            return new PlayerRunState(
                12,
                12,
                cards,
                new RunDemonDefinition[0]);
        }

        private static IReadOnlyList<StageDefinition> CreateStages(int seed)
        {
            return new[]
            {
                StageDefinition.CreateForEnemyProfile(
                    TutorialBattleFactory.TutorialStageId,
                    "Ash Gate",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.GunslingerKey,
                    seed,
                    unchecked(seed + 1)),
                StageDefinition.CreateForEnemyProfile(
                    "normal-2",
                    "Blood Hall",
                    StageKind.NormalCombat,
                    EnemyCombatProfileCatalog.EnforcerKey,
                    unchecked(seed + 2),
                    unchecked(seed + 3)),
                StageDefinition.CreateForEnemyProfile(
                    "final-boss",
                    "Black Throne",
                    StageKind.FinalBossCombat,
                    EnemyCombatProfileCatalog.FinalBossKey,
                    unchecked(seed + 4),
                    unchecked(seed + 5))
            };
        }

        private sealed class MemoryRunFileStore : IRunSaveFileStore
        {
            private readonly Dictionary<string, string> _files =
                new Dictionary<string, string>(StringComparer.Ordinal);

            public bool Exists(string fileName)
            {
                return _files.ContainsKey(fileName);
            }

            public bool TryRead(string fileName, out string contents)
            {
                return _files.TryGetValue(fileName, out contents);
            }

            public bool TryWrite(string fileName, string contents)
            {
                _files[fileName] = contents;
                return true;
            }

            public bool TryDelete(string fileName)
            {
                _files.Remove(fileName);
                return true;
            }

            public bool TryMove(
                string sourceFileName,
                string destinationFileName,
                bool overwrite)
            {
                if (!_files.TryGetValue(sourceFileName, out string contents))
                {
                    return false;
                }

                _files[destinationFileName] = contents;
                _files.Remove(sourceFileName);
                return true;
            }
        }
    }
}
