using System.Reflection;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class ShopControllerTests
    {
        private GameObject _root;
        private ShopController _shop;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Shop Controller Test Root");
            _shop = _root.AddComponent<ShopController>();
            SetPrivateField("goldPerWin", 20);
            SetPrivateField("lighterPrice", 2);
            SetPrivateField("whiskeyPrice", 3);
            SetPrivateField("utilityPriceIncreasePerUsedVisit", 1);
            SetPrivateField("whiskeySoulRestore", 2);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void RFM01_U01_UtilityPricesIncreaseOncePerUsedShop()
        {
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(2));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out int firstRestore),
                Is.True);
            Assert.That(firstRestore, Is.EqualTo(2));
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out _),
                Is.False);
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));

            _shop.Close();
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(3));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(4));
            Assert.That(_shop.TryPurchaseLighterRemoval(2), Is.True);
            Assert.That(_shop.TryPurchaseLighterRemoval(2), Is.False);
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out int secondRestore),
                Is.True);
            Assert.That(secondRestore, Is.EqualTo(2));

            _shop.Close();
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(4));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(5));
        }

        [Test]
        public void RFM01_U02_UnusedShopDoesNotIncreaseUtilityPricesAndRunResetClearsThem()
        {
            _shop.Open();
            _shop.Close();
            _shop.Open();

            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(2));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
            Assert.That(
                _shop.TryPurchaseWhiskey(5, 12, out _),
                Is.True);

            _shop.Close();
            _shop.ResetRunEconomy();

            Assert.That(_shop.Gold, Is.Zero);
            Assert.That(_shop.CurrentLighterPrice, Is.EqualTo(2));
            Assert.That(_shop.CurrentWhiskeyPrice, Is.EqualTo(3));
        }

        [Test]
        public void RFM01_U03_AllCardSlotsCanBePurchasedWithoutAVisitLimit()
        {
            GameObject holderObject = new GameObject("Normal Card Holder");
            holderObject.transform.SetParent(_root.transform);
            GameObject prefabObject = new GameObject("Normal Card Prefab");
            prefabObject.transform.SetParent(_root.transform);
            CardView prefab = prefabObject.AddComponent<CardView>();
            SetPrivateField("normalCardHolder", holderObject.transform);
            SetPrivateField("normalCardPrefab", prefab);
            SetPrivateField("normalCardOfferCount", 3);
            SetPrivateField("normalCardPrice", 3);

            _shop.Open();
            CardView[] offers = holderObject.GetComponentsInChildren<CardView>(true);

            Assert.That(offers, Has.Length.EqualTo(3));
            foreach (CardView offer in offers)
            {
                Assert.That(
                    _shop.TryPurchaseNormalCard(
                        offer.CardId,
                        out string definitionKey,
                        out _),
                    Is.True);
                Assert.That(definitionKey, Is.Not.Empty);
            }

            Assert.That(_shop.Gold, Is.EqualTo(11));
            Assert.That(
                _shop.TryPurchaseNormalCard(offers[0].CardId, out _, out _),
                Is.False);
            Assert.That(_shop.Gold, Is.EqualTo(11));
        }

        private void SetPrivateField(string fieldName, object value)
        {
            FieldInfo field = typeof(ShopController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(_shop, value);
        }
    }
}
