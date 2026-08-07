using System.Reflection;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class ShopPriceBadgeTests
    {
        [Test]
        [Category("GSV06")]
        public void GSV06_U06_HudPriceBadgeShowsPriceOnlyAndSoldOut()
        {
            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/UI/HUD.prefab");
            GameObject pricePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/UI/GameScene/ShopPriceBadge.prefab");
            Assert.That(hudPrefab, Is.Not.Null);
            Assert.That(pricePrefab, Is.Not.Null);
            ShopPriceBadgeView pricePrefabView =
                pricePrefab.GetComponent<ShopPriceBadgeView>();
            Assert.That(pricePrefabView, Is.Not.Null);
            Assert.That(pricePrefabView.HasRequiredReferences, Is.True);
            int priceTextCount = 0;
            foreach (Component component in
                     pricePrefab.GetComponentsInChildren<Component>(true))
            {
                if (component.GetType().FullName ==
                    "TMPro.TextMeshProUGUI")
                {
                    priceTextCount++;
                }
            }

            Assert.That(priceTextCount, Is.EqualTo(1));
            Assert.That(pricePrefab.transform.Find("CardHoverHeader"), Is.Null);
            RectTransform authoredPriceRoot =
                pricePrefab.transform as RectTransform;
            Assert.That(authoredPriceRoot, Is.Not.Null);
            Assert.That(authoredPriceRoot.pivot.y, Is.EqualTo(1f));

            GameObject hudObject = Object.Instantiate(hudPrefab);
            GameObject cameraObject = new GameObject("Shop Price Test Camera");
            GameObject productObject = new GameObject("Shop Price Product");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                productObject.transform.position = new Vector3(0f, 0f, 5f);
                ShopPriceTarget target =
                    productObject.AddComponent<ShopPriceTarget>();
                SetPrivateField(target, "priceAnchor", productObject.transform);
                target.Bind("위협용 해머", 3, isSoldOut: false);

                GameHudView hud = hudObject.GetComponent<GameHudView>();
                Assert.That(hud, Is.Not.Null);
                hud.RenderShopPriceBadges(
                    new[] { target },
                    camera);

                ShopPriceBadgeView[] badges =
                    hudObject.GetComponentsInChildren<ShopPriceBadgeView>(true);
                Assert.That(badges, Has.Length.EqualTo(1));
                Assert.That(badges[0].gameObject.activeSelf, Is.True);
                Assert.That(
                    badges[0].transform.parent.name,
                    Is.EqualTo("ShopPriceBadgeContainer"));
                Assert.That(
                    badges[0].transform.parent.GetSiblingIndex(),
                    Is.Zero);
                Canvas priceCanvas =
                    badges[0].transform.parent.GetComponent<Canvas>();
                Assert.That(priceCanvas, Is.Not.Null);
                Assert.That(priceCanvas.overrideSorting, Is.True);
                Assert.That(priceCanvas.sortingOrder, Is.EqualTo(short.MinValue));
                RectTransform badgeRoot =
                    badges[0].transform as RectTransform;
                Assert.That(badgeRoot, Is.Not.Null);
                RectTransform prefabRoot =
                    pricePrefab.transform as RectTransform;
                Assert.That(prefabRoot, Is.Not.Null);
                Assert.That(
                    badgeRoot.rect.size,
                    Is.EqualTo(prefabRoot.rect.size));
                RectTransform badgeBounds =
                    badgeRoot.parent as RectTransform;
                Assert.That(badgeBounds, Is.Not.Null);
                badges[0].SetLocalPosition(
                    new Vector2(0f, badgeBounds.rect.yMin - 100f),
                    badgeBounds);
                float badgeBottom = badgeRoot.anchoredPosition.y -
                    badgeRoot.rect.height * badgeRoot.pivot.y;
                Assert.That(
                    badgeBottom,
                    Is.EqualTo(badgeBounds.rect.yMin).Within(0.1f));
                Assert.That(badges[0].Value, Does.Contain("× 3"));
                Assert.That(
                    badges[0].Value,
                    Does.Not.Contain("위협용 해머"));
                int graphicCount = 0;
                foreach (Component component in
                         badges[0].GetComponentsInChildren<Component>(true))
                {
                    PropertyInfo raycastTargetProperty = component.GetType()
                        .GetProperty("raycastTarget");
                    if (raycastTargetProperty == null ||
                        raycastTargetProperty.PropertyType != typeof(bool))
                    {
                        continue;
                    }

                    graphicCount++;
                    Assert.That(
                        (bool)raycastTargetProperty.GetValue(component, null),
                        Is.False);
                }

                Assert.That(graphicCount, Is.GreaterThanOrEqualTo(2));

                target.Bind("위협용 해머", 3, isSoldOut: true);
                hud.RenderShopPriceBadges(new[] { target }, camera);
                Assert.That(badges[0].Value, Is.EqualTo("품절"));

                productObject.SetActive(false);
                hud.RenderShopPriceBadges(new[] { target }, camera);
                Assert.That(badges[0].gameObject.activeSelf, Is.False);

                productObject.SetActive(true);
                hud.RenderShopPriceBadges(new[] { target }, camera);
                Assert.That(badges[0].gameObject.activeSelf, Is.True);

                hud.RenderShopPriceBadges(
                    System.Array.Empty<ShopPriceTarget>(),
                    camera);
                Assert.That(badges[0].gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(productObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(hudObject);
            }
        }

        private static void SetPrivateField(
            object target,
            string fieldName,
            object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }
    }
}
