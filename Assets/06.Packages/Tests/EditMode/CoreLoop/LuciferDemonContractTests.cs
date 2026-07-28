using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class LuciferDemonContractTests
    {
        [Test]
        public void DCR04_U36_ActivationPaysIndividualCostAndOffersFiveWithSkip()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: Enumerable.Repeat(
                    DemonContractKind.Lucifer,
                    6));

            ActivateBaseLucifer(battle);

            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.EqualTo(1));
            Assert.That(pending.Kind, Is.EqualTo(
                DemonContractInteractionKind.LuciferChooseAdditionalContract));
            Assert.That(pending.ContractKind, Is.EqualTo(DemonContractKind.Lucifer));
            Assert.That(pending.Options.Count, Is.EqualTo(6));
            Assert.That(pending.Options.Count(option => option.ContractCardId.HasValue),
                Is.EqualTo(DemonContractDeck.LuciferCandidateCount));
            Assert.That(pending.Options.Single(option => !option.ContractCardId.HasValue)
                .OptionId, Is.EqualTo(
                    LuciferDemonContractHandler.SkipAdditionalContractOptionId));
            Assert.That(model.DemonContract.Choices.Count, Is.EqualTo(6));
            Assert.That(model.DemonContract.Choices.Count(choice =>
                !string.IsNullOrEmpty(choice.Ability) &&
                !string.IsNullOrEmpty(choice.Cost)), Is.EqualTo(5));
        }

        [Test]
        public void DCR04_U37_SkipDiscardsCandidatesAndCompletesOriginalAction()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Baphomet
                });
            ActivateBaseLucifer(battle);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                LuciferDemonContractHandler.SkipAdditionalContractOptionId),
                Is.True);

            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.ActivePlayerDemonContracts.Select(contract => contract.Kind),
                Is.EqualTo(new[] { DemonContractKind.Lucifer }));
            Assert.That(battle.PlayerDemonDeck.AvailableCardCount, Is.EqualTo(1));
            Assert.That(battle.PlayerDemonDeck.CardsInPlayCount, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.EqualTo(1));
        }

        [Test]
        public void DCR04_U38_SelectedContractActivatesWithoutAnotherBaseUseOrCost()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Baphomet
                });
            ActivateBaseLucifer(battle);

            ResolveAdditionalContract(battle, DemonContractKind.Baphomet);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.EqualTo(1));
            Assert.That(battle.ActivePlayerDemonContracts.Select(contract => contract.Kind),
                Is.EquivalentTo(new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Baphomet
                }));
            Assert.That(battle.BaphometWaveCount, Is.EqualTo(2));
            Assert.That(battle.BaphometPentagramCount, Is.EqualTo(8));
        }

        [Test]
        public void DCR04_U39_SecondLuciferIsIndependentAndCompoundsItsCost()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 4,
                playerDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Lucifer
                });
            ActivateBaseLucifer(battle);

            ResolveAdditionalContract(battle, DemonContractKind.Lucifer);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(1));
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.EqualTo(1));
            Assert.That(battle.ActivePlayerDemonContracts.Count, Is.EqualTo(2));
            Assert.That(battle.ActivePlayerDemonContracts.Select(
                contract => contract.SourceCardId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(battle.ActivePlayerDemonContracts.Select(
                contract => contract.RuntimeState).Distinct().Count(), Is.EqualTo(2));
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
        }

        [Test]
        public void DCR04_U40_CompoundedLuciferCostCanDepleteSoulAndEndBattle()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 3,
                playerDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Lucifer
                });
            ActivateBaseLucifer(battle);

            ResolveAdditionalContract(battle, DemonContractKind.Lucifer);

            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.ActivePlayerDemonContracts, Is.Empty);
            Assert.That(battle.UsedPlayerBaseDemonContractCount, Is.EqualTo(1));
        }

        [Test]
        public void DCR04_U41_NoRemainingCandidateCompletesOptionalLuciferEffect()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: new[] { DemonContractKind.Lucifer });

            ActivateBaseLucifer(battle);

            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.ActivePlayerDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Lucifer));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void DCR04_U42_StaleLuciferChoiceIsAtomic()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Baphomet
                });
            ActivateBaseLucifer(battle);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            int soulBefore = battle.Player.Soul.Current;
            int cardsInPlayBefore = battle.PlayerDemonDeck.CardsInPlayCount;

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId + 1,
                pending.Options[0].OptionId), Is.False);
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                optionId: 999), Is.False);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(soulBefore));
            Assert.That(battle.PlayerDemonDeck.CardsInPlayCount,
                Is.EqualTo(cardsInPlayBefore));
            Assert.That(battle.ActivePlayerDemonContracts.Count, Is.EqualTo(1));
            Assert.That(battle.PendingPlayerDemonContractInteraction,
                Is.SameAs(pending));
        }

        [Test]
        public void DCR04_U43_CultistSelectsLuciferThenUsefulAdditionalContract()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: Array.Empty<DemonContractKind>(),
                enemyMaximumSoul: 3,
                enemyPolicy: new CultistEnemyPolicy(),
                enemyDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Baphomet
                });

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.EqualTo(1));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(1));
            Assert.That(battle.ActiveEnemyDemonContracts.Select(contract => contract.Kind),
                Is.EquivalentTo(new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Baphomet
                }));
            Assert.That(battle.PendingEnemyDemonContractInteraction, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void DCR04_U44_SelectedMammonSixUsesExistingContractBustBoundary()
        {
            var resolver = new DemonContractResolver(
                new LuciferDemonContractHandler(),
                new MammonDemonContractHandler(new FixedDieRoller(6)));
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Mammon
                },
                demonContractResolver: resolver);
            ActivateBaseLucifer(battle);

            ResolveAdditionalContract(battle, DemonContractKind.Mammon);

            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget,
                Is.EqualTo(CombatantSide.Player));
        }

        [Test]
        public void DCR04_U45_CultistSkipsFatalAdditionalLucifer()
        {
            CoreLoopBattle battle = CreateBattle(
                playerSoul: 12,
                playerDemonKinds: Array.Empty<DemonContractKind>(),
                enemyMaximumSoul: 3,
                enemyPolicy: new CultistEnemyPolicy(),
                enemyDemonKinds: new[]
                {
                    DemonContractKind.Lucifer,
                    DemonContractKind.Lucifer
                });

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(1));
            Assert.That(battle.ActiveEnemyDemonContracts.Select(
                contract => contract.Kind), Is.EqualTo(
                    new[] { DemonContractKind.Lucifer }));
            Assert.That(battle.PendingEnemyDemonContractInteraction, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        private static void ActivateBaseLucifer(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption lucifer = pending.Options.First(option =>
                option.ContractDefinitionKey == DemonContractCatalog.LuciferKey);
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                lucifer.OptionId), Is.True);
            Assert.That(battle.ActivePlayerDemonContracts[0].Kind,
                Is.EqualTo(DemonContractKind.Lucifer));
        }

        private static void ResolveAdditionalContract(
            CoreLoopBattle battle,
            DemonContractKind kind)
        {
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption option = pending.Options.First(candidate =>
                candidate.ContractDefinitionKey != null &&
                DemonContractCatalog.Default.GetByKey(
                    candidate.ContractDefinitionKey).Kind == kind);
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                option.OptionId), Is.True);
        }

        private static CoreLoopBattle CreateBattle(
            int playerSoul,
            IEnumerable<DemonContractKind> playerDemonKinds,
            int enemyMaximumSoul = 5,
            IEnemyBehaviorPolicy enemyPolicy = null,
            IEnumerable<DemonContractKind> enemyDemonKinds = null,
            DemonContractResolver demonContractResolver = null)
        {
            var battle = new CoreLoopBattle(
                PlainDeck(startId: 0),
                PlainDeck(startId: 100),
                playerMaximumSoul: 12,
                playerCurrentSoul: playerSoul,
                enemyMaximumSoul,
                enemyPolicy ?? new StandPolicy(),
                CardEffectResolver.CreateDefault(),
                CreateDemonDeck(playerDemonKinds, seed: 71),
                demonContractResolver ?? DemonContractResolver.CreateDefault(),
                CreateDemonDeck(
                    enemyDemonKinds ?? Array.Empty<DemonContractKind>(),
                    seed: 73));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static DemonContractDeck CreateDemonDeck(
            IEnumerable<DemonContractKind> kinds,
            int seed)
        {
            DemonContractCard[] cards = kinds.Select((kind, index) =>
            {
                DemonContractDefinition definition = DemonContractCatalog.Default
                    .Definitions.Single(candidate => candidate.Kind == kind);
                return new DemonContractCard(index, definition);
            }).ToArray();
            return new DemonContractDeck(cards, seed);
        }

        private static BlackjackDeck PlainDeck(int startId)
        {
            return BlackjackDeck.CreateInDrawOrder(Enumerable.Range(0, 20)
                .Select(index => new BlackjackCard(startId + index, rank: 2)));
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate stand = observation.ActionCandidates
                    .First(candidate => candidate.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(stand, "test-stand");
            }
        }

        private sealed class FixedDieRoller : IDemonDieRoller
        {
            private readonly int _value;

            public FixedDieRoller(int value)
            {
                _value = value;
            }

            public int RollD6()
            {
                return _value;
            }
        }
    }
}
