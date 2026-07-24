using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class PoisonAutomaticCardTests
    {
        private static readonly CardDefinition Poison =
            CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey);
        private static readonly CardDefinition CrystalOrb =
            CardDefinitionCatalog.GetByKey("crystal-orb-5");

        [Test]
        public void AC02_U01_StandChoiceStandsOnlyOwnerAndDiscardsSource()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, Poison),
                EnemyCards(4, 3),
                playerCurrentSoul: 12,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            const int poisonCardId = 2;
            bool observedPlayerStanding = false;
            bool observedSourceDiscarded = false;
            battle.Stepped += () =>
            {
                if (battle.LastAutomaticCardResult.HasValue &&
                    battle.LastAutomaticCardResult.Value.SourceCardId ==
                        poisonCardId)
                {
                    observedPlayerStanding |= battle.Player.IsStanding;
                    observedSourceDiscarded |=
                        battle.Player.Deck.GetDiscardedCards()
                            .Count(card => card.Id == poisonCardId) == 1;
                }
            };

            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);

            Assert.That(ResolvePlayerChoice(battle, pending, "스탠드"), Is.True);

            Assert.That(observedPlayerStanding, Is.True);
            Assert.That(observedSourceDiscarded, Is.True);
            Assert.That(battle.LastAutomaticCardResult.Value.SourceDisposition,
                Is.EqualTo(AutomaticCardSourceDisposition.Discard));
        }

        [Test]
        public void AC02_U02_SatanStandRestrictionRemovesStandOption()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, Poison),
                EnemyCards(4, 3),
                playerCurrentSoul: 12,
                enemyPolicy: new StandPolicy(),
                playerDemonDeck: CreateRepeatedSatanDeck());
            Assert.That(battle.Start(), Is.True);
            ActivateSatan(battle);

            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.Options.Any(option =>
                option.Label.Contains("스탠드")), Is.False);
            Assert.That(pending.Options.Any(option =>
                option.Label.Contains("영혼")), Is.True);
        }

        [TestCase(6, 3, false)]
        [TestCase(3, 0, true)]
        [TestCase(2, 0, true)]
        [TestCase(1, 0, true)]
        public void AC02_U03_PaySoulLosesThreeOrAllRemainingSoul(
            int initialSoul,
            int expectedSoul,
            bool expectedBattleEnd)
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 3, Poison),
                EnemyCards(4, 3),
                playerCurrentSoul: initialSoul,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;

            Assert.That(ResolvePlayerChoice(battle, pending, "영혼"), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(expectedSoul));
            Assert.That(
                battle.State == CoreLoopState.BattleEnded,
                Is.EqualTo(expectedBattleEnd));
        }

        [Test]
        public void AC02_U04_SoulDeathCancelsEnemyTurnAndParentCardEffect()
        {
            var enemyPolicy = new CountingStandPolicy();
            CoreLoopBattle battle = CreateBattle(
                new[]
                {
                    new BlackjackCard(0, Plain(2)),
                    new BlackjackCard(1, CrystalOrb),
                    new BlackjackCard(2, Poison),
                    new BlackjackCard(3, Plain(4))
                },
                EnemyCards(4, 3),
                playerCurrentSoul: 1,
                enemyPolicy: enemyPolicy);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard crystalOrb = battle.Player.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(crystalOrb.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(1), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);

            Assert.That(ResolvePlayerChoice(battle, pending, "영혼"), Is.True);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(enemyPolicy.DecideCount, Is.Zero);
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(crystalOrb.UseState, Is.EqualTo(CardUseState.Used));
        }

        [TestCase(10)]
        [TestCase(11)]
        public void AC02_U05_PaidPoisonWinHealsFiveWithoutExceedingMaximum(
            int initialSoul)
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(4, 3, Poison),
                EnemyCards(3, 3),
                playerCurrentSoul: initialSoul,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(
                ResolvePlayerChoice(
                    battle,
                    battle.PendingPlayerAutomaticInteraction,
                    "영혼"),
                Is.True);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerWin));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12));
            Assert.That(battle.PendingPoisonWinRewardCount, Is.Zero);
        }

        [Test]
        public void AC02_U06_PaidPoisonLossDoesNotHealAndClearsReservation()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 2, Poison),
                EnemyCards(7, 3),
                playerCurrentSoul: 10,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(
                ResolvePlayerChoice(
                    battle,
                    battle.PendingPlayerAutomaticInteraction,
                    "영혼"),
                Is.True);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyWin));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(6));
            Assert.That(battle.PendingPoisonWinRewardCount, Is.Zero);
        }

        [Test]
        public void AC02_U07_MultiplePhysicalPoisonRewardsResolveOnceEach()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(4, 3, Poison, Poison),
                EnemyCards(3, 3),
                playerMaximumSoul: 20,
                playerCurrentSoul: 12,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(
                ResolvePlayerChoice(
                    battle,
                    battle.PendingPlayerAutomaticInteraction,
                    "영혼"),
                Is.True);
            Assert.That(battle.PendingPoisonWinRewardCount, Is.EqualTo(1));

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(
                ResolvePlayerChoice(
                    battle,
                    battle.PendingPlayerAutomaticInteraction,
                    "영혼"),
                Is.True);
            Assert.That(battle.PendingPoisonWinRewardCount, Is.EqualTo(2));

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerWin));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(16));
            Assert.That(battle.PendingPoisonWinRewardCount, Is.Zero);
        }

        [Test]
        public void AC02_U08_EnemyOwnerUsesSamePaymentAndWinRewardRules()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(2, 2, Plain(2)),
                EnemyCards(4, 3, Poison),
                playerCurrentSoul: 12,
                enemyMaximumSoul: 6,
                enemyPolicy: new SequencePolicy(
                    EnemyActionType.Hit,
                    EnemyActionType.Stand));
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.OwnerSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(pending.DecisionSide, Is.EqualTo(CombatantSide.Enemy));

            Assert.That(
                ResolveChoice(battle, CombatantSide.Enemy, pending, "영혼"),
                Is.True);
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(3));
            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyWin));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(6));
            Assert.That(battle.PendingPoisonWinRewardCount, Is.Zero);
        }

        private static bool ResolvePlayerChoice(
            CoreLoopBattle battle,
            PendingAutomaticCardInteraction pending,
            string labelPart)
        {
            AutomaticCardChoiceOption option = FindOption(pending, labelPart);
            return battle.TryResolvePlayerAutomaticCardChoice(
                pending.InteractionId,
                option.OptionId);
        }

        private static bool ResolveChoice(
            CoreLoopBattle battle,
            CombatantSide decisionSide,
            PendingAutomaticCardInteraction pending,
            string labelPart)
        {
            AutomaticCardChoiceOption option = FindOption(pending, labelPart);
            return battle.TryResolveAutomaticCardChoice(
                decisionSide,
                pending.InteractionId,
                option.OptionId);
        }

        private static AutomaticCardChoiceOption FindOption(
            PendingAutomaticCardInteraction pending,
            string labelPart)
        {
            return pending.Options.Single(option =>
                option.Label.Contains(labelPart));
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<BlackjackCard> playerCards,
            IReadOnlyList<BlackjackCard> enemyCards,
            int playerCurrentSoul,
            IEnemyBehaviorPolicy enemyPolicy,
            int playerMaximumSoul = 12,
            int enemyMaximumSoul = 3,
            DemonContractDeck playerDemonDeck = null)
        {
            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(playerCards),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                playerMaximumSoul,
                playerCurrentSoul,
                enemyMaximumSoul,
                enemyPolicy,
                playerDemonDeck);
        }

        private static IReadOnlyList<BlackjackCard> PlayerCards(
            object first,
            object second,
            params object[] remaining)
        {
            return CreateCards(0, first, second, remaining);
        }

        private static IReadOnlyList<BlackjackCard> EnemyCards(
            object first,
            object second,
            params object[] remaining)
        {
            return CreateCards(100, first, second, remaining);
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(
            int startId,
            object first,
            object second,
            IReadOnlyList<object> remaining)
        {
            var values = new List<object> { first, second };
            values.AddRange(remaining);
            return values.Select((value, index) =>
            {
                CardDefinition definition = value is CardDefinition cardDefinition
                    ? cardDefinition
                    : Plain((int)value);
                return new BlackjackCard(startId + index, definition);
            }).ToArray();
        }

        private static CardDefinition Plain(int rank)
        {
            return CardDefinitionCatalog.GetDefaultForRank(rank);
        }

        private static DemonContractDeck CreateRepeatedSatanDeck()
        {
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(
                    DemonContractCatalog.SatanKey);
            return new DemonContractDeck(
                Enumerable.Range(0, 4).Select(id =>
                    new DemonContractCard(id, definition)),
                seed: 73);
        }

        private static void ActivateSatan(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);
            Assert.That(battle.ActivePlayerDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Satan));
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "ac02-test-stand");
            }
        }

        private sealed class CountingStandPolicy : IEnemyBehaviorPolicy
        {
            public int DecideCount { get; private set; }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                DecideCount++;
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "ac02-test-counting-stand");
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
                return new EnemyDecision(action, "ac02-test-sequence");
            }
        }
    }
}
