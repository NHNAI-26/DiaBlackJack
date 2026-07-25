using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class AutomaticCardStageIntegrationTests
    {
        [Test]
        public void AC06_I01_StageSessionForwardsAutomaticCardChoice()
        {
            var session = new StageProgressionSession(
                CreateProgress(playerDeckSeed: 11),
                (stage, player) => CreateAutomaticChoiceBattle(
                    player,
                    stage.EnemyMaximumSoul));
            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(session.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                session.Battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);

            Assert.That(
                session.TryResolvePlayerAutomaticCardChoice(
                    pending.InteractionId,
                    PoisonEffectHandler.PaySoulOptionId),
                Is.True);

            Assert.That(
                session.Battle.LastAutomaticCardResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.Poison));
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
        }

        [Test]
        public void AC06_I02_DefaultNormalRewardAddsOnlyFiveAutomaticCards()
        {
            BattleRewardCatalog catalog =
                BattleRewardCatalog.CreateDefault();
            string[] automaticKeys =
            {
                CardDefinitionCatalog.PoisonKey,
                CardDefinitionCatalog.ResurrectionHerbKey,
                CardDefinitionCatalog.LieDetectorKey,
                CardDefinitionCatalog.FlamethrowerKey,
                CardDefinitionCatalog.PocketWatchKey
            };

            Assert.That(
                automaticKeys.All(key =>
                    catalog.Contains(BattleRewardTier.Normal, key)),
                Is.True);
            Assert.That(
                automaticKeys.Any(key =>
                    catalog.Contains(BattleRewardTier.HighGrade, key)),
                Is.False);
            Assert.That(
                catalog.GetDefinitionKeys(BattleRewardTier.Normal)
                    .Count(key => automaticKeys.Contains(key)),
                Is.EqualTo(5));
        }

        [Test]
        public void AC06_I03_RewardedPoisonTriggersInNextFactoryBattle()
        {
            int nextBattleSeed = FindSeedWithPoisonAsThirdDraw();
            RunProgress progress = CreateProgress(nextBattleSeed);
            var rewardCatalog = new BattleRewardCatalog(
                new[]
                {
                    CardDefinitionCatalog.PoisonKey,
                    "standard-plain-2",
                    "standard-plain-3"
                },
                new[]
                {
                    "crystal-orb-5",
                    "threat-hammer-6",
                    "auto-pistol-7"
                });
            var session = new StageProgressionSession(
                progress,
                (stage, player) => stage.Id == "ac06-first"
                    ? CreateImmediateVictoryBattle(
                        player,
                        stage.EnemyMaximumSoul)
                    : StageBattleFactory.Create(stage, player),
                new BattleRewardGenerator(rewardCatalog, seed: 606));
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryPlayerStand(), Is.True);
            BattleRewardOption poison = progress.PendingReward.Offer.Options
                .Single(option =>
                    option.DefinitionKey ==
                    CardDefinitionCatalog.PoisonKey);

            Assert.That(
                session.TrySelectBattleReward(poison.OptionId),
                Is.True);
            Assert.That(session.TryAdvanceToNextStage(), Is.True);
            Assert.That(
                progress.Player.Deck.Count(card =>
                    card.DefinitionKey ==
                    CardDefinitionCatalog.PoisonKey),
                Is.EqualTo(1));

            Assert.That(session.TryPlayerHit(), Is.True);
            Assert.That(
                session.Battle.PendingPlayerAutomaticInteraction.EffectKind,
                Is.EqualTo(CardEffectKind.Poison));
        }

        private static RunProgress CreateProgress(int playerDeckSeed)
        {
            return new RunProgress(
                new[]
                {
                    new StageDefinition(
                        "ac06-first",
                        "AC-06 First",
                        StageKind.NormalCombat,
                        enemyMaximumSoul: 1,
                        playerDeckSeed: 7,
                        enemyDeckSeed: 9),
                    new StageDefinition(
                        "ac06-next",
                        "AC-06 Next",
                        StageKind.FinalBossCombat,
                        enemyMaximumSoul: 3,
                        playerDeckSeed,
                        enemyDeckSeed: 13)
                },
                new PlayerRunState(
                    maximumSoul: 12,
                    currentSoul: 12,
                    new[]
                    {
                        new RunCardDefinition(0, 10),
                        new RunCardDefinition(1, 8),
                        new RunCardDefinition(2, 10),
                        new RunCardDefinition(3, 1)
                    }));
        }

        private static CoreLoopBattle CreateAutomaticChoiceBattle(
            PlayerRunState player,
            int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(new[]
                {
                    new BlackjackCard(0, rank: 2),
                    new BlackjackCard(1, rank: 3),
                    new BlackjackCard(
                        2,
                        CardDefinitionCatalog.GetByKey(
                            CardDefinitionCatalog.PoisonKey)),
                    new BlackjackCard(3, rank: 4)
                }),
                BlackjackDeck.CreateInDrawOrder(new[]
                {
                    new BlackjackCard(100, rank: 4),
                    new BlackjackCard(101, rank: 5),
                    new BlackjackCard(102, rank: 2)
                }),
                player.MaximumSoul,
                player.CurrentSoul,
                enemyMaximumSoul,
                new StandPolicy());
        }

        private static CoreLoopBattle CreateImmediateVictoryBattle(
            PlayerRunState player,
            int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                CreateRankDeck(10, 1),
                CreateRankDeck(10, 10),
                player.MaximumSoul,
                player.CurrentSoul,
                enemyMaximumSoul);
        }

        private static BlackjackDeck CreateRankDeck(params int[] ranks)
        {
            return BlackjackDeck.CreateInDrawOrder(
                ranks.Select((rank, id) =>
                    new BlackjackCard(id, rank)));
        }

        private static int FindSeedWithPoisonAsThirdDraw()
        {
            for (int seed = 0; seed < 128; seed++)
            {
                var cards = new[]
                {
                    new BlackjackCard(0, rank: 10),
                    new BlackjackCard(1, rank: 8),
                    new BlackjackCard(2, rank: 10),
                    new BlackjackCard(3, rank: 1),
                    new BlackjackCard(
                        4,
                        CardDefinitionCatalog.GetByKey(
                            CardDefinitionCatalog.PoisonKey))
                };
                var deck = new BlackjackDeck(cards, seed);
                deck.Draw();
                deck.Draw();
                if (deck.Draw().DefinitionKey ==
                    CardDefinitionCatalog.PoisonKey)
                {
                    return seed;
                }
            }

            throw new AssertionException(
                "No seed placed rewarded poison on the first hit.");
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "ac06-stage-test-stand");
            }
        }
    }
}
