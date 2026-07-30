using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class FormalRunSessionTests
    {
        [Test]
        public void RF03_I01_FirstVictoryAwardsGoldAndOpensFirstShopWithoutCardReward()
        {
            FormalRunSession run = CreateFormalRun();

            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);

            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Shop));
            Assert.That(run.ActiveShop.Offer.VisitIndex, Is.Zero);
            Assert.That(run.CompletedShopCount, Is.Zero);
            Assert.That(run.LastGoldReward, Is.EqualTo(4));
            Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.EqualTo(4));
            Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.Zero);
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.StageCleared));
            Assert.That(run.CombatSession.Progress.PendingReward, Is.Null);
            Assert.That(run.TrySelectBattleReward(0), Is.False);
            Assert.That(run.TrySkipBattleReward(), Is.False);
        }

        [Test]
        public void RF03_I02_FirstShopLeaveAdvancesToOpponentSelection()
        {
            FormalRunSession run = CreateFormalRun(enableOpponentSelection: true);
            Assert.That(run.TryStartRun(), Is.True);
            SelectFirstOpponent(run);
            WinCurrentBattle(run.CombatSession);
            int offerId = run.ActiveShop.Offer.OfferId;

            Assert.That(run.TryLeaveShop(offerId), Is.True);

            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Combat));
            Assert.That(run.CompletedShopCount, Is.EqualTo(1));
            Assert.That(run.ActiveShop, Is.Null);
            Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.OpponentSelection));
            Assert.That(run.CombatSession.PendingOpponentSelection, Is.Not.Null);
        }

        [Test]
        public void RF03_I03_SecondVictoryAccumulatesGoldAndOpensSecondShop()
        {
            FormalRunSession run = CreateFormalRun();
            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);
            Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);

            WinCurrentBattle(run.CombatSession);

            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Shop));
            Assert.That(run.ActiveShop.Offer.VisitIndex, Is.EqualTo(1));
            Assert.That(run.LastGoldReward, Is.EqualTo(9));
            Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.EqualTo(13));
            Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.EqualTo(1));
            Assert.That(run.CombatSession.Progress.PendingReward, Is.Null);
        }

        [Test]
        public void RF03_I04_SecondShopLeavePreparesFixedBossWithoutOpponentOffer()
        {
            FormalRunSession run = AdvanceToSecondShop();

            Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);

            Assert.That(run.CompletedShopCount, Is.EqualTo(2));
            Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.EqualTo(2));
            Assert.That(run.CombatSession.Progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(run.CombatSession.PendingOpponentSelection, Is.Null);
            Assert.That(run.CombatSession.ActiveStage.Kind, Is.EqualTo(StageKind.FinalBossCombat));
        }

        [Test]
        public void RF03_I05_BossVictoryAwardsGoldAndEndsRunWithoutShop()
        {
            FormalRunSession run = AdvanceToSecondShop();
            Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);

            WinCurrentBattle(run.CombatSession, StageProgressionState.RunVictory);

            Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.RunVictory));
            Assert.That(run.LastGoldReward, Is.EqualTo(15));
            Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.EqualTo(28));
            Assert.That(run.ActiveShop, Is.Null);
            Assert.That(run.CombatSession.Progress.State, Is.EqualTo(StageProgressionState.RunVictory));
            Assert.That(run.CombatSession.Progress.PendingReward, Is.Null);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void RF03_I06_DefeatAtAnyCombatEndsRunWithoutGoldOrShop(int defeatBattleIndex)
        {
            int createdBattleCount = 0;
            FormalRunSession run = CreateFormalRun((stage, player) =>
            {
                int battleIndex = createdBattleCount++;
                return battleIndex == defeatBattleIndex
                    ? CreateImmediateDefeatBattle(stage)
                    : CreateVictoryBattle(stage, player);
            });
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
            Assert.That(run.CombatSession.Progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
        }

        [Test]
        public void RF03_I07_ShopChangesReachTheNextBattleFactory()
        {
            int observedSoul = -1;
            int observedDeckCount = -1;
            int battleIndex = 0;
            PlayerRunState player = CreatePlayer(currentSoul: 10);
            FormalRunSession run = CreateFormalRun((stage, state) =>
            {
                if (battleIndex++ == 1)
                {
                    observedSoul = state.CurrentSoul;
                    observedDeckCount = state.Deck.Count;
                }

                return CreateVictoryBattle(stage, state);
            }, player: player);
            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);
            player.SetCurrentSoul(10);
            player.AddGold(3);
            ShopOffer offer = run.ActiveShop.Offer;

            Assert.That(
                run.TryRestAtShop(offer.OfferId),
                Is.True,
                $"Soul={player.CurrentSoul}, Gold={player.CurrentGold}, Whiskey={offer.WhiskeyPrice}");
            Assert.That(run.TryBuyShopCard(
                offer.OfferId,
                offer.CardOptions[0].OptionId), Is.True);
            Assert.That(run.TryLeaveShop(offer.OfferId), Is.True);

            Assert.That(observedSoul, Is.EqualTo(12));
            Assert.That(observedDeckCount, Is.EqualTo(player.Deck.Count));
            Assert.That(observedDeckCount, Is.EqualTo(5));
            Assert.That(run.UtilityPriceLevel, Is.EqualTo(1));
        }

        [Test]
        public void RF03_I08_StaleShopInputsAreRejectedWithoutAdvancing()
        {
            FormalRunSession run = CreateFormalRun();
            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);
            ShopVisit shop = run.ActiveShop;
            int gold = run.CombatSession.Progress.Player.CurrentGold;

            Assert.That(run.TryLeaveShop(shop.Offer.OfferId + 1), Is.False);
            Assert.That(run.TryBuyShopCard(
                shop.Offer.OfferId + 1,
                shop.Offer.CardOptions[0].OptionId), Is.False);
            Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.Zero);
            Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.EqualTo(gold));
            Assert.That(run.ActiveShop, Is.SameAs(shop));

            Assert.That(run.TryLeaveShop(shop.Offer.OfferId), Is.True);
            Assert.That(run.TryLeaveShop(shop.Offer.OfferId), Is.False);
            Assert.That(run.TryRestAtShop(shop.Offer.OfferId), Is.False);
            Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.EqualTo(1));
        }

        [Test]
        public void RF03_I09_RestartResetsFormalRunAndShopState()
        {
            FormalRunSession run = CreateFormalRun();
            Assert.That(run.TryStartRun(), Is.True);

            for (int iteration = 0; iteration < 10; iteration++)
            {
                WinCurrentBattle(run.CombatSession);
                Assert.That(run.ActiveShop.Offer.OfferId, Is.Zero, $"Offer {iteration}");
                Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
                WinCurrentBattle(run.CombatSession);
                Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
                WinCurrentBattle(
                    run.CombatSession,
                    StageProgressionState.RunVictory);
                Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.RunVictory));
                Assert.That(
                    run.CombatSession.Progress.State,
                    Is.Not.EqualTo(StageProgressionState.RewardSelection));

                Assert.That(run.TryRestartRun(), Is.True);
                Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Combat));
                Assert.That(run.CompletedShopCount, Is.Zero);
                Assert.That(run.UtilityPriceLevel, Is.Zero);
                Assert.That(run.LastGoldReward, Is.Zero);
                Assert.That(run.ActiveShop, Is.Null);
                Assert.That(run.CombatSession.Progress.CurrentStageIndex, Is.Zero);
                Assert.That(run.CombatSession.Progress.Player.CurrentGold, Is.Zero);
            }
        }

        [Test]
        public void RF03_I10_LegacySessionStillUsesCardRewards()
        {
            StageProgressionSession session = CreateCombatSession(
                CreateProgress(CreatePlayer()),
                CreateVictoryBattle,
                usesBattleRewards: true);

            Assert.That(session.TryStartRun(), Is.True);
            WinCurrentBattle(session, StageProgressionState.RewardSelection);

            Assert.That(session.Progress.PendingReward, Is.Not.Null);
            Assert.That(session.UsesBattleRewards, Is.True);
        }

        [Test]
        public void SV06_I04_RestoredUtilityLevelRebuildsSameSecondShop()
        {
            FormalRunSession live = CreateFormalRun();
            Assert.That(live.TryStartRun(), Is.True);
            WinCurrentBattle(live.CombatSession);
            ShopOffer firstOffer = live.ActiveShop.Offer;
            live.CombatSession.Progress.Player.AddGold(10);
            Assert.That(
                live.TryRemoveShopCard(
                    firstOffer.OfferId,
                    live.CombatSession.Progress.Player.Deck[0].Id),
                Is.True);
            Assert.That(live.TryLeaveShop(firstOffer.OfferId), Is.True);
            WinCurrentBattle(live.CombatSession);
            ShopOffer expected = live.ActiveShop.Offer;

            PlayerRunState restoredPlayer = live.CombatSession.Progress.Player;
            RunProgress restoredProgress = RunProgress.Restore(
                CreateProgress(restoredPlayer).Stages,
                restoredPlayer,
                1,
                StageProgressionState.StageCleared);
            StageProgressionSession restoredCombat = CreateCombatSession(
                restoredProgress,
                CreateVictoryBattle,
                usesBattleRewards: false);
            FormalRunSession restored = new FormalRunSession(
                restoredCombat,
                new ShopOfferGenerator(20260730),
                1,
                1);

            restored.SynchronizeExternalState();

            Assert.That(restored.Phase, Is.EqualTo(FormalRunPhase.Shop));
            Assert.That(restored.ActiveShop.Offer.OfferId, Is.EqualTo(expected.OfferId));
            Assert.That(restored.ActiveShop.Offer.LighterPrice, Is.EqualTo(3));
            Assert.That(restored.ActiveShop.Offer.WhiskeyPrice, Is.EqualTo(3));
            Assert.That(
                restored.ActiveShop.Offer.CardOptions
                    .Select(option => option.DefinitionKey),
                Is.EqualTo(
                    expected.CardOptions.Select(option => option.DefinitionKey)));
        }

        [Test]
        public void SV06_U01_CheckpointSignalsExposeOnlyStableFormalStates()
        {
            FormalRunSession run = CreateFormalRun();
            int settlementSignals = 0;
            int exitSignals = 0;
            run.CombatSettlementCompleted += () =>
            {
                settlementSignals++;
                Assert.That(run.Phase, Is.EqualTo(FormalRunPhase.Shop));
                Assert.That(
                    run.CombatSession.Progress.State,
                    Is.EqualTo(StageProgressionState.StageCleared));
            };
            run.ShopExited += () =>
            {
                exitSignals++;
                Assert.That(run.CompletedShopCount, Is.EqualTo(1));
                Assert.That(
                    run.CombatSession.Progress.State,
                    Is.EqualTo(StageProgressionState.StageCleared));
            };

            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);
            Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);

            Assert.That(settlementSignals, Is.EqualTo(1));
            Assert.That(exitSignals, Is.EqualTo(1));
            Assert.That(
                run.CombatSession.Progress.State,
                Is.EqualTo(StageProgressionState.InBattle));
        }

        private static FormalRunSession AdvanceToSecondShop()
        {
            FormalRunSession run = CreateFormalRun();
            Assert.That(run.TryStartRun(), Is.True);
            WinCurrentBattle(run.CombatSession);
            Assert.That(run.TryLeaveShop(run.ActiveShop.Offer.OfferId), Is.True);
            WinCurrentBattle(run.CombatSession);
            return run;
        }

        private static FormalRunSession CreateFormalRun(
            Func<StageDefinition, PlayerRunState, CoreLoopBattle> battleFactory = null,
            bool enableOpponentSelection = false,
            PlayerRunState player = null)
        {
            RunProgress progress = CreateProgress(player ?? CreatePlayer());
            OpponentSelectionGenerator opponentGenerator = enableOpponentSelection
                ? CreateOpponentGenerator()
                : null;
            StageProgressionSession combatSession = CreateCombatSession(
                progress,
                battleFactory ?? CreateVictoryBattle,
                opponentGenerator,
                usesBattleRewards: false);
            return new FormalRunSession(combatSession, new ShopOfferGenerator(20260730));
        }

        private static StageProgressionSession CreateCombatSession(
            RunProgress progress,
            Func<StageDefinition, PlayerRunState, CoreLoopBattle> battleFactory,
            OpponentSelectionGenerator opponentSelectionGenerator = null,
            bool usesBattleRewards = false)
        {
            return new StageProgressionSession(
                progress,
                battleFactory,
                opponentSelectionGenerator: opponentSelectionGenerator,
                usesBattleRewards: usesBattleRewards);
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

        private static OpponentSelectionGenerator CreateOpponentGenerator()
        {
            EnemyCombatProfileCatalog catalog = EnemyCombatProfileCatalog.Default;
            return new OpponentSelectionGenerator(
                new EnemyCombatProfileCatalog(new[]
                {
                    catalog.GetByKey(EnemyCombatProfileCatalog.GunslingerKey),
                    catalog.GetByKey(EnemyCombatProfileCatalog.CultistKey)
                }),
                20260730,
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

        private static void SelectFirstOpponent(FormalRunSession run)
        {
            OpponentSelectionOffer offer = run.CombatSession.PendingOpponentSelection;
            Assert.That(run.TrySelectOpponent(
                offer.OfferId,
                offer.Candidates[0].ProfileKey), Is.True);
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
