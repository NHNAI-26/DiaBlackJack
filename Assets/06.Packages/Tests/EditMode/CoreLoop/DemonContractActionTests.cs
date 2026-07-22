using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class DemonContractActionTests
    {
        [Test]
        public void DC02_U01_StrictSoulGateAndAvailabilityLookupAreAtomic()
        {
            CoreLoopBattle battle = CreateStartedBattle(playerCurrentSoul: 1);

            DemonContractAvailability first = battle.PlayerDemonContractAvailability;
            DemonContractAvailability second = battle.PlayerDemonContractAvailability;

            Assert.That(first.CanBegin, Is.False);
            Assert.That(first.FailureReason,
                Is.EqualTo(DemonContractFailureReason.InsufficientSoul));
            Assert.That(first.SoulCost, Is.EqualTo(1));
            Assert.That(first.SoulAfterCost, Is.Zero);
            Assert.That(first.RemainingBaseUses, Is.EqualTo(1));
            Assert.That(second.FailureReason, Is.EqualTo(first.FailureReason));
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.False);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.DrawCount, Is.EqualTo(4));
            Assert.That(battle.PlayerDemonDeck.DiscardCount, Is.Zero);
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.Zero);
        }

        [Test]
        public void DC02_U02_BeginPaysOnceAndCreatesExactlyThreeOptions()
        {
            CoreLoopBattle battle = CreateStartedBattle(playerCurrentSoul: 2);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);

            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(1));
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.EqualTo(1));
            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.PlayerResolvingDemonContract));
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.InteractionId, Is.GreaterThan(0));
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.ChooseContract));
            Assert.That(pending.ContractKind, Is.Null);
            Assert.That(pending.Options.Count,
                Is.EqualTo(DemonContractDeck.CandidateCount));
            Assert.That(pending.Options.Select(option => option.OptionId).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(pending.Options.Select(option => option.ContractCardId).Distinct().Count(),
                Is.EqualTo(3));
            Assert.That(battle.PlayerDemonDeck.AvailableCardCount, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.CardsInPlayCount, Is.EqualTo(3));
            Assert.That(battle.PlayerDemonContractAvailability.FailureReason,
                Is.EqualTo(DemonContractFailureReason.PendingInteraction));
            Assert.That(battle.PlayerDemonContractAvailability.RemainingBaseUses, Is.Zero);
        }

        [Test]
        public void DC02_U03_PendingChoiceLocksAllOtherPlayerInputs()
        {
            CoreLoopBattle battle = CreateStartedBattle(playerCurrentSoul: 2);
            int manualCardId = battle.Player.Hand.Cards[0].Id;
            Assert.That(battle.CanUsePlayerCard(manualCardId), Is.True,
                "The selected card must be usable before the contract choice opens.");

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.False);
            Assert.That(battle.TryPlayerStand(), Is.False);
            Assert.That(battle.TryBeginPlayerChange(), Is.False);
            Assert.That(battle.TryBeginPlayerCardUse(manualCardId), Is.False);
            Assert.That(battle.TryResolvePlayerCardChoice(0), Is.False);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.False);
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Not.Null);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(1));
        }

        [Test]
        public void DC02_U04_StaleInteractionAndUnknownOptionLeaveChoiceUntouched()
        {
            CoreLoopBattle battle = CreateStartedBattle(playerCurrentSoul: 2);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            int validOptionId = pending.Options[0].OptionId;

            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId + 1,
                    validOptionId),
                Is.False);
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    optionId: 999),
                Is.False);

            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.SameAs(pending));
            Assert.That(battle.ActivePlayerDemonContracts, Is.Empty);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.DrawCount, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.DiscardCount, Is.Zero);
            Assert.That(battle.PlayerDemonDeck.CardsInPlayCount, Is.EqualTo(3));
            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.PlayerResolvingDemonContract));
        }

        [Test]
        public void DC02_U05_ResolveActivatesOneDiscardsTwoAndRunsEnemyOnce()
        {
            var enemyPolicy = new CountingStandPolicy();
            var handler = new TrackingDemonContractHandler(
                DemonContractKind.Satan,
                soulDamage: 0);
            CoreLoopBattle battle = CreateStartedBattle(
                playerCurrentSoul: 2,
                enemyPolicy,
                CreateDemonDeck(DemonContractKind.Satan),
                new DemonContractResolver(handler));
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption selected = pending.Options[0];

            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    selected.OptionId),
                Is.True);

            Assert.That(handler.ActivationCount, Is.EqualTo(1));
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.ActivePlayerDemonContracts.Count, Is.EqualTo(1));
            ActiveDemonContract active = battle.ActivePlayerDemonContracts[0];
            Assert.That(active.SourceCardId, Is.EqualTo(selected.ContractCardId));
            Assert.That(active.Kind, Is.EqualTo(DemonContractKind.Satan));
            Assert.That(active.RuntimeState, Is.SameAs(handler.LastRuntimeState));
            Assert.That(battle.PlayerDemonDeck.DrawCount, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.DiscardCount, Is.EqualTo(2));
            Assert.That(battle.PlayerDemonDeck.AvailableCardCount, Is.EqualTo(3));
            Assert.That(battle.PlayerDemonDeck.CardsInPlayCount, Is.EqualTo(1));
            Assert.That(battle.LastDemonContractResult.ActiveContract, Is.SameAs(active));
            Assert.That(battle.LastDemonContractResult.PaidSoulCost, Is.EqualTo(1));
            Assert.That(battle.LastDemonContractResult.SoulAfterBaseCost, Is.EqualTo(1));
            Assert.That(battle.LastDemonContractResult.EndedBattle, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void DC02_U06_DuplicateResolutionAndSecondBaseContractAreRejected()
        {
            var enemyPolicy = new CountingStandPolicy();
            CoreLoopBattle battle = CreateStartedBattle(
                playerCurrentSoul: 3,
                enemyPolicy);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            int optionId = pending.Options[0].OptionId;
            Assert.That(
                battle.TryResolvePlayerDemonContract(pending.InteractionId, optionId),
                Is.True);

            int soul = battle.Player.Soul.Current;
            int activeCount = battle.ActivePlayerDemonContracts.Count;
            Assert.That(
                battle.TryResolvePlayerDemonContract(pending.InteractionId, optionId),
                Is.False);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.False);
            Assert.That(battle.PlayerDemonContractAvailability.FailureReason,
                Is.EqualTo(DemonContractFailureReason.BaseUseLimitReached));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(soul));
            Assert.That(battle.ActivePlayerDemonContracts.Count, Is.EqualTo(activeCount));
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
        }

        [Test]
        public void DC02_U07_SameDemonPhysicalInstancesKeepSeparateRuntimeState()
        {
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(DemonContractCatalog.SatanKey);
            var first = new ActiveDemonContract(
                new DemonContractCard(11, definition),
                CombatantSide.Player,
                new EmptyDemonContractRuntimeState());
            var second = new ActiveDemonContract(
                new DemonContractCard(12, definition),
                CombatantSide.Player,
                new EmptyDemonContractRuntimeState());

            Assert.That(first.Kind, Is.EqualTo(second.Kind));
            Assert.That(first.SourceCardId, Is.Not.EqualTo(second.SourceCardId));
            Assert.That(first.RuntimeState, Is.Not.SameAs(second.RuntimeState));
        }

        [Test]
        public void DC02_U08_ActivationPenaltyAtZeroEndsBattleWithoutEnemyOrBust()
        {
            var enemyPolicy = new CountingStandPolicy();
            var handler = new TrackingDemonContractHandler(
                DemonContractKind.Satan,
                soulDamage: 1);
            CoreLoopBattle battle = CreateStartedBattle(
                playerCurrentSoul: 2,
                enemyPolicy,
                CreateDemonDeck(DemonContractKind.Satan),
                new DemonContractResolver(handler));
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);

            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.LastResolution, Is.Null,
                "Soul depletion is not a numeric or card bust round result.");
            Assert.That(battle.LastDemonContractResult.OwnerSoulDepleted, Is.True);
            Assert.That(battle.LastDemonContractResult.EndedBattle, Is.True);
            Assert.That(enemyPolicy.DecisionCount, Is.Zero);
        }

        [Test]
        public void DC02_U09_InsufficientCandidatesRejectWithoutPaying()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerCurrentSoul: 12,
                demonDeck: CreateDemonDeck(DemonContractKind.Satan, count: 2));

            Assert.That(battle.PlayerDemonContractAvailability.CanBegin, Is.False);
            Assert.That(battle.PlayerDemonContractAvailability.FailureReason,
                Is.EqualTo(DemonContractFailureReason.InsufficientCandidates));
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.False);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12));
            Assert.That(battle.PlayerDemonDeck.DrawCount, Is.EqualTo(2));
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.Zero);
        }

        [Test]
        public void DC02_U10_PendingInteractionRejectsDuplicateOptionIdentity()
        {
            var first = new DemonContractOption(0, 10, null, "첫째");
            var duplicateOption = new DemonContractOption(0, 11, null, "둘째");
            var duplicateCard = new DemonContractOption(1, 10, null, "셋째");
            var third = new DemonContractOption(2, 12, null, "넷째");

            Assert.Throws<ArgumentException>(() =>
                new PendingDemonContractInteraction(
                    1,
                    DemonContractInteractionKind.ChooseContract,
                    null,
                    new[] { first, duplicateOption, third },
                    "선택"));
            Assert.Throws<ArgumentException>(() =>
                new PendingDemonContractInteraction(
                    1,
                    DemonContractInteractionKind.ChooseContract,
                    null,
                    new[] { first, duplicateCard, third },
                    "선택"));
        }

        [Test]
        public void DC02_U11_CoreLoopSessionForwardsContractInteractionIdentity()
        {
            var enemyPolicy = new CountingStandPolicy();
            var session = new CoreLoopSession(() => CreateBattle(
                playerCurrentSoul: 2,
                enemyPolicy));

            Assert.That(session.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                session.Battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                session.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);
            Assert.That(session.Battle.ActivePlayerDemonContracts.Count, Is.EqualTo(1));
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
        }

        private static CoreLoopBattle CreateStartedBattle(
            int playerCurrentSoul,
            IEnemyBehaviorPolicy enemyPolicy = null,
            DemonContractDeck demonDeck = null,
            DemonContractResolver demonContractResolver = null)
        {
            CoreLoopBattle battle = CreateBattle(
                playerCurrentSoul,
                enemyPolicy,
                demonDeck,
                demonContractResolver);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateBattle(
            int playerCurrentSoul,
            IEnemyBehaviorPolicy enemyPolicy = null,
            DemonContractDeck demonDeck = null,
            DemonContractResolver demonContractResolver = null)
        {
            return new CoreLoopBattle(
                CreateManualCardDeck(seed: 31),
                CreatePlainDeck(seed: 47),
                playerMaximumSoul: 12,
                playerCurrentSoul,
                enemyMaximumSoul: 3,
                enemyPolicy ?? new CountingStandPolicy(),
                CardEffectResolver.CreateDefault(),
                demonDeck ?? CreateDemonDeck(),
                demonContractResolver ?? DemonContractResolver.CreateDefault());
        }

        private static BlackjackDeck CreateManualCardDeck(int seed)
        {
            CardDefinition definition =
                CardDefinitionCatalog.GetByKey("crystal-orb-5");
            return new BlackjackDeck(
                Enumerable.Range(0, 8)
                    .Select(id => new BlackjackCard(id, definition)),
                seed);
        }

        private static BlackjackDeck CreatePlainDeck(int seed)
        {
            return new BlackjackDeck(
                Enumerable.Range(100, 8)
                    .Select(id => new BlackjackCard(id, rank: 2)),
                seed);
        }

        private static DemonContractDeck CreateDemonDeck(
            DemonContractKind? repeatedKind = null,
            int count = 4)
        {
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            DemonContractKind[] defaultKinds =
            {
                DemonContractKind.Satan,
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon,
                DemonContractKind.Leviathan
            };
            var cards = new List<DemonContractCard>(count);
            for (int i = 0; i < count; i++)
            {
                DemonContractKind kind = repeatedKind ?? defaultKinds[i % defaultKinds.Length];
                string key = GetDefinitionKey(kind);
                cards.Add(new DemonContractCard(i, catalog.GetByKey(key)));
            }

            return new DemonContractDeck(cards, seed: 73);
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
                return new EnemyDecision(EnemyActionType.Stand, "dc02-test-stand");
            }
        }

        private sealed class TrackingDemonContractHandler : IDemonContractHandler
        {
            private readonly int _soulDamage;

            public TrackingDemonContractHandler(
                DemonContractKind kind,
                int soulDamage)
            {
                Kind = kind;
                _soulDamage = soulDamage;
            }

            public int ActivationCount { get; private set; }

            public DemonContractKind Kind { get; }

            public DemonContractRuntimeState LastRuntimeState { get; private set; }

            public DemonContractRuntimeState Activate(DemonContractContext context)
            {
                ActivationCount++;
                context.ApplyOwnerSoulDamage(_soulDamage);
                LastRuntimeState = new TrackingRuntimeState(ActivationCount);
                return LastRuntimeState;
            }
        }

        private sealed class TrackingRuntimeState : DemonContractRuntimeState
        {
            public TrackingRuntimeState(int activationOrdinal)
            {
                ActivationOrdinal = activationOrdinal;
            }

            public int ActivationOrdinal { get; }
        }
    }
}
