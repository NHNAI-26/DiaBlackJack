using System;
using System.Collections.Generic;
using Border.Core;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// GameScene-local shop for the MVP. It grants placeholder gold, hides combat table objects,
    /// shows merchant goods, and owns the temporary card offers on the table.
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
        [Tooltip("Parent for normal card shop offers.")]
        [SerializeField] private Transform normalCardHolder;
        [Tooltip("World-space normal card prefab used for shop offers.")]
        [SerializeField] private CardView normalCardPrefab;
        [Tooltip("Temporary table item that removes one card from the player's deck.")]
        [SerializeField] private ShopUtilityItemView lighterItem;
        [Tooltip("Temporary table item that restores player soul for gold.")]
        [SerializeField] private ShopUtilityItemView whiskeyItem;
        [Tooltip("Combat-only table objects hidden while the shop is open and restored on close.")]
        [SerializeField] private GameObject[] combatTableObjects;
        [Tooltip("Gold granted once per battle victory.")]
        [SerializeField] private int goldPerWin = 3;
        [SerializeField] private int demonCardPrice = 3;
        [SerializeField] private int demonCardOfferCount = 3;
        [SerializeField] private float demonCardSpacing = 1.1f;
        [SerializeField] private int normalCardPrice = 3;
        [SerializeField] private int normalCardOfferCount = 3;
        [SerializeField] private float normalCardSpacing = 1.1f;
        [SerializeField] private int lighterPrice = 2;
        [SerializeField] private int whiskeyPrice = 2;
        [Tooltip("Added to both utility prices for every earlier shop where either utility was purchased.")]
        [SerializeField] private int utilityPriceIncreasePerUsedVisit = 1;
        [SerializeField] private int whiskeySoulRestore = 2;
        [SerializeField] private int shopRandomSeed = 20260726;

        private readonly List<DemonCardOffer> _demonOffers = new List<DemonCardOffer>();
        private readonly List<NormalCardOffer> _normalOffers = new List<NormalCardOffer>();
        private readonly DeterministicRng _random = new DeterministicRng();
        private int _nextOfferId;
        private int _openCount;
        private int _utilityPriceLevel;
        private bool _lighterPurchasedThisVisit;
        private bool _whiskeyPurchasedThisVisit;
        private bool _utilityPurchasedThisVisit;

        public bool IsOpen { get; private set; }

        public int Gold { get; private set; }

        public int CurrentLighterPrice => CalculateUtilityPrice(lighterPrice);

        public int CurrentWhiskeyPrice => CalculateUtilityPrice(whiskeyPrice);

        public void Open()
        {
            if (IsOpen)
            {
                return;
            }

            _lighterPurchasedThisVisit = false;
            _whiskeyPurchasedThisVisit = false;
            _utilityPurchasedThisVisit = false;
            IsOpen = true;
            Gold += goldPerWin;
            int offerSeed = shopRandomSeed + _openCount++;
            GenerateDemonCardOffers(offerSeed);
            GenerateNormalCardOffers(offerSeed + 9973);

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

            if (_utilityPurchasedThisVisit)
            {
                _utilityPriceLevel++;
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
            RefreshOfferViews();
        }

        public void ResetRunEconomy()
        {
            Gold = 0;
            _nextOfferId = 0;
            _openCount = 0;
            _utilityPriceLevel = 0;
            _lighterPurchasedThisVisit = false;
            _whiskeyPurchasedThisVisit = false;
            _utilityPurchasedThisVisit = false;
            RefreshOfferViews();
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

            RefreshOfferViews();
            return true;
        }

        public bool TryPurchaseNormalCard(
            int offerId,
            out string definitionKey,
            out CardSuit suit)
        {
            definitionKey = null;
            suit = CardSuit.Spade;
            if (!IsOpen)
            {
                return false;
            }

            NormalCardOffer offer = FindNormalOffer(offerId);
            if (offer == null || offer.SoldOut || Gold < offer.Price)
            {
                return false;
            }

            Gold -= offer.Price;
            offer.SoldOut = true;
            definitionKey = offer.DefinitionKey;
            suit = offer.Suit;

            if (offer.View != null)
            {
                offer.View.SetHovered(false);
            }

            RefreshOfferViews();
            return true;
        }

        public void RefreshUtilityItems(
            int removableCardCount,
            int playerCurrentSoul,
            int playerMaximumSoul)
        {
            int currentLighterPrice = CurrentLighterPrice;
            string lighterDescription =
                "Remove 1 card from your deck.\nPRICE " +
                currentLighterPrice +
                " GOLD";
            if (_lighterPurchasedThisVisit)
            {
                lighterDescription += "\nPURCHASED THIS SHOP";
            }

            BindUtilityItem(
                lighterItem,
                ShopUtilityItemKind.Lighter,
                "LIGHTER",
                lighterDescription,
                IsOpen &&
                    !_lighterPurchasedThisVisit &&
                    Gold >= currentLighterPrice &&
                    removableCardCount > 1);

            int currentWhiskeyPrice = CurrentWhiskeyPrice;
            string whiskeyDescription =
                "Restore " +
                whiskeySoulRestore +
                " soul.\nPRICE " +
                currentWhiskeyPrice +
                " GOLD";
            if (_whiskeyPurchasedThisVisit)
            {
                whiskeyDescription += "\nPURCHASED THIS SHOP";
            }

            if (playerCurrentSoul >= playerMaximumSoul)
            {
                whiskeyDescription += "\nSOUL IS FULL";
            }

            BindUtilityItem(
                whiskeyItem,
                ShopUtilityItemKind.Whiskey,
                "WHISKEY",
                whiskeyDescription,
                IsOpen &&
                    !_whiskeyPurchasedThisVisit &&
                    Gold >= currentWhiskeyPrice &&
                    playerCurrentSoul < playerMaximumSoul);
        }

        public bool TryPurchaseLighterRemoval(int removableCardCount)
        {
            int price = CurrentLighterPrice;
            if (!IsOpen ||
                _lighterPurchasedThisVisit ||
                removableCardCount <= 1 ||
                Gold < price)
            {
                return false;
            }

            Gold -= price;
            _lighterPurchasedThisVisit = true;
            _utilityPurchasedThisVisit = true;
            RefreshOfferViews();
            return true;
        }

        public bool TryPurchaseWhiskey(
            int playerCurrentSoul,
            int playerMaximumSoul,
            out int restoreAmount)
        {
            restoreAmount = 0;
            int price = CurrentWhiskeyPrice;
            if (!IsOpen ||
                _whiskeyPurchasedThisVisit ||
                Gold < price ||
                playerCurrentSoul < 0 ||
                playerMaximumSoul <= 0 ||
                playerCurrentSoul >= playerMaximumSoul)
            {
                return false;
            }

            restoreAmount = Mathf.Min(
                whiskeySoulRestore,
                playerMaximumSoul - playerCurrentSoul);
            if (restoreAmount <= 0)
            {
                return false;
            }

            Gold -= price;
            _whiskeyPurchasedThisVisit = true;
            _utilityPurchasedThisVisit = true;
            RefreshOfferViews();
            return true;
        }

        private int CalculateUtilityPrice(int basePrice)
        {
            return Mathf.Max(0, basePrice) +
                _utilityPriceLevel * Mathf.Max(0, utilityPriceIncreasePerUsedVisit);
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

        private void GenerateDemonCardOffers(int seed)
        {
            ClearDemonCardOffers();
            if (demonCardHolder == null || demonCardPrefab == null)
            {
                return;
            }

            DemonContractCatalog catalog = DemonContractCatalog.Default;
            var definitions = new List<DemonContractDefinition>(catalog.Definitions);
            Shuffle(definitions, seed);

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

        private void GenerateNormalCardOffers(int seed)
        {
            ClearNormalCardOffers();
            if (normalCardHolder == null || normalCardPrefab == null)
            {
                return;
            }

            List<NormalCardOfferData> candidates = CreateNormalCardOfferPool();
            Shuffle(candidates, seed);

            int count = Mathf.Min(normalCardOfferCount, candidates.Count);
            for (int i = 0; i < count; i++)
            {
                NormalCardOfferData candidate = candidates[i];
                CardView view = Instantiate(normalCardPrefab, normalCardHolder);
                view.transform.localRotation = Quaternion.identity;

                var offer = new NormalCardOffer(
                    _nextOfferId++,
                    candidate.Definition,
                    candidate.Suit,
                    normalCardPrice,
                    view);
                _normalOffers.Add(offer);
                BindOfferView(offer);
            }

            LayoutNormalOfferViews();
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

        private void ClearNormalCardOffers()
        {
            foreach (NormalCardOffer offer in _normalOffers)
            {
                if (offer.View != null)
                {
                    Destroy(offer.View.gameObject);
                }
            }

            _normalOffers.Clear();
        }

        private void RefreshOfferViews()
        {
            foreach (DemonCardOffer offer in _demonOffers)
            {
                BindOfferView(offer);
            }

            LayoutDemonOfferViews();

            foreach (NormalCardOffer offer in _normalOffers)
            {
                BindOfferView(offer);
            }

            LayoutNormalOfferViews();
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

        private void BindOfferView(NormalCardOffer offer)
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
            CardDefinition definition = offer.Definition;
            offer.View.Bind(new GameSceneCardViewModel(
                offer.OfferId,
                definition.Rank,
                isFaceUp: true,
                revealRank: true,
                canUse: Gold >= offer.Price,
                definition.DisplayName,
                abilityDescription: FormatNormalCardOfferText(definition, offer.Price),
                suit: offer.Suit,
                showHoverBadgeWhenUnavailable: true));
        }

        private static void BindUtilityItem(
            ShopUtilityItemView item,
            ShopUtilityItemKind kind,
            string displayName,
            string description,
            bool canUse)
        {
            if (item == null)
            {
                return;
            }

            item.gameObject.SetActive(true);
            item.Bind(kind, displayName, description, canUse);
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

        private void LayoutNormalOfferViews()
        {
            int visibleCount = 0;
            foreach (NormalCardOffer offer in _normalOffers)
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

            float offset = -(visibleCount - 1) * 0.5f * normalCardSpacing;
            int visibleIndex = 0;
            foreach (NormalCardOffer offer in _normalOffers)
            {
                if (offer == null || offer.SoldOut || offer.View == null)
                {
                    continue;
                }

                offer.View.transform.localPosition = new Vector3(
                    offset + visibleIndex * normalCardSpacing,
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

        private NormalCardOffer FindNormalOffer(int offerId)
        {
            foreach (NormalCardOffer offer in _normalOffers)
            {
                if (offer.OfferId == offerId)
                {
                    return offer;
                }
            }

            return null;
        }

        private void Shuffle<T>(List<T> items, int seed)
        {
            _random.Reseed(seed);
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = _random.Next(i + 1);
                T item = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = item;
            }
        }

        private static List<NormalCardOfferData> CreateNormalCardOfferPool()
        {
            var candidates = new List<NormalCardOfferData>(20);
            for (int rank = 1; rank <= 10; rank++)
            {
                CardDefinition definition = CardDefinitionCatalog.GetDefaultForRank(rank);
                candidates.Add(new NormalCardOfferData(definition, CardSuit.Spade));
                candidates.Add(new NormalCardOfferData(definition, CardSuit.Clover));
            }

            return candidates;
        }

        private static string FormatNormalCardOfferText(
            CardDefinition definition,
            int price)
        {
            string text = "PRICE " + price + " GOLD";
            string effect = FormatCardEffect(definition.Effect);
            if (!string.IsNullOrEmpty(effect))
            {
                text += "\n" + effect;
            }

            return text;
        }

        private static string FormatCardEffect(CardEffectKind effect)
        {
            switch (effect)
            {
                case CardEffectKind.CrystalOrb:
                    return "덱 맨 위 2장 훔쳐보고 1장 가져오기";
                case CardEffectKind.ThreatHammer:
                    return "적 공개 카드 1장 제거";
                case CardEffectKind.AutoPistol:
                    return "적 비공개 숫자 맞히면 적 즉사";
                case CardEffectKind.MilitaryKnife:
                    return "적에게 공개카드 1장 강제로 뽑게 함";
                default:
                    return string.Empty;
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

        private readonly struct NormalCardOfferData
        {
            public NormalCardOfferData(CardDefinition definition, CardSuit suit)
            {
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                Suit = suit;
            }

            public CardDefinition Definition { get; }

            public CardSuit Suit { get; }
        }

        private sealed class NormalCardOffer
        {
            public NormalCardOffer(
                int offerId,
                CardDefinition definition,
                CardSuit suit,
                int price,
                CardView view)
            {
                OfferId = offerId;
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                Suit = suit;
                Price = price;
                View = view;
            }

            public CardDefinition Definition { get; }

            public string DefinitionKey => Definition.Key;

            public int OfferId { get; }

            public int Price { get; }

            public bool SoldOut { get; set; }

            public CardSuit Suit { get; }

            public CardView View { get; }
        }
    }
}
