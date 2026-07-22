using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyDemonContractTests
    {
        [Test]
        public void DC07_U01_ContractEvaluationDoesNotConsumeEnemyDemonDeck()
        {
            var policy = new CapturingStandPolicy();
            DemonContractDeck enemyDemonDeck = CreateRepeatedDemonDeck(
                DemonContractKind.Belphegor);
            CoreLoopBattle battle = CreateBattle(
                policy,
                enemyDemonDeck,
                enemyMaximumSoul: 3);

            Assert.That(battle.TryPlayerHit(), Is.True);

            EnemyObservation observation = policy.Observations.Single();
            Assert.That(observation.ActionCandidates.Any(candidate =>
                candidate.ActionType == EnemyActionType.DemonContract &&
                !candidate.DemonContractOptionId.HasValue), Is.True);
            Assert.That(enemyDemonDeck.AvailableCardCount, Is.EqualTo(4));
            Assert.That(enemyDemonDeck.CardsInPlayCount, Is.Zero);
            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.Zero);
            Assert.That(battle.ActiveEnemyDemonContracts, Is.Empty);
        }

        [Test]
        public void DC07_U02_CultistPaysCostThenSelectsFromActualCandidates()
        {
            CoreLoopBattle battle = CreateBattle(
                new CultistEnemyPolicy(),
                CreateRepeatedDemonDeck(DemonContractKind.Belphegor),
                enemyMaximumSoul: 3);

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.EqualTo(1));
            Assert.That(battle.PendingEnemyDemonContractInteraction, Is.Null);
            Assert.That(battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Belphegor));
            Assert.That(battle.LastDemonContractResult.ActiveContract.OwnerSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(battle.EnemyDemonDeck.AvailableCardCount, Is.EqualTo(3));
            Assert.That(battle.EnemyDemonDeck.CardsInPlayCount, Is.EqualTo(1));
        }

        [Test]
        public void DC07_U03_CultistAvoidsGuaranteedSatanSoulDeath()
        {
            EnemyActionCandidate satan = CreateContractChoiceCandidate(
                optionId: 0,
                DemonContractKind.Satan);
            EnemyActionCandidate belphegor = CreateContractChoiceCandidate(
                optionId: 1,
                DemonContractKind.Belphegor);
            EnemyObservation observation = CreateObservation(
                enemySoul: 2,
                satan,
                belphegor);

            EnemyDecision decision = new CultistEnemyPolicy().Decide(observation);

            Assert.That(decision.ActionType, Is.EqualTo(EnemyActionType.DemonContract));
            Assert.That(decision.DemonContractOptionId, Is.EqualTo(1));
            Assert.That(decision.ReasonCode, Is.EqualTo("cultist-select-belphegor"));
        }

        [Test]
        public void DC07_U04_EnemySatanPreventsStandAndVisibleBust()
        {
            CoreLoopBattle battle = CreateBattle(
                new ForcedSatanPolicy(),
                CreateRepeatedDemonDeck(DemonContractKind.Satan),
                enemyMaximumSoul: 5,
                enemyRanks: new[] { 10, 2, 10, 10, 2, 2, 2, 2 });

            Assert.That(battle.TryPlayerHit(), Is.True);
            SatanRuntimeState satan = (SatanRuntimeState)battle
                .ActiveEnemyDemonContracts.Single().RuntimeState;
            Assert.That(battle.CanEnemyStand, Is.False);
            Assert.That(battle.Enemy.Hand.Contains(satan.PowerCardId), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.Enemy.VisibleHandValue.IsBust, Is.True);
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Satan));
        }

        [Test]
        public void DC07_U05_EnemySatanExpirationCanEndBattleAndRemovesPower()
        {
            CoreLoopBattle battle = CreateBattle(
                new ForcedSatanPolicy(),
                CreateRepeatedDemonDeck(DemonContractKind.Satan),
                enemyMaximumSoul: 3);

            Assert.That(battle.TryPlayerHit(), Is.True);
            int powerCardId = ((SatanRuntimeState)battle
                .ActiveEnemyDemonContracts.Single().RuntimeState).PowerCardId;
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(battle.Enemy.Soul.Current, Is.Zero);
            Assert.That(battle.ActiveEnemyDemonContracts, Is.Empty);
            Assert.That(battle.Enemy.Hand.Contains(powerCardId), Is.False);
            Assert.That(battle.Enemy.Deck.ContainsKnownCardId(powerCardId), Is.False);
        }

        [Test]
        public void DC07_U06_EnemyBelphegorMovesUnsafePreviewWithoutLeakingPlayerCard()
        {
            var policy = new ForcedBelphegorPolicy();
            CoreLoopBattle battle = CreateBattle(
                policy,
                CreateRepeatedDemonDeck(DemonContractKind.Belphegor),
                enemyMaximumSoul: 4,
                enemyRanks: new[] { 10, 2, 10, 5, 2, 2, 2, 2 });

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.Enemy.VisibleHandValue.Total, Is.EqualTo(20));
            int handCount = battle.Enemy.Hand.Count;

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(handCount));
            Assert.That(battle.Enemy.VisibleHandValue.Total, Is.EqualTo(20));
            Assert.That(policy.LastPreviewRank, Is.EqualTo(5));
            Assert.That(policy.LastPlayerHiddenCount, Is.EqualTo(1));
            Assert.That(typeof(EnemyObservation).GetProperties().Any(property =>
                property.Name.Contains("PlayerHidden") &&
                property.Name != nameof(EnemyObservation.PlayerHiddenCardCount)), Is.False);
        }

        [Test]
        public void DC07_U07_EnemyMammonResolvesTurnChoiceBeforeNormalAction()
        {
            var dieRoller = new SequenceDieRoller(1, 2);
            var resolver = new DemonContractResolver(
                new SatanDemonContractHandler(),
                new BelphegorDemonContractHandler(),
                new MammonDemonContractHandler(dieRoller),
                new LeviathanDemonContractHandler());
            CoreLoopBattle battle = CreateBattle(
                new CultistEnemyPolicy(),
                CreateRepeatedDemonDeck(DemonContractKind.Mammon),
                enemyMaximumSoul: 4,
                demonContractResolver: resolver);

            Assert.That(battle.TryPlayerHit(), Is.True);
            MammonRuntimeState mammon = (MammonRuntimeState)battle
                .ActiveEnemyDemonContracts.Single().RuntimeState;
            Assert.That(mammon.CurrentDieValue, Is.EqualTo(1));

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(mammon.CurrentDieValue, Is.EqualTo(2));
            Assert.That(mammon.TurnChoiceResolved, Is.True);
            Assert.That(battle.PendingEnemyDemonContractInteraction, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void DC07_U08_PresentationShowsEnemyActiveContractWithoutOwnerPreview()
        {
            CoreLoopBattle battle = CreateBattle(
                new CultistEnemyPolicy(),
                CreateRepeatedDemonDeck(DemonContractKind.Belphegor),
                enemyMaximumSoul: 3);
            Assert.That(battle.TryPlayerHit(), Is.True);

            DemonContractPanelViewModel model = DemonContractPresenter.Create(battle);

            Assert.That(model.ActiveContracts.Any(label =>
                label.Contains("상대") && label.Contains("벨페고르")), Is.True);
            Assert.That(model.OwnerPreview, Is.Empty);
            Assert.That(model.LastContractResult, Does.Contain("상대 계약"));
        }

        [Test]
        public void DC07_U09_EnemyMammonCanApplyItsDieAtFinalComparison()
        {
            var resolver = new DemonContractResolver(
                new SatanDemonContractHandler(),
                new BelphegorDemonContractHandler(),
                new MammonDemonContractHandler(new SequenceDieRoller(3)),
                new LeviathanDemonContractHandler());
            CoreLoopBattle battle = CreateBattle(
                new ForcedMammonStandPolicy(),
                CreateRepeatedDemonDeck(DemonContractKind.Mammon),
                enemyMaximumSoul: 4,
                enemyRanks: new[] { 10, 7, 2, 2, 2 },
                demonContractResolver: resolver);

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastEnemyDecision.ActionType,
                Is.EqualTo(EnemyActionType.DemonContract));
            Assert.That(battle.LastEnemyDecision.DemonContractOptionId,
                Is.EqualTo(MammonDemonContractHandler.ApplyDieOptionId));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyWin));
        }

        [Test]
        public void DC07_U10_EnemyLeviathanPaysSoulAfterFailedPistol()
        {
            CoreLoopBattle battle = CreateBattle(
                new ForcedLeviathanPistolPolicy(),
                CreateRepeatedDemonDeck(DemonContractKind.Leviathan),
                enemyMaximumSoul: 4,
                enemyRanks: new[] { 7, 2, 2, 2, 2, 2 });

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastCardEffectActorSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.False);
            Assert.That(battle.LastDemonContractEffectResult.Triggered, Is.True);
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget, Is.Null);
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost, Is.EqualTo(1));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void DC07_I01_StageFactoryGivesContractsOnlyToCultistAndIsolatesFiftyRuns()
        {
            PlayerRunState player = CreateRunPlayer();
            DemonContractDeck previous = null;
            for (int iteration = 0; iteration < 50; iteration++)
            {
                StageDefinition stage = CreateStage(
                    EnemyCombatProfileCatalog.CultistKey,
                    iteration,
                    iteration + 100);
                CoreLoopBattle battle = StageBattleFactory.Create(stage, player);

                Assert.That(battle.EnemyDemonDeck.TotalCardCount, Is.EqualTo(4));
                Assert.That(battle.EnemyDemonDeck.AvailableCardCount, Is.EqualTo(4));
                Assert.That(battle.ActiveEnemyDemonContracts, Is.Empty);
                Assert.That(battle.EnemyDemonDeck, Is.Not.SameAs(previous));
                previous = battle.EnemyDemonDeck;
            }

            CoreLoopBattle gunslinger = StageBattleFactory.Create(
                CreateStage(EnemyCombatProfileCatalog.GunslingerKey, 90, 91),
                player);
            Assert.That(gunslinger.EnemyDemonDeck.TotalCardCount, Is.Zero);
        }

        [Test]
        public void DC07_I02_FourEnemyContractsRemainIsolatedAcrossTenBattlesEach()
        {
            foreach (DemonContractKind kind in new[]
            {
                DemonContractKind.Satan,
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon,
                DemonContractKind.Leviathan
            })
            {
                for (int iteration = 0; iteration < 10; iteration++)
                {
                    CoreLoopBattle battle = CreateBattle(
                        CreateForcedPolicy(kind),
                        CreateRepeatedDemonDeck(kind),
                        enemyMaximumSoul: kind == DemonContractKind.Satan ? 5 : 4);

                    Assert.That(battle.TryPlayerHit(), Is.True);
                    Assert.That(battle.ActiveEnemyDemonContracts.Count, Is.EqualTo(1));
                    Assert.That(battle.ActiveEnemyDemonContracts[0].Kind, Is.EqualTo(kind));
                    Assert.That(battle.ActiveEnemyDemonContracts[0].OwnerSide,
                        Is.EqualTo(CombatantSide.Enemy));
                    Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.EqualTo(1));
                    Assert.That(battle.PendingEnemyDemonContractInteraction, Is.Null);
                }
            }
        }

        private static IEnemyBehaviorPolicy CreateForcedPolicy(DemonContractKind kind)
        {
            switch (kind)
            {
                case DemonContractKind.Satan:
                    return new ForcedSatanPolicy();
                case DemonContractKind.Belphegor:
                    return new ForcedBelphegorPolicy();
                case DemonContractKind.Mammon:
                    return new ForcedMammonStandPolicy();
                case DemonContractKind.Leviathan:
                    return new ForcedLeviathanPistolPolicy();
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private static CoreLoopBattle CreateBattle(
            IEnemyBehaviorPolicy policy,
            DemonContractDeck enemyDemonDeck,
            int enemyMaximumSoul,
            IReadOnlyList<int> enemyRanks = null,
            DemonContractResolver demonContractResolver = null)
        {
            var battle = new CoreLoopBattle(
                CreatePlainDeck(new[] { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 }),
                CreatePlainDeck(
                    enemyRanks ?? new[] { 10, 2, 2, 2, 2, 2, 2, 2, 2, 2 },
                    startId: 100),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul,
                policy,
                CardEffectResolver.CreateDefault(),
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0),
                demonContractResolver ?? DemonContractResolver.CreateDefault(),
                enemyDemonDeck);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static BlackjackDeck CreatePlainDeck(
            IReadOnlyList<int> ranks,
            int startId = 0)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks
                .Select((rank, index) => new BlackjackCard(startId + index, rank)));
        }

        private static DemonContractDeck CreateRepeatedDemonDeck(
            DemonContractKind kind)
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .Definitions.Single(candidate => candidate.Kind == kind);
            return new DemonContractDeck(
                Enumerable.Range(0, 4)
                    .Select(id => new DemonContractCard(1000 + id, definition)),
                seed: 17);
        }

        private static EnemyActionCandidate CreateContractChoiceCandidate(
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

        private static EnemyObservation CreateObservation(
            int enemySoul,
            params EnemyActionCandidate[] candidates)
        {
            return new EnemyObservation(
                new HandValue(15),
                Array.Empty<EnemyOwnedCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 1,
                new SoulObservation(12, 12),
                new SoulObservation(enemySoul, 3),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 8,
                playerDeckAvailableCount: 8,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                candidates,
                Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: null,
                decisionSeed: 7);
        }

        private static StageDefinition CreateStage(
            string profileKey,
            int playerSeed,
            int enemySeed)
        {
            EnemyProfilePreview preview = EnemyCombatProfileCatalog.Default
                .GetPreviewByKey(profileKey);
            return StageDefinition.CreateForEnemyProfile(
                "dc07-" + profileKey,
                preview.DisplayName,
                StageKind.NormalCombat,
                profileKey,
                playerSeed,
                enemySeed);
        }

        private static PlayerRunState CreateRunPlayer()
        {
            return new PlayerRunState(
                12,
                12,
                Enumerable.Range(0, 20)
                    .Select(id => new RunCardDefinition(id, id % 10 + 1)));
        }

        private sealed class CapturingStandPolicy : IEnemyBehaviorPolicy
        {
            public List<EnemyObservation> Observations { get; } =
                new List<EnemyObservation>();

            public EnemyDecision Decide(EnemyObservation observation)
            {
                Observations.Add(observation);
                EnemyActionCandidate stand = observation.ActionCandidates.Single(
                    candidate => candidate.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(stand, "capture-stand");
            }
        }

        private sealed class ForcedSatanPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        option.DemonContractKind == DemonContractKind.Satan)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Hit);
                return EnemyDecision.FromCandidate(candidate, "force-satan-or-hit");
            }
        }

        private sealed class ForcedBelphegorPolicy : IEnemyBehaviorPolicy
        {
            public int LastPlayerHiddenCount { get; private set; }

            public int? LastPreviewRank { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                LastPlayerHiddenCount = observation.PlayerHiddenCardCount;
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        option.DemonContractKind == DemonContractKind.Belphegor);
                if (candidate != null &&
                    candidate.DemonContractInteractionKind ==
                        DemonContractInteractionKind.BelphegorTopCard)
                {
                    LastPreviewRank = candidate.DemonContractOptionNumericValue;
                    int total = observation.OwnCards
                        .Where(card => card.IsFaceUp)
                        .Sum(card => card.Rank) +
                        candidate.DemonContractOptionNumericValue.GetValueOrDefault();
                    int optionId = total > 21
                        ? BelphegorDemonContractHandler.MoveTopCardToBottomOptionId
                        : BelphegorDemonContractHandler.KeepTopCardOptionId;
                    candidate = observation.ActionCandidates.Single(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        option.DemonContractOptionId == optionId);
                }

                candidate = candidate ?? observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Hit);
                return EnemyDecision.FromCandidate(candidate, "force-belphegor-or-hit");
            }
        }

        private sealed class ForcedMammonStandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        option.DemonContractKind == DemonContractKind.Mammon &&
                        (option.DemonContractInteractionKind ==
                                DemonContractInteractionKind.ChooseContract ||
                            (option.DemonContractInteractionKind ==
                                    DemonContractInteractionKind.MammonReroll &&
                                option.DemonContractOptionId ==
                                    MammonDemonContractHandler.KeepDieOptionId) ||
                            (option.DemonContractInteractionKind ==
                                    DemonContractInteractionKind.MammonApplyDie &&
                                option.DemonContractOptionId ==
                                    MammonDemonContractHandler.ApplyDieOptionId)))
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(candidate, "force-mammon-or-stand");
            }
        }

        private sealed class ForcedLeviathanPistolPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        option.DemonContractKind == DemonContractKind.Leviathan)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.UseCard &&
                        option.CardEffectOptionNumericValue == 10)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.UseCard)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(candidate, "force-leviathan-pistol");
            }
        }

        private sealed class SequenceDieRoller : IDemonDieRoller
        {
            private readonly Queue<int> _values;

            public SequenceDieRoller(params int[] values)
            {
                _values = new Queue<int>(values);
            }

            public int RollD6()
            {
                return _values.Dequeue();
            }
        }
    }
}
