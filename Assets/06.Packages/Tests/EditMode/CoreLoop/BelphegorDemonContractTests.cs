using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class BelphegorDemonContractTests
    {
        [Test]
        public void DC03_U01_HitCreatesOwnerOnlyPreviewWithoutDrawingOrLogging()
        {
            CoreLoopBattle battle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(EnemyActionType.Hit));
            int handCount = battle.Player.Hand.Count;
            int deckCount = battle.Player.Deck.AvailableCardCount;
            int publicActionCount = battle.PublicActionHistory.Count;

            Assert.That(battle.TryPlayerHit(), Is.True);

            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            PlayerDemonContractPreview preview = battle.PlayerDemonContractPreview;
            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.PlayerResolvingDemonContract));
            Assert.That(pending.Kind,
                Is.EqualTo(DemonContractInteractionKind.BelphegorTopCard));
            Assert.That(pending.ContractKind, Is.EqualTo(DemonContractKind.Belphegor));
            Assert.That(pending.Options.Count, Is.EqualTo(2));
            Assert.That(pending.Options.All(option =>
                !option.ContractCardId.HasValue && !option.NumericValue.HasValue), Is.True);
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.InteractionId, Is.EqualTo(pending.InteractionId));
            Assert.That(preview.ContractKind, Is.EqualTo(DemonContractKind.Belphegor));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(handCount));
            Assert.That(battle.Player.Deck.AvailableCardCount, Is.EqualTo(deckCount));
            Assert.That(battle.PublicActionHistory.Count, Is.EqualTo(publicActionCount));
        }

        [Test]
        public void DC03_U02_KeepOptionDrawsTheExactPreviewedCardFaceUp()
        {
            CoreLoopBattle battle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(
                    EnemyActionType.Hit,
                    EnemyActionType.Hit));
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            int previewCardId = battle.PlayerDemonContractPreview.CardId;

            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    BelphegorDemonContractHandler.KeepTopCardOptionId),
                Is.True);

            Assert.That(battle.Player.Hand.TryGetCard(
                previewCardId,
                out BlackjackCard drawnCard), Is.True);
            Assert.That(drawnCard.IsFaceUp, Is.True);
            Assert.That(battle.PlayerDemonContractPreview, Is.Null);
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.PublicActionHistory.Any(action =>
                action.ActorSide == CombatantSide.Player &&
                action.ActionType == PublicCombatActionType.Hit), Is.True);
        }

        [Test]
        public void DC03_U03_MoveBottomPreservesCardIdentityAndEndsTheTurnWithoutHit()
        {
            CoreLoopBattle battle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(
                    EnemyActionType.Hit,
                    EnemyActionType.Hit));
            int handCount = battle.Player.Hand.Count;
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            int previewCardId = battle.PlayerDemonContractPreview.CardId;

            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    BelphegorDemonContractHandler.MoveTopCardToBottomOptionId),
                Is.True);

            Assert.That(battle.Player.Hand.Count, Is.EqualTo(handCount));
            Assert.That(battle.PublicActionHistory.Any(action =>
                action.ActorSide == CombatantSide.Player &&
                action.ActionType == PublicCombatActionType.Hit), Is.False);
            int availableCount = battle.Player.Deck.AvailableCardCount;
            int[] remainingOrder = Enumerable.Range(0, availableCount)
                .Select(_ => battle.Player.Deck.Draw().Id)
                .ToArray();
            Assert.That(remainingOrder[remainingOrder.Length - 1],
                Is.EqualTo(previewCardId));
        }

        [Test]
        public void DC03_U04_StalePreviewInputDoesNotMoveOrDrawTheCard()
        {
            CoreLoopBattle battle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(EnemyActionType.Hit));
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            PlayerDemonContractPreview preview = battle.PlayerDemonContractPreview;
            int handCount = battle.Player.Hand.Count;
            int deckCount = battle.Player.Deck.AvailableCardCount;

            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId + 1,
                    BelphegorDemonContractHandler.KeepTopCardOptionId),
                Is.False);
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    optionId: 999),
                Is.False);

            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.SameAs(pending));
            Assert.That(battle.PlayerDemonContractPreview, Is.SameAs(preview));
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(handCount));
            Assert.That(battle.Player.Deck.AvailableCardCount, Is.EqualTo(deckCount));
        }

        [Test]
        public void DC03_U05_ChangeAndCardUseDoNotCreateTopCardPreview()
        {
            CoreLoopBattle changeBattle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(EnemyActionType.Hit));

            Assert.That(changeBattle.TryBeginPlayerChange(), Is.True);
            Assert.That(changeBattle.State,
                Is.EqualTo(CoreLoopState.PlayerChoosingChangeCard));
            Assert.That(changeBattle.PlayerDemonContractPreview, Is.Null);
            Assert.That(changeBattle.PendingPlayerDemonContractInteraction, Is.Null);

            CoreLoopBattle cardBattle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(EnemyActionType.Hit),
                CreateManualPlayerDeck());
            int manualCardId = cardBattle.Player.Hand.Cards[0].Id;
            Assert.That(cardBattle.CanUsePlayerCard(manualCardId), Is.True);

            Assert.That(cardBattle.TryBeginPlayerCardUse(manualCardId), Is.True);
            Assert.That(cardBattle.PlayerDemonContractPreview, Is.Null);
            Assert.That(cardBattle.PendingPlayerDemonContractInteraction, Is.Null);
        }

        [Test]
        public void DC03_U06_EnemyStandReservesOneAutoStandAfterTheNextAction()
        {
            CoreLoopBattle battle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(EnemyActionType.Stand));
            BelphegorRuntimeState runtimeState = GetBelphegorState(battle);
            int automaticStandSteps = 0;
            battle.Stepped += () =>
            {
                if (battle.RoundNumber == 1 &&
                    battle.State == CoreLoopState.PlayerTurn &&
                    battle.Player.IsStanding &&
                    battle.LastPublicAction?.ActorSide == CombatantSide.Player &&
                    battle.LastPublicAction.ActionType == PublicCombatActionType.Stand)
                {
                    automaticStandSteps++;
                }
            };

            Assert.That(battle.Enemy.IsStanding, Is.True);
            Assert.That(runtimeState.AutoStandPending, Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    BelphegorDemonContractHandler.MoveTopCardToBottomOptionId),
                Is.True);

            Assert.That(automaticStandSteps, Is.EqualTo(1));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
            Assert.That(battle.Player.IsStanding, Is.False);
            Assert.That(runtimeState.AutoStandPending, Is.False);
        }

        [Test]
        public void DC03_U07_KeptCardUsesVisibleSumForImmediateNumericBust()
        {
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(0, 10),
                new BlackjackCard(1, 1),
                new BlackjackCard(2, 10),
                new BlackjackCard(3, 2),
                new BlackjackCard(4, 3),
                new BlackjackCard(5, 4),
                new BlackjackCard(6, 5),
                new BlackjackCard(7, 6)
            });
            var enemyPolicy = new SequenceEnemyPolicy(
                EnemyActionType.Hit,
                EnemyActionType.Hit);
            CoreLoopBattle battle = CreateStartedBattle(playerDeck, enemyPolicy);

            Assert.That(battle.TryPlayerHit(), Is.True);
            ActivateBelphegor(battle);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;

            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    BelphegorDemonContractHandler.KeepTopCardOptionId),
                Is.True);

            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.PlayerBust));
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(battle.PlayerDemonContractPreview, Is.Null);
            Assert.That(GetBelphegorState(battle).AutoStandPending, Is.False);
        }

        [Test]
        public void DC03_U08_EnemyObservationAndPublicLogDoNotChangeDuringPreview()
        {
            CoreLoopBattle battle = CreateActiveBelphegorBattle(
                new SequenceEnemyPolicy(EnemyActionType.Hit));
            EnemyObservation before = EnemyObservationFactory.Create(battle, 17);
            Assert.That(battle.TryPlayerHit(), Is.True);
            EnemyObservation after = EnemyObservationFactory.Create(battle, 17);

            Assert.That(after.PlayerFaceUpCards.Select(card => card.Rank),
                Is.EqualTo(before.PlayerFaceUpCards.Select(card => card.Rank)));
            Assert.That(after.PlayerHiddenCardCount,
                Is.EqualTo(before.PlayerHiddenCardCount));
            Assert.That(after.PlayerDeckAvailableCount,
                Is.EqualTo(before.PlayerDeckAvailableCount));
            Assert.That(after.PublicActionHistory.Count,
                Is.EqualTo(before.PublicActionHistory.Count));
            Assert.That(after.NumberInferences.Select(item =>
                    $"{item.Number}:{item.ProbabilityPercent}"),
                Is.EqualTo(before.NumberInferences.Select(item =>
                    $"{item.Number}:{item.ProbabilityPercent}")));
        }

        private static CoreLoopBattle CreateActiveBelphegorBattle(
            SequenceEnemyPolicy enemyPolicy,
            BlackjackDeck playerDeck = null)
        {
            CoreLoopBattle battle = CreateStartedBattle(
                playerDeck ?? CreatePlainPlayerDeck(),
                enemyPolicy);
            ActivateBelphegor(battle);
            return battle;
        }

        private static CoreLoopBattle CreateStartedBattle(
            BlackjackDeck playerDeck,
            IEnemyBehaviorPolicy enemyPolicy)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                CreateEnemyDeck(),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 12,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                CreateBelphegorDeck(),
                DemonContractResolver.CreateDefault());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static void ActivateBelphegor(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);
            Assert.That(battle.ActivePlayerDemonContracts[0].Kind,
                Is.EqualTo(DemonContractKind.Belphegor));
        }

        private static BelphegorRuntimeState GetBelphegorState(CoreLoopBattle battle)
        {
            return (BelphegorRuntimeState)battle.ActivePlayerDemonContracts[0].RuntimeState;
        }

        private static BlackjackDeck CreatePlainPlayerDeck()
        {
            return BlackjackDeck.CreateInDrawOrder(Enumerable.Range(0, 10)
                .Select(id => new BlackjackCard(id, rank: 2)));
        }

        private static BlackjackDeck CreateManualPlayerDeck()
        {
            CardDefinition definition = CardDefinitionCatalog.GetByKey("crystal-orb-5");
            return BlackjackDeck.CreateInDrawOrder(Enumerable.Range(0, 10)
                .Select(id => new BlackjackCard(id, definition)));
        }

        private static BlackjackDeck CreateEnemyDeck()
        {
            return BlackjackDeck.CreateInDrawOrder(Enumerable.Range(100, 12)
                .Select(id => new BlackjackCard(id, rank: 1)));
        }

        private static DemonContractDeck CreateBelphegorDeck()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default.GetByKey(
                DemonContractCatalog.BelphegorKey);
            return new DemonContractDeck(Enumerable.Range(0, 4)
                .Select(id => new DemonContractCard(id, definition)), seed: 73);
        }

        private sealed class SequenceEnemyPolicy : IEnemyBehaviorPolicy
        {
            private readonly Queue<EnemyActionType> _actions;

            public SequenceEnemyPolicy(params EnemyActionType[] actions)
            {
                _actions = new Queue<EnemyActionType>(actions);
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionType action = _actions.Count > 0
                    ? _actions.Dequeue()
                    : EnemyActionType.Stand;
                return new EnemyDecision(action, "dc03-sequence");
            }
        }
    }
}
