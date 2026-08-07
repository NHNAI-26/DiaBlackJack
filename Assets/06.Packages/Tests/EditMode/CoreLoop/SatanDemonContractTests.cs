using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class SatanDemonContractTests
    {
        [Test]
        public void DCR02_U01_ActivationStartsAtFourUpperWithoutCreatingPowerCard()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                playerRanks: LowRanks(10),
                enemyRanks: new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            int totalCardCount = battle.Player.Deck.TotalCardCount;
            int handCount = battle.Player.Hand.Count;
            int? activationCount = null;
            SatanContractFace? activationFace = null;
            battle.Stepped += () =>
            {
                if (battle.ActivePlayerDemonContracts.Count != 1 ||
                    activationCount.HasValue)
                {
                    return;
                }

                SatanRuntimeState state = GetSatanState(battle);
                activationCount = state.RemainingDoomCount;
                activationFace = state.CurrentFace;
            };

            ActivateSatan(battle);

            Assert.That(activationCount, Is.EqualTo(4));
            Assert.That(activationFace, Is.EqualTo(SatanContractFace.Upper));
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(totalCardCount));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(handCount));
            Assert.That(CardDefinitionCatalog.All.Count, Is.EqualTo(15));
        }

        [Test]
        public void DCR02_U02_DoomCostRepeatsAtEveryOwnerTurnStartAtZero()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(14),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);
            SatanRuntimeState state = GetSatanState(battle);

            Assert.That(state.RemainingDoomCount, Is.EqualTo(3));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(11));

            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(HitSkippingSatanChoice(battle), Is.True);

            Assert.That(state.RemainingDoomCount, Is.Zero);
            Assert.That(state.DoomPenaltyActive, Is.True);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(9));
            Assert.That(battle.ActivePlayerDemonContracts.Single().RuntimeState,
                Is.SameAs(state));

            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(state.RemainingDoomCount, Is.Zero);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(7));
        }

        [Test]
        public void DCR02_U03_DoomCostAtZeroSoulEndsBattleImmediately()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(12),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy(),
                playerCurrentSoul: 3);
            ActivateSatan(battle);

            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(HitSkippingSatanChoice(battle), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.LastResolution, Is.Null);
        }

        [Test]
        public void DCR02_U04_StandAndAllBustPreventionRemainAfterDoomCost()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                new[] { 10, 2, 10, 10, 2, 2, 2, 2, 2, 2, 2, 2 },
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);

            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(HitSkippingSatanChoice(battle), Is.True);

            SatanRuntimeState state = GetSatanState(battle);
            Assert.That(state.RemainingDoomCount, Is.Zero);
            Assert.That(battle.CanPlayerStand, Is.False);
            Assert.That(battle.TryPlayerStand(), Is.False);
            Assert.That(battle.Player.VisibleHandValue.IsBust, Is.True);
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.ActivePlayerDemonContracts, Has.Count.EqualTo(1));
        }

        [Test]
        public void DCR09_U01_RegularActionsKeepCurrentFace()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(10),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);
            SatanRuntimeState state = GetSatanState(battle);

            Assert.That(state.CurrentFace, Is.EqualTo(SatanContractFace.Upper));
            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(state.CurrentFace, Is.EqualTo(SatanContractFace.Upper));

            FailUpperPower(battle);
            Assert.That(state.CurrentFace, Is.EqualTo(SatanContractFace.Lower));

            Assert.That(HitSkippingSatanChoice(battle), Is.True);
            Assert.That(state.CurrentFace, Is.EqualTo(SatanContractFace.Lower));
        }

        [Test]
        public void DCR09_U02_InvalidAbilityInputPreservesTurnAndFace()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(10),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);
            SatanRuntimeState state = GetSatanState(battle);

            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.SatanTurnStartChoice));

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId + 1,
                SatanDemonContractHandler.UseAbilityOptionId), Is.False);
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                99), Is.False);

            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.PlayerResolvingDemonContract));
            Assert.That(battle.PendingPlayerDemonContractInteraction,
                Is.SameAs(pending));
            Assert.That(state.CurrentFace, Is.EqualTo(SatanContractFace.Upper));

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                SatanDemonContractHandler.SkipAbilityOptionId), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
        }

        [TestCase(14, EnemyActionType.Hit)]
        [TestCase(19, EnemyActionType.DemonContract)]
        public void DCR09_U03_CultistComparesSatanWithNormalAction(
            int ownTotal,
            EnemyActionType expectedAction)
        {
            EnemyDecision decision = new CultistEnemyPolicy().Decide(
                CreateActiveSatanObservation(ownTotal, enemyMaximumSoul: 5));

            Assert.That(decision.ActionType, Is.EqualTo(expectedAction));
        }

        [Test]
        public void DCR02_U06_UpperFaceDeclaresDistinctNumbersAndBustsOnMatch()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(10),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy(),
                enemyMaximumSoul: 3);
            ActivateSatan(battle);

            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);
            PendingDemonContractInteraction first =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(first.Kind,
                Is.EqualTo(DemonContractInteractionKind.SatanDeclareFirstNumber));
            Assert.That(first.Options, Has.Count.EqualTo(10));
            Assert.That(ResolveNumber(battle, first, 3), Is.True);

            PendingDemonContractInteraction second =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(second.Kind,
                Is.EqualTo(DemonContractInteractionKind.SatanDeclareSecondNumber));
            Assert.That(second.ContextNumericValue, Is.EqualTo(3));
            Assert.That(second.Options.Any(option => option.NumericValue == 3), Is.False);
            Assert.That(ResolveNumber(battle, second, 7), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(((SatanRuntimeState)battle.ActivePlayerDemonContracts
                .Single().RuntimeState).CurrentFace,
                Is.EqualTo(SatanContractFace.Lower));
        }

        [Test]
        public void DCR02_U07_UpperFaceFailureConsumesActionWithoutHiddenRankLeak()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(10),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);

            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);
            PendingDemonContractInteraction first =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(ResolveNumber(battle, first, 3), Is.True);
            PendingDemonContractInteraction second =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(ResolveNumber(battle, second, 4), Is.True);

            Assert.That(battle.LastDemonContractEffectResult.Triggered, Is.True);
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget, Is.Null);
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(GetSatanState(battle).CurrentFace,
                Is.EqualTo(SatanContractFace.Lower));
            Assert.That(first.Options.Select(option => option.NumericValue),
                Is.EquivalentTo(Enumerable.Range(1, 10).Select(number => (int?)number)));
        }

        [Test]
        public void DCR02_U13_AtomicUpperFaceNumbersResolveTogetherOnMatch()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(10),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy(),
                enemyMaximumSoul: 3);
            ActivateSatan(battle);

            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(battle.TryResolvePlayerSatanNumbers(
                pending.InteractionId,
                3,
                7), Is.True);

            // The bust ends the round and a new one begins immediately; Satan is still
            // active, so the new round's own owner-turn start legitimately re-offers a
            // turn-start choice here (same as DCR02_U06's non-atomic sibling, which
            // doesn't assert this is null either) — it is not leftover from this
            // resolution.
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(GetSatanState(battle).CurrentFace,
                Is.EqualTo(SatanContractFace.Lower));
        }

        [Test]
        public void DCR02_U14_InvalidAtomicNumbersPreservePendingInteraction()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(10),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);

            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            SatanRuntimeState state = GetSatanState(battle);
            DemonContractEffectResult previousResult =
                battle.LastDemonContractEffectResult;

            Assert.That(battle.TryResolvePlayerSatanNumbers(
                pending.InteractionId,
                3,
                3), Is.False);
            Assert.That(battle.TryResolvePlayerSatanNumbers(
                pending.InteractionId,
                0,
                4), Is.False);
            Assert.That(battle.TryResolvePlayerSatanNumbers(
                pending.InteractionId + 1,
                3,
                4), Is.False);

            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.PlayerResolvingDemonContract));
            Assert.That(battle.PendingPlayerDemonContractInteraction,
                Is.SameAs(pending));
            Assert.That(state.CurrentFace, Is.EqualTo(SatanContractFace.Upper));
            Assert.That(battle.LastDemonContractEffectResult,
                Is.SameAs(previousResult));
        }

        [Test]
        public void DCR02_U08_LowerFaceForcesSafeHitThenDiscardsNewCard()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(12),
                new[] { 10, 7, 2, 3, 4, 5 },
                new StandPolicy());
            ActivateSatan(battle);
            FailUpperPower(battle);
            int enemyHandCount = battle.Enemy.Hand.Count;

            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);

            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(enemyHandCount));
            Assert.That(battle.Enemy.Deck.GetDiscardedCards().Select(card => card.Rank),
                Does.Contain(2));
            Assert.That(GetSatanState(battle).CurrentFace,
                Is.EqualTo(SatanContractFace.Upper));
        }

        [Test]
        public void DCR02_U09_LowerFaceResumesAfterAutomaticCardChoiceOnce()
        {
            var automaticHandler = new WaitingAutomaticHandler();
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(12),
                new BlackjackCard[]
                {
                    new BlackjackCard(100, 10),
                    new BlackjackCard(101, 7),
                    new BlackjackCard(102,
                        CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey)),
                    new BlackjackCard(103, 3)
                },
                new StandPolicy(),
                automaticCardEffectResolver:
                    new AutomaticCardEffectResolver(automaticHandler));
            ActivateSatan(battle);
            FailUpperPower(battle);

            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(GetSatanState(battle).CurrentFace,
                Is.EqualTo(SatanContractFace.Lower));

            Assert.That(battle.TryResolvePlayerAutomaticCardChoice(
                pending.InteractionId,
                WaitingAutomaticHandler.ResolveOptionId), Is.True);

            Assert.That(automaticHandler.ResolveCount, Is.EqualTo(1));
            Assert.That(battle.Enemy.Hand.Contains(102), Is.False);
            Assert.That(GetSatanState(battle).CurrentFace,
                Is.EqualTo(SatanContractFace.Upper));
        }

        [Test]
        public void DCR02_U10_LowerFaceDoesNotCompleteTwiceAfterEnemyAutomaticChoice()
        {
            var automaticHandler = new WaitingAutomaticHandler(
                CombatantSide.Enemy);
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(12),
                new BlackjackCard[]
                {
                    new BlackjackCard(100, 10),
                    new BlackjackCard(101, 7),
                    new BlackjackCard(102,
                        CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey)),
                    new BlackjackCard(103, 3)
                },
                new StandPolicy(),
                automaticCardEffectResolver:
                    new AutomaticCardEffectResolver(automaticHandler),
                enemyAutomaticCardDecisionPolicy:
                    DefaultAutomaticCardDecisionPolicy.Instance);
            ActivateSatan(battle);
            FailUpperPower(battle);

            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);

            Assert.That(automaticHandler.ResolveCount, Is.EqualTo(1));
            Assert.That(battle.PendingPlayerAutomaticInteraction, Is.Null);
            Assert.That(GetSatanState(battle).CurrentFace,
                Is.EqualTo(SatanContractFace.Upper));
        }

        [Test]
        public void DCR02_U11_EnemyOwnedSatanUsesPublicCandidatesSymmetrically()
        {
            var policy = new EnemySatanContractActionPolicy();
            CoreLoopBattle battle = CreateStartedBattle(
                CreatePlainDeck(new[] { 10, 2, 2, 2, 2, 2 }),
                CreatePlainDeck(LowRanks(12), startId: 100),
                policy,
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0),
                playerCurrentSoul: 12,
                enemyMaximumSoul: 5,
                enemyDemonDeck: CreateRepeatedSatanDeck());

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.ActiveEnemyDemonContracts, Has.Count.EqualTo(1));
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(policy.ObservedHiddenRanks, Is.False);
        }

        [Test]
        public void DCR02_U12_PresentationShowsDoomCountAndContractFace()
        {
            CoreLoopBattle battle = CreateSatanBattle(
                LowRanks(10),
                new[] { 10, 7, 2, 2, 2, 2 },
                new StandPolicy());
            ActivateSatan(battle);

            DemonContractPanelViewModel model = DemonContractPresenter.Create(battle);

            Assert.That(model.ActiveContracts.Single(), Does.Contain("종말 카운트 3"));
            Assert.That(model.ActiveContracts.Single(), Does.Contain("정방향"));
            Assert.That(model.ActiveContracts.Single(), Does.Not.Contain("권능"));
        }

        private static CoreLoopBattle CreateSatanBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            IEnemyBehaviorPolicy enemyPolicy,
            int playerCurrentSoul = 12,
            int enemyMaximumSoul = 3)
        {
            return CreateSatanBattle(
                playerRanks,
                CreateCards(enemyRanks, startId: 100),
                enemyPolicy,
                playerCurrentSoul,
                enemyMaximumSoul);
        }

        private static CoreLoopBattle CreateSatanBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<BlackjackCard> enemyCards,
            IEnemyBehaviorPolicy enemyPolicy,
            int playerCurrentSoul = 12,
            int enemyMaximumSoul = 3,
            AutomaticCardEffectResolver automaticCardEffectResolver = null,
            IAutomaticCardDecisionPolicy enemyAutomaticCardDecisionPolicy = null)
        {
            return CreateStartedBattle(
                CreatePlainDeck(playerRanks),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                enemyPolicy,
                CreateRepeatedSatanDeck(),
                playerCurrentSoul,
                enemyMaximumSoul,
                automaticCardEffectResolver: automaticCardEffectResolver,
                enemyAutomaticCardDecisionPolicy:
                    enemyAutomaticCardDecisionPolicy);
        }

        private static CoreLoopBattle CreateStartedBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractDeck playerDemonDeck,
            int playerCurrentSoul,
            int enemyMaximumSoul,
            DemonContractDeck enemyDemonDeck = null,
            AutomaticCardEffectResolver automaticCardEffectResolver = null,
            IAutomaticCardDecisionPolicy enemyAutomaticCardDecisionPolicy = null)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                playerDemonDeck,
                DemonContractResolver.CreateDefault(),
                enemyDemonDeck,
                automaticCardEffectResolver,
                enemyAutomaticCardDecisionPolicy);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static void ActivateSatan(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                pending.Options[0].OptionId), Is.True);
            Assert.That(battle.ActivePlayerDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Satan));

            // Signing a contract is itself a full player action; since the newly-signed
            // Satan contract can't stand and the enemy immediately stands too (this
            // file's StandPolicy), that cascades straight into a fresh player-turn
            // start where Satan's own ability is already offered. Skip it here so every
            // test sees the same clean PlayerTurn state right after activation as it
            // did before Satan had a turn-start choice at all.
            Assert.That(SkipPendingSatanTurnStartChoice(battle), Is.True);
        }

        // Hits once (which, if this is not the same turn Satan was signed on, first
        // resolves the pending Satan turn-start choice by skipping it) so ordinary
        // hitting tests are unaffected by the new once-per-turn choice gate.
        private static bool HitSkippingSatanChoice(CoreLoopBattle battle)
        {
            SkipPendingSatanTurnStartChoice(battle);
            return battle.TryPlayerHit();
        }

        private static bool SkipPendingSatanTurnStartChoice(CoreLoopBattle battle)
        {
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            if (pending == null ||
                pending.Kind != DemonContractInteractionKind.SatanTurnStartChoice)
            {
                return false;
            }

            return battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                SatanDemonContractHandler.SkipAbilityOptionId);
        }

        // Hits once to reach a fresh owner-turn start (Satan's ability can now only be
        // offered there), then selects "use ability" on the resulting turn-start choice
        // — asserting that one is actually offered.
        //
        // Any earlier action in this same round (a plain hit, or a prior Satan choice)
        // can itself have looped straight back into a fresh turn-start offer once the
        // enemy is already standing (CompletePlayerActionAndRunEnemyTurn's
        // already-standing enemy short-circuits back into BeginPlayerTurn within the
        // same call). Skip any such leftover choice first so this helper's own hit
        // starts from a clean PlayerTurn state.
        private static bool BeginSatanAbilityViaTurnStart(CoreLoopBattle battle)
        {
            SkipPendingSatanTurnStartChoice(battle);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.SatanTurnStartChoice));
            return battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                SatanDemonContractHandler.UseAbilityOptionId);
        }

        private static bool ResolveNumber(
            CoreLoopBattle battle,
            PendingDemonContractInteraction pending,
            int number)
        {
            DemonContractOption option = pending.Options.Single(
                candidate => candidate.NumericValue == number);
            return battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                option.OptionId);
        }

        // Uses the ability at the next turn-start choice and declares (3, 4) — a
        // guaranteed miss against every enemy setup used in this file (their hidden
        // card is never 3 or 4) — flipping the contract to its lower face.
        private static void FailUpperPower(CoreLoopBattle battle)
        {
            Assert.That(BeginSatanAbilityViaTurnStart(battle), Is.True);
            Assert.That(ResolveNumber(
                battle,
                battle.PendingPlayerDemonContractInteraction,
                3), Is.True);
            Assert.That(ResolveNumber(
                battle,
                battle.PendingPlayerDemonContractInteraction,
                4), Is.True);
            Assert.That(GetSatanState(battle).CurrentFace,
                Is.EqualTo(SatanContractFace.Lower));
        }

        private static SatanRuntimeState GetSatanState(CoreLoopBattle battle)
        {
            return (SatanRuntimeState)battle.ActivePlayerDemonContracts
                .Single(contract => contract.Kind == DemonContractKind.Satan)
                .RuntimeState;
        }

        private static DemonContractDeck CreateRepeatedSatanDeck()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default.GetByKey(
                DemonContractCatalog.SatanKey);
            return new DemonContractDeck(
                Enumerable.Range(0, 4).Select(id =>
                    new DemonContractCard(id, definition)),
                seed: 73);
        }

        private static EnemyObservation CreateActiveSatanObservation(
            int ownTotal,
            int enemyMaximumSoul)
        {
            EnemyActionCandidate hit = new EnemyActionCandidate(
                EnemyActionType.Hit);
            EnemyActionCandidate satan = new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractKind: DemonContractKind.Satan,
                demonContractDefinitionKey: DemonContractCatalog.SatanKey,
                demonContractSourceCardId: 42);
            return new EnemyObservation(
                new HandValue(ownTotal),
                Array.Empty<EnemyOwnedCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 1,
                new SoulObservation(12, 12),
                new SoulObservation(enemyMaximumSoul, enemyMaximumSoul),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 8,
                playerDeckAvailableCount: 8,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                new[] { satan, hit },
                Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: null,
                decisionSeed: 73);
        }

        private static IReadOnlyList<int> LowRanks(int count)
        {
            return Enumerable.Repeat(2, count).ToArray();
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(
            IReadOnlyList<int> ranks,
            int startId)
        {
            return ranks.Select((rank, index) =>
                new BlackjackCard(startId + index, rank)).ToArray();
        }

        private static BlackjackDeck CreatePlainDeck(
            IReadOnlyList<int> ranks,
            int startId = 0)
        {
            return BlackjackDeck.CreateInDrawOrder(CreateCards(ranks, startId));
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate stand = observation.ActionCandidates.FirstOrDefault(
                    candidate => candidate.ActionType == EnemyActionType.Stand);
                EnemyActionCandidate fallback = stand ??
                    observation.ActionCandidates.First();
                return EnemyDecision.FromCandidate(fallback, "dcr02-stand-or-first");
            }
        }

        private sealed class WaitingAutomaticHandler : IAutomaticCardEffectHandler
        {
            public const int ResolveOptionId = 7;

            private readonly CombatantSide _decisionSide;

            public WaitingAutomaticHandler(
                CombatantSide decisionSide = CombatantSide.Player)
            {
                _decisionSide = decisionSide;
            }

            public CardEffectKind EffectKind => CardEffectKind.Poison;

            public int ResolveCount { get; private set; }

            public AutomaticCardEffectStep Begin(AutomaticCardEffectContext context)
            {
                return AutomaticCardEffectStep.AwaitChoice(
                    _decisionSide,
                    AutomaticCardChoiceKind.PoisonDecision,
                    "Resolve the forced automatic card.",
                    new[]
                    {
                        new AutomaticCardChoiceOption(ResolveOptionId, "Resolve")
                    });
            }

            public AutomaticCardEffectStep ResolveChoice(
                AutomaticCardEffectContext context,
                PendingAutomaticCardInteraction pendingInteraction,
                AutomaticCardChoiceOption selectedOption)
            {
                ResolveCount++;
                return AutomaticCardEffectStep.Complete(
                    AutomaticCardSourceDisposition.RetainFaceUp);
            }
        }

        private sealed class EnemySatanContractActionPolicy : IEnemyBehaviorPolicy
        {
            private int _declarationCount;

            public bool ObservedHiddenRanks { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                ObservedHiddenRanks |= observation.PlayerFaceUpCards.Count > 0 &&
                    observation.PlayerFaceUpCards.Any(card => card.Rank == 10) &&
                    observation.PlayerHiddenCardCount == 0;

                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(action =>
                        action.ActionType == EnemyActionType.DemonContract &&
                        action.DemonContractInteractionKind ==
                            DemonContractInteractionKind.ChooseContract &&
                        action.DemonContractKind == DemonContractKind.Satan);

                if (candidate == null)
                {
                    candidate = observation.ActionCandidates.FirstOrDefault(action =>
                        action.ActionType == EnemyActionType.DemonContract &&
                        action.DemonContractInteractionKind ==
                            DemonContractInteractionKind.SatanTurnStartChoice &&
                        action.DemonContractOptionId ==
                            SatanDemonContractHandler.UseAbilityOptionId);
                }

                if (candidate == null)
                {
                    int desiredNumber = _declarationCount++ == 0 ? 1 : 2;
                    candidate = observation.ActionCandidates.FirstOrDefault(action =>
                        action.ActionType == EnemyActionType.DemonContract &&
                        action.DemonContractOptionNumericValue == desiredNumber);
                }

                candidate = candidate ?? observation.ActionCandidates.FirstOrDefault(action =>
                    action.ActionType == EnemyActionType.DemonContract &&
                    !action.DemonContractOptionId.HasValue) ??
                    observation.ActionCandidates.First();
                return EnemyDecision.FromCandidate(candidate, "dcr02-enemy-satan");
            }
        }
    }
}
