using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class ResurrectionHerbAutomaticCardTests
    {
        private static readonly CardDefinition ResurrectionHerb =
            CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.ResurrectionHerbKey);

        [Test]
        public void ACRV03_U01_EachParticipantReceivesAnIndependentDecision()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ownerChoice.DecisionSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(ownerChoice.Options.Select(option => option.OptionId),
                Is.EquivalentTo(new[]
                {
                    ResurrectionHerbEffectHandler.DeclineOptionId,
                    ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId
                }));
            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);

            PendingAutomaticCardInteraction opponentChoice =
                battle.PendingAutomaticInteraction;
            Assert.That(opponentChoice.ChoiceKind,
                Is.EqualTo(AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision));
            Assert.That(opponentChoice.DecisionSide, Is.EqualTo(CombatantSide.Enemy));
        }

        [Test]
        public void ACRV03_U02_OnlyPlayerPaymentChangesPlayerSoulAndHand()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            IReadOnlyList<int> enemyIds = battle.Enemy.Hand.Cards
                .Select(card => card.Id).ToArray();
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);
            Assert.That(ResolveEnemy(battle,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(4));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(6));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Id),
                Is.EqualTo(enemyIds));
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void ACRV03_U03_OnlyEnemyPaymentChangesEnemySoulAndHand()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            IReadOnlyList<int> playerInitialIds = battle.Player.Hand.Cards
                .Select(card => card.Id).ToArray();
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);
            Assert.That(ResolveEnemy(battle,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.Player.Hand.Cards.Select(card => card.Id),
                Is.EqualTo(playerInitialIds.Append(ownerChoice.SourceCardId)));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void ACRV03_U04_BothPaymentsRedealEachParticipantOnce()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);
            Assert.That(ResolveEnemy(battle,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(4));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            AssertCardConservation(battle.Player);
            AssertCardConservation(battle.Enemy);
        }

        [Test]
        public void ACRV03_U05_BothDeclinesRetainSourceFaceUp()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            IReadOnlyList<int> playerInitialIds = battle.Player.Hand.Cards
                .Select(card => card.Id).ToArray();
            IReadOnlyList<int> enemyIds = battle.Enemy.Hand.Cards
                .Select(card => card.Id).ToArray();
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);
            int sourceCardId = ownerChoice.SourceCardId;

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);
            Assert.That(ResolveEnemy(battle,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);

            Assert.That(battle.Player.Hand.Cards.Select(card => card.Id),
                Is.EqualTo(playerInitialIds.Append(sourceCardId)));
            Assert.That(battle.Enemy.Hand.Cards.Select(card => card.Id),
                Is.EqualTo(enemyIds));
            Assert.That(battle.Player.Hand.TryGetCard(
                sourceCardId,
                out BlackjackCard sourceCard), Is.True);
            Assert.That(sourceCard.IsFaceUp, Is.True);
            Assert.That(battle.Player.Deck.GetDiscardedCards()
                .Any(card => card.Id == sourceCardId), Is.False);
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void ACRV03_U06_PaymentAtOneSoulKillsWithoutRedeal()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 1, enemySoul: 6);
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(0));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(3));
        }

        [Test]
        public void ACRV03_U07_SurvivingPayerReceivesOneHiddenAndOnePublicCard()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);
            Assert.That(ResolveEnemy(battle,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);

            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.Player.Hand.HiddenCardCount, Is.EqualTo(1));
            Assert.That(battle.Player.Hand.GetPublicCards(), Has.Count.EqualTo(1));
        }

        [Test]
        public void ACRV03_U08_NonPayerStandAndRoundStateRemainUnchanged()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            battle.Enemy.Stand();
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);
            Assert.That(ResolveEnemy(battle,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);

            Assert.That(battle.Enemy.IsStanding, Is.True);
            Assert.That(battle.RoundNumber, Is.EqualTo(1));
            Assert.That(battle.LastResolution.HasValue, Is.False);
            Assert.That(battle.LastRoundTransition.HasValue, Is.False);
        }

        [Test]
        public void ACRV03_U09_RedealDoesNotResolvePoisonRewardOrRoundDamage()
        {
            CoreLoopBattle battle = CreateBattle(playerSoul: 5, enemySoul: 6);
            battle.RegisterPoisonWinReward(900, CombatantSide.Player, 5);
            PendingAutomaticCardInteraction ownerChoice = BeginHerb(battle);

            Assert.That(ResolvePlayer(battle, ownerChoice,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);
            Assert.That(ResolveEnemy(battle,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.EqualTo(4));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(6));
            Assert.That(battle.PendingPoisonWinRewardCount, Is.EqualTo(1));
            Assert.That(battle.LastResolution.HasValue, Is.False);
        }

        [Test]
        public void ACRV03_I02_EnemyOwnedHerbHandsSecondDecisionToPlayer()
        {
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 10, 7, 2, 3)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100, 2, 3, ResurrectionHerb, 4, 5)),
                playerMaximumSoul: 5,
                playerCurrentSoul: 5,
                enemyMaximumSoul: 6,
                enemyPolicy: new HitThenStandPolicy(),
                cardEffectResolver: CardEffectResolver.CreateDefault(),
                enemyAutomaticCardDecisionPolicy: null);
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerStand(), Is.True);
            PendingAutomaticCardInteraction enemyChoice =
                battle.PendingAutomaticInteraction;
            Assert.That(enemyChoice.OwnerSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(enemyChoice.DecisionSide, Is.EqualTo(CombatantSide.Enemy));

            Assert.That(battle.TryResolveAutomaticCardChoice(
                CombatantSide.Enemy,
                enemyChoice.InteractionId,
                ResurrectionHerbEffectHandler.PaySoulAndRedealOptionId), Is.True);

            Assert.That(battle.PendingPlayerAutomaticInteraction, Is.Not.Null);
            Assert.That(battle.PendingPlayerAutomaticInteraction.ChoiceKind,
                Is.EqualTo(AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision));
            PendingAutomaticCardInteraction playerChoice =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(battle.TryResolvePlayerAutomaticCardChoice(
                playerChoice.InteractionId,
                ResurrectionHerbEffectHandler.DeclineOptionId), Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.PendingAutomaticInteraction, Is.Null);
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
        }

        private static PendingAutomaticCardInteraction BeginHerb(
            CoreLoopBattle battle)
        {
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction pending =
                battle.PendingPlayerAutomaticInteraction;
            Assert.That(pending, Is.Not.Null);
            return pending;
        }

        private static bool ResolvePlayer(
            CoreLoopBattle battle,
            PendingAutomaticCardInteraction pending,
            int optionId)
        {
            return battle.TryResolvePlayerAutomaticCardChoice(
                pending.InteractionId,
                optionId);
        }

        private static bool ResolveEnemy(CoreLoopBattle battle, int optionId)
        {
            PendingAutomaticCardInteraction pending =
                battle.PendingAutomaticInteraction;
            return battle.TryResolveAutomaticCardChoice(
                CombatantSide.Enemy,
                pending.InteractionId,
                optionId);
        }

        private static void AssertCardConservation(BattleParticipant participant)
        {
            Assert.That(
                participant.Hand.Count + participant.Deck.DrawCount +
                participant.Deck.DiscardCount,
                Is.EqualTo(participant.Deck.TotalCardCount));
        }

        private static CoreLoopBattle CreateBattle(int playerSoul, int enemySoul)
        {
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 2, 3, ResurrectionHerb, 4, 5, 6, 7)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100, 6, 7, 8, 9, 2, 3)),
                playerMaximumSoul: playerSoul,
                playerCurrentSoul: playerSoul,
                enemyMaximumSoul: enemySoul,
                enemyPolicy: new StandPolicy(),
                cardEffectResolver: CardEffectResolver.CreateDefault(),
                enemyAutomaticCardDecisionPolicy: null);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(
            int startId,
            params object[] values)
        {
            return values.Select((value, index) => new BlackjackCard(
                startId + index,
                value is CardDefinition definition
                    ? definition
                    : CardDefinitionCatalog.GetDefaultForRank((int)value)))
                .ToArray();
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "acrv03-stand");
            }
        }

        private sealed class HitThenStandPolicy : IEnemyBehaviorPolicy
        {
            private bool _hasHit;

            public EnemyDecision Decide(EnemyObservation observation)
            {
                if (!_hasHit)
                {
                    _hasHit = true;
                    return new EnemyDecision(EnemyActionType.Hit, "acrv03-hit");
                }

                return new EnemyDecision(EnemyActionType.Stand, "acrv03-stand");
            }
        }
    }
}
