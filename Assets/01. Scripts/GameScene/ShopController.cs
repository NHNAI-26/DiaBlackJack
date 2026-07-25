using System;
using System.Collections.Generic;
using Border.Core;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// GameScene-local shop for the MVP. It grants placeholder gold, hides combat table objects,
    /// shows merchant goods, and owns the temporary demon-card offers on the table.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopController : MonoBehaviour
    {
        [Tooltip("The enemy character that becomes the merchant while the shop is open.")]
        [SerializeField] private CharacterView merchant;
        [Tooltip("Root of the shop-item sprites shown on the table; toggled active only while the shop is open.")]
        [SerializeField] private GameObject itemsRoot;
        [Tooltip("Parent for demon-card shop offers.")]
        [SerializeField] private Transform demonCardHolder;
        [Tooltip("World-space demon-card prefab used for shop offers.")]
        [SerializeField] private DemonCardView demonCardPrefab;
        [Tooltip("Combat-only table objects hidden while the shop is open and restored on close.")]
        [SerializeField] private GameObject[] combatTableObjects;
        [Tooltip("Gold granted once per battle victory.")]
        [SerializeField] private int goldPerWin = 3;
        [SerializeField] private int demonCardPrice = 3;
        [SerializeField] private int demonCardOfferCount = 3;
        [SerializeField] private float demonCardSpacing = 1.1f;
        [SerializeField] private int shopRandomSeed = 20260726;

        private readonly List<DemonCardOffer> _demonOffers = new List<DemonCardOffer>();
        private readonly DeterministicRng _random = new DeterministicRng();
        private int _nextOfferId;
        private int _openCount;

        public bool IsOpen { get; private set; }

        public int Gold { get; private set; }

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            IsOpen = true;
            Gold += goldPerWin;
            GenerateDemonCardOffers();

            if (merchant != null)
            {
                merchant.EnterMerchant();
            }

            SetCombatTableActive(false);

            if (itemsRoot != null)
            {
                itemsRoot.SetActive(true);
            }
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            IsOpen = false;

            if (merchant != null)
            {
                merchant.ExitMerchant();
            }

            if (itemsRoot != null)
            {
                itemsRoot.SetActive(false);
            }

            SetCombatTableActive(true);
        }

        public void ResetGold()
        {
            Gold = 0;
            RefreshDemonOfferViews();
        }

        public bool TryPurchaseDemonCard(int offerId, out string definitionKey)
        {
            definitionKey = null;
            if (!IsOpen)
            {
                return false;
            }

            DemonCardOffer offer = FindDemonOffer(offerId);
            if (offer == null || offer.SoldOut || Gold < offer.Price)
            {
                return false;
            }

            Gold -= offer.Price;
            offer.SoldOut = true;
            definitionKey = offer.DefinitionKey;

            if (offer.View != null)
            {
                offer.View.SetHovered(false);
            }

            RefreshDemonOfferViews();
            return true;
        }

        private void SetCombatTableActive(bool active)
        {
            if (combatTableObjects == null)
            {
                return;
            }

            foreach (GameObject tableObject in combatTableObjects)
            {
                if (tableObject != null)
                {
                    tableObject.SetActive(active);
                }
            }
        }

        private void GenerateDemonCardOffers()
        {
            ClearDemonCardOffers();
            if (demonCardHolder == null || demonCardPrefab == null)
            {
                return;
            }

            DemonContractCatalog catalog = DemonContractCatalog.Default;
            var definitions = new List<DemonContractDefinition>(catalog.Definitions);
            Shuffle(definitions);

            int count = Mathf.Min(demonCardOfferCount, definitions.Count);
            for (int i = 0; i < count; i++)
            {
                DemonContractDefinition definition = definitions[i];
                DemonCardView view = Instantiate(demonCardPrefab, demonCardHolder);
                view.transform.localRotation = Quaternion.identity;

                var offer = new DemonCardOffer(
                    _nextOfferId++,
                    definition,
                    FaceSpriteIndexFor(definition.Key),
                    demonCardPrice,
                    view);
                _demonOffers.Add(offer);
                BindOfferView(offer);
            }

            LayoutDemonOfferViews();
        }

        private void ClearDemonCardOffers()
        {
            foreach (DemonCardOffer offer in _demonOffers)
            {
                if (offer.View != null)
                {
                    Destroy(offer.View.gameObject);
                }
            }

            _demonOffers.Clear();
        }

        private void RefreshDemonOfferViews()
        {
            foreach (DemonCardOffer offer in _demonOffers)
            {
                BindOfferView(offer);
            }

            LayoutDemonOfferViews();
        }

        private void BindOfferView(DemonCardOffer offer)
        {
            if (offer == null || offer.View == null)
            {
                return;
            }

            if (offer.SoldOut)
            {
                offer.View.gameObject.SetActive(false);
                return;
            }

            offer.View.gameObject.SetActive(true);
            DemonContractDefinition definition = offer.Definition;
            string costSummary = "PRICE " + offer.Price + " GOLD";
            if (!string.IsNullOrEmpty(definition.CostSummary))
            {
                costSummary += "\n" + definition.CostSummary;
            }

            offer.View.Bind(new GameSceneDemonCardViewModel(
                offer.OfferId,
                offer.FaceSpriteIndex,
                isFaceUp: true,
                canUse: Gold >= offer.Price,
                definition.DisplayName,
                definition.Summary,
                costSummary));
        }

        private void LayoutDemonOfferViews()
        {
            int visibleCount = 0;
            foreach (DemonCardOffer offer in _demonOffers)
            {
                if (offer != null && !offer.SoldOut && offer.View != null)
                {
                    visibleCount++;
                }
            }

            if (visibleCount == 0)
            {
                return;
            }

            float offset = -(visibleCount - 1) * 0.5f * demonCardSpacing;
            int visibleIndex = 0;
            foreach (DemonCardOffer offer in _demonOffers)
            {
                if (offer == null || offer.SoldOut || offer.View == null)
                {
                    continue;
                }

                offer.View.transform.localPosition = new Vector3(
                    offset + visibleIndex * demonCardSpacing,
                    0f,
                    visibleIndex * 0.01f);
                offer.View.transform.localRotation = Quaternion.identity;
                visibleIndex++;
            }
        }

        private DemonCardOffer FindDemonOffer(int offerId)
        {
            foreach (DemonCardOffer offer in _demonOffers)
            {
                if (offer.OfferId == offerId)
                {
                    return offer;
                }
            }

            return null;
        }

        private void Shuffle(List<DemonContractDefinition> definitions)
        {
            _random.Reseed(shopRandomSeed + _openCount++);
            for (int i = definitions.Count - 1; i > 0; i--)
            {
                int swapIndex = _random.Next(i + 1);
                DemonContractDefinition definition = definitions[i];
                definitions[i] = definitions[swapIndex];
                definitions[swapIndex] = definition;
            }
        }

        private static int FaceSpriteIndexFor(string definitionKey)
        {
            if (StringComparer.Ordinal.Equals(definitionKey, DemonContractCatalog.SatanKey))
            {
                return 1;
            }

            if (StringComparer.Ordinal.Equals(definitionKey, DemonContractCatalog.BelphegorKey))
            {
                return 2;
            }

            if (StringComparer.Ordinal.Equals(definitionKey, DemonContractCatalog.MammonKey))
            {
                return 3;
            }

            if (StringComparer.Ordinal.Equals(definitionKey, DemonContractCatalog.LeviathanKey))
            {
                return 4;
            }

            return 1;
        }

        private sealed class DemonCardOffer
        {
            public DemonCardOffer(
                int offerId,
                DemonContractDefinition definition,
                int faceSpriteIndex,
                int price,
                DemonCardView view)
            {
                OfferId = offerId;
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                FaceSpriteIndex = faceSpriteIndex;
                Price = price;
                View = view;
            }

            public DemonContractDefinition Definition { get; }

            public string DefinitionKey => Definition.Key;

            public int FaceSpriteIndex { get; }

            public int OfferId { get; }

            public int Price { get; }

            public bool SoldOut { get; set; }

            public DemonCardView View { get; }
        }
    }
}
