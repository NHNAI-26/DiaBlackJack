using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class AutomaticCardIntegrationTests
    {
        [Test]
        public void AC06_U02_StandaloneSessionPresentsAndResolvesPlayerChoice()
        {
            var session = new CoreLoopSession(() => CreateBattle(
                PlayerCards(2, 3, Automatic(CardDefinitionCatalog.PoisonKey), 4),
                EnemyCards(4, 5, 2),
                new StandPolicy()));

            Assert.That(session.TryPlayerHit(), Is.True);
            CoreLoopViewModel pendingModel =
                CoreLoopPresenter.Create(session.Battle);
            AutomaticCardInteractionViewModel interaction =
                pendingModel.AutomaticCardInteraction;

            Assert.That(interaction, Is.Not.Null);
            Assert.That(pendingModel.IsResolvingAutomaticCardEffect, Is.True);
            Assert.That(interaction.EffectKind, Is.EqualTo(CardEffectKind.Poison));
            Assert.That(interaction.Choices.Count, Is.GreaterThan(0));

            Assert.That(
                session.TryResolvePlayerAutomaticCardChoice(
                    interaction.InteractionId,
                    PoisonEffectHandler.PaySoulOptionId),
                Is.True);

            CoreLoopViewModel resultModel =
                CoreLoopPresenter.Create(session.Battle);
            Assert.That(resultModel.AutomaticCardInteraction, Is.Null);
            Assert.That(resultModel.AutomaticCardResult, Is.Not.Null);
            Assert.That(
                resultModel.AutomaticCardResult.PublicSummary,
                Does.Contain("WIN HEAL RESERVED"));
        }

        [Test]
        public void AC06_U03_PlayerFlamethrowerHandsOpponentChoiceToEnemyPolicy()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(
                    2,
                    3,
                    Automatic(CardDefinitionCatalog.FlamethrowerKey),
                    4),
                EnemyCards(4, 5, 2),
                new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingAutomaticCardInteraction ownerChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(
                ownerChoice.ChoiceKind,
                Is.EqualTo(
                    AutomaticCardChoiceKind.FlamethrowerOwnerDiscard));
            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    ownerChoice.InteractionId,
                    FlamethrowerEffectHandler.SkipOptionId),
                Is.True);

            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
            Assert.That(battle.LastEnemyAutomaticCardDecision.HasValue, Is.True);
            Assert.That(
                battle.LastEnemyAutomaticCardDecision.Value.ReasonCode,
                Does.StartWith("flamethrower-"));
            Assert.That(
                battle.LastAutomaticCardResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.Flamethrower));
        }

        [Test]
        public void AC06_U04_EnemyLieDetectorResultDoesNotExposePrivateComparison()
        {
            var enemyPolicy = new SequencePolicy(
                EnemyActionType.Hit,
                EnemyActionType.Stand);
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(10, 10, 2, 3),
                EnemyCards(
                    2,
                    3,
                    Automatic(CardDefinitionCatalog.LieDetectorKey),
                    4),
                enemyPolicy);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(
                battle.LastAutomaticCardResult.Value.OwnerSide,
                Is.EqualTo(CombatantSide.Enemy));
            Assert.That(
                battle.LastLieDetectorPublicResult.Value.WasComparable,
                Is.True);
            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);
            Assert.That(model.AutomaticCardResult, Is.Not.Null);
            Assert.That(model.AutomaticCardResult.PrivateSummary, Is.Empty);
            Assert.That(
                model.AutomaticCardResult.PublicSummary,
                Does.Contain("DECLARED"));
        }

        [Test]
        public void AC06_U05_InvalidEnemyPolicyOptionFallsBackToFirstOption()
        {
            CoreLoopBattle battle = CreateBattle(
                PlayerCards(
                    2,
                    3,
                    Automatic(CardDefinitionCatalog.FlamethrowerKey),
                    4),
                EnemyCards(4, 5, 2),
                new StandPolicy(),
                new InvalidAutomaticPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction ownerChoice =
                battle.PendingPlayerAutomaticInteraction;

            Assert.That(
                battle.TryResolvePlayerAutomaticCardChoice(
                    ownerChoice.InteractionId,
                    FlamethrowerEffectHandler.SkipOptionId),
                Is.True);

            Assert.That(
                battle.LastEnemyAutomaticCardDecision.Value.ReasonCode,
                Is.EqualTo("invalid-policy-option-safe-first"));
            Assert.That(
                battle.LastEnemyAutomaticCardDecision.Value.OptionId,
                Is.EqualTo(FlamethrowerEffectHandler.SkipOptionId));
            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
        }

        [TestCase(AutomaticCardChoiceKind.PoisonDecision)]
        [TestCase(AutomaticCardChoiceKind.ResurrectionHerbDecision)]
        [TestCase(AutomaticCardChoiceKind.LieDetectorNumber)]
        [TestCase(AutomaticCardChoiceKind.FlamethrowerOwnerDiscard)]
        [TestCase(AutomaticCardChoiceKind.FlamethrowerOpponentDiscard)]
        [TestCase(AutomaticCardChoiceKind.PocketWatchManualCard)]
        [TestCase(AutomaticCardChoiceKind.PocketWatchSourceDisposition)]
        public void AC06_U06_DefaultPolicyReturnsAvailableOptionWithoutHiddenData(
            AutomaticCardChoiceKind choiceKind)
        {
            AutomaticCardDecisionObservation observation =
                CreateObservation(choiceKind);

            AutomaticCardDecision decision =
                DefaultAutomaticCardDecisionPolicy.Instance.Decide(
                    observation);

            Assert.That(
                observation.Options.Any(option =>
                    option.OptionId == decision.OptionId),
                Is.True);
            Assert.That(
                typeof(AutomaticCardDecisionObservation)
                    .GetProperties()
                    .Any(property =>
                        property.Name.IndexOf(
                            "Hidden",
                            StringComparison.OrdinalIgnoreCase) >= 0),
                Is.False);
        }

        [Test]
        public void AC06_U07_TenRestartedBattlesKeepCardsAndStateIsolated()
        {
            for (int iteration = 0; iteration < 10; iteration++)
            {
                CoreLoopBattle battle = CreateBattle(
                    PlayerCards(
                        2,
                        3,
                        Automatic(
                            CardDefinitionCatalog.ResurrectionHerbKey),
                        4,
                        2,
                        3),
                    EnemyCards(4, 5, 2, 3, 4, 5),
                    new StandPolicy());
                Assert.That(battle.Start(), Is.True);
                Assert.That(battle.TryPlayerHit(), Is.True);
                PendingAutomaticCardInteraction pending =
                    battle.PendingPlayerAutomaticInteraction;

                Assert.That(
                    battle.TryResolvePlayerAutomaticCardChoice(
                        pending.InteractionId,
                        ResurrectionHerbEffectHandler
                            .RestartRoundOptionId),
                    Is.True);

                Assert.That(battle.RoundNumber, Is.EqualTo(2));
                Assert.That(battle.PendingAutomaticInteraction, Is.Null);
                Assert.That(
                    battle.Player.Hand.Count +
                    battle.Player.Deck.DrawCount +
                    battle.Player.Deck.DiscardCount,
                    Is.EqualTo(battle.Player.Deck.TotalCardCount));
                Assert.That(
                    battle.Enemy.Hand.Count +
                    battle.Enemy.Deck.DrawCount +
                    battle.Enemy.Deck.DiscardCount,
                    Is.EqualTo(battle.Enemy.Deck.TotalCardCount));
            }
        }

        private static AutomaticCardDecisionObservation CreateObservation(
            AutomaticCardChoiceKind choiceKind)
        {
            IReadOnlyList<AutomaticCardOptionObservation> options;
            CardEffectKind effectKind;
            switch (choiceKind)
            {
                case AutomaticCardChoiceKind.PoisonDecision:
                    effectKind = CardEffectKind.Poison;
                    options = Options(
                        new AutomaticCardOptionObservation(0, null, null),
                        new AutomaticCardOptionObservation(1, null, null));
                    break;
                case AutomaticCardChoiceKind.ResurrectionHerbDecision:
                    effectKind = CardEffectKind.ResurrectionHerb;
                    options = Options(
                        new AutomaticCardOptionObservation(0, null, null),
                        new AutomaticCardOptionObservation(1, null, null));
                    break;
                case AutomaticCardChoiceKind.LieDetectorNumber:
                    effectKind = CardEffectKind.LieDetector;
                    options = Enumerable.Range(1, 10)
                        .Select(number =>
                            new AutomaticCardOptionObservation(
                                number,
                                number,
                                null))
                        .ToArray();
                    break;
                case AutomaticCardChoiceKind.FlamethrowerOwnerDiscard:
                case AutomaticCardChoiceKind.FlamethrowerOpponentDiscard:
                    effectKind = CardEffectKind.Flamethrower;
                    options = Options(
                        new AutomaticCardOptionObservation(-1, null, null),
                        new AutomaticCardOptionObservation(41, null, 9));
                    break;
                case AutomaticCardChoiceKind.PocketWatchManualCard:
                    effectKind = CardEffectKind.PocketWatch;
                    options = Options(
                        new AutomaticCardOptionObservation(-1, null, null),
                        new AutomaticCardOptionObservation(42, null, 10));
                    break;
                case AutomaticCardChoiceKind.PocketWatchSourceDisposition:
                    effectKind = CardEffectKind.PocketWatch;
                    options = Options(
                        new AutomaticCardOptionObservation(0, null, null),
                        new AutomaticCardOptionObservation(1, null, null));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(choiceKind));
            }

            return new AutomaticCardDecisionObservation(
                effectKind,
                choiceKind,
                CombatantSide.Enemy,
                CombatantSide.Enemy,
                playerPublicTotal: 16,
                enemyPublicTotal: 18,
                playerSoul: 12,
                enemySoul: 4,
                options,
                new[]
                {
                    new EnemyNumberInference(4, 40),
                    new EnemyNumberInference(7, 60)
                });
        }

        private static IReadOnlyList<AutomaticCardOptionObservation> Options(
            params AutomaticCardOptionObservation[] options)
        {
            return options;
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<BlackjackCard> playerCards,
            IReadOnlyList<BlackjackCard> enemyCards,
            IEnemyBehaviorPolicy enemyPolicy,
            IAutomaticCardDecisionPolicy automaticPolicy = null)
        {
            if (automaticPolicy == null)
            {
                return new CoreLoopBattle(
                    BlackjackDeck.CreateInDrawOrder(playerCards),
                    BlackjackDeck.CreateInDrawOrder(enemyCards),
                    playerMaximumSoul: 12,
                    enemyMaximumSoul: 4,
                    enemyPolicy);
            }

            return new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(playerCards),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                enemyAutomaticCardDecisionPolicy: automaticPolicy);
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
                CardDefinition definition = value is CardDefinition card
                    ? card
                    : CardDefinitionCatalog.GetDefaultForRank((int)value);
                return new BlackjackCard(startId + index, definition);
            }).ToArray();
        }

        private static CardDefinition Automatic(string definitionKey)
        {
            return CardDefinitionCatalog.GetByKey(definitionKey);
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(
                    EnemyActionType.Stand,
                    "ac06-test-stand");
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
                return new EnemyDecision(action, "ac06-test-sequence");
            }
        }

        private sealed class InvalidAutomaticPolicy :
            IAutomaticCardDecisionPolicy
        {
            public AutomaticCardDecision Decide(
                AutomaticCardDecisionObservation observation)
            {
                return new AutomaticCardDecision(
                    int.MaxValue,
                    "ac06-test-invalid");
            }
        }
    }
}
