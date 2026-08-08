using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class FormalRunSystemValidationTests
    {
        [Test]
        public void RF05_V01_FullVictoryAndRestartRemainStableForTenRuns()
        {
            FormalRunSession run = CreateFormalRun();
            Assert.That(run.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < 10; iteration++)
            {
                WinCurrentBattle(run.CombatSession);
                Assert.That(run.TrySkipBattleReward(), Is.False);
                Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
                WinCurrentBattle(run.CombatSession);
                Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
                WinCurrentBattle(run.CombatSession, StageProgressionState.RunVictory);

                Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.RunVictory), $"Victory {iteration}");
                Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.EqualTo(420));
                Assert.That(run.ActiveShop, Is.Null);
                Assert.That(run.TryRestartRun(), Is.True, $"Restart {iteration}");
                Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.Zero);
                Assert.That(run.CompletedShopCount, Is.Zero);
                Assert.That(run.UtilityPriceLevel, Is.Zero);
            }
        }

        [Test]
        public void RF05_V02_AllShopTransactionsRemainAtomicForTenVisits()
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                PlayerRunState player = CreatePlayer(currentSoul: 10);
                FormalRunSession run = CreateFormalRun(player: player, seed: 20260800 + iteration);
                Assert.That(run.TryStartRun(), Is.True);
                WinCurrentBattle(run.CombatSession);
                player.SetCurrentSoul(10);
                player.AddGold(30);
                ShopVisit shop = run.ActiveShop;
                ShopCardOption normal = shop.Offer.CardOptions.First(
                    option => option.DeckKind == ShopCardDeckKind.Normal);
                ShopCardOption demon = shop.Offer.CardOptions.First(
                    option => option.DeckKind == ShopCardDeckKind.Demon);
                int removedCardId = player.Deck[0].Id;

                Assert.That(run.TryBuyShopCard(shop.Offer.OfferId, normal.OptionId), Is.True);
                Assert.That(run.TryBuyShopCard(shop.Offer.OfferId, demon.OptionId), Is.True);
                Assert.That(run.TryRemoveShopCard(shop.Offer.OfferId, removedCardId), Is.True);
                Assert.That(run.TryRestAtShop(shop.Offer.OfferId), Is.True);

                int goldAfterTransactions = player.CurrentGold;
                int deckCountAfterTransactions = player.Deck.Count;
                int demonCountAfterTransactions = player.DemonDeck.Count;
                Assert.That(run.TryBuyShopCard(shop.Offer.OfferId, normal.OptionId), Is.False);
                Assert.That(run.TryRemoveShopCard(shop.Offer.OfferId, player.Deck[0].Id), Is.False);
                Assert.That(run.TryRestAtShop(shop.Offer.OfferId), Is.False);
                Assert.That(player.CurrentGold, Is.EqualTo(goldAfterTransactions));
                Assert.That(player.Deck.Count, Is.EqualTo(deckCountAfterTransactions));
                Assert.That(player.DemonDeck.Count, Is.EqualTo(demonCountAfterTransactions));

                Assert.That(run.TryLeaveShop(shop.Offer.OfferId), Is.True);
                Assert.That(run.TryLeaveShop(shop.Offer.OfferId), Is.False);
                Assert.That(run.UtilityPriceLevel, Is.EqualTo(1));
            }
        }

        [Test]
        public void RF05_V03_StaleOpponentAndShopInputsRemainIsolatedForTenRuns()
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                FormalRunSession run = CreateFormalRun(
                    enableOpponentSelection: true,
                    seed: 20260900 + iteration);
                Assert.That(run.TryStartRun(), Is.True);
                OpponentSelectionOffer opponent = run.CombatSession.PendingOpponentSelection;
                OpponentSelectionCandidate candidate = opponent.Candidates[0];

                Assert.That(run.TrySelectOpponent(opponent.OfferId + 1, candidate.ProfileKey), Is.False);
                Assert.That(run.TrySelectOpponent(opponent.OfferId, "missing-profile"), Is.False);
                Assert.That(run.TrySelectOpponent(opponent.OfferId, candidate.ProfileKey), Is.True);
                Assert.That(run.TrySelectOpponent(opponent.OfferId, candidate.ProfileKey), Is.False);
                WinCurrentBattle(run.CombatSession);

                ShopVisit shop = run.ActiveShop;
                int gold = run.CombatSession.Progress.Player.CurrentGold;
                Assert.That(run.TryBuyShopCard(
                    shop.Offer.OfferId + 1,
                    shop.Offer.CardOptions[0].OptionId), Is.False);
                Assert.That(run.TryLeaveShop(shop.Offer.OfferId + 1), Is.False);
                Assert.That(run.ActiveShop, Is.SameAs(shop));
                Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.EqualTo(gold));
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RF05_V04_DefeatAndRestartRemainStableAtEveryCombat(
            int defeatBattleIndex)
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                int createdBattleCount = 0;
                FormalRunSession run = CreateFormalRun((stage, player) =>
                {
                    int battleIndex = createdBattleCount++;
                    return battleIndex == defeatBattleIndex
                        ? CreateImmediateDefeatBattle(stage)
                        : CreateVictoryBattle(stage, player);
                }, seed: 20261000 + iteration);
                Assert.That(run.TryStartRun(), Is.True);

                for (int battleIndex = 0; battleIndex < defeatBattleIndex; battleIndex++)
                {
                    WinCurrentBattle(run.CombatSession);
                    Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
                }

                EndCurrentBattle(run.CombatSession);
                Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.RunDefeat));
                Assert.That(run.ActiveShop, Is.Null);
                Assert.That(run.LastGoldReward, Is.Zero);
                Assert.That(run.TryRestartRun(), Is.True);
                Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Combat));
                Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.Zero);
                Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.Zero);
            }
        }

        [Test]
        public void RF05_V05_NormalOpponentProfilesAwardTheirConfiguredGold()
        {
            string[] profileKeys =
            {
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                EnemyCombatProfileCatalog.GunslingerKey,
                EnemyCombatProfileCatalog.CultistKey
            };
            GoldRewardCatalog rewards = GoldRewardCatalog.CreatePrototype();
            EnemyCombatProfileCatalog catalog = CreateNormalProfileCatalog(profileKeys);

            foreach (string profileKey in profileKeys)
            {
                FormalRunSession run = CreateRunOfferingProfile(profileKey, catalog);
                OpponentSelectionOffer offer = run.CombatSession.PendingOpponentSelection;
                Assert.That(run.TrySelectOpponent(offer.OfferId, profileKey), Is.True, profileKey);
                WinCurrentBattle(run.CombatSession);

                Assert.That(run.LastGoldReward, Is.EqualTo(rewards.GetAmount(profileKey)), profileKey);
                Assert.That(
                    run.CombatSession.ActiveStage.BattleProfileKey,
                    Is.EqualTo(profileKey));
            }
        }

        [Test]
        public void RF05_V06_FormalRunScenesAreWiredWithoutMissingScripts()
        {
            const string stagePath = "Assets/00. Scenes/StageTest.unity";
            const string battlePath = "Assets/00. Scenes/GameScene.unity";
            Assert.That(
                EditorBuildSettings.scenes.Any(scene =>
                    scene.enabled && scene.path == stagePath),
                Is.True);
            Assert.That(
                EditorBuildSettings.scenes.Any(scene =>
                    scene.enabled && scene.path == battlePath),
                Is.True);

            Scene stageScene = EditorSceneManager.OpenScene(
                stagePath,
                OpenSceneMode.Single);
            StageProgressionRuntime runtime =
                UnityEngine.Object.FindFirstObjectByType<StageProgressionRuntime>();
            Assert.That(runtime, Is.Not.Null);
            SerializedProperty battleSceneName = new SerializedObject(runtime)
                .FindProperty("battleSceneName");
            Assert.That(battleSceneName, Is.Not.Null);
            Assert.That(battleSceneName.stringValue, Is.EqualTo("GameScene"));
            AssertSceneHasNoMissingScripts(stageScene);

            Scene battleScene = EditorSceneManager.OpenScene(
                battlePath,
                OpenSceneMode.Single);
            Assert.That(
                UnityEngine.Object.FindFirstObjectByType<GameManager>(),
                Is.Not.Null);
            Assert.That(
                UnityEngine.Object.FindFirstObjectByType<ShopController>(),
                Is.Not.Null);
            AssertSceneHasNoMissingScripts(battleScene);
        }

        [Test]
        public void RFM02_U01_StageTestPrototypeGrantsTwoDemonsBeforeOpponentSelection()
        {
            StageProgressionSession session =
                StageProgressionRuntime.CreatePrototypeSession(20260730);

            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.PendingStartingDemonGrant, Is.Not.Null);
            Assert.That(session.Progress.Player.DemonDeck.Count, Is.EqualTo(2));
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.NotStarted));
            Assert.That(
                session.TryCompleteStartingDemonReveal(),
                Is.True);
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(
                session.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(session.PendingOpponentSelection, Is.Not.Null);
            Assert.That(
                session.PendingOpponentSelection.Candidates.Count,
                Is.EqualTo(2));
        }

        private static FormalRunSession CreateRunOfferingProfile(
            string targetProfileKey,
            EnemyCombatProfileCatalog catalog)
        {
            for (int seed = 0; seed < 1000; seed++)
            {
                FormalRunSession run = CreateFormalRun(
                    opponentGenerator: new OpponentSelectionGenerator(catalog, seed, 0),
                    seed: 20261100 + seed);
                Assert.That(run.TryStartRun(), Is.True);
                if (run.CombatSession.PendingOpponentSelection.Candidates.Any(
                    candidate => candidate.ProfileKey == targetProfileKey))
                {
                    return run;
                }
            }

            throw new AssertionException(
                $"No deterministic offer contained profile '{targetProfileKey}'.");
        }

        private static EnemyCombatProfileCatalog CreateNormalProfileCatalog(
            IEnumerable<string> profileKeys)
        {
            EnemyCombatProfileCatalog source = EnemyCombatProfileCatalog.Default;
            return new EnemyCombatProfileCatalog(
                profileKeys.Select(source.GetByKey));
        }

        private static void AssertSceneHasNoMissingScripts(Scene scene)
        {
            int missingScriptCount = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
                {
                    missingScriptCount +=
                        GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(
                            transform.gameObject);
                }
            }

            Assert.That(missingScriptCount, Is.Zero, scene.path);
        }

        private static FormalRunSession CreateFormalRun(
            Func<StageDefinition, PlayerRunState, CoreLoopBattle> battleFactory = null,
            bool enableOpponentSelection = false,
            PlayerRunState player = null,
            OpponentSelectionGenerator opponentGenerator = null,
            int seed = 20260730)
        {
            RunProgress progress = CreateProgress(player ?? CreatePlayer());
            OpponentSelectionGenerator selectionGenerator = opponentGenerator;
            if (selectionGenerator == null && enableOpponentSelection)
            {
                selectionGenerator = CreateDefaultOpponentGenerator(seed);
            }

            StageProgressionSession combatSession = new StageProgressionSession(
                progress,
                battleFactory ?? CreateVictoryBattle,
                opponentSelectionGenerator: selectionGenerator,
                usesBattleRewards: false);
            return new FormalRunSession(
                combatSession,
                new ShopOfferGenerator(seed));
        }

        private static RunProgress CreateProgress(PlayerRunState player)
        {
            return new RunProgress(
                new[]
                {
                    StageDefinition.CreateForEnemyProfile(
                        "normal-1", "Ash Gate", StageKind.NormalCombat,
                        EnemyCombatProfileCatalog.GunslingerKey, 10, 11),
                    StageDefinition.CreateForEnemyProfile(
                        "normal-2", "Blood Hall", StageKind.NormalCombat,
                        EnemyCombatProfileCatalog.EnforcerKey, 20, 21),
                    StageDefinition.CreateForEnemyProfile(
                        "boss", "Black Throne", StageKind.FinalBossCombat,
                        EnemyCombatProfileCatalog.FinalBossKey, 30, 31)
                },
                player);
        }

        private static OpponentSelectionGenerator CreateDefaultOpponentGenerator(int seed)
        {
            EnemyCombatProfileCatalog source = EnemyCombatProfileCatalog.Default;
            return new OpponentSelectionGenerator(
                new EnemyCombatProfileCatalog(new[]
                {
                    source.GetByKey(EnemyCombatProfileCatalog.GunslingerKey),
                    source.GetByKey(EnemyCombatProfileCatalog.CultistKey)
                }),
                seed,
                0);
        }

        private static PlayerRunState CreatePlayer(int currentSoul = 12)
        {
            return new PlayerRunState(
                12,
                currentSoul,
                new[]
                {
                    new RunCardDefinition(0, 10),
                    new RunCardDefinition(1, 1),
                    new RunCardDefinition(2, 10),
                    new RunCardDefinition(3, 1)
                });
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

        private static CoreLoopBattle CreateImmediateDefeatBattle(StageDefinition stage)
        {
            return new CoreLoopBattle(
                CreateRepeatedDeck(40, 10, 8),
                CreateRepeatedDeck(40, 10, 10),
                1,
                1,
                stage.EnemyMaximumSoul,
                new SimpleEnemyPolicy());
        }

        private static BlackjackDeck CreateRepeatedDeck(
            int count,
            int firstRank,
            int secondRank)
        {
            var cards = new List<BlackjackCard>(count);
            for (int index = 0; index < count; index++)
            {
                cards.Add(new BlackjackCard(
                    index,
                    index % 2 == 0 ? firstRank : secondRank));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static void WinCurrentBattle(
            StageProgressionSession session,
            StageProgressionState expectedState = StageProgressionState.StageCleared)
        {
            EndCurrentBattle(session);
            Assert.That(session.Progress.State, Is.EqualTo(expectedState));
        }

        private static void EndCurrentBattle(StageProgressionSession session)
        {
            for (int action = 0;
                action < 40 && session.Progress.State == StageProgressionState.InBattle;
                action++)
            {
                Assert.That(session.TryPlayerStand(), Is.True, $"Stand action {action}");
            }

            Assert.That(session.Progress.State, Is.Not.EqualTo(StageProgressionState.InBattle));
        }
    }
}
