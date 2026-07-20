using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CardEffectFoundationTests
    {
        private const int ChoiceOptionId = 42;

        [Test]
        public void CU02_U01_AvailabilityReportsMachineReasonWithoutMutation()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 1 },
                enemyRanks: new[] { 10, 7 });
            battle.Start();
            BlackjackCard manualCard = battle.Player.Hand.Cards[0];
            BlackjackCard passiveCard = battle.Player.Hand.Cards[1];

            CardUseAvailability[] availability = battle.PlayerCardUseAvailability.ToArray();

            Assert.That(availability.Length, Is.EqualTo(2));
            Assert.That(availability.Single(item => item.CardId == manualCard.Id).CanUse, Is.False);
            Assert.That(
                availability.Single(item => item.CardId == manualCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.EffectNotImplemented));
            Assert.That(
                availability.Single(item => item.CardId == passiveCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.CardIsNotManual));
            Assert.That(manualCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(manualCard.IsFaceUp, Is.True);
            Assert.That(passiveCard.IsFaceUp, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void CU02_U02_CardUseOutsidePlayerTurnIsRejectedWithoutMutation()
        {
            var handler = new TestChoiceEffectHandler();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 5, 2, 3 },
                enemyRanks: new[] { 10, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];
            Assert.That(battle.TryBeginPlayerChange(), Is.True);
            int enemyHandCount = battle.Enemy.Hand.Count;

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(sourceCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.NotPlayerTurn));
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(handler.BeginCount, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerChoosingChangeCard));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(enemyHandCount));
        }

        [Test]
        public void CU02_U03_MissingPassiveAndUsedCardsAreRejected()
        {
            var handler = new TestChoiceEffectHandler();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 1 },
                enemyRanks: new[] { 10, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard manualCard = battle.Player.Hand.Cards[0];
            BlackjackCard passiveCard = battle.Player.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(999), Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(999).Reason,
                Is.EqualTo(CardUseUnavailableReason.CardNotInHand));
            Assert.That(battle.TryBeginPlayerCardUse(passiveCard.Id), Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(passiveCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.CardIsNotManual));

            Assert.That(manualCard.TryBeginUse(), Is.True);
            Assert.That(manualCard.TryCompleteUse(), Is.True);
            Assert.That(battle.TryBeginPlayerCardUse(manualCard.Id), Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(manualCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.CardIsUnavailable));
            Assert.That(handler.BeginCount, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
        }

        [Test]
        public void CU02_U04_UnmetRequirementsDoNotRevealOrBeginCardUse()
        {
            var handler = new TestChoiceEffectHandler(canStart: false);
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 10, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            int enemyHandCount = battle.Enemy.Hand.Count;

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.False);
            Assert.That(sourceCard.IsFaceUp, Is.False);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.LastCardEffectResult, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(enemyHandCount));
            Assert.That(handler.BeginCount, Is.Zero);
        }

        [Test]
        public void CU02_U05_ApprovedHiddenCardIsRevealedAndWaitsForChoice()
        {
            var handler = new TestChoiceEffectHandler();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 10, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.IsFaceUp, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Resolving));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
            Assert.That(battle.PendingPlayerCardEffect.SourceCardId, Is.EqualTo(sourceCard.Id));
            Assert.That(battle.PendingPlayerCardEffect.EffectKind, Is.EqualTo(CardEffectKind.AutoPistol));
            Assert.That(battle.PendingPlayerCardEffect.ChoiceKind, Is.EqualTo(CardEffectChoiceKind.DeclareNumber));
            Assert.That(battle.PendingPlayerCardEffect.Options.Single().Id, Is.EqualTo(ChoiceOptionId));
            Assert.That(handler.BeginCount, Is.EqualTo(1));
        }

        [Test]
        public void CU02_U06_PendingEffectRejectsEveryOtherPlayerAction()
        {
            var handler = new TestChoiceEffectHandler();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7, 2, 3 },
                enemyRanks: new[] { 10, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard otherCard = battle.Player.Hand.Cards[0];
            battle.TryBeginPlayerCardUse(sourceCard.Id);
            int enemyHandCount = battle.Enemy.Hand.Count;

            Assert.That(battle.TryPlayerHit(), Is.False);
            Assert.That(battle.TryPlayerStand(), Is.False);
            Assert.That(battle.TryBeginPlayerChange(), Is.False);
            Assert.That(battle.TrySelectChangedCard(0), Is.False);
            Assert.That(battle.TryBeginPlayerCardUse(otherCard.Id), Is.False);
            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.False);
            Assert.That(
                battle.EvaluatePlayerCardUse(otherCard.Id).Reason,
                Is.EqualTo(CardUseUnavailableReason.EffectInProgress));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(enemyHandCount));
            Assert.That(handler.ResolveCount, Is.Zero);
        }

        [Test]
        public void CU02_U07_InvalidOptionPreservesPendingEffect()
        {
            var handler = new TestChoiceEffectHandler();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 10, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.TryBeginPlayerCardUse(sourceCard.Id);
            PendingCardEffect pending = battle.PendingPlayerCardEffect;

            bool accepted = battle.TryResolvePlayerCardChoice(999);

            Assert.That(accepted, Is.False);
            Assert.That(battle.PendingPlayerCardEffect, Is.SameAs(pending));
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Resolving));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
            Assert.That(handler.ResolveCount, Is.Zero);
        }

        [Test]
        public void CU02_U08_ValidChoiceCompletesOnceAndRunsOneEnemyTurn()
        {
            var handler = new TestChoiceEffectHandler();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 5, 5, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.TryBeginPlayerCardUse(sourceCard.Id);

            bool accepted = battle.TryResolvePlayerCardChoice(ChoiceOptionId);
            int enemyHandCountAfterCompletion = battle.Enemy.Hand.Count;
            bool acceptedAgain = battle.TryResolvePlayerCardChoice(ChoiceOptionId);

            Assert.That(accepted, Is.True);
            Assert.That(acceptedAgain, Is.False);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.LastCardEffectResult.Value.SourceCardId, Is.EqualTo(sourceCard.Id));
            Assert.That(battle.LastCardEffectResult.Value.Succeeded, Is.True);
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.False);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(enemyHandCountAfterCompletion, Is.EqualTo(3));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(enemyHandCountAfterCompletion));
            Assert.That(handler.ResolveCount, Is.EqualTo(1));
        }

        [Test]
        public void CU02_U09_EffectRoundEndSkipsEnemyTurnAndRecordsCause()
        {
            var handler = new TestChoiceEffectHandler(endsRound: true);
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 5, 5, 7 },
                handler: handler,
                enemyMaximumSoul: 1);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.TryBeginPlayerCardUse(sourceCard.Id);

            bool accepted = battle.TryResolvePlayerCardChoice(ChoiceOptionId);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(battle.Enemy.Soul.Current, Is.Zero);
            Assert.That(battle.Enemy.Deck.DrawCount, Is.EqualTo(1));
            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(battle.LastResolution.Value.Cause, Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(battle.LastResolution.Value.SourceCardKey, Is.EqualTo("auto-pistol-7"));
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.True);
            Assert.That(handler.ResolveCount, Is.EqualTo(1));
        }

        [Test]
        public void CU02_U10_HandIdQueriesAndTakePreserveOtherCards()
        {
            var hand = new BlackjackHand();
            var visibleCard = new BlackjackCard(0, 5, isFaceUp: true);
            var hiddenCard = new BlackjackCard(1, 7);
            hand.Add(visibleCard);
            hand.Add(hiddenCard);

            Assert.That(hand.Contains(0), Is.True);
            Assert.That(hand.Contains(99), Is.False);
            Assert.That(hand.TryGetCard(1, out BlackjackCard found), Is.True);
            Assert.That(found, Is.SameAs(hiddenCard));
            Assert.That(hand.GetFaceUpCards(), Is.EqualTo(new[] { visibleCard }));

            Assert.That(hand.TryTakeCard(1, out BlackjackCard taken), Is.True);
            Assert.That(taken, Is.SameAs(hiddenCard));
            Assert.That(hand.TryTakeCard(1, out _), Is.False);
            Assert.That(hand.Cards, Is.EqualTo(new[] { visibleCard }));
        }

        [Test]
        public void CU02_U11_DeckTakeAndReturnPreserveNextDrawOrderAtomically()
        {
            BlackjackDeck deck = CreateDeck(new[] { 2, 3, 4 });

            IReadOnlyList<BlackjackCard> taken = deck.TakeTop(2);

            Assert.That(taken.Select(card => card.Rank), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(deck.DrawCount, Is.EqualTo(1));
            Assert.That(deck.CardsInPlayCount, Is.EqualTo(2));

            Assert.Throws<InvalidOperationException>(() =>
                deck.ReturnToTop(new[] { taken[0], taken[0] }));
            Assert.That(deck.DrawCount, Is.EqualTo(1));
            Assert.That(deck.CardsInPlayCount, Is.EqualTo(2));

            deck.ReturnToTop(taken);

            Assert.That(deck.Draw().Rank, Is.EqualTo(2));
            Assert.That(deck.Draw().Rank, Is.EqualTo(3));
            Assert.That(deck.Draw().Rank, Is.EqualTo(4));
        }

        [Test]
        public void CU02_U12_RoundResolutionDistinguishesNumericAndCardEffectBusts()
        {
            RoundResolution numericBust = RoundResolver.Resolve(
                2,
                CreateCards(10, 8, 4),
                CreateCards(10, 7));
            RoundResolution cardEffectBust = RoundResolver.ResolveCardEffectBust(
                3,
                playerIsTarget: false,
                sourceCardKey: "auto-pistol-7");

            Assert.That(numericBust.Cause, Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(cardEffectBust.Cause, Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(cardEffectBust.EnemyDamage, Is.EqualTo(1));
            Assert.That(cardEffectBust.SourceCardKey, Is.EqualTo("auto-pistol-7"));
        }

        [Test]
        public void CU02_U13_CoreLoopSessionForwardsCardEffectFlow()
        {
            var handler = new TestChoiceEffectHandler();
            var session = new CoreLoopSession(() => CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 10, 7 },
                handler: handler));
            BlackjackCard sourceCard = session.Battle.Player.Hand.Cards[1];

            bool began = session.TryBeginPlayerCardUse(sourceCard.Id);
            bool resolved = session.TryResolvePlayerCardChoice(ChoiceOptionId);

            Assert.That(began, Is.True);
            Assert.That(resolved, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(handler.BeginCount, Is.EqualTo(1));
            Assert.That(handler.ResolveCount, Is.EqualTo(1));
        }

        [Test]
        public void CU02_U14_PendingEffectCopiesAndValidatesOptions()
        {
            var options = new List<CardEffectChoiceOption>
            {
                new CardEffectChoiceOption(0, "Seven", numericValue: 7)
            };
            var pending = new PendingCardEffect(
                3,
                CardEffectKind.AutoPistol,
                "Declare a number.",
                CardEffectChoiceKind.DeclareNumber,
                options);

            options.Clear();

            Assert.That(pending.Options.Count, Is.EqualTo(1));
            Assert.That(pending.Options[0].NumericValue, Is.EqualTo(7));
            Assert.Throws<ArgumentException>(() => new PendingCardEffect(
                3,
                CardEffectKind.AutoPistol,
                "Declare a number.",
                CardEffectChoiceKind.DeclareNumber,
                new[]
                {
                    new CardEffectChoiceOption(0, "Seven"),
                    new CardEffectChoiceOption(0, "Eight")
                }));
        }

        [Test]
        public void CU02_U15_ImmediateEffectCompletionRunsEnemyTurnWithoutPendingState()
        {
            var handler = new ImmediateTestEffectHandler();
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 5, 5, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            bool accepted = battle.TryBeginPlayerCardUse(sourceCard.Id);

            Assert.That(accepted, Is.True);
            Assert.That(sourceCard.IsFaceUp, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.False);
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(3));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(handler.BeginCount, Is.EqualTo(1));
        }

        [Test]
        public void CU02_U16_ValidChoiceCanAdvanceToOneMorePendingChoice()
        {
            var handler = new TestChoiceEffectHandler(requiredChoiceCount: 2);
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 5, 7 },
                enemyRanks: new[] { 5, 5, 7 },
                handler: handler);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            battle.TryBeginPlayerCardUse(sourceCard.Id);

            bool firstAccepted = battle.TryResolvePlayerCardChoice(ChoiceOptionId);

            Assert.That(firstAccepted, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Resolving));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
            Assert.That(battle.PendingPlayerCardEffect.Options.Single().Id, Is.EqualTo(ChoiceOptionId + 1));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2));
            Assert.That(battle.TryResolvePlayerCardChoice(ChoiceOptionId), Is.False);

            bool secondAccepted = battle.TryResolvePlayerCardChoice(ChoiceOptionId + 1);

            Assert.That(secondAccepted, Is.True);
            Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(battle.PendingPlayerCardEffect, Is.Null);
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(3));
            Assert.That(handler.ResolveCount, Is.EqualTo(2));
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            ICardEffectHandler handler = null,
            int enemyMaximumSoul = 3)
        {
            var resolver = handler == null
                ? new CardEffectResolver()
                : new CardEffectResolver(handler);
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: enemyMaximumSoul,
                enemyPolicy: null,
                cardEffectResolver: resolver);
        }

        private static BlackjackDeck CreateDeck(IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Count);
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return cards;
        }

        private sealed class TestChoiceEffectHandler : ICardEffectHandler
        {
            private readonly bool _canStart;
            private readonly bool _endsRound;
            private readonly int _requiredChoiceCount;

            public TestChoiceEffectHandler(
                bool canStart = true,
                bool endsRound = false,
                int requiredChoiceCount = 1)
            {
                if (requiredChoiceCount <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(requiredChoiceCount));
                }

                _canStart = canStart;
                _endsRound = endsRound;
                _requiredChoiceCount = requiredChoiceCount;
            }

            public int BeginCount { get; private set; }

            public CardEffectKind EffectKind => CardEffectKind.AutoPistol;

            public int ResolveCount { get; private set; }

            public bool CanStart(CardEffectContext context)
            {
                return _canStart;
            }

            public CardEffectStep Begin(CardEffectContext context)
            {
                BeginCount++;
                return CardEffectStep.AwaitChoice(CreatePendingEffect(
                    context.SourceCard.Id,
                    ChoiceOptionId));
            }

            public CardEffectStep ResolveChoice(
                CardEffectContext context,
                PendingCardEffect pendingEffect,
                CardEffectChoiceOption selectedOption)
            {
                ResolveCount++;
                if (ResolveCount < _requiredChoiceCount)
                {
                    return CardEffectStep.AwaitChoice(CreatePendingEffect(
                        context.SourceCard.Id,
                        ChoiceOptionId + ResolveCount));
                }

                var result = new CardEffectResult(
                    context.SourceCard.Id,
                    EffectKind,
                    succeeded: true,
                    endedRound: _endsRound);
                return _endsRound
                    ? CardEffectStep.Complete(
                        result,
                        context.CreateEnemyCardEffectBustResolution())
                    : CardEffectStep.Complete(result);
            }

            private static PendingCardEffect CreatePendingEffect(int sourceCardId, int optionId)
            {
                return new PendingCardEffect(
                    sourceCardId,
                    CardEffectKind.AutoPistol,
                    "Declare a number.",
                    CardEffectChoiceKind.DeclareNumber,
                    new[]
                    {
                        new CardEffectChoiceOption(
                            optionId,
                            "Seven",
                            numericValue: 7)
                    });
            }
        }

        private sealed class ImmediateTestEffectHandler : ICardEffectHandler
        {
            public int BeginCount { get; private set; }

            public CardEffectKind EffectKind => CardEffectKind.AutoPistol;

            public bool CanStart(CardEffectContext context)
            {
                return true;
            }

            public CardEffectStep Begin(CardEffectContext context)
            {
                BeginCount++;
                return CardEffectStep.Complete(new CardEffectResult(
                    context.SourceCard.Id,
                    EffectKind,
                    succeeded: true,
                    endedRound: false));
            }

            public CardEffectStep ResolveChoice(
                CardEffectContext context,
                PendingCardEffect pendingEffect,
                CardEffectChoiceOption selectedOption)
            {
                throw new InvalidOperationException("Immediate test effect has no choices.");
            }
        }
    }
}
