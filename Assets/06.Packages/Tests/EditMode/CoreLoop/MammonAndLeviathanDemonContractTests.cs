using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class MammonAndLeviathanDemonContractTests
    {
        [Test]
        public void DCR03_U01_MammonActivationSixBustsBeforeEnemyActs()
        {
            var enemyPolicy = new SequenceEnemyPolicy(EnemyActionType.Stand);
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 10, 5, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                enemyPolicy,
                dieValues: new[] { 6 });

            ActivateFirstContract(battle);

            Assert.That(enemyPolicy.DecisionCount, Is.Zero);
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(9));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget,
                Is.EqualTo(CombatantSide.Player));
        }

        [Test]
        public void DCR03_U02_MammonRerollIsANormalActionAndSixBusts()
        {
            var enemyPolicy = new SequenceEnemyPolicy(EnemyActionType.Stand);
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 10, 5, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                enemyPolicy,
                dieValues: new[] { 2, 6 });
            ActivateFirstContract(battle);
            ActiveDemonContract mammon = battle.ActivePlayerDemonContracts.Single();
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(((MammonRuntimeState)mammon.RuntimeState).CurrentDieValue,
                Is.EqualTo(2));

            Assert.That(
                battle.TryBeginPlayerMammonReroll(mammon.SourceCardId),
                Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(9));
            Assert.That(((MammonRuntimeState)mammon.RuntimeState).CurrentDieValue,
                Is.EqualTo(6));
        }

        [Test]
        public void DCR03_U03_MammonNoLongerBlocksTurnStartWithKeepChoice()
        {
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 5, 5, 2, 3, 4, 5 },
                enemyRanks: new[] { 10, 7, 2, 3, 4, 5 },
                new SequenceEnemyPolicy(EnemyActionType.Stand),
                dieValues: new[] { 3 });
            ActivateFirstContract(battle);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(
                ((MammonRuntimeState)battle.ActivePlayerDemonContracts.Single()
                    .RuntimeState).CurrentDieValue,
                Is.EqualTo(3));
        }

        [TestCase(false, RoundOutcome.EnemyWin, 10, 3)]
        [TestCase(true, RoundOutcome.PlayerWin, 11, 2)]
        public void DCR03_U04_MammonFinalChoiceDistinguishesIgnoredAndAppliedDie(
            bool applyDie,
            RoundOutcome expectedOutcome,
            int expectedPlayerSoul,
            int expectedEnemySoul)
        {
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 10, 5, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand),
                dieValues: new[] { 3 });
            ActivateFirstContract(battle);

            Assert.That(battle.TryPlayerStand(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.MammonApplyDie));

            int optionId = applyDie
                ? MammonDemonContractHandler.ApplyDieOptionId
                : MammonDemonContractHandler.DoNotApplyDieOptionId;
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                optionId), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(expectedOutcome));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(expectedPlayerSoul));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(expectedEnemySoul));
        }

        [Test]
        public void DCR03_U05_MammonAppliedDieCanCauseNumericBust()
        {
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 10, 10, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand),
                dieValues: new[] { 3 });
            ActivateFirstContract(battle);
            Assert.That(battle.TryPlayerStand(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                MammonDemonContractHandler.ApplyDieOptionId), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(9));
        }

        [Test]
        public void DCR03_U06_LeviathanRequestsTwoFreshDeclarationsBeforeCost()
        {
            var enemyPolicy = new SequenceEnemyPolicy(EnemyActionType.Hit);
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 12,
                enemyRanks: new[] { 10, 7, 5, 2, 3 },
                enemyPolicy);
            ActivateFirstContract(battle);
            BlackjackCard hiddenEnemyCard = battle.Enemy.Hand.Cards[1];

            BlackjackCard autoPistol = battle.Player.Hand.Cards.Single(card =>
                card.Definition.Effect == CardEffectKind.AutoPistol);
            Assert.That(battle.TryBeginPlayerCardUse(autoPistol.Id), Is.True);
            PendingCardEffect first = battle.PendingPlayerCardEffect;
            Assert.That(battle.TryResolvePlayerCardChoice(6), Is.True);
            PendingCardEffect second = battle.PendingPlayerCardEffect;

            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.False);
            Assert.That(second, Is.Not.Null);
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(11));
            Assert.That(battle.TryResolvePlayerCardChoice(8), Is.True);

            Assert.That(hiddenEnemyCard.IsFaceUp, Is.False);
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.LastDemonContractEffectResult.Triggered, Is.True);
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget,
                Is.Null);
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost, Is.EqualTo(1));
            Assert.That(
                battle.LastLeviathanCardEffectResult.ActivationSuccesses,
                Is.EqualTo(new[] { false, false }));
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(2));
        }

        [Test]
        public void DCR03_U07_LeviathanStopsAfterFirstActivationBustsOpponent()
        {
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 12,
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand));
            ActivateFirstContract(battle);
            int soulAfterBaseCost = battle.Player.Soul.Current;

            UseAutoPistolWithGuess(battle, guess: 7);

            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.True);
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(soulAfterBaseCost));
            Assert.That(battle.LastDemonContractEffectResult, Is.Null);
            Assert.That(
                battle.LastLeviathanCardEffectResult.ActivationSuccesses,
                Is.EqualTo(new[] { true }));
            Assert.That(battle.LastLeviathanCardEffectResult.PaidSoulCost, Is.Zero);
        }

        [Test]
        public void DCR03_U08_LeviathanSecondActivationCanBustWithoutSoulCost()
        {
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 12,
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand));
            ActivateFirstContract(battle);
            int soulAfterBaseCost = battle.Player.Soul.Current;

            UseAutoPistolWithGuess(battle, guess: 6);
            Assert.That(battle.PendingPlayerCardEffect, Is.Not.Null);
            Assert.That(battle.TryResolvePlayerCardChoice(7), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(soulAfterBaseCost));
            Assert.That(
                battle.LastLeviathanCardEffectResult.ActivationSuccesses,
                Is.EqualTo(new[] { false, true }));
            Assert.That(battle.LastLeviathanCardEffectResult.PaidSoulCost, Is.Zero);
        }

        [Test]
        public void DCR03_U09_LeviathanSoulCostAtZeroEndsBattleAfterTwoFailures()
        {
            var enemyPolicy = new SequenceEnemyPolicy(EnemyActionType.Stand);
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 2,
                enemyRanks: new[] { 10, 7, 2, 3 },
                enemyPolicy);
            ActivateFirstContract(battle);

            UseAutoPistolWithGuess(battle, guess: 6);
            Assert.That(battle.TryResolvePlayerCardChoice(8), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
        }

        [Test]
        public void DCR03_U10_PublicLeviathanResultDoesNotExposeHiddenRank()
        {
            string[] propertyNames = typeof(LeviathanCardEffectResult)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.That(propertyNames,
                Is.EqualTo(new[]
                {
                    "ActivationSuccesses",
                    "BustedTarget",
                    "PaidSoulCost"
                }));
        }

        [Test]
        public void DCR03_U11_InvalidSecondDeclarationLeavesSequenceUnchanged()
        {
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 12,
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand));
            ActivateFirstContract(battle);
            int soulBeforePistol = battle.Player.Soul.Current;

            UseAutoPistolWithGuess(battle, guess: 6);
            PendingCardEffect pending = battle.PendingPlayerCardEffect;

            Assert.That(battle.TryResolvePlayerCardChoice(99), Is.False);
            Assert.That(battle.PendingPlayerCardEffect, Is.SameAs(pending));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(soulBeforePistol));
            Assert.That(battle.LastLeviathanCardEffectResult, Is.Null);

            Assert.That(battle.TryResolvePlayerCardChoice(8), Is.True);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(soulBeforePistol - 1));
        }

        [Test]
        public void DCR03_U12_PresentationShowsRerollActionAndTwoPistolRule()
        {
            CoreLoopBattle mammonBattle = CreateMammonBattle(
                playerRanks: new[] { 5, 5, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand),
                dieValues: new[] { 2 });
            ActivateFirstContract(mammonBattle);
            DemonContractPanelViewModel mammonModel =
                DemonContractPresenter.Create(mammonBattle);

            CoreLoopBattle leviathanBattle = CreateLeviathanBattle(
                playerCurrentSoul: 12,
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand));
            ActivateFirstContract(leviathanBattle);
            DemonContractPanelViewModel leviathanModel =
                DemonContractPresenter.Create(leviathanBattle);

            Assert.That(mammonModel.ActiveContracts.Single(),
                Does.Contain("재굴림 행동 가능"));
            Assert.That(leviathanModel.ActiveContracts.Single(),
                Does.Contain("리볼버 최대 2회 발동"));
        }

        [Test]
        public void DCR03_U13_InvalidMammonSourceLeavesTurnAndDieUnchanged()
        {
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 5, 5, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand),
                dieValues: new[] { 2, 4 });
            ActivateFirstContract(battle);
            ActiveDemonContract mammon = battle.ActivePlayerDemonContracts.Single();
            MammonRuntimeState state = (MammonRuntimeState)mammon.RuntimeState;

            Assert.That(
                battle.TryBeginPlayerMammonReroll(mammon.SourceCardId + 1000),
                Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(state.CurrentDieValue, Is.EqualTo(2));
            Assert.That(state.CanRerollThisTurn, Is.True);

            Assert.That(
                battle.TryBeginPlayerMammonReroll(mammon.SourceCardId),
                Is.True);
            Assert.That(state.CurrentDieValue, Is.EqualTo(4));
        }

        private static CoreLoopBattle CreateMammonBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            IEnemyBehaviorPolicy enemyPolicy,
            IReadOnlyList<int> dieValues)
        {
            return CreateStartedBattle(
                CreatePlainDeck(playerRanks),
                CreatePlainDeck(enemyRanks),
                playerCurrentSoul: 12,
                enemyPolicy,
                CreateDemonDeck(DemonContractKind.Mammon),
                new DemonContractResolver(
                    new MammonDemonContractHandler(new SequenceDieRoller(dieValues))));
        }

        private static CoreLoopBattle CreateLeviathanBattle(
            int playerCurrentSoul,
            IReadOnlyList<int> enemyRanks,
            IEnemyBehaviorPolicy enemyPolicy)
        {
            return CreateStartedBattle(
                CreateAutoPistolDeck(),
                CreatePlainDeck(enemyRanks),
                playerCurrentSoul,
                enemyPolicy,
                CreateDemonDeck(DemonContractKind.Leviathan),
                new DemonContractResolver(new LeviathanDemonContractHandler()));
        }

        private static CoreLoopBattle CreateStartedBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            int playerCurrentSoul,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractDeck demonDeck,
            DemonContractResolver resolver)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                playerCurrentSoul,
                enemyMaximumSoul: 3,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                demonDeck,
                resolver);
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

        private static void UseAutoPistolWithGuess(CoreLoopBattle battle, int guess)
        {
            BlackjackCard autoPistol = battle.Player.Hand.Cards.Single(card =>
                card.Definition.Effect == CardEffectKind.AutoPistol);
            Assert.That(battle.TryBeginPlayerCardUse(autoPistol.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(guess), Is.True);
        }

        private static BlackjackDeck CreateAutoPistolDeck()
        {
            CardDefinition autoPistol =
                CardDefinitionCatalog.GetByKey("auto-pistol-7");
            return BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(0, rank: 5),
                new BlackjackCard(1, autoPistol),
                new BlackjackCard(2, rank: 2),
                new BlackjackCard(3, rank: 3),
                new BlackjackCard(4, rank: 4),
                new BlackjackCard(5, rank: 5)
            });
        }

        private static BlackjackDeck CreatePlainDeck(IReadOnlyList<int> ranks)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks.Select(
                (rank, id) => new BlackjackCard(id, rank)));
        }

        private static DemonContractDeck CreateDemonDeck(DemonContractKind kind)
        {
            string key = kind == DemonContractKind.Mammon
                ? DemonContractCatalog.MammonKey
                : DemonContractCatalog.LeviathanKey;
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(key);
            return new DemonContractDeck(Enumerable.Range(0, 4)
                .Select(id => new DemonContractCard(id, definition)), seed: 73);
        }

        private sealed class SequenceDieRoller : IDemonDieRoller
        {
            private readonly Queue<int> _values;

            public SequenceDieRoller(IEnumerable<int> values)
            {
                _values = new Queue<int>(values);
            }

            public int RollD6()
            {
                if (_values.Count == 0)
                {
                    throw new InvalidOperationException("No fixed die value remains.");
                }

                return _values.Dequeue();
            }
        }

        private sealed class SequenceEnemyPolicy : IEnemyBehaviorPolicy
        {
            private readonly Queue<EnemyActionType> _actions;

            public SequenceEnemyPolicy(params EnemyActionType[] actions)
            {
                _actions = new Queue<EnemyActionType>(actions);
            }

            public int DecisionCount { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                DecisionCount++;
                EnemyActionType action = _actions.Count > 0
                    ? _actions.Dequeue()
                    : EnemyActionType.Stand;
                return new EnemyDecision(action, "dc04-sequence");
            }
        }
    }
}
