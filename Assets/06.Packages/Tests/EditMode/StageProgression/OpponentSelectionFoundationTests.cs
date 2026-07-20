using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class OpponentSelectionFoundationTests
    {
        [Test]
        public void EUI01_U01_SameSeedProducesSameOrderedOfferSequence()
        {
            var firstGenerator = new OpponentSelectionGenerator(
                EnemyCombatProfileCatalog.Default,
                7120);
            var secondGenerator = new OpponentSelectionGenerator(
                EnemyCombatProfileCatalog.Default,
                7120);

            for (int stageIndex = 0; stageIndex < 8; stageIndex++)
            {
                OpponentSelectionOffer first = firstGenerator.Generate(stageIndex);
                OpponentSelectionOffer second = secondGenerator.Generate(stageIndex);

                Assert.That(first.OfferId, Is.EqualTo(stageIndex));
                Assert.That(second.OfferId, Is.EqualTo(first.OfferId));
                Assert.That(second.StageIndex, Is.EqualTo(first.StageIndex));
                Assert.That(
                    second.Candidates[0].ProfileKey,
                    Is.EqualTo(first.Candidates[0].ProfileKey));
                Assert.That(
                    second.Candidates[1].ProfileKey,
                    Is.EqualTo(first.Candidates[1].ProfileKey));
            }
        }

        [Test]
        public void EUI01_U02_EveryOfferHasTwoDistinctNonBossCandidatesAndAtMostOneElite()
        {
            var generator = new OpponentSelectionGenerator(
                EnemyCombatProfileCatalog.Default,
                20260720);

            for (int index = 0; index < 100; index++)
            {
                OpponentSelectionOffer offer = generator.Generate(index);

                Assert.That(offer.Candidates.Count, Is.EqualTo(2));
                Assert.That(
                    offer.Candidates[0].ProfileKey,
                    Is.Not.EqualTo(offer.Candidates[1].ProfileKey));
                Assert.That(CountGrade(offer, EnemyGrade.Boss), Is.Zero);
                Assert.That(CountGrade(offer, EnemyGrade.Elite), Is.LessThanOrEqualTo(1));
            }
        }

        [Test]
        public void EUI01_U03_ZeroPercentProducesTwoNormalCandidates()
        {
            var generator = new OpponentSelectionGenerator(
                EnemyCombatProfileCatalog.Default,
                301,
                0);

            for (int index = 0; index < 20; index++)
            {
                OpponentSelectionOffer offer = generator.Generate(index);

                Assert.That(CountGrade(offer, EnemyGrade.Normal), Is.EqualTo(2));
                Assert.That(CountGrade(offer, EnemyGrade.Elite), Is.Zero);
            }
        }

        [Test]
        public void EUI01_U04_OneHundredPercentProducesOneNormalAndOneElite()
        {
            var generator = new OpponentSelectionGenerator(
                EnemyCombatProfileCatalog.Default,
                401,
                100);

            for (int index = 0; index < 20; index++)
            {
                OpponentSelectionOffer offer = generator.Generate(index);

                Assert.That(CountGrade(offer, EnemyGrade.Normal), Is.EqualTo(1));
                Assert.That(CountGrade(offer, EnemyGrade.Elite), Is.EqualTo(1));
            }
        }

        [Test]
        public void EUI01_U05_GeneratorRejectsInvalidChanceAndInsufficientPools()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new OpponentSelectionGenerator(EnemyCombatProfileCatalog.Default, 1, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new OpponentSelectionGenerator(EnemyCombatProfileCatalog.Default, 1, 101));

            EnemyCombatProfileCatalog oneNormalCatalog = CreateCatalog(
                CreateProfile("normal-a", EnemyGrade.Normal));
            Assert.Throws<ArgumentException>(() =>
                new OpponentSelectionGenerator(oneNormalCatalog, 1, 0));

            EnemyCombatProfileCatalog noEliteCatalog = CreateCatalog(
                CreateProfile("normal-a", EnemyGrade.Normal),
                CreateProfile("normal-b", EnemyGrade.Normal));
            Assert.Throws<ArgumentException>(() =>
                new OpponentSelectionGenerator(noEliteCatalog, 1, 1));
            Assert.DoesNotThrow(() =>
                new OpponentSelectionGenerator(noEliteCatalog, 1, 0));
        }

        [Test]
        public void EUI01_U06_CandidateAndOfferRejectInvalidOrContaminatedData()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;
            EnemyProfilePreview normalA = catalog.GetPreviewByKey(
                EnemyCombatProfileCatalog.GunslingerKey);
            EnemyProfilePreview normalB = catalog.GetPreviewByKey(
                EnemyCombatProfileCatalog.CultistKey);
            EnemyProfilePreview boss = catalog.GetPreviewByKey(
                EnemyCombatProfileCatalog.FinalBossKey);
            var candidateA = new OpponentSelectionCandidate(normalA.ProfileKey, normalA);
            var candidateB = new OpponentSelectionCandidate(normalB.ProfileKey, normalB);
            var bossCandidate = new OpponentSelectionCandidate(boss.ProfileKey, boss);

            Assert.Throws<ArgumentException>(() =>
                new OpponentSelectionCandidate("wrong-key", normalA));
            Assert.Throws<ArgumentNullException>(() =>
                new OpponentSelectionCandidate(normalA.ProfileKey, null));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new OpponentSelectionOffer(-1, 0, new[] { candidateA, candidateB }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new OpponentSelectionOffer(0, -1, new[] { candidateA, candidateB }));
            Assert.Throws<ArgumentNullException>(() =>
                new OpponentSelectionOffer(0, 0, null));
            Assert.Throws<ArgumentException>(() =>
                new OpponentSelectionOffer(0, 0, new[] { candidateA }));
            Assert.Throws<ArgumentException>(() =>
                new OpponentSelectionOffer(0, 0, new[] { candidateA, candidateA }));
            Assert.Throws<ArgumentException>(() =>
                new OpponentSelectionOffer(0, 0, new[] { candidateA, bossCandidate }));

            EnemyCombatProfileCatalog eliteCatalog = CreateCatalog(
                CreateProfile("elite-a", EnemyGrade.Elite),
                CreateProfile("elite-b", EnemyGrade.Elite));
            EnemyProfilePreview eliteA = eliteCatalog.GetPreviewByKey("elite-a");
            EnemyProfilePreview eliteB = eliteCatalog.GetPreviewByKey("elite-b");
            Assert.Throws<ArgumentException>(() => new OpponentSelectionOffer(
                0,
                0,
                new[]
                {
                    new OpponentSelectionCandidate(eliteA.ProfileKey, eliteA),
                    new OpponentSelectionCandidate(eliteB.ProfileKey, eliteB)
                }));
        }

        [Test]
        public void EUI01_U07_OfferCopiesCandidateCollection()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;
            EnemyProfilePreview first = catalog.GetPreviewByKey(
                EnemyCombatProfileCatalog.GunslingerKey);
            EnemyProfilePreview second = catalog.GetPreviewByKey(
                EnemyCombatProfileCatalog.CultistKey);
            var source = new List<OpponentSelectionCandidate>
            {
                new OpponentSelectionCandidate(first.ProfileKey, first),
                new OpponentSelectionCandidate(second.ProfileKey, second)
            };
            var offer = new OpponentSelectionOffer(3, 2, source);

            source.Clear();

            Assert.That(offer.Candidates.Count, Is.EqualTo(2));
            Assert.That(offer.OfferId, Is.EqualTo(3));
            Assert.That(offer.StageIndex, Is.EqualTo(2));
        }

        [Test]
        public void EUI01_U08_UninjectedSessionKeepsFixedBattleFlow()
        {
            RunProgress progress = CreateProgress();
            var session = new StageProgressionSession(progress);

            Assert.That(session.IsOpponentSelectionEnabled, Is.False);
            Assert.That(session.ActiveStage, Is.SameAs(progress.CurrentStage));
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.Battle, Is.Not.Null);
            Assert.That(session.ActiveStage, Is.SameAs(progress.CurrentStage));
            Assert.That(session.PendingOpponentSelection, Is.Null);
        }

        [Test]
        public void EUI01_U09_InjectedSessionStartsNormalStageInOpponentSelection()
        {
            RunProgress progress = CreateProgress();
            StageProgressionSession session = CreateSelectionSession(progress, 901);

            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(session.IsOpponentSelectionEnabled, Is.True);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.Battle, Is.Null);
            Assert.That(session.ActiveStage, Is.Null);
            Assert.That(session.PendingOpponentSelection, Is.Not.Null);
            Assert.That(session.PendingOpponentSelection.OfferId, Is.Zero);
            Assert.That(session.PendingOpponentSelection.StageIndex, Is.Zero);
        }

        [Test]
        public void EUI01_I01_FinalBossBypassesOpponentSelection()
        {
            StageDefinition bossStage = StageDefinition.CreateForEnemyProfile(
                "final-boss",
                "Final Boss",
                StageKind.FinalBossCombat,
                EnemyCombatProfileCatalog.FinalBossKey,
                30,
                31);
            var progress = new RunProgress(new[] { bossStage }, CreatePlayer());
            StageProgressionSession session = CreateSelectionSession(progress, 1001);

            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.PendingOpponentSelection, Is.Null);
            Assert.That(session.ActiveStage, Is.SameAs(bossStage));
            Assert.That(session.Battle, Is.Not.Null);
        }

        [Test]
        public void EUI01_I02_AdvanceToNextNormalStageCreatesNewSelectionOffer()
        {
            RunProgress progress = CreateProgress();
            Assert.That(progress.StartRun(), Is.True);
            Assert.That(
                progress.TryBeginBattleReward(
                    CreateRewardOffer(BattleRewardTier.Normal),
                    BattleRewardCompletionTarget.StageCleared),
                Is.True);
            Assert.That(progress.TrySkipBattleReward(), Is.True);
            StageProgressionSession session = CreateSelectionSession(progress, 1101);

            Assert.That(session.TryAdvanceToNextStage(), Is.True);

            Assert.That(progress.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.PendingOpponentSelection.StageIndex, Is.EqualTo(1));
            Assert.That(session.PendingOpponentSelection.OfferId, Is.Zero);
            Assert.That(session.Battle, Is.Null);
            Assert.That(session.ActiveStage, Is.Null);
        }

        [Test]
        public void EUI01_I03_RestartCreatesFreshFirstSelectionAndResetsOfferId()
        {
            RunProgress progress = CreateProgress();
            Assert.That(progress.StartRun(), Is.True);
            progress.Player.SetCurrentSoul(0);
            Assert.That(progress.TryDefeatRun(), Is.True);
            var usedGenerator = new OpponentSelectionGenerator(
                EnemyCombatProfileCatalog.Default,
                1201);
            Assert.That(usedGenerator.Generate(9).OfferId, Is.Zero);
            var session = new StageProgressionSession(
                progress,
                opponentSelectionGenerator: usedGenerator);

            Assert.That(session.TryRestartRun(), Is.True);

            Assert.That(progress.State, Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(progress.CurrentStageIndex, Is.Zero);
            Assert.That(progress.Player.CurrentSoul, Is.EqualTo(12));
            Assert.That(session.PendingOpponentSelection.OfferId, Is.Zero);
            Assert.That(session.PendingOpponentSelection.StageIndex, Is.Zero);
            Assert.That(session.Battle, Is.Null);
        }

        [Test]
        public void EUI01_I04_SelectionStateRejectsEveryExistingBattleAndRewardInput()
        {
            RunProgress progress = CreateProgress();
            StageProgressionSession session = CreateSelectionSession(progress, 1301);
            Assert.That(session.TryStartRun(), Is.True);
            OpponentSelectionOffer pending = session.PendingOpponentSelection;
            int originalSoul = progress.Player.CurrentSoul;
            int originalDeckCount = progress.Player.Deck.Count;

            Assert.That(session.TryStartRun(), Is.False);
            Assert.That(session.TryPlayerHit(), Is.False);
            Assert.That(session.TryPlayerStand(), Is.False);
            Assert.That(session.TryBeginPlayerChange(), Is.False);
            Assert.That(session.TrySelectChangedCard(0), Is.False);
            Assert.That(session.TryBeginPlayerCardUse(0), Is.False);
            Assert.That(session.TryResolvePlayerCardChoice(0), Is.False);
            Assert.That(session.TrySelectBattleReward(0), Is.False);
            Assert.That(session.TrySkipBattleReward(), Is.False);
            Assert.That(session.TryAdvanceToNextStage(), Is.False);
            Assert.That(session.TryRestartRun(), Is.False);

            Assert.That(progress.State, Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.PendingOpponentSelection, Is.SameAs(pending));
            Assert.That(progress.Player.CurrentSoul, Is.EqualTo(originalSoul));
            Assert.That(progress.Player.Deck.Count, Is.EqualTo(originalDeckCount));
            Assert.That(session.Battle, Is.Null);
        }

        private static int CountGrade(
            OpponentSelectionOffer offer,
            EnemyGrade grade)
        {
            int count = 0;
            for (int index = 0; index < offer.Candidates.Count; index++)
            {
                if (offer.Candidates[index].Preview.Grade == grade)
                {
                    count++;
                }
            }

            return count;
        }

        private static EnemyCombatProfileCatalog CreateCatalog(
            params EnemyCombatProfile[] profiles)
        {
            return new EnemyCombatProfileCatalog(profiles);
        }

        private static EnemyCombatProfile CreateProfile(
            string key,
            EnemyGrade grade)
        {
            return new EnemyCombatProfile(
                key,
                key,
                grade,
                3,
                EnemyBehaviorPolicyCatalog.Simple,
                new[] { "standard-ace-1" },
                $"{key} summary",
                EnemyInformationMode.Standard);
        }

        private static StageProgressionSession CreateSelectionSession(
            RunProgress progress,
            int seed)
        {
            return new StageProgressionSession(
                progress,
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    EnemyCombatProfileCatalog.Default,
                    seed));
        }

        private static RunProgress CreateProgress()
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
                        "normal-2",
                        "Normal 2",
                        StageKind.NormalCombat,
                        4,
                        20,
                        21),
                    StageDefinition.CreateForEnemyProfile(
                        "final-boss",
                        "Final Boss",
                        StageKind.FinalBossCombat,
                        EnemyCombatProfileCatalog.FinalBossKey,
                        30,
                        31)
                },
                CreatePlayer());
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

        private static BattleRewardOffer CreateRewardOffer(BattleRewardTier tier)
        {
            return new BattleRewardGenerator(BattleRewardCatalog.CreateDefault(), 2001)
                .Generate(tier);
        }
    }
}
