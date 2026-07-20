using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class BattleRewardSystemValidationTests
    {
        private const int InitialDeckCount = 4;
        private const int RepeatCount = 10;

        [Test]
        public void RW05_V01_TenNormalSelectionsReachNextBattleWithOwnedRewardCard()
        {
            StageProgressionSession session = CreateSession(
                includeNormalStage: true,
                playerMaximumSoul: 12,
                rewardSeed: 5001);
            var observedOfferIds = new HashSet<int>();
            Assert.That(session.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                AssertFreshRun(session, expectedSoul: 12, iteration);
                CoreLoopBattle normalBattle = session.Battle;
                WinCurrentBattle(session, iteration);
                BattleRewardOffer normalOffer = AssertRewardOffer(
                    session,
                    BattleRewardTier.Normal,
                    BattleRewardCompletionTarget.StageCleared,
                    observedOfferIds,
                    iteration);
                BattleRewardOption selected = normalOffer.Options[iteration % 3];

                Assert.That(
                    session.TrySelectBattleReward(selected.OptionId),
                    Is.True,
                    $"Normal selection {iteration}");
                BattleRewardResolution resolution = session.Progress.LastRewardResolution;
                int addedCardId = resolution.AddedCardId.Value;

                Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.StageCleared));
                Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(InitialDeckCount + 1));
                Assert.That(resolution.SelectedDefinitionKey, Is.EqualTo(selected.DefinitionKey));
                Assert.That(
                    session.Progress.Player.Deck.Single(card => card.Id == addedCardId).DefinitionKey,
                    Is.EqualTo(selected.DefinitionKey));
                AssertRunDeckIdsAreUnique(session.Progress.Player, iteration);

                Assert.That(session.TryAdvanceToNextStage(), Is.True, $"Advance {iteration}");
                Assert.That(session.Battle, Is.Not.SameAs(normalBattle));
                AssertBattleOwnsRunDeck(session, InitialDeckCount + 1, iteration);
                Assert.That(
                    session.Progress.Player.Deck.Any(
                        card => card.Id == addedCardId &&
                            card.DefinitionKey == selected.DefinitionKey),
                    Is.True);

                CompleteBossAndRestartBySkipping(session, observedOfferIds, iteration);
            }

            Assert.That(observedOfferIds.Count, Is.EqualTo(RepeatCount * 2));
        }

        [Test]
        public void RW05_V02_TenNormalSkipsKeepTheNextBattleDeckUnchanged()
        {
            StageProgressionSession session = CreateSession(
                includeNormalStage: true,
                playerMaximumSoul: 12,
                rewardSeed: 5002);
            var observedOfferIds = new HashSet<int>();
            Assert.That(session.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                AssertFreshRun(session, expectedSoul: 12, iteration);
                WinCurrentBattle(session, iteration);
                AssertRewardOffer(
                    session,
                    BattleRewardTier.Normal,
                    BattleRewardCompletionTarget.StageCleared,
                    observedOfferIds,
                    iteration);

                Assert.That(
                    session.TrySkipBattleReward(),
                    Is.True,
                    $"Normal skip {iteration}");
                Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.StageCleared));
                Assert.That(session.Progress.LastRewardResolution.WasSkipped, Is.True);
                Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(InitialDeckCount));
                Assert.That(session.TryAdvanceToNextStage(), Is.True, $"Advance {iteration}");
                AssertBattleOwnsRunDeck(session, InitialDeckCount, iteration);

                CompleteBossAndRestartBySkipping(session, observedOfferIds, iteration);
            }

            Assert.That(observedOfferIds.Count, Is.EqualTo(RepeatCount * 2));
        }

        [Test]
        public void RW05_V03_TenBossSelectionsUseHighGradeRewardsAndResetTheDeck()
        {
            StageProgressionSession session = CreateSession(
                includeNormalStage: false,
                playerMaximumSoul: 12,
                rewardSeed: 5003);
            var observedOfferIds = new HashSet<int>();
            Assert.That(session.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                AssertFreshRun(session, expectedSoul: 12, iteration);
                CoreLoopBattle bossBattle = session.Battle;
                WinCurrentBattle(session, iteration);
                BattleRewardOffer offer = AssertRewardOffer(
                    session,
                    BattleRewardTier.HighGrade,
                    BattleRewardCompletionTarget.RunVictory,
                    observedOfferIds,
                    iteration);
                BattleRewardOption selected = offer.Options[iteration % 3];

                Assert.That(
                    session.TrySelectBattleReward(selected.OptionId),
                    Is.True,
                    $"Boss selection {iteration}");
                BattleRewardResolution resolution = session.Progress.LastRewardResolution;
                int addedCardId = resolution.AddedCardId.Value;

                Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
                Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(InitialDeckCount + 1));
                Assert.That(resolution.SelectedDefinitionKey, Is.EqualTo(selected.DefinitionKey));
                AssertRunDeckIdsAreUnique(session.Progress.Player, iteration);

                Assert.That(session.TryRestartRun(), Is.True, $"Restart {iteration}");
                Assert.That(session.Battle, Is.Not.SameAs(bossBattle));
                Assert.That(session.Progress.Player.Deck.Any(card => card.Id == addedCardId), Is.False);
                AssertFreshRun(session, expectedSoul: 12, iteration);
            }

            Assert.That(observedOfferIds.Count, Is.EqualTo(RepeatCount));
        }

        [Test]
        public void RW05_V04_TenBossSkipsKeepTheInitialDeckAcrossRestarts()
        {
            StageProgressionSession session = CreateSession(
                includeNormalStage: false,
                playerMaximumSoul: 12,
                rewardSeed: 5004);
            var observedOfferIds = new HashSet<int>();
            Assert.That(session.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                AssertFreshRun(session, expectedSoul: 12, iteration);
                CoreLoopBattle bossBattle = session.Battle;
                WinCurrentBattle(session, iteration);
                AssertRewardOffer(
                    session,
                    BattleRewardTier.HighGrade,
                    BattleRewardCompletionTarget.RunVictory,
                    observedOfferIds,
                    iteration);

                Assert.That(
                    session.TrySkipBattleReward(),
                    Is.True,
                    $"Boss skip {iteration}");
                Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
                Assert.That(session.Progress.LastRewardResolution.WasSkipped, Is.True);
                Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(InitialDeckCount));

                Assert.That(session.TryRestartRun(), Is.True, $"Restart {iteration}");
                Assert.That(session.Battle, Is.Not.SameAs(bossBattle));
                AssertFreshRun(session, expectedSoul: 12, iteration);
            }

            Assert.That(observedOfferIds.Count, Is.EqualTo(RepeatCount));
        }

        [Test]
        public void RW05_V05_TenDefeatsCreateNoRewardAndRestartCleanly()
        {
            StageProgressionSession session = CreateSession(
                includeNormalStage: false,
                playerMaximumSoul: 1,
                rewardSeed: 5005);
            Assert.That(session.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < RepeatCount; iteration++)
            {
                AssertFreshRun(session, expectedSoul: 1, iteration);
                CoreLoopBattle defeatedBattle = session.Battle;

                Assert.That(session.TryPlayerStand(), Is.True, $"Stand {iteration}");
                Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
                Assert.That(session.Progress.Player.CurrentSoul, Is.Zero);
                Assert.That(session.Progress.PendingReward, Is.Null);
                Assert.That(session.Progress.LastRewardResolution, Is.Null);
                Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(InitialDeckCount));

                Assert.That(session.TryRestartRun(), Is.True, $"Restart {iteration}");
                Assert.That(session.Battle, Is.Not.SameAs(defeatedBattle));
                AssertFreshRun(session, expectedSoul: 1, iteration);
            }
        }

        private static StageProgressionSession CreateSession(
            bool includeNormalStage,
            int playerMaximumSoul,
            int rewardSeed)
        {
            var stages = new List<StageDefinition>();
            if (includeNormalStage)
            {
                stages.Add(new StageDefinition(
                    "normal-1",
                    "Normal 1",
                    StageKind.NormalCombat,
                    1,
                    10,
                    11));
            }

            stages.Add(new StageDefinition(
                "final-boss",
                "Final Boss",
                StageKind.FinalBossCombat,
                1,
                30,
                31));

            var progress = new RunProgress(
                stages,
                new PlayerRunState(
                    playerMaximumSoul,
                    playerMaximumSoul,
                    new[]
                    {
                        new RunCardDefinition(0, 10),
                        new RunCardDefinition(1, 8),
                        new RunCardDefinition(2, 10),
                        new RunCardDefinition(3, 1)
                    }));

            return new StageProgressionSession(
                progress,
                CreateVictoryBattleFromRunDeck,
                new BattleRewardGenerator(
                    BattleRewardCatalog.CreateDefault(),
                    rewardSeed));
        }

        private static CoreLoopBattle CreateVictoryBattleFromRunDeck(
            StageDefinition stage,
            PlayerRunState player)
        {
            var playerCards = new List<BlackjackCard>(player.Deck.Count);
            foreach (RunCardDefinition card in player.Deck)
            {
                playerCards.Add(new BlackjackCard(
                    card.Id,
                    CardDefinitionCatalog.GetByKey(card.DefinitionKey)));
            }

            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(playerCards),
                CreateDeck(new[] { 10, 10, 10, 10 }),
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul);
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

        private static void WinCurrentBattle(
            StageProgressionSession session,
            int iteration)
        {
            for (int action = 0;
                action < 8 && session.Progress.State == StageProgressionState.InBattle;
                action++)
            {
                Assert.That(
                    session.TryPlayerStand(),
                    Is.True,
                    $"Stand {iteration}:{action}");
            }

            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.RewardSelection),
                $"Victory {iteration}");
        }

        private static BattleRewardOffer AssertRewardOffer(
            StageProgressionSession session,
            BattleRewardTier expectedTier,
            BattleRewardCompletionTarget expectedTarget,
            ISet<int> observedOfferIds,
            int iteration)
        {
            PendingBattleReward pending = session.Progress.PendingReward;
            Assert.That(pending, Is.Not.Null, $"Pending reward {iteration}");
            BattleRewardOffer offer = pending.Offer;

            Assert.That(offer.Tier, Is.EqualTo(expectedTier), $"Tier {iteration}");
            Assert.That(
                pending.CompletionTarget,
                Is.EqualTo(expectedTarget),
                $"Completion target {iteration}");
            Assert.That(offer.Options.Count, Is.EqualTo(3));
            Assert.That(
                offer.Options.Select(option => option.OptionId).Distinct().Count(),
                Is.EqualTo(3),
                $"Option ids {iteration}");
            Assert.That(
                offer.Options.Select(option => option.DefinitionKey).Distinct().Count(),
                Is.EqualTo(3),
                $"Definitions {iteration}");
            Assert.That(
                observedOfferIds.Add(offer.OfferId),
                Is.True,
                $"Offer id {offer.OfferId} at iteration {iteration}");
            return offer;
        }

        private static void CompleteBossAndRestartBySkipping(
            StageProgressionSession session,
            ISet<int> observedOfferIds,
            int iteration)
        {
            WinCurrentBattle(session, iteration);
            AssertRewardOffer(
                session,
                BattleRewardTier.HighGrade,
                BattleRewardCompletionTarget.RunVictory,
                observedOfferIds,
                iteration);
            Assert.That(session.TrySkipBattleReward(), Is.True, $"Boss skip {iteration}");
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(session.TryRestartRun(), Is.True, $"Restart {iteration}");
            AssertFreshRun(session, expectedSoul: 12, iteration);
        }

        private static void AssertFreshRun(
            StageProgressionSession session,
            int expectedSoul,
            int iteration)
        {
            Assert.That(session.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(session.Progress.CurrentStageIndex, Is.Zero);
            Assert.That(session.Progress.Player.CurrentSoul, Is.EqualTo(expectedSoul));
            Assert.That(session.Progress.Player.Deck.Count, Is.EqualTo(InitialDeckCount));
            Assert.That(session.Progress.PendingReward, Is.Null);
            Assert.That(session.Progress.LastRewardResolution, Is.Null);
            AssertRunDeckIdsAreUnique(session.Progress.Player, iteration);
            AssertBattleOwnsRunDeck(session, InitialDeckCount, iteration);
        }

        private static void AssertRunDeckIdsAreUnique(
            PlayerRunState player,
            int iteration)
        {
            Assert.That(
                player.Deck.Select(card => card.Id).Distinct().Count(),
                Is.EqualTo(player.Deck.Count),
                $"Run deck ids {iteration}");
        }

        private static void AssertBattleOwnsRunDeck(
            StageProgressionSession session,
            int expectedCount,
            int iteration)
        {
            BlackjackDeck deck = session.Battle.Player.Deck;
            Assert.That(deck.TotalCardCount, Is.EqualTo(expectedCount));
            Assert.That(
                deck.AvailableCardCount + deck.CardsInPlayCount,
                Is.EqualTo(expectedCount),
                $"Battle deck ownership {iteration}");
        }
    }
}
