using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class AsmodeusAndAzazelDemonContractTests
    {
        [Test]
        public void DCR04_U09_AsmodeusSkipKeepsTheOwnersNormalAction()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 7, 2, 3, 4, 5, 6 }),
                PlainDeck(new[] { 4, 5, 2, 3, 4, 5 }, 100),
                new SequencePolicy(EnemyActionType.Hit),
                DemonContractKind.Asmodeus);

            ActivateFirstContract(battle);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(pending.Kind, Is.EqualTo(
                DemonContractInteractionKind.AsmodeusForceOpponentHit));
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                AsmodeusDemonContractHandler.SkipForcedHitOptionId), Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.CanPlayerAct, Is.True);
        }

        [Test]
        public void DCR04_U10_AsmodeusForceHitDrawsForOpponentWithoutSpendingAction()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 7, 2, 3, 4, 5, 6 }),
                PlainDeck(new[] { 4, 5, 2, 3, 4, 5 }, 100),
                new SequencePolicy(EnemyActionType.Hit),
                DemonContractKind.Asmodeus);
            ActivateFirstContract(battle);
            int enemyFaceUpCount = battle.Enemy.Hand.GetFaceUpCards().Count;
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                AsmodeusDemonContractHandler.ForceHitOptionId), Is.True);

            Assert.That(battle.Enemy.Hand.GetFaceUpCards(),
                Has.Count.EqualTo(enemyFaceUpCount + 1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.CanPlayerAct, Is.True);
            Assert.That(battle.LastPublicAction.SourceCardDefinitionKey,
                Is.EqualTo(DemonContractCatalog.AsmodeusKey));
        }

        [Test]
        public void DCR04_U11_AsmodeusDoesNotOfferForceHitAgainstStandingOpponent()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 7, 2, 3, 4, 5, 6 }),
                PlainDeck(new[] { 4, 5, 2, 3, 4, 5 }, 100),
                new StandPolicy(),
                DemonContractKind.Asmodeus);

            ActivateFirstContract(battle);

            Assert.That(battle.Enemy.IsStanding, Is.True);
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [TestCase("threat-hammer-6", CardUseUnavailableReason.DemonContractRestricted)]
        [TestCase("auto-pistol-7", CardUseUnavailableReason.DemonContractRestricted)]
        [TestCase("auto-pistol-8", CardUseUnavailableReason.None)]
        public void EPR04_U04_AsmodeusBlocksManualCardsThroughRankSeven(
            string definitionKey,
            CardUseUnavailableReason expectedReason)
        {
            CoreLoopBattle battle = CreateBattle(
                DefinitionDeck(definitionKey, new[] { 2, 3, 4, 5, 6 }),
                PlainDeck(new[] { 4, 5, 2, 3, 4, 5 }, 100),
                new StandPolicy(),
                DemonContractKind.Asmodeus);
            ActivateFirstContract(battle);
            BlackjackCard card = battle.Player.Hand.GetFaceUpCards().Single();

            CardUseAvailability availability = battle.EvaluatePlayerCardUse(card.Id);

            Assert.That(availability.Reason, Is.EqualTo(expectedReason));
            Assert.That(availability.CanUse,
                Is.EqualTo(expectedReason == CardUseUnavailableReason.None));
        }

        [Test]
        public void DCR04_U13_AzazelActivationReactivatesUsedFaceUpCards()
        {
            CoreLoopBattle battle = CreateBattle(
                DefinitionDeck("military-knife-9", new[] { 2, 3, 4, 5, 6 }),
                PlainDeck(new[] { 2, 2, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Azazel);
            BlackjackCard knife = battle.Player.Hand.GetFaceUpCards().Single();
            Assert.That(battle.TryBeginPlayerCardUse(knife.Id), Is.True);
            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Used));

            ActivateFirstContract(battle);

            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Available));
        }

        [Test]
        public void DCR04_U14_AzazelOwnerHitReactivatesUsedFaceUpCardsAgain()
        {
            CoreLoopBattle battle = CreateBattle(
                DefinitionDeck("military-knife-9", new[] { 2, 3, 4, 5, 6, 7 }),
                PlainDeck(new[] { 2, 2, 2, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Azazel);
            BlackjackCard knife = battle.Player.Hand.GetFaceUpCards().Single();
            ActivateFirstContract(battle);
            Assert.That(battle.TryBeginPlayerCardUse(knife.Id), Is.True);
            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Used));

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(knife.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(battle.LastResolution, Is.Null);
        }

        [Test]
        public void DCR04_U15_AzazelDuplicateFaceUpRankImmediatelyBustsOwner()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 5, 2, 5, 3, 4, 6 }),
                PlainDeck(new[] { 2, 2, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Azazel);
            ActivateFirstContract(battle);

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget,
                Is.EqualTo(CombatantSide.Player));
        }

        [Test]
        public void DCR04_U16_AzazelBustPrecedesAutomaticCardActivation()
        {
            CardDefinition poison = CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.PoisonKey);
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(0, rank: 1),
                new BlackjackCard(1, rank: 2),
                new BlackjackCard(2, poison),
                new BlackjackCard(3, rank: 3),
                new BlackjackCard(4, rank: 4),
                new BlackjackCard(5, rank: 5)
            });
            CoreLoopBattle battle = CreateBattle(
                playerDeck,
                PlainDeck(new[] { 2, 2, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Azazel);
            ActivateFirstContract(battle);

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastAutomaticCardResult, Is.Null);
        }

        [Test]
        public void DCR04_U17_EnemyAsmodeusUsesTheSameOwnerRelativeForcedHit()
        {
            CoreLoopBattle battle = CreateEnemyContractBattle(
                PlainDeck(new[] { 2, 2, 2, 2, 2, 2, 2, 2 }),
                PlainDeck(new[] { 4, 5, 2, 3, 4, 5 }, 100),
                DemonContractKind.Asmodeus,
                new EnemyContractPolicy(EnemyActionType.Stand));
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Asmodeus));
            int playerFaceUpCount = battle.Player.Hand.GetFaceUpCards().Count;

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.Player.Hand.GetFaceUpCards(),
                Has.Count.EqualTo(playerFaceUpCount + 2));
        }

        [Test]
        public void DCR04_U18_EnemyAzazelBustsOnItsOwnDuplicateHit()
        {
            CoreLoopBattle battle = CreateEnemyContractBattle(
                PlainDeck(new[] { 2, 2, 2, 2, 2, 2, 2, 2 }),
                PlainDeck(new[] { 5, 2, 5, 3, 4, 6 }, 100),
                DemonContractKind.Azazel,
                new EnemyContractPolicy(EnemyActionType.Hit));
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Azazel));

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyBust));
        }

        private static CoreLoopBattle CreateBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractKind contractKind)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 5,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                CreateDemonDeck(contractKind),
                DemonContractResolver.CreateDefault());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateEnemyContractBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            DemonContractKind contractKind,
            IEnemyBehaviorPolicy enemyPolicy)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 5,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                playerDemonDeck: null,
                demonContractResolver: DemonContractResolver.CreateDefault(),
                enemyDemonDeck: CreateDemonDeck(contractKind));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static void ActivateFirstContract(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                pending.Options[0].OptionId), Is.True);
        }

        private static DemonContractDeck CreateDemonDeck(DemonContractKind kind)
        {
            string key = kind == DemonContractKind.Asmodeus
                ? DemonContractCatalog.AsmodeusKey
                : DemonContractCatalog.AzazelKey;
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(key);
            return new DemonContractDeck(
                new[] { new DemonContractCard(0, definition) },
                seed: 91);
        }

        private static BlackjackDeck DefinitionDeck(
            string definitionKey,
            IReadOnlyList<int> remainingRanks)
        {
            var cards = new List<BlackjackCard>
            {
                new BlackjackCard(0, CardDefinitionCatalog.GetByKey(definitionKey))
            };
            cards.AddRange(remainingRanks.Select(
                (rank, index) => new BlackjackCard(index + 1, rank)));
            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static BlackjackDeck PlainDeck(
            IReadOnlyList<int> ranks,
            int startId = 0)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks.Select(
                (rank, index) => new BlackjackCard(startId + index, rank)));
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return EnemyDecision.FromCandidate(
                    observation.ActionCandidates.First(candidate =>
                        candidate.ActionType == EnemyActionType.Stand),
                    "dcr04-stand");
            }
        }

        private sealed class SequencePolicy : IEnemyBehaviorPolicy
        {
            private readonly Queue<EnemyActionType> _actions;

            public SequencePolicy(params EnemyActionType[] actions)
            {
                _actions = new Queue<EnemyActionType>(actions);
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionType action = _actions.Count > 0
                    ? _actions.Dequeue()
                    : EnemyActionType.Stand;
                return EnemyDecision.FromCandidate(
                    observation.ActionCandidates.First(candidate =>
                        candidate.ActionType == action),
                    "dcr04-sequence");
            }
        }

        private sealed class EnemyContractPolicy : IEnemyBehaviorPolicy
        {
            private readonly EnemyActionType _normalAction;

            public EnemyContractPolicy(EnemyActionType normalAction)
            {
                _normalAction = normalAction;
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        option.DemonContractInteractionKind ==
                            DemonContractInteractionKind.AsmodeusForceOpponentHit &&
                        option.DemonContractOptionId ==
                            AsmodeusDemonContractHandler.ForceHitOptionId)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        option.DemonContractOptionId.HasValue)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.DemonContract &&
                        !option.DemonContractSourceCardId.HasValue)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == _normalAction)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(candidate, "dcr04-enemy-contract");
            }
        }
    }
}
