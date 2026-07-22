using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CultistContractBalanceTests
    {
        private const int SimulationCount = 400;

        [Test]
        public void DC08_U01_SatanRequiresSoulToSurviveItsExpirationCost()
        {
            EnemyActionCandidate satan = CreateContractChoice(
                0,
                DemonContractKind.Satan);
            EnemyActionCandidate belphegor = CreateContractChoice(
                1,
                DemonContractKind.Belphegor);
            var policy = new CultistEnemyPolicy();

            EnemyDecision fatal = policy.Decide(CreateObservation(
                enemySoul: SatanDemonContractHandler.ExpirationSoulCost,
                candidates: new[] { satan, belphegor }));
            EnemyDecision survivable = policy.Decide(CreateObservation(
                enemySoul: SatanDemonContractHandler.ExpirationSoulCost + 1,
                enemyMaximumSoul: SatanDemonContractHandler.ExpirationSoulCost + 1,
                candidates: new[] { satan, belphegor }));

            Assert.That(GetSelectedContractKind(fatal),
                Is.EqualTo(DemonContractKind.Belphegor));
            Assert.That(fatal.ReasonCode, Is.EqualTo("cultist-select-belphegor"));
            Assert.That(GetSelectedContractKind(survivable),
                Is.EqualTo(DemonContractKind.Satan));
            Assert.That(survivable.ReasonCode, Is.EqualTo("cultist-select-satan"));
        }

        [Test]
        public void DC08_U02_LeviathanRequiresAnUnusedRevolverInOwnHand()
        {
            EnemyActionCandidate leviathan = CreateContractChoice(
                0,
                DemonContractKind.Leviathan);
            EnemyActionCandidate mammon = CreateContractChoice(
                1,
                DemonContractKind.Mammon);
            var policy = new CultistEnemyPolicy();

            EnemyDecision withoutRevolver = policy.Decide(CreateObservation(
                enemySoul: 2,
                candidates: new[] { leviathan, mammon }));
            EnemyDecision withRevolver = policy.Decide(CreateObservation(
                enemySoul: 2,
                ownCards: new[]
                {
                    new EnemyOwnedCardObservation(
                        10,
                        "auto-pistol-7",
                        7,
                        isFaceUp: false,
                        CardUseState.Available,
                        canUse: false)
                },
                candidates: new[] { leviathan, mammon }));

            Assert.That(GetSelectedContractKind(withoutRevolver),
                Is.EqualTo(DemonContractKind.Mammon));
            Assert.That(GetSelectedContractKind(withRevolver),
                Is.EqualTo(DemonContractKind.Leviathan));
            Assert.That(withRevolver.ReasonCode,
                Is.EqualTo("cultist-select-leviathan-with-revolver"));
        }

        [Test]
        public void DC08_U03_BelphegorUsesVisibleTotalAndTwentyOneBoundary()
        {
            var keep = new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId:
                    BelphegorDemonContractHandler.KeepTopCardOptionId,
                demonContractInteractionKind:
                    DemonContractInteractionKind.BelphegorTopCard,
                demonContractKind: DemonContractKind.Belphegor,
                demonContractDefinitionKey: DemonContractCatalog.BelphegorKey,
                demonContractOptionNumericValue: 1);
            var move = new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId:
                    BelphegorDemonContractHandler.MoveTopCardToBottomOptionId,
                demonContractInteractionKind:
                    DemonContractInteractionKind.BelphegorTopCard,
                demonContractKind: DemonContractKind.Belphegor,
                demonContractDefinitionKey: DemonContractCatalog.BelphegorKey,
                demonContractOptionNumericValue: 1);
            EnemyOwnedCardObservation[] visibleTwentyAndHiddenTen =
            {
                CreateOwnedCard(0, 10, isFaceUp: true),
                CreateOwnedCard(1, 10, isFaceUp: true),
                CreateOwnedCard(2, 10, isFaceUp: false)
            };

            EnemyDecision atTwentyOne = new CultistEnemyPolicy().Decide(
                CreateObservation(
                    enemySoul: 2,
                    ownHandTotal: 30,
                    ownCards: visibleTwentyAndHiddenTen,
                    candidates: new[] { keep, move }));

            keep = CreateBelphegorOption(
                BelphegorDemonContractHandler.KeepTopCardOptionId,
                previewRank: 2);
            move = CreateBelphegorOption(
                BelphegorDemonContractHandler.MoveTopCardToBottomOptionId,
                previewRank: 2);
            EnemyDecision overTwentyOne = new CultistEnemyPolicy().Decide(
                CreateObservation(
                    enemySoul: 2,
                    ownHandTotal: 30,
                    ownCards: visibleTwentyAndHiddenTen,
                    candidates: new[] { keep, move }));

            Assert.That(atTwentyOne.DemonContractOptionId,
                Is.EqualTo(BelphegorDemonContractHandler.KeepTopCardOptionId));
            Assert.That(overTwentyOne.DemonContractOptionId,
                Is.EqualTo(BelphegorDemonContractHandler.MoveTopCardToBottomOptionId));
        }

        [Test]
        public void DC08_U04_MammonRerollsTwoButKeepsThree()
        {
            AssertMammonTurnChoice(
                dieValue: 2,
                MammonDemonContractHandler.RerollDieOptionId);
            AssertMammonTurnChoice(
                dieValue: 3,
                MammonDemonContractHandler.KeepDieOptionId);
        }

        [Test]
        public void DC08_U05_MammonFinalChoiceIncludesHiddenCardsAtShowdown()
        {
            AssertMammonFinalChoice(
                handTotal: 18,
                dieValue: 3,
                MammonDemonContractHandler.ApplyDieOptionId);
            AssertMammonFinalChoice(
                handTotal: 19,
                dieValue: 3,
                MammonDemonContractHandler.DoNotApplyDieOptionId);
        }

        [Test]
        public void DC08_U06_SameSeedRepeatsTheSameContractSelection()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                Assert.That(
                    StartCultistContract(seed).ActiveEnemyDemonContracts.Single().Kind,
                    Is.EqualTo(
                        StartCultistContract(seed).ActiveEnemyDemonContracts.Single().Kind));
            }
        }

        [Test]
        public void DC08_I01_PrototypeSeedsBalanceUsefulContractSelection()
        {
            var counts = new Dictionary<DemonContractKind, int>();
            foreach (DemonContractKind kind in Enum.GetValues(typeof(DemonContractKind)))
            {
                counts.Add(kind, 0);
            }

            for (int seed = 0; seed < SimulationCount; seed++)
            {
                CoreLoopBattle battle = StartCultistContract(seed);
                Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.EqualTo(1));
                Assert.That(battle.ActiveEnemyDemonContracts.Count, Is.EqualTo(1));
                counts[battle.ActiveEnemyDemonContracts[0].Kind]++;
            }

            TestContext.WriteLine(
                $"DC-08 {SimulationCount}회 선택: " +
                string.Join(", ", counts.Select(pair => $"{pair.Key}={pair.Value}")));
            Assert.That(counts[DemonContractKind.Belphegor],
                Is.InRange(160, 240));
            Assert.That(counts[DemonContractKind.Mammon],
                Is.InRange(160, 240));
            Assert.That(counts[DemonContractKind.Satan], Is.Zero);
            Assert.That(counts[DemonContractKind.Leviathan], Is.Zero);
        }

        [Test]
        public void DC08_I02_OneHundredAutoplayBattlesFinishWithoutContractStall()
        {
            int playerVictories = 0;
            int enemyVictories = 0;
            for (int seed = 0; seed < 100; seed++)
            {
                CoreLoopBattle battle = CreateCultistBattle(
                    seed,
                    BlackjackDeck.CreateStandard(seed ^ 0x13579));
                Assert.That(battle.Start(), Is.True);

                int actionCount = 0;
                while (battle.State != CoreLoopState.BattleEnded && actionCount < 200)
                {
                    Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
                    bool acted = battle.Player.VisibleHandValue.Total <= 16 &&
                        battle.Player.Deck.CanDraw(1)
                            ? battle.TryPlayerHit()
                            : battle.TryPlayerStand();
                    Assert.That(acted, Is.True);
                    actionCount++;
                }

                Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded),
                    $"seed {seed} stalled after {actionCount} player actions");
                Assert.That(actionCount, Is.LessThan(200));
                Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.EqualTo(1));
                if (battle.Outcome == BattleOutcome.PlayerVictory)
                {
                    playerVictories++;
                }
                else
                {
                    enemyVictories++;
                }
            }

            TestContext.WriteLine(
                $"DC-08 자동전투 100회: player={playerVictories}, enemy={enemyVictories}");
            Assert.That(playerVictories + enemyVictories, Is.EqualTo(100));
        }

        private static CoreLoopBattle StartCultistContract(int seed)
        {
            CoreLoopBattle battle = CreateCultistBattle(
                seed,
                CreatePlainDeck(Enumerable.Repeat(2, 20)));
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateCultistBattle(
            int seed,
            BlackjackDeck playerDeck)
        {
            EnemyBattleConfiguration enemy = EnemyBattleConfigurationFactory.Create(
                EnemyCombatProfileCatalog.CultistKey,
                seed ^ 0x24680);
            return new CoreLoopBattle(
                playerDeck,
                enemy.CreateEnemyDeck(),
                playerMaximumSoul: 7,
                playerCurrentSoul: 7,
                enemy.EnemyMaximumSoul,
                enemy.BehaviorPolicy,
                CardEffectResolver.CreateDefault(),
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0),
                DemonContractResolver.CreateDefault(),
                DemonContractDeck.CreatePrototype(seed));
        }

        private static BlackjackDeck CreatePlainDeck(IEnumerable<int> ranks)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks.Select(
                (rank, index) => new BlackjackCard(index, rank)));
        }

        private static EnemyOwnedCardObservation CreateOwnedCard(
            int id,
            int rank,
            bool isFaceUp)
        {
            return new EnemyOwnedCardObservation(
                id,
                CardDefinitionCatalog.GetDefaultForRank(rank).Key,
                rank,
                isFaceUp,
                CardUseState.Unavailable,
                canUse: false);
        }

        private static EnemyActionCandidate CreateContractChoice(
            int optionId,
            DemonContractKind kind)
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .Definitions.Single(candidate => candidate.Kind == kind);
            return new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId: optionId,
                demonContractInteractionKind: DemonContractInteractionKind.ChooseContract,
                demonContractKind: kind,
                demonContractDefinitionKey: definition.Key);
        }

        private static DemonContractKind GetSelectedContractKind(
            EnemyDecision decision)
        {
            return decision.CandidateScores.Single(score =>
                    score.Candidate.DemonContractOptionId ==
                        decision.DemonContractOptionId)
                .Candidate.DemonContractKind.Value;
        }

        private static EnemyActionCandidate CreateBelphegorOption(
            int optionId,
            int previewRank)
        {
            return new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId: optionId,
                demonContractInteractionKind: DemonContractInteractionKind.BelphegorTopCard,
                demonContractKind: DemonContractKind.Belphegor,
                demonContractDefinitionKey: DemonContractCatalog.BelphegorKey,
                demonContractOptionNumericValue: previewRank);
        }

        private static void AssertMammonTurnChoice(int dieValue, int expectedOptionId)
        {
            EnemyActionCandidate keep = CreateMammonOption(
                DemonContractInteractionKind.MammonReroll,
                MammonDemonContractHandler.KeepDieOptionId,
                dieValue);
            EnemyActionCandidate reroll = CreateMammonOption(
                DemonContractInteractionKind.MammonReroll,
                MammonDemonContractHandler.RerollDieOptionId,
                dieValue);
            EnemyDecision decision = new CultistEnemyPolicy().Decide(
                CreateObservation(
                    enemySoul: 2,
                    candidates: new[] { keep, reroll }));
            Assert.That(decision.DemonContractOptionId, Is.EqualTo(expectedOptionId));
        }

        private static void AssertMammonFinalChoice(
            int handTotal,
            int dieValue,
            int expectedOptionId)
        {
            EnemyActionCandidate decline = CreateMammonOption(
                DemonContractInteractionKind.MammonApplyDie,
                MammonDemonContractHandler.DoNotApplyDieOptionId,
                dieValue);
            EnemyActionCandidate apply = CreateMammonOption(
                DemonContractInteractionKind.MammonApplyDie,
                MammonDemonContractHandler.ApplyDieOptionId,
                dieValue);
            EnemyDecision decision = new CultistEnemyPolicy().Decide(
                CreateObservation(
                    enemySoul: 2,
                    ownHandTotal: handTotal,
                    candidates: new[] { decline, apply }));
            Assert.That(decision.DemonContractOptionId, Is.EqualTo(expectedOptionId));
        }

        private static EnemyActionCandidate CreateMammonOption(
            DemonContractInteractionKind kind,
            int optionId,
            int dieValue)
        {
            return new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId: optionId,
                demonContractInteractionKind: kind,
                demonContractKind: DemonContractKind.Mammon,
                demonContractDefinitionKey: DemonContractCatalog.MammonKey,
                demonContractOptionNumericValue: dieValue);
        }

        private static EnemyObservation CreateObservation(
            int enemySoul,
            IReadOnlyList<EnemyActionCandidate> candidates,
            int enemyMaximumSoul = 3,
            int ownHandTotal = 15,
            IReadOnlyList<EnemyOwnedCardObservation> ownCards = null)
        {
            return new EnemyObservation(
                new HandValue(ownHandTotal),
                ownCards ?? Array.Empty<EnemyOwnedCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 1,
                new SoulObservation(7, 7),
                new SoulObservation(enemySoul, enemyMaximumSoul),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 8,
                playerDeckAvailableCount: 18,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                candidates,
                Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: null,
                decisionSeed: 7);
        }
    }
}
