using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class MammonAndLeviathanDemonContractTests
    {
        [Test]
        public void DC04_U01_MammonActivationSixBustsBeforeEnemyActs()
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
        public void DC04_U02_MammonRerollIsOfferedOncePerTurnAndSixBusts()
        {
            var enemyPolicy = new SequenceEnemyPolicy(EnemyActionType.Stand);
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 10, 5, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                enemyPolicy,
                dieValues: new[] { 2, 6 });
            ActivateFirstContract(battle);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.MammonReroll));
            Assert.That(((MammonRuntimeState)battle.ActivePlayerDemonContracts[0]
                .RuntimeState).CurrentDieValue, Is.EqualTo(2));

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                MammonDemonContractHandler.RerollDieOptionId), Is.True);

            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.ContractEffectBust));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(9));
            Assert.That(((MammonRuntimeState)battle.ActivePlayerDemonContracts[0]
                .RuntimeState).CurrentDieValue, Is.EqualTo(6));
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                MammonDemonContractHandler.RerollDieOptionId), Is.False);
        }

        [Test]
        public void DC04_U03_MammonKeepDoesNotConsumeTheNormalPlayerAction()
        {
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 5, 5, 2, 3, 4, 5 },
                enemyRanks: new[] { 10, 7, 2, 3, 4, 5 },
                new SequenceEnemyPolicy(EnemyActionType.Stand),
                dieValues: new[] { 3 });
            ActivateFirstContract(battle);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                MammonDemonContractHandler.KeepDieOptionId), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.PendingPlayerDemonContractInteraction.Kind,
                Is.EqualTo(DemonContractInteractionKind.MammonReroll));
        }

        [TestCase(false, RoundOutcome.EnemyWin, 10, 3)]
        [TestCase(true, RoundOutcome.PlayerWin, 11, 2)]
        public void DC04_U04_MammonFinalChoiceDistinguishesIgnoredAndAppliedDie(
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
            KeepMammonDie(battle);

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
        public void DC04_U05_MammonAppliedDieCanCauseNumericBust()
        {
            CoreLoopBattle battle = CreateMammonBattle(
                playerRanks: new[] { 10, 10, 2, 3 },
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand),
                dieValues: new[] { 3 });
            ActivateFirstContract(battle);
            KeepMammonDie(battle);
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
        public void DC04_U06_LeviathanIgnoresHiddenTotalBeforeShowdown()
        {
            var enemyPolicy = new SequenceEnemyPolicy(EnemyActionType.Hit);
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 12,
                enemyRanks: new[] { 10, 7, 5, 2, 3 },
                enemyPolicy);
            ActivateFirstContract(battle);
            BlackjackCard hiddenEnemyCard = battle.Enemy.Hand.Cards[1];

            UseAutoPistolWithGuess(battle, guess: 6);

            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.False);
            Assert.That(hiddenEnemyCard.IsFaceUp, Is.False);
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.LastDemonContractEffectResult.Triggered, Is.True);
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget,
                Is.Null);
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost, Is.EqualTo(1));
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(2));
        }

        [Test]
        public void DC04_U07_LeviathanDoesNotChargeWhenOriginalPistolSucceeds()
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
        }

        [Test]
        public void DC04_U08_LeviathanTotalFailurePaysExactlyOneSoul()
        {
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 3,
                enemyRanks: new[] { 10, 7, 2, 3 },
                new SequenceEnemyPolicy(EnemyActionType.Stand));
            ActivateFirstContract(battle);

            UseAutoPistolWithGuess(battle, guess: 6);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(1));
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.LastDemonContractEffectResult.BustedTarget, Is.Null);
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost, Is.EqualTo(1));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void DC04_U09_LeviathanSoulCostAtZeroEndsBattleWithoutEnemyTurn()
        {
            var enemyPolicy = new SequenceEnemyPolicy(EnemyActionType.Stand);
            CoreLoopBattle battle = CreateLeviathanBattle(
                playerCurrentSoul: 2,
                enemyRanks: new[] { 10, 7, 2, 3 },
                enemyPolicy);
            ActivateFirstContract(battle);

            UseAutoPistolWithGuess(battle, guess: 6);

            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(enemyPolicy.DecisionCount, Is.EqualTo(1));
        }

        [Test]
        public void DC04_U10_PublicLeviathanResultDoesNotExposeHiddenTotal()
        {
            string[] propertyNames = typeof(DemonContractEffectResult)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Select(property => property.Name)
                .OrderBy(name => name)
                .ToArray();

            Assert.That(propertyNames,
                Is.EqualTo(new[] { "BustedTarget", "PaidSoulCost", "Triggered" }));
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

        private static void KeepMammonDie(CoreLoopBattle battle)
        {
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.MammonReroll));
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                MammonDemonContractHandler.KeepDieOptionId), Is.True);
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
