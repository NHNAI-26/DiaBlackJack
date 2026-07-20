using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class OpponentSelectionIntegrationTests
    {
        public enum InvalidSelectionInput
        {
            StaleOfferId,
            ProfileOutsideOffer,
            WrongCase,
            EmptyKey
        }

        [TestCase(
            EnemyCombatProfileCatalog.GunslingerKey,
            3,
            "GunslingerEnemyPolicy")]
        [TestCase(
            EnemyCombatProfileCatalog.CultistKey,
            3,
            "CultistEnemyPolicy")]
        [TestCase(
            EnemyCombatProfileCatalog.TricksterKey,
            4,
            "TricksterEnemyPolicy")]
        [TestCase(
            EnemyCombatProfileCatalog.EnforcerKey,
            5,
            "EnforcerEnemyPolicy")]
        public void EUI03_I01_SelectedCandidateCreatesItsProfileBattle(
            string profileKey,
            int expectedMaximumSoul,
            string expectedPolicyName)
        {
            StageProgressionSession session = CreateSelectionSession(profileKey);
            Assert.That(session.TryStartRun(), Is.True);
            OpponentSelectionOffer offer = session.PendingOpponentSelection;
            StageDefinition template = session.Progress.CurrentStage;

            Assert.That(session.TrySelectOpponent(offer.OfferId, profileKey), Is.True);

            EnemyProfilePreview preview =
                EnemyCombatProfileCatalog.Default.GetPreviewByKey(profileKey);
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.PendingOpponentSelection, Is.Null);
            Assert.That(session.ActiveStage, Is.Not.Null);
            Assert.That(session.ActiveStage, Is.Not.SameAs(template));
            Assert.That(session.ActiveStage.Id, Is.EqualTo(template.Id));
            Assert.That(session.ActiveStage.DisplayName, Is.EqualTo(preview.DisplayName));
            Assert.That(session.ActiveStage.Kind, Is.EqualTo(template.Kind));
            Assert.That(session.ActiveStage.PlayerDeckSeed,
                Is.EqualTo(template.PlayerDeckSeed));
            Assert.That(session.ActiveStage.EnemyDeckSeed,
                Is.EqualTo(template.EnemyDeckSeed));
            Assert.That(session.ActiveStage.BattleProfileKey, Is.EqualTo(profileKey));
            Assert.That(session.Battle, Is.Not.Null);
            Assert.That(session.Battle.Enemy.Soul.Maximum,
                Is.EqualTo(expectedMaximumSoul));
            Assert.That(session.Battle.Enemy.Deck.TotalCardCount, Is.EqualTo(10));
            Assert.That(session.Battle.EnemyBehaviorPolicy.GetType().Name,
                Is.EqualTo(expectedPolicyName));
        }

        [TestCase(InvalidSelectionInput.StaleOfferId)]
        [TestCase(InvalidSelectionInput.ProfileOutsideOffer)]
        [TestCase(InvalidSelectionInput.WrongCase)]
        [TestCase(InvalidSelectionInput.EmptyKey)]
        public void EUI03_U01_InvalidSelectionIsAtomic(
            InvalidSelectionInput invalidInput)
        {
            StageProgressionSession session = CreateSelectionSession(
                EnemyCombatProfileCatalog.GunslingerKey);
            Assert.That(session.TryStartRun(), Is.True);
            OpponentSelectionOffer offer = session.PendingOpponentSelection;
            string candidateKey = offer.Candidates[0].ProfileKey;
            int offerId = offer.OfferId;
            string profileKey = candidateKey;
            switch (invalidInput)
            {
                case InvalidSelectionInput.StaleOfferId:
                    offerId++;
                    break;
                case InvalidSelectionInput.ProfileOutsideOffer:
                    profileKey = EnemyCombatProfileCatalog.FinalBossKey;
                    break;
                case InvalidSelectionInput.WrongCase:
                    profileKey = candidateKey.ToUpperInvariant();
                    break;
                case InvalidSelectionInput.EmptyKey:
                    profileKey = string.Empty;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(invalidInput),
                        invalidInput,
                        null);
            }

            int currentSoul = session.Progress.Player.CurrentSoul;
            RunCardDefinition[] deck = session.Progress.Player.Deck.ToArray();

            Assert.That(session.TrySelectOpponent(offerId, profileKey), Is.False);
            AssertSelectionUnchanged(session, offer, currentSoul, deck);
        }

        [Test]
        public void EUI03_U02_BattleFactoryFailureIsAtomic()
        {
            StageProgressionSession session = CreateSelectionSession(
                EnemyCombatProfileCatalog.GunslingerKey,
                (stage, player) => throw new InvalidOperationException(
                    "Expected battle factory failure."));
            Assert.That(session.TryStartRun(), Is.True);
            OpponentSelectionOffer offer = session.PendingOpponentSelection;
            int currentSoul = session.Progress.Player.CurrentSoul;
            RunCardDefinition[] deck = session.Progress.Player.Deck.ToArray();

            Assert.Throws<InvalidOperationException>(() =>
                session.TrySelectOpponent(
                    offer.OfferId,
                    EnemyCombatProfileCatalog.GunslingerKey));

            AssertSelectionUnchanged(session, offer, currentSoul, deck);
        }

        [TestCase(
            EnemyCombatProfileCatalog.GunslingerKey,
            BattleRewardTier.Normal)]
        [TestCase(
            EnemyCombatProfileCatalog.EnforcerKey,
            BattleRewardTier.HighGrade)]
        public void EUI03_I02_SelectedProfileDeterminesRewardTier(
            string profileKey,
            BattleRewardTier expectedTier)
        {
            StageProgressionSession session = CreateSelectionSession(
                profileKey,
                CreateDeterministicVictoryBattle);
            Assert.That(session.TryStartRun(), Is.True);
            OpponentSelectionOffer offer = session.PendingOpponentSelection;
            Assert.That(session.TrySelectOpponent(offer.OfferId, profileKey), Is.True);

            WinCurrentBattle(session);

            Assert.That(session.Progress.PendingReward, Is.Not.Null);
            Assert.That(session.Progress.PendingReward.Offer.Tier,
                Is.EqualTo(expectedTier));
            Assert.That(session.Progress.PendingReward.CompletionTarget,
                Is.EqualTo(BattleRewardCompletionTarget.StageCleared));
        }

        [Test]
        public void EUI03_I03_FirstRewardToSecondSelectionPreservesRunState()
        {
            StageProgressionSession session = CreateThreeStageSession();
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(SelectFirstCandidate(session), Is.True);
            WinCurrentBattle(session);

            int initialDeckCount = session.Progress.Player.Deck.Count;
            int rewardOptionId =
                session.Progress.PendingReward.Offer.Options[0].OptionId;
            Assert.That(session.TrySelectBattleReward(rewardOptionId), Is.True);
            int preservedSoul = session.Progress.Player.CurrentSoul;
            int preservedDeckCount = session.Progress.Player.Deck.Count;
            Assert.That(preservedDeckCount, Is.EqualTo(initialDeckCount + 1));

            Assert.That(session.TryAdvanceToNextStage(), Is.True);

            Assert.That(session.Progress.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(preservedSoul));
            Assert.That(session.Progress.Player.Deck.Count,
                Is.EqualTo(preservedDeckCount));
            Assert.That(session.PendingOpponentSelection, Is.Not.Null);
            Assert.That(session.PendingOpponentSelection.OfferId, Is.EqualTo(1));
            Assert.That(session.PendingOpponentSelection.StageIndex, Is.EqualTo(1));
            Assert.That(session.ActiveStage, Is.Null);
            Assert.That(session.Battle, Is.Null);
        }

        [Test]
        public void EUI03_I04_SecondRewardAdvancesDirectlyToFixedBoss()
        {
            StageProgressionSession session = CreateThreeStageSession();
            Assert.That(session.TryStartRun(), Is.True);
            CompleteSelectedStage(session);
            Assert.That(session.TryAdvanceToNextStage(), Is.True);
            CompleteSelectedStage(session);

            Assert.That(session.TryAdvanceToNextStage(), Is.True);

            Assert.That(session.Progress.CurrentStageIndex, Is.EqualTo(2));
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.PendingOpponentSelection, Is.Null);
            Assert.That(session.ActiveStage,
                Is.SameAs(session.Progress.CurrentStage));
            Assert.That(session.ActiveStage.BattleProfileKey,
                Is.EqualTo(EnemyCombatProfileCatalog.FinalBossKey));
            Assert.That(session.Battle, Is.Not.Null);
            Assert.That(session.Battle.Enemy.Soul.Maximum, Is.EqualTo(7));
        }

        [Test]
        public void EUI03_I05_RestartAfterBossVictoryCreatesFreshFirstOffer()
        {
            StageProgressionSession session = CreateThreeStageSession();
            int initialDeckCount = session.Progress.Player.Deck.Count;
            Assert.That(session.TryStartRun(), Is.True);
            CompleteSelectedStage(session, selectReward: true);
            Assert.That(session.TryAdvanceToNextStage(), Is.True);
            CompleteSelectedStage(session);
            Assert.That(session.TryAdvanceToNextStage(), Is.True);
            WinCurrentBattle(session);
            Assert.That(session.TrySkipBattleReward(), Is.True);
            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.RunVictory));

            Assert.That(session.TryRestartRun(), Is.True);

            Assert.That(session.Progress.CurrentStageIndex, Is.Zero);
            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.Progress.Player.CurrentSoul,
                Is.EqualTo(session.Progress.Player.MaximumSoul));
            Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(initialDeckCount));
            Assert.That(session.PendingOpponentSelection, Is.Not.Null);
            Assert.That(session.PendingOpponentSelection.OfferId, Is.Zero);
            Assert.That(session.PendingOpponentSelection.StageIndex, Is.Zero);
            Assert.That(session.ActiveStage, Is.Null);
            Assert.That(session.Battle, Is.Null);
        }

        private static StageProgressionSession CreateSelectionSession(
            string guaranteedProfileKey,
            Func<StageDefinition, PlayerRunState, CoreLoopBattle> battleFactory = null)
        {
            return new StageProgressionSession(
                CreateProgress(includeSecondNormalStage: false),
                battleFactory,
                opponentSelectionGenerator: CreateGeneratorForProfile(
                    guaranteedProfileKey));
        }

        private static StageProgressionSession CreateThreeStageSession()
        {
            EnemyCombatProfileCatalog defaultCatalog =
                EnemyCombatProfileCatalog.Default;
            var catalog = new EnemyCombatProfileCatalog(
                new[]
                {
                    defaultCatalog.GetByKey(EnemyCombatProfileCatalog.GunslingerKey),
                    defaultCatalog.GetByKey(EnemyCombatProfileCatalog.CultistKey)
                });
            return new StageProgressionSession(
                CreateProgress(includeSecondNormalStage: true),
                CreateDeterministicVictoryBattle,
                opponentSelectionGenerator: new OpponentSelectionGenerator(
                    catalog,
                    20260720,
                    0));
        }

        private static OpponentSelectionGenerator CreateGeneratorForProfile(
            string profileKey)
        {
            EnemyCombatProfileCatalog defaultCatalog =
                EnemyCombatProfileCatalog.Default;
            EnemyCombatProfile target = defaultCatalog.GetByKey(profileKey);
            if (target.Grade == EnemyGrade.Elite)
            {
                return new OpponentSelectionGenerator(
                    new EnemyCombatProfileCatalog(
                        new[]
                        {
                            defaultCatalog.GetByKey(
                                EnemyCombatProfileCatalog.GunslingerKey),
                            defaultCatalog.GetByKey(
                                EnemyCombatProfileCatalog.CultistKey),
                            target
                        }),
                    20260720,
                    100);
            }

            string otherNormalKey = profileKey == EnemyCombatProfileCatalog.GunslingerKey
                ? EnemyCombatProfileCatalog.CultistKey
                : EnemyCombatProfileCatalog.GunslingerKey;
            return new OpponentSelectionGenerator(
                new EnemyCombatProfileCatalog(
                    new[]
                    {
                        target,
                        defaultCatalog.GetByKey(otherNormalKey)
                    }),
                20260720,
                0);
        }

        private static RunProgress CreateProgress(bool includeSecondNormalStage)
        {
            var stages = new List<StageDefinition>
            {
                new StageDefinition(
                    "normal-1",
                    "Ash Gate",
                    StageKind.NormalCombat,
                    3,
                    10,
                    11)
            };
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
                int rank = index % 2 == 0 ? firstRank : secondRank;
                cards.Add(new BlackjackCard(index, rank));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static void WinCurrentBattle(StageProgressionSession session)
        {
            for (int action = 0;
                action < 12 &&
                    session.Progress.State == StageProgressionState.InBattle;
                action++)
            {
                Assert.That(session.TryPlayerStand(), Is.True, $"Stand action {action}");
            }

            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.RewardSelection));
        }

        private static bool SelectFirstCandidate(StageProgressionSession session)
        {
            OpponentSelectionOffer offer = session.PendingOpponentSelection;
            return session.TrySelectOpponent(
                offer.OfferId,
                offer.Candidates[0].ProfileKey);
        }

        private static void CompleteSelectedStage(
            StageProgressionSession session,
            bool selectReward = false)
        {
            Assert.That(SelectFirstCandidate(session), Is.True);
            WinCurrentBattle(session);
            bool resolved = selectReward
                ? session.TrySelectBattleReward(
                    session.Progress.PendingReward.Offer.Options[0].OptionId)
                : session.TrySkipBattleReward();
            Assert.That(resolved, Is.True);
            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared));
        }

        private static void AssertSelectionUnchanged(
            StageProgressionSession session,
            OpponentSelectionOffer offer,
            int currentSoul,
            IReadOnlyList<RunCardDefinition> deck)
        {
            Assert.That(session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.Progress.CurrentStageIndex, Is.Zero);
            Assert.That(session.PendingOpponentSelection, Is.SameAs(offer));
            Assert.That(session.ActiveStage, Is.Null);
            Assert.That(session.Battle, Is.Null);
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(currentSoul));
            Assert.That(session.Progress.Player.Deck, Is.EqualTo(deck));
            Assert.That(session.Progress.PendingReward, Is.Null);
        }
    }
}
