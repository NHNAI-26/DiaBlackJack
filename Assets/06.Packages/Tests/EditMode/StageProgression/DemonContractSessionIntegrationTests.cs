using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    public sealed class DemonContractSessionIntegrationTests
    {
        [Test]
        public void DC02_I01_StageSessionForwardsChoiceWithoutMutatingRunDeck()
        {
            RunProgress progress = CreateProgress(maximumSoul: 12);
            var enemyPolicy = new CountingStandPolicy();
            var session = new StageProgressionSession(
                progress,
                (stage, player) => CreateBattle(
                    stage,
                    player,
                    DemonContractResolver.CreateDefault(),
                    enemyPolicy));
            Assert.That(session.TryStartRun(), Is.True);

            Assert.That(session.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                session.Battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                session.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);

            Assert.That(session.Battle.ActivePlayerDemonContracts.Count, Is.EqualTo(1));
            Assert.That(session.Battle.Player.Soul.Current, Is.EqualTo(11));
            Assert.That(progress.Player.CurrentSoul, Is.EqualTo(12),
                "Persistent soul synchronizes only after the battle reaches a final outcome.");
            Assert.That(progress.Player.DemonDeck.Count, Is.EqualTo(4));
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.InBattle));
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
        }

        [Test]
        public void DC02_I02_ContractPenaltyDefeatSynchronizesRunExactlyOnce()
        {
            RunProgress progress = CreateProgress(
                maximumSoul: 2,
                repeatedKind: DemonContractKind.Satan);
            var enemyPolicy = new CountingStandPolicy();
            var fatalHandler = new FatalContractHandler(DemonContractKind.Satan);
            var resolver = new DemonContractResolver(fatalHandler);
            var session = new StageProgressionSession(
                progress,
                (stage, player) => CreateBattle(
                    stage,
                    player,
                    resolver,
                    enemyPolicy));
            Assert.That(session.TryStartRun(), Is.True);
            Assert.That(session.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                session.Battle.PendingPlayerDemonContractInteraction;

            Assert.That(
                session.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);

            Assert.That(session.Battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(session.Battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(progress.Player.CurrentSoul, Is.Zero);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
            Assert.That(fatalHandler.ActivationCount, Is.EqualTo(1));
            Assert.That(enemyPolicy.DecisionCount, Is.Zero);

            Assert.That(
                session.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.False);
            Assert.That(progress.Player.CurrentSoul, Is.Zero);
            Assert.That(progress.State, Is.EqualTo(StageProgressionState.RunDefeat));
            Assert.That(fatalHandler.ActivationCount, Is.EqualTo(1));
        }

        private static RunProgress CreateProgress(
            int maximumSoul,
            DemonContractKind? repeatedKind = null)
        {
            var normalDeck = Enumerable.Range(0, 8)
                .Select(id => new RunCardDefinition(id, rank: 2))
                .ToArray();
            IEnumerable<RunDemonDefinition> demonDeck = CreateRunDemonDeck(repeatedKind);
            var player = new PlayerRunState(
                maximumSoul,
                maximumSoul,
                normalDeck,
                demonDeck);
            var stages = new[]
            {
                new StageDefinition(
                    "dc02-normal",
                    "계약 통합 테스트",
                    StageKind.NormalCombat,
                    enemyMaximumSoul: 3,
                    playerDeckSeed: 13,
                    enemyDeckSeed: 17),
                new StageDefinition(
                    "dc02-boss",
                    "계약 통합 보스",
                    StageKind.FinalBossCombat,
                    enemyMaximumSoul: 7,
                    playerDeckSeed: 19,
                    enemyDeckSeed: 23)
            };
            return new RunProgress(stages, player);
        }

        private static IEnumerable<RunDemonDefinition> CreateRunDemonDeck(
            DemonContractKind? repeatedKind)
        {
            DemonContractKind[] kinds =
            {
                DemonContractKind.Satan,
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon,
                DemonContractKind.Leviathan
            };

            for (int i = 0; i < kinds.Length; i++)
            {
                DemonContractKind kind = repeatedKind ?? kinds[i];
                yield return new RunDemonDefinition(i, GetDefinitionKey(kind));
            }
        }

        private static CoreLoopBattle CreateBattle(
            StageDefinition stage,
            PlayerRunState player,
            DemonContractResolver resolver,
            IEnemyBehaviorPolicy enemyPolicy)
        {
            var demonCards = new List<DemonContractCard>(player.DemonDeck.Count);
            foreach (RunDemonDefinition runCard in player.DemonDeck)
            {
                demonCards.Add(new DemonContractCard(
                    runCard.Id,
                    DemonContractCatalog.Default.GetByKey(runCard.DefinitionKey)));
            }

            return new CoreLoopBattle(
                CreatePlainDeck(0, stage.PlayerDeckSeed),
                CreatePlainDeck(100, stage.EnemyDeckSeed),
                player.MaximumSoul,
                player.CurrentSoul,
                stage.EnemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                new DemonContractDeck(demonCards, seed: 29),
                resolver);
        }

        private static BlackjackDeck CreatePlainDeck(int firstId, int seed)
        {
            return new BlackjackDeck(
                Enumerable.Range(firstId, 8)
                    .Select(id => new BlackjackCard(id, rank: 2)),
                seed);
        }

        private static string GetDefinitionKey(DemonContractKind kind)
        {
            switch (kind)
            {
                case DemonContractKind.Satan:
                    return DemonContractCatalog.SatanKey;
                case DemonContractKind.Belphegor:
                    return DemonContractCatalog.BelphegorKey;
                case DemonContractKind.Mammon:
                    return DemonContractCatalog.MammonKey;
                case DemonContractKind.Leviathan:
                    return DemonContractCatalog.LeviathanKey;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private sealed class CountingStandPolicy : IEnemyBehaviorPolicy
        {
            public int DecisionCount { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                DecisionCount++;
                return new EnemyDecision(EnemyActionType.Stand, "dc02-stage-test-stand");
            }
        }

        private sealed class FatalContractHandler : IDemonContractHandler
        {
            public FatalContractHandler(DemonContractKind kind)
            {
                Kind = kind;
            }

            public int ActivationCount { get; private set; }

            public DemonContractKind Kind { get; }

            public DemonContractRuntimeState Activate(DemonContractContext context)
            {
                ActivationCount++;
                context.ApplyOwnerSoulDamage(context.OwnerSoul);
                return new FatalContractRuntimeState();
            }
        }

        private sealed class FatalContractRuntimeState : DemonContractRuntimeState
        {
        }
    }
}
