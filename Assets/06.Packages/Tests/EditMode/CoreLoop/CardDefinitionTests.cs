using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CardDefinitionTests
    {
        [TestCase(1, "standard-ace-1", "에이스", CardActivationKind.Passive, CardEffectKind.None)]
        [TestCase(2, "standard-plain-2", "기본 카드", CardActivationKind.None, CardEffectKind.None)]
        [TestCase(3, "standard-plain-3", "기본 카드", CardActivationKind.None, CardEffectKind.None)]
        [TestCase(4, "standard-plain-4", "기본 카드", CardActivationKind.None, CardEffectKind.None)]
        [TestCase(5, "crystal-orb-5", "수정 구슬", CardActivationKind.Manual, CardEffectKind.CrystalOrb)]
        [TestCase(6, "threat-hammer-6", "위협용 해머", CardActivationKind.Manual, CardEffectKind.ThreatHammer)]
        [TestCase(7, "auto-pistol-7", "리볼버", CardActivationKind.Manual, CardEffectKind.AutoPistol)]
        [TestCase(8, "auto-pistol-8", "리볼버", CardActivationKind.Manual, CardEffectKind.AutoPistol)]
        [TestCase(9, "military-knife-9", "보위 나이프", CardActivationKind.Manual, CardEffectKind.MilitaryKnife)]
        [TestCase(10, "military-knife-10", "보위 나이프", CardActivationKind.Manual, CardEffectKind.MilitaryKnife)]
        public void CU_U01_DefaultCatalogMapsRankToStableDefinition(
            int rank,
            string expectedKey,
            string expectedDisplayName,
            CardActivationKind expectedActivation,
            CardEffectKind expectedEffect)
        {
            CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);

            Assert.That(definition.Key, Is.EqualTo(expectedKey));
            Assert.That(definition.DisplayName, Is.EqualTo(expectedDisplayName));
            Assert.That(definition.Rank, Is.EqualTo(rank));
            Assert.That(definition.Activation, Is.EqualTo(expectedActivation));
            Assert.That(definition.Effect, Is.EqualTo(expectedEffect));
            Assert.That(CardDefinitionCatalog.GetByKey(expectedKey), Is.SameAs(definition));
            Assert.That(CardDefinitionCatalog.All.Count, Is.EqualTo(17));
        }

        [Test]
        public void CU_U02_LegacyCardConstructorPreservesIdentityRankAndVisibility()
        {
            var card = new BlackjackCard(17, 7, isFaceUp: true);

            Assert.That(card.Id, Is.EqualTo(17));
            Assert.That(card.Rank, Is.EqualTo(7));
            Assert.That(card.DefinitionKey, Is.EqualTo("auto-pistol-7"));
            Assert.That(card.IsFaceUp, Is.True);
            Assert.That(card.UseState, Is.EqualTo(CardUseState.Unavailable));
        }

        [Test]
        public void CU_U03_UnknownDefinitionKeyFailsExplicitly()
        {
            Assert.Throws<ArgumentException>(() => CardDefinitionCatalog.GetByKey(" "));
            Assert.Throws<KeyNotFoundException>(() =>
                CardDefinitionCatalog.GetByKey("missing-card"));
        }

        [Test]
        public void CU_U04_OnlyManualCardBecomesAvailableWhenAddedToHand()
        {
            var hand = new BlackjackHand();
            var manualCard = new BlackjackCard(0, 5);
            var passiveCard = new BlackjackCard(1, 1);
            var plainCard = new BlackjackCard(2, 2);

            hand.Add(manualCard);
            hand.Add(passiveCard);
            hand.Add(plainCard);

            Assert.That(manualCard.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(manualCard.CanUse, Is.True);
            Assert.That(passiveCard.UseState, Is.EqualTo(CardUseState.Unavailable));
            Assert.That(plainCard.UseState, Is.EqualTo(CardUseState.Unavailable));
        }

        [Test]
        public void CU_U05_ManualCardMovesThroughUseStatesExactlyOnceWhileHeld()
        {
            var card = new BlackjackCard(0, 6);
            var hand = new BlackjackHand();
            hand.Add(card);

            Assert.That(card.TryCompleteUse(), Is.False);
            Assert.That(card.TryBeginUse(), Is.True);
            Assert.That(card.UseState, Is.EqualTo(CardUseState.Resolving));
            Assert.That(card.TryBeginUse(), Is.False);
            Assert.That(card.TryCompleteUse(), Is.True);
            Assert.That(card.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(card.TryBeginUse(), Is.False);
            Assert.That(card.CanUse, Is.False);
        }

        [Test]
        public void CU_U06_RedrawnPhysicalCardResetsItsUseState()
        {
            var card = new BlackjackCard(0, 9);
            BlackjackDeck deck = BlackjackDeck.CreateInDrawOrder(new[] { card });
            var hand = new BlackjackHand();

            BlackjackCard firstDraw = deck.Draw();
            hand.Add(firstDraw);
            Assert.That(firstDraw.TryBeginUse(), Is.True);
            Assert.That(firstDraw.TryCompleteUse(), Is.True);

            deck.Discard(hand.TakeAll());
            BlackjackCard secondDraw = deck.Draw();
            Assert.That(secondDraw, Is.SameAs(firstDraw));
            Assert.That(secondDraw.UseState, Is.EqualTo(CardUseState.Used));

            hand.Add(secondDraw);
            Assert.That(secondDraw.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(secondDraw.CanUse, Is.True);
        }

        [Test]
        public void CU_U07_DefinitionRejectsInvalidIdentityAndEnums()
        {
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                " ",
                "Card",
                1,
                CardActivationKind.None,
                CardEffectKind.None));
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                "card",
                " ",
                1,
                CardActivationKind.None,
                CardEffectKind.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardDefinition(
                "card",
                "Card",
                11,
                CardActivationKind.None,
                CardEffectKind.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardDefinition(
                "card",
                "Card",
                1,
                (CardActivationKind)99,
                CardEffectKind.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => new CardDefinition(
                "card",
                "Card",
                1,
                CardActivationKind.None,
                (CardEffectKind)99));
        }
    }
}
