using System;
using System.Collections.Generic;
using Border.Core;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal enum ShopPurchaseAvailability
    {
        Available,
        InsufficientGold,
        SoldOut,
        Unavailable,
        SoulFull,
    }

    /// <summary>
    /// GameScene-local shop for the MVP. It grants placeholder gold, hides combat table objects,
    /// shows merchant goods, and owns the temporary card offers on the table.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShopController : MonoBehaviour
    {
        private const string TableLayoutPreviewPrefix =
            "__TableLayoutPreview_";

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
        [Tooltip("Codex book prop; nudged by codexShopLocalPositionOffset while the shop is open and restored on close.")]
        [SerializeField] private Transform codexBook;
        [SerializeField] private Vector3 codexShopLocalPositionOffset =
            new Vector3(0.25f, 0f, 0f);
        [SerializeField] private int demonCardOfferCount = 2;
        [SerializeField] private float demonCardSpacing = 1.1f;
        [SerializeField] private int normalCardOfferCount = 3;
        [SerializeField] private float normalCardSpacing = 0.95f;
        [SerializeField] private int lighterPrice = 2;
        [SerializeField] private int whiskeyPrice = 2;
        [Tooltip("Added to the lighter price for every earlier shop where the lighter was used. Whiskey's price never rises.")]
        [SerializeField] private int lighterPriceIncreasePerUsedVisit = 1;
        [SerializeField] private int whiskeySoulRestore = 2;
        [SerializeField] private int shopRandomSeed = 20260726;

        [Header("Merchant speech")]
        [SerializeField] private SpeechProfileSO merchantSpeechProfile;
        [SerializeField] private int merchantSpeechSeed = 20260804;

        private readonly List<DemonCardOffer> _demonOffers = new List<DemonCardOffer>();
        private readonly List<NormalCardOffer> _normalOffers = new List<NormalCardOffer>();
        private readonly List<DemonCardView> _formalDemonOffers =
            new List<DemonCardView>();
        private readonly List<CardView> _formalNormalOffers =
            new List<CardView>();
        private readonly List<ShopCardOfferStatusView> _formalDemonStatuses =
            new List<ShopCardOfferStatusView>();
        private readonly List<ShopCardOfferStatusView> _formalNormalStatuses =
            new List<ShopCardOfferStatusView>();
        private readonly List<ShopPriceTarget> _activePriceTargets =
            new List<ShopPriceTarget>();
        private readonly List<ShopOfferLayout> _demonOfferLayouts =
            new List<ShopOfferLayout>();
        private readonly List<ShopOfferLayout> _normalOfferLayouts =
            new List<ShopOfferLayout>();
        private readonly DeterministicRng _random = new DeterministicRng();
        private SpeechLineResolver _speechResolver;
        private int _nextOfferId;
        private int _openCount;
        private int _lighterPriceLevel;
        private bool _lighterPurchasedThisVisit;
        private bool _whiskeyPurchasedThisVisit;
        private Vector3 _codexBookBaseLocalPosition;
        private bool _hasCodexBookBaseLocalPosition;

        public bool IsOpen { get; private set; }

        public bool IsFormal { get; private set; }

        public int FormalDemonOfferCount => _formalDemonOffers.Count;

        public int FormalNormalOfferCount => _formalNormalOffers.Count;

        public IReadOnlyList<ShopPriceTarget> ActivePriceTargets =>
            _activePriceTargets;

        public int Gold { get; private set; }

        public int CurrentLighterPrice => Mathf.Max(0, lighterPrice) +
            _lighterPriceLevel * Mathf.Max(0, lighterPriceIncreasePerUsedVisit);

        public int CurrentWhiskeyPrice => Mathf.Max(0, whiskeyPrice);

        private void Awake()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            CaptureAuthoredOfferLayouts();
            SetCombatTableActive(true);
            if (itemsRoot != null)
            {
                itemsRoot.SetActive(false);
            }
        }

        public Sprite GetDemonCardFaceSprite(string definitionKey)
        {
            return demonCardPrefab == null
                ? null
                : demonCardPrefab.GetFaceSprite(definitionKey);
        }

        public void Open(string enemyProfileKey)
        {
            if (IsOpen)
            {
                return;
            }

            _lighterPurchasedThisVisit = false;
            _whiskeyPurchasedThisVisit = false;
            _activePriceTargets.Clear();
            IsOpen = true;
            IsFormal = false;
            CaptureAuthoredOfferLayouts();
            Gold += GoldRewardCatalog.CreatePrototype().GetAmount(enemyProfileKey);
            int offerSeed = shopRandomSeed + _openCount++;
            GenerateDemonCardOffers(offerSeed);
            GenerateNormalCardOffers(offerSeed + 9973);

            if (merchant != null)
            {
                merchant.EnterMerchant();
                ShowMerchantSpeech(SpeechCueKeys.ShopGreeting);
            }

            SetCombatTableActive(false);

            if (itemsRoot != null)
            {
                itemsRoot.SetActive(true);
            }
        }

        public void OpenFormal(StageProgressionViewModel model)
        {
            if (model == null || !model.IsShop || !model.ShopOfferId.HasValue)
            {
                CloseFormal();
                return;
            }

            bool isEnteringShop = !IsOpen || !IsFormal;
            IsOpen = true;
            IsFormal = true;
            ClearFormalOffers();
            _activePriceTargets.Clear();
            CaptureAuthoredOfferLayouts();
            CreateFormalCardOffers(model);
            BindFormalUtilityItems(model);

            // GameFlowController applies merchant mode after the entrance door animation so the
            // defeated enemy completes its exit animation before the merchant sprite is shown.
            if (isEnteringShop)
            {
                ShowMerchantSpeech(SpeechCueKeys.ShopGreeting);
            }
            SetCombatTableActive(false);
            if (itemsRoot != null)
            {
                itemsRoot.SetActive(true);
            }
        }

        public void CloseFormal()
        {
            if (!IsFormal)
            {
                return;
            }

            IsFormal = false;
            IsOpen = false;
            ClearFormalOffers();
            _activePriceTargets.Clear();
            lighterItem?.SetHovered(false);
            whiskeyItem?.SetHovered(false);
            ShowMerchantSpeech(SpeechCueKeys.ShopFarewell);
            merchant?.ExitMerchant();
            if (itemsRoot != null)
            {
                itemsRoot.SetActive(false);
            }

            SetCombatTableActive(true);
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            if (_lighterPurchasedThisVisit)
            {
                _lighterPriceLevel++;
            }

            IsOpen = false;
            IsFormal = false;
            _activePriceTargets.Clear();

            ShowMerchantSpeech(SpeechCueKeys.ShopFarewell);

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

        /// <summary>Test-only hook — production gold only ever comes from Open's per-enemy reward.</summary>
        internal void GrantGoldForTesting(int amount)
        {
            Gold += amount;
        }

        public void ResetRunEconomy()
        {
            Gold = 0;
            _nextOfferId = 0;
            _openCount = 0;
            _lighterPriceLevel = 0;
            _lighterPurchasedThisVisit = false;
            _whiskeyPurchasedThisVisit = false;
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

        internal ShopPurchaseAvailability GetDemonCardAvailability(int offerId)
        {
            if (!IsOpen)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            DemonCardOffer offer = FindDemonOffer(offerId);
            if (offer == null)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            if (offer.SoldOut)
            {
                return ShopPurchaseAvailability.SoldOut;
            }

            return Gold < offer.Price
                ? ShopPurchaseAvailability.InsufficientGold
                : ShopPurchaseAvailability.Available;
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

        internal ShopPurchaseAvailability GetNormalCardAvailability(int offerId)
        {
            if (!IsOpen)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            NormalCardOffer offer = FindNormalOffer(offerId);
            if (offer == null)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            if (offer.SoldOut)
            {
                return ShopPurchaseAvailability.SoldOut;
            }

            return Gold < offer.Price
                ? ShopPurchaseAvailability.InsufficientGold
                : ShopPurchaseAvailability.Available;
        }

        public void RefreshUtilityItems(
            int removableCardCount,
            int playerCurrentSoul,
            int playerMaximumSoul)
        {
            if (IsFormal)
            {
                return;
            }

            int currentLighterPrice = CurrentLighterPrice;
            BindUtilityItem(
                lighterItem,
                ShopUtilityItemKind.Lighter,
                "LIGHTER",
                currentLighterPrice,
                amount: 0,
                isPlayerSoulFull: false,
                !_lighterPurchasedThisVisit,
                IsOpen &&
                    !_lighterPurchasedThisVisit &&
                    Gold >= currentLighterPrice &&
                    removableCardCount > 1);

            int currentWhiskeyPrice = CurrentWhiskeyPrice;
            BindUtilityItem(
                whiskeyItem,
                ShopUtilityItemKind.Whiskey,
                "WHISKEY",
                currentWhiskeyPrice,
                whiskeySoulRestore,
                playerCurrentSoul >= playerMaximumSoul,
                !_whiskeyPurchasedThisVisit,
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
            lighterItem?.gameObject.SetActive(false);
            RefreshOfferViews();
            return true;
        }

        internal ShopPurchaseAvailability GetLighterAvailability(
            int removableCardCount)
        {
            if (!IsOpen || _lighterPurchasedThisVisit || removableCardCount <= 1)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            return Gold < CurrentLighterPrice
                ? ShopPurchaseAvailability.InsufficientGold
                : ShopPurchaseAvailability.Available;
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
            whiskeyItem?.gameObject.SetActive(false);
            RefreshOfferViews();
            return true;
        }

        internal ShopPurchaseAvailability GetWhiskeyAvailability(
            int playerCurrentSoul,
            int playerMaximumSoul)
        {
            if (!IsOpen ||
                _whiskeyPurchasedThisVisit ||
                playerCurrentSoul < 0 ||
                playerMaximumSoul <= 0)
            {
                return ShopPurchaseAvailability.Unavailable;
            }

            if (playerCurrentSoul >= playerMaximumSoul)
            {
                return ShopPurchaseAvailability.SoulFull;
            }

            return Gold < CurrentWhiskeyPrice
                ? ShopPurchaseAvailability.InsufficientGold
                : ShopPurchaseAvailability.Available;
        }

        internal void ShowMerchantSpeech(string cueKey)
        {
            if (merchant == null || string.IsNullOrWhiteSpace(cueKey))
            {
                return;
            }

            _speechResolver ??= new SpeechLineResolver(merchantSpeechSeed);
            merchant.ShowSpeech(
                _speechResolver.Resolve(merchantSpeechProfile, cueKey));
        }

        internal static string ResolveAvailabilitySpeech(
            ShopPurchaseAvailability availability)
        {
            switch (availability)
            {
                case ShopPurchaseAvailability.InsufficientGold:
                    return SpeechCueKeys.ShopInsufficientGold;
                case ShopPurchaseAvailability.SoldOut:
                    return SpeechCueKeys.ShopSoldOut;
                case ShopPurchaseAvailability.Available:
                    return SpeechCueKeys.ShopPurchaseSuccess;
                case ShopPurchaseAvailability.SoulFull:
                    return SpeechCueKeys.ShopWhiskeySoulFull;
                default:
                    return SpeechCueKeys.ShopUnavailable;
            }
        }

        private void CreateFormalCardOffers(StageProgressionViewModel model)
        {
            foreach (ShopCardOptionViewModel option in model.ShopCardOptions)
            {
                if (option.Category == "DEMON")
                {
                    CreateFormalDemonOffer(option);
                }
                else
                {
                    CreateFormalNormalOffer(option);
                }
            }

            LayoutFormalOffers(
                _formalNormalOffers,
                _formalNormalStatuses,
                _normalOfferLayouts,
                normalCardSpacing);
            LayoutFormalOffers(
                _formalDemonOffers,
                _formalDemonStatuses,
                _demonOfferLayouts,
                demonCardSpacing);
        }

        private void CreateFormalDemonOffer(ShopCardOptionViewModel option)
        {
            if (demonCardHolder == null || demonCardPrefab == null)
            {
                return;
            }

            DemonCardView view = Instantiate(demonCardPrefab, demonCardHolder);
            view.transform.localRotation = Quaternion.identity;
            view.SetShopPresentation();
            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(option.DefinitionKey);
            view.Bind(CreateShopDemonCardViewModel(
                option.OptionId,
                definition,
                option.CanBuy));
            view.SetShopSoldOut(option.IsSold);
            BindPriceTarget(
                view.GetComponent<ShopPriceTarget>(),
                definition.DisplayName,
                option.PriceAmount,
                option.IsSold);
            _formalDemonOffers.Add(view);
            _formalDemonStatuses.Add(PrepareOfferStatus(
                view.DetachShopOfferStatus(demonCardHolder),
                option.IsSold));
        }

        private void CreateFormalNormalOffer(ShopCardOptionViewModel option)
        {
            if (normalCardHolder == null || normalCardPrefab == null)
            {
                return;
            }

            CardDefinition definition = CardDefinitionCatalog.GetByKey(
                option.DefinitionKey);
            CardView view = Instantiate(normalCardPrefab, normalCardHolder);
            view.transform.localRotation = Quaternion.identity;
            view.SetShopPresentation();
            view.Bind(new GameSceneCardViewModel(
                option.OptionId,
                definition.Rank,
                isFaceUp: true,
                revealRank: true,
                canUse: option.CanBuy,
                definition.DisplayName,
                abilityDescription: definition.Description,
                suit: CardSuit.Spade,
                definitionKey: option.DefinitionKey,
                showHoverBadgeWhenUnavailable: true));
            view.SetShopSoldOut(option.IsSold);
            BindPriceTarget(
                view.GetComponent<ShopPriceTarget>(),
                definition.DisplayName,
                option.PriceAmount,
                option.IsSold);
            _formalNormalOffers.Add(view);
            _formalNormalStatuses.Add(PrepareOfferStatus(
                view.DetachShopOfferStatus(normalCardHolder),
                option.IsSold));
        }

        private void BindFormalUtilityItems(StageProgressionViewModel model)
        {
            bool canRemove = false;
            foreach (ShopOwnedCardViewModel card in model.ShopOwnedCards)
            {
                if (card.CanRemove)
                {
                    canRemove = true;
                    break;
                }
            }

            BindUtilityItem(
                lighterItem,
                ShopUtilityItemKind.Lighter,
                "LIGHTER",
                model.LighterPriceAmount,
                amount: 0,
                isPlayerSoulFull: false,
                !model.IsLighterUsed,
                canRemove);
            BindUtilityItem(
                whiskeyItem,
                ShopUtilityItemKind.Whiskey,
                "WHISKEY",
                model.WhiskeyPriceAmount,
                model.WhiskeyRecoveryAmount,
                model.IsPlayerSoulFull,
                !model.IsWhiskeyUsed,
                model.CanRestAtShop);
        }

        private void ClearFormalOffers()
        {
            foreach (DemonCardView view in _formalDemonOffers)
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    DestroyOfferObject(view.gameObject);
                }
            }

            foreach (CardView view in _formalNormalOffers)
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    DestroyOfferObject(view.gameObject);
                }
            }

            DestroyStatusViews(_formalDemonStatuses);
            DestroyStatusViews(_formalNormalStatuses);

            _formalDemonOffers.Clear();
            _formalNormalOffers.Clear();
            _formalDemonStatuses.Clear();
            _formalNormalStatuses.Clear();
        }

        private static void LayoutFormalOffers<T>(
            IReadOnlyList<T> views,
            IReadOnlyList<ShopCardOfferStatusView> statuses,
            IReadOnlyList<ShopOfferLayout> layouts,
            float spacing)
            where T : Component
        {
            float offset = -(views.Count - 1) * 0.5f * spacing;
            for (int i = 0; i < views.Count; i++)
            {
                T view = views[i];
                if (view == null)
                {
                    continue;
                }

                ApplyOfferLayout(
                    view.transform,
                    i,
                    layouts,
                    new Vector3(
                        offset + i * spacing,
                        0f,
                        i * 0.01f));
                LayoutStatus(statuses, i, view.transform.localPosition);
            }
        }

        private void CaptureAuthoredOfferLayouts()
        {
            CaptureAuthoredOfferLayouts(
                demonCardHolder,
                _demonOfferLayouts);
            CaptureAuthoredOfferLayouts(
                normalCardHolder,
                _normalOfferLayouts);
        }

        private static void CaptureAuthoredOfferLayouts(
            Transform holder,
            List<ShopOfferLayout> layouts)
        {
            if (holder == null || layouts.Count > 0)
            {
                return;
            }

            List<Transform> authoredPreviews = new List<Transform>();
            for (int i = 0; i < holder.childCount; i++)
            {
                Transform child = holder.GetChild(i);
                if (child != null &&
                    child.name.StartsWith(
                        TableLayoutPreviewPrefix,
                        StringComparison.Ordinal))
                {
                    authoredPreviews.Add(child);
                }
            }

            authoredPreviews.Sort((left, right) =>
                string.CompareOrdinal(left.name, right.name));
            foreach (Transform preview in authoredPreviews)
            {
                layouts.Add(new ShopOfferLayout(
                    preview.localPosition,
                    preview.localRotation,
                    preview.localScale));
                preview.gameObject.SetActive(false);
            }
        }

        private void SetCombatTableActive(bool active)
        {
            if (combatTableObjects != null)
            {
                foreach (GameObject tableObject in combatTableObjects)
                {
                    if (tableObject != null)
                    {
                        tableObject.SetActive(active);
                    }
                }
            }

            ApplyCodexBookShopPosition(shopOpen: !active);
        }

        private void ApplyCodexBookShopPosition(bool shopOpen)
        {
            if (codexBook == null)
            {
                return;
            }

            if (!_hasCodexBookBaseLocalPosition)
            {
                _codexBookBaseLocalPosition = codexBook.localPosition;
                _hasCodexBookBaseLocalPosition = true;
            }

            codexBook.localPosition = shopOpen
                ? _codexBookBaseLocalPosition + codexShopLocalPositionOffset
                : _codexBookBaseLocalPosition;
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
                view.SetShopPresentation();

                var offer = new DemonCardOffer(
                    _nextOfferId++,
                    definition,
                    definition.BasePurchasePrice,
                    view,
                    PrepareOfferStatus(
                        view.DetachShopOfferStatus(demonCardHolder),
                        isSoldOut: false));
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
                view.SetShopPresentation();

                var offer = new NormalCardOffer(
                    _nextOfferId++,
                    candidate.Definition,
                    candidate.Suit,
                    candidate.Definition.BasePurchasePrice,
                    view,
                    PrepareOfferStatus(
                        view.DetachShopOfferStatus(normalCardHolder),
                        isSoldOut: false));
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
                    DestroyOfferObject(offer.View.gameObject);
                }

                if (offer.Status != null)
                {
                    DestroyOfferObject(offer.Status.gameObject);
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
                    DestroyOfferObject(offer.View.gameObject);
                }

                if (offer.Status != null)
                {
                    DestroyOfferObject(offer.Status.gameObject);
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

            offer.View.gameObject.SetActive(true);
            DemonContractDefinition definition = offer.Definition;
            offer.View.Bind(CreateShopDemonCardViewModel(
                offer.OfferId,
                definition,
                !offer.SoldOut && Gold >= offer.Price));
            offer.View.SetShopSoldOut(offer.SoldOut);
            offer.Status?.Bind(offer.SoldOut);
            BindPriceTarget(
                offer.View.GetComponent<ShopPriceTarget>(),
                definition.DisplayName,
                offer.Price,
                offer.SoldOut);
        }

        private static GameSceneDemonCardViewModel CreateShopDemonCardViewModel(
            int cardId,
            DemonContractDefinition definition,
            bool canUse)
        {
            return new GameSceneDemonCardViewModel(
                cardId,
                definition.Key,
                isFaceUp: true,
                canUse,
                definition.DisplayName,
                definition.Summary,
                definition.CostSummary,
                showHoverBadgeWhenUnavailable: true);
        }

        private void BindOfferView(NormalCardOffer offer)
        {
            if (offer == null || offer.View == null)
            {
                return;
            }

            offer.View.gameObject.SetActive(true);
            CardDefinition definition = offer.Definition;
            offer.View.Bind(new GameSceneCardViewModel(
                offer.OfferId,
                definition.Rank,
                isFaceUp: true,
                revealRank: true,
                canUse: !offer.SoldOut && Gold >= offer.Price,
                definition.DisplayName,
                abilityDescription: definition.Description,
                suit: offer.Suit,
                definitionKey: definition.Key,
                showHoverBadgeWhenUnavailable: true));
            offer.View.SetShopSoldOut(offer.SoldOut);
            offer.Status?.Bind(offer.SoldOut);
            BindPriceTarget(
                offer.View.GetComponent<ShopPriceTarget>(),
                definition.DisplayName,
                offer.Price,
                offer.SoldOut);
        }

        private void BindUtilityItem(
            ShopUtilityItemView item,
            ShopUtilityItemKind kind,
            string displayName,
            int price,
            int amount,
            bool isPlayerSoulFull,
            bool visible,
            bool canUse)
        {
            if (item == null)
            {
                return;
            }

            item.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            item.Bind(
                kind,
                displayName,
                price,
                amount,
                isPlayerSoulFull,
                canUse);
            RegisterPriceTarget(item.ShopPriceTarget);
        }

        private void BindPriceTarget(
            ShopPriceTarget target,
            string displayName,
            int price,
            bool isSoldOut)
        {
            if (target == null)
            {
                return;
            }

            target.Bind(displayName, price, isSoldOut);
            RegisterPriceTarget(target);
        }

        private void RegisterPriceTarget(ShopPriceTarget target)
        {
            if (target != null && !_activePriceTargets.Contains(target))
            {
                _activePriceTargets.Add(target);
            }
        }

        private void LayoutDemonOfferViews()
        {
            if (_demonOffers.Count == 0)
            {
                return;
            }

            float offset = -(_demonOffers.Count - 1) * 0.5f * demonCardSpacing;
            for (int i = 0; i < _demonOffers.Count; i++)
            {
                DemonCardOffer offer = _demonOffers[i];
                if (offer == null || offer.View == null)
                {
                    continue;
                }

                ApplyOfferLayout(
                    offer.View.transform,
                    i,
                    _demonOfferLayouts,
                    new Vector3(
                        offset + i * demonCardSpacing,
                        0f,
                        i * 0.01f));
                LayoutStatus(offer.Status, offer.View.transform.localPosition);
            }
        }

        private void LayoutNormalOfferViews()
        {
            if (_normalOffers.Count == 0)
            {
                return;
            }

            float offset = -(_normalOffers.Count - 1) * 0.5f * normalCardSpacing;
            for (int i = 0; i < _normalOffers.Count; i++)
            {
                NormalCardOffer offer = _normalOffers[i];
                if (offer == null || offer.View == null)
                {
                    continue;
                }

                ApplyOfferLayout(
                    offer.View.transform,
                    i,
                    _normalOfferLayouts,
                    new Vector3(
                        offset + i * normalCardSpacing,
                        0f,
                        i * 0.01f));
                LayoutStatus(offer.Status, offer.View.transform.localPosition);
            }
        }

        private static void ApplyOfferLayout(
            Transform offer,
            int index,
            IReadOnlyList<ShopOfferLayout> layouts,
            Vector3 fallbackLocalPosition)
        {
            if (layouts != null &&
                index >= 0 &&
                index < layouts.Count)
            {
                ShopOfferLayout layout = layouts[index];
                offer.localPosition = layout.LocalPosition;
                offer.localRotation = layout.LocalRotation;
                offer.localScale = layout.LocalScale;
                return;
            }

            offer.localPosition = fallbackLocalPosition;
            offer.localRotation = Quaternion.identity;
        }

        private static ShopCardOfferStatusView PrepareOfferStatus(
            ShopCardOfferStatusView status,
            bool isSoldOut)
        {
            if (status == null)
            {
                return null;
            }

            status.Bind(isSoldOut);
            return status;
        }

        private static void DestroyStatusViews(
            IReadOnlyList<ShopCardOfferStatusView> statuses)
        {
            foreach (ShopCardOfferStatusView status in statuses)
            {
                if (status != null)
                {
                    status.gameObject.SetActive(false);
                    DestroyOfferObject(status.gameObject);
                }
            }
        }

        private static void DestroyOfferObject(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private static void LayoutStatus(
            IReadOnlyList<ShopCardOfferStatusView> statuses,
            int index,
            Vector3 localPosition)
        {
            if (statuses == null || index < 0 || index >= statuses.Count)
            {
                return;
            }

            LayoutStatus(statuses[index], localPosition);
        }

        private static void LayoutStatus(
            ShopCardOfferStatusView status,
            Vector3 localPosition)
        {
            if (status == null)
            {
                return;
            }

            status.LayoutAt(localPosition);
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

        private sealed class DemonCardOffer
        {
            public DemonCardOffer(
                int offerId,
                DemonContractDefinition definition,
                int price,
                DemonCardView view,
                ShopCardOfferStatusView status)
            {
                OfferId = offerId;
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                Price = price;
                View = view;
                Status = status;
            }

            public DemonContractDefinition Definition { get; }

            public string DefinitionKey => Definition.Key;

            public int OfferId { get; }

            public int Price { get; }

            public bool SoldOut { get; set; }

            public ShopCardOfferStatusView Status { get; }

            public DemonCardView View { get; }
        }

        private readonly struct ShopOfferLayout
        {
            public ShopOfferLayout(
                Vector3 localPosition,
                Quaternion localRotation,
                Vector3 localScale)
            {
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                LocalScale = localScale;
            }

            public Vector3 LocalPosition { get; }

            public Quaternion LocalRotation { get; }

            public Vector3 LocalScale { get; }
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
                CardView view,
                ShopCardOfferStatusView status)
            {
                OfferId = offerId;
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                Suit = suit;
                Price = price;
                View = view;
                Status = status;
            }

            public CardDefinition Definition { get; }

            public string DefinitionKey => Definition.Key;

            public int OfferId { get; }

            public int Price { get; }

            public bool SoldOut { get; set; }

            public ShopCardOfferStatusView Status { get; }

            public CardSuit Suit { get; }

            public CardView View { get; }
        }
    }
}
