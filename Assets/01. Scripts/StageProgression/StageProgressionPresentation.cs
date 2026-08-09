using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression.UI
{
    public sealed class OpponentCandidateViewModel
    {
        public OpponentCandidateViewModel(
            string profileKey,
            string displayName,
            string grade,
            string maximumSoul,
            string summary,
            string rewardTier,
            string soulAmountText,
            string defeatGoldAmountText,
            bool isFocused)
        {
            ProfileKey = profileKey;
            DisplayName = displayName;
            Grade = grade;
            MaximumSoul = maximumSoul;
            Summary = summary;
            RewardTier = rewardTier;
            SoulAmountText = soulAmountText;
            DefeatGoldAmountText = defeatGoldAmountText;
            IsFocused = isFocused;
        }

        public string ProfileKey { get; }

        public string DisplayName { get; }

        public string Grade { get; }

        public string MaximumSoul { get; }

        public string Summary { get; }

        public string RewardTier { get; }

        public string SoulAmountText { get; }

        public string DefeatGoldAmountText { get; }

        public bool IsFocused { get; }
    }

    public sealed class BattleRewardOptionViewModel
    {
        public BattleRewardOptionViewModel(
            int optionId,
            string definitionKey,
            string displayName,
            int rank,
            string effectSummary)
        {
            OptionId = optionId;
            DefinitionKey = definitionKey;
            DisplayName = displayName;
            Rank = rank;
            EffectSummary = effectSummary;
        }

        public int OptionId { get; }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public int Rank { get; }

        public string EffectSummary { get; }
    }

    public sealed class StartingDemonGrantCardViewModel
    {
        public StartingDemonGrantCardViewModel(
            string definitionKey,
            string displayName,
            string summary,
            string costSummary)
        {
            DefinitionKey = definitionKey;
            DisplayName = displayName;
            Summary = summary;
            CostSummary = costSummary;
        }

        public string CostSummary { get; }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public string Summary { get; }
    }

    public sealed class ShopCardOptionViewModel
    {
        public ShopCardOptionViewModel(
            int optionId,
            string definitionKey,
            string displayName,
            string category,
            string summary,
            string price,
            int priceAmount,
            bool canBuy,
            bool isSold)
        {
            OptionId = optionId;
            DefinitionKey = definitionKey;
            DisplayName = displayName;
            Category = category;
            Summary = summary;
            Price = price;
            PriceAmount = priceAmount;
            CanBuy = canBuy;
            IsSold = isSold;
        }

        public bool CanBuy { get; }
        public string Category { get; }
        public string DefinitionKey { get; }
        public string DisplayName { get; }
        public bool IsSold { get; }
        public int OptionId { get; }
        public string Price { get; }
        public int PriceAmount { get; }
        public string Summary { get; }
    }

    public sealed class ShopOwnedCardViewModel
    {
        public ShopOwnedCardViewModel(
            int cardId,
            string definitionKey,
            int rank,
            string displayName,
            string abilityDescription,
            CardSuit suit,
            bool canRemove)
        {
            CardId = cardId;
            DefinitionKey = definitionKey ?? string.Empty;
            Rank = rank;
            DisplayName = displayName;
            AbilityDescription = abilityDescription ?? string.Empty;
            Suit = suit;
            CanRemove = canRemove;
        }

        public string AbilityDescription { get; }
        public int CardId { get; }
        public bool CanRemove { get; }
        public string DefinitionKey { get; }
        public string DisplayName { get; }
        public int Rank { get; }
        public CardSuit Suit { get; }
    }

    public sealed class StageProgressionViewModel
    {
        private readonly ReadOnlyCollection<BattleRewardOptionViewModel> _rewardOptions;
        private readonly ReadOnlyCollection<OpponentCandidateViewModel> _opponentCandidates;
        private readonly ReadOnlyCollection<StartingDemonGrantCardViewModel>
            _startingDemonGrantCards;
        private readonly ReadOnlyCollection<ShopCardOptionViewModel>
            _shopCardOptions;
        private readonly ReadOnlyCollection<ShopOwnedCardViewModel>
            _shopOwnedCards;

        public StageProgressionViewModel(
            string stageProgress,
            string stageName,
            string stageKind,
            string playerSoul,
            StageProgressionState state,
            string message,
            bool canStartRun,
            bool canAdvanceStage,
            bool canRestartRun,
            string rewardTier,
            IEnumerable<BattleRewardOptionViewModel> rewardOptions,
            bool canSelectReward,
            bool canSkipReward,
            string rewardCompletionMessage,
            string rewardResult,
            int deckCount,
            int? opponentOfferId,
            IEnumerable<OpponentCandidateViewModel> opponentCandidates,
            string focusedOpponentProfileKey,
            bool canFocusOpponent,
            bool canConfirmOpponent,
            int? startingDemonGrantId,
            IEnumerable<StartingDemonGrantCardViewModel> startingDemonGrantCards,
            bool isStartingDemonReveal,
            string playerGold,
            string goldResult,
            bool isShop,
            int? shopOfferId,
            IEnumerable<ShopCardOptionViewModel> shopCardOptions,
            IEnumerable<ShopOwnedCardViewModel> shopOwnedCards,
            string lighterLabel,
            int lighterPriceAmount,
            bool isLighterUsed,
            string whiskeyLabel,
            int whiskeyPriceAmount,
            bool isWhiskeyUsed,
            bool canRestAtShop,
            bool canLeaveShop,
            string shopTransactionResult,
            int whiskeyRecoveryAmount = 0,
            bool isPlayerSoulFull = false)
        {
            StageProgress = stageProgress;
            StageName = stageName;
            StageKind = stageKind;
            PlayerSoul = playerSoul;
            State = state;
            Message = message;
            CanStartRun = canStartRun;
            CanAdvanceStage = canAdvanceStage;
            CanRestartRun = canRestartRun;
            RewardTier = rewardTier;
            _rewardOptions = new List<BattleRewardOptionViewModel>(
                rewardOptions ?? throw new ArgumentNullException(nameof(rewardOptions)))
                .AsReadOnly();
            CanSelectReward = canSelectReward;
            CanSkipReward = canSkipReward;
            RewardCompletionMessage = rewardCompletionMessage;
            RewardResult = rewardResult;
            DeckCount = deckCount;
            OpponentOfferId = opponentOfferId;
            _opponentCandidates = new List<OpponentCandidateViewModel>(
                opponentCandidates ?? throw new ArgumentNullException(
                    nameof(opponentCandidates)))
                .AsReadOnly();
            FocusedOpponentProfileKey = focusedOpponentProfileKey;
            CanFocusOpponent = canFocusOpponent;
            CanConfirmOpponent = canConfirmOpponent;
            StartingDemonGrantId = startingDemonGrantId;
            _startingDemonGrantCards =
                new List<StartingDemonGrantCardViewModel>(
                    startingDemonGrantCards ?? throw new ArgumentNullException(
                        nameof(startingDemonGrantCards)))
                .AsReadOnly();
            IsStartingDemonReveal = isStartingDemonReveal;
            PlayerGold = playerGold;
            GoldResult = goldResult;
            IsShop = isShop;
            ShopOfferId = shopOfferId;
            _shopCardOptions = new List<ShopCardOptionViewModel>(
                shopCardOptions ?? throw new ArgumentNullException(
                    nameof(shopCardOptions))).AsReadOnly();
            _shopOwnedCards = new List<ShopOwnedCardViewModel>(
                shopOwnedCards ?? throw new ArgumentNullException(
                    nameof(shopOwnedCards))).AsReadOnly();
            LighterLabel = lighterLabel;
            LighterPriceAmount = lighterPriceAmount;
            IsLighterUsed = isLighterUsed;
            WhiskeyLabel = whiskeyLabel;
            WhiskeyPriceAmount = whiskeyPriceAmount;
            IsWhiskeyUsed = isWhiskeyUsed;
            CanRestAtShop = canRestAtShop;
            CanLeaveShop = canLeaveShop;
            ShopTransactionResult = shopTransactionResult;
            WhiskeyRecoveryAmount = whiskeyRecoveryAmount;
            IsPlayerSoulFull = isPlayerSoulFull;
        }

        public string StageProgress { get; }

        public string StageName { get; }

        public string StageKind { get; }

        public string PlayerSoul { get; }

        public StageProgressionState State { get; }

        public string Message { get; }

        public bool CanStartRun { get; }

        public bool CanAdvanceStage { get; }

        public bool CanRestartRun { get; }

        public string RewardTier { get; }

        public IReadOnlyList<BattleRewardOptionViewModel> RewardOptions => _rewardOptions;

        public bool CanSelectReward { get; }

        public bool CanSkipReward { get; }

        public string RewardCompletionMessage { get; }

        public string RewardResult { get; }

        public int DeckCount { get; }

        public int? OpponentOfferId { get; }

        public IReadOnlyList<OpponentCandidateViewModel> OpponentCandidates =>
            _opponentCandidates;

        public string FocusedOpponentProfileKey { get; }

        public bool CanFocusOpponent { get; }

        public bool CanConfirmOpponent { get; }

        public int? StartingDemonGrantId { get; }

        public IReadOnlyList<StartingDemonGrantCardViewModel>
            StartingDemonGrantCards => _startingDemonGrantCards;

        public bool IsStartingDemonReveal { get; }

        public bool CanLeaveShop { get; }
        public bool CanRestAtShop { get; }
        public string GoldResult { get; }
        public bool IsShop { get; }
        public bool IsLighterUsed { get; }
        public bool IsWhiskeyUsed { get; }
        public string LighterLabel { get; }
        public int LighterPriceAmount { get; }
        public string PlayerGold { get; }
        public IReadOnlyList<ShopCardOptionViewModel> ShopCardOptions =>
            _shopCardOptions;
        public int? ShopOfferId { get; }
        public IReadOnlyList<ShopOwnedCardViewModel> ShopOwnedCards =>
            _shopOwnedCards;
        public string ShopTransactionResult { get; }
        public string WhiskeyLabel { get; }
        public int WhiskeyPriceAmount { get; }
        public int WhiskeyRecoveryAmount { get; }
        public bool IsPlayerSoulFull { get; }
    }

    public static class StageProgressionPresenter
    {
        public static StageProgressionViewModel Create(RunProgress progress)
        {
            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
            }

            return Create(progress, null, null, null, null);
        }

        public static StageProgressionViewModel Create(
            StageProgressionSession session,
            string focusedProfileKey = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return Create(
                session.Progress,
                session.PendingOpponentSelection,
                session.PendingStartingDemonGrant,
                focusedProfileKey,
                null);
        }

        public static StageProgressionViewModel Create(
            FormalRunSession session,
            string focusedProfileKey = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            session.SynchronizeExternalState();
            return Create(
                session.CombatSession.Progress,
                session.CombatSession.PendingOpponentSelection,
                session.CombatSession.PendingStartingDemonGrant,
                focusedProfileKey,
                session);
        }

        private static StageProgressionViewModel Create(
            RunProgress progress,
            OpponentSelectionOffer opponentOffer,
            StartingDemonGrant startingDemonGrant,
            string focusedProfileKey,
            FormalRunSession formalSession)
        {
            bool isStartingDemonReveal =
                progress.State == StageProgressionState.NotStarted &&
                startingDemonGrant != null;
            bool isOpponentSelection =
                progress.State == StageProgressionState.OpponentSelection;
            if (isOpponentSelection && opponentOffer == null)
            {
                throw new InvalidOperationException(
                    "Opponent selection state requires a pending opponent offer.");
            }

            string validatedFocusedProfileKey = isOpponentSelection &&
                ContainsProfileKey(opponentOffer, focusedProfileKey)
                    ? focusedProfileKey
                    : null;
            IReadOnlyList<OpponentCandidateViewModel> opponentCandidates =
                isOpponentSelection
                    ? CreateOpponentCandidates(
                        opponentOffer,
                        validatedFocusedProfileKey,
                        formalSession != null)
                    : Array.Empty<OpponentCandidateViewModel>();

            StageDefinition stage = progress.CurrentStage;
            bool canResolveReward = formalSession == null &&
                progress.State == StageProgressionState.RewardSelection;
            PendingBattleReward pendingReward = progress.PendingReward;
            if (canResolveReward && pendingReward == null)
            {
                throw new InvalidOperationException(
                    "Reward selection state requires a pending battle reward.");
            }

            ShopVisit shop = formalSession?.ActiveShop;
            bool isShop = formalSession?.Phase == FormalRunPhase.Shop;
            if (isShop && shop == null)
            {
                throw new InvalidOperationException(
                    "Formal shop phase requires an active shop visit.");
            }

            IReadOnlyList<ShopCardOptionViewModel> shopOptions = isShop
                ? CreateShopOptions(shop, progress.Player)
                : Array.Empty<ShopCardOptionViewModel>();
            IReadOnlyList<ShopOwnedCardViewModel> ownedCards = isShop
                ? CreateOwnedCards(shop, progress.Player)
                : Array.Empty<ShopOwnedCardViewModel>();

            return new StageProgressionViewModel(
                $"STAGE {progress.CurrentStageIndex + 1} / {progress.Stages.Count}",
                stage.DisplayName,
                stage.Kind == StageKind.FinalBossCombat ? "FINAL BOSS" : "NORMAL COMBAT",
                $"{progress.Player.CurrentSoul} / {progress.Player.MaximumSoul}",
                progress.State,
                isShop
                    ? "SHOP"
                    : isStartingDemonReveal
                        ? "STARTING DEMONS"
                        : GetMessage(progress.State),
                progress.State == StageProgressionState.NotStarted &&
                    !isStartingDemonReveal,
                formalSession == null &&
                    progress.State == StageProgressionState.StageCleared,
                formalSession == null
                    ? progress.State == StageProgressionState.RunVictory ||
                        progress.State == StageProgressionState.RunDefeat
                    : formalSession.Phase == FormalRunPhase.RunVictory ||
                        formalSession.Phase == FormalRunPhase.RunDefeat,
                canResolveReward ? GetRewardTier(pendingReward.Offer.Tier) : string.Empty,
                canResolveReward
                    ? CreateRewardOptions(pendingReward.Offer)
                    : Array.Empty<BattleRewardOptionViewModel>(),
                canResolveReward,
                canResolveReward,
                canResolveReward
                    ? GetRewardCompletionMessage(pendingReward.CompletionTarget)
                    : string.Empty,
                GetRewardResult(progress),
                progress.Player.Deck.Count,
                isOpponentSelection ? opponentOffer.OfferId : (int?)null,
                opponentCandidates,
                validatedFocusedProfileKey,
                isOpponentSelection,
                isOpponentSelection && validatedFocusedProfileKey != null,
                isStartingDemonReveal
                    ? startingDemonGrant.GrantId
                    : (int?)null,
                isStartingDemonReveal
                    ? CreateStartingDemonGrantCards(startingDemonGrant)
                    : Array.Empty<StartingDemonGrantCardViewModel>(),
                isStartingDemonReveal,
                $"{progress.Player.CurrentGold} GOLD",
                formalSession != null && formalSession.LastGoldReward > 0
                    ? $"VICTORY +{formalSession.LastGoldReward} GOLD"
                    : string.Empty,
                isShop,
                isShop ? shop.Offer.OfferId : (int?)null,
                shopOptions,
                ownedCards,
                isShop
                    ? CreateUtilityLabel(
                        "LIGHTER",
                        shop.Offer.LighterPrice,
                        shop.HasRemovedCard)
                    : string.Empty,
                isShop ? shop.Offer.LighterPrice : 0,
                isShop && shop.HasRemovedCard,
                isShop
                    ? CreateUtilityLabel(
                        "WHISKEY",
                        shop.Offer.WhiskeyPrice,
                        shop.HasRested)
                    : string.Empty,
                isShop ? shop.Offer.WhiskeyPrice : 0,
                isShop && shop.HasRested,
                isShop &&
                    !shop.HasRested &&
                    progress.Player.CurrentSoul < progress.Player.MaximumSoul &&
                    progress.Player.CurrentGold >= shop.Offer.WhiskeyPrice,
                isShop && !shop.IsClosed,
                isShop ? CreateShopTransactionResult(shop) : string.Empty,
                isShop ? shop.Offer.WhiskeyRecovery : 0,
                isShop &&
                    progress.Player.CurrentSoul >= progress.Player.MaximumSoul);
        }

        private static IReadOnlyList<StartingDemonGrantCardViewModel>
            CreateStartingDemonGrantCards(StartingDemonGrant grant)
        {
            var cards = new List<StartingDemonGrantCardViewModel>(
                grant.Cards.Count);
            foreach (StartingDemonGrantCard card in grant.Cards)
            {
                cards.Add(new StartingDemonGrantCardViewModel(
                    card.DefinitionKey,
                    card.DisplayName,
                    card.Summary,
                    card.CostSummary));
            }

            return cards;
        }

        private static IReadOnlyList<OpponentCandidateViewModel> CreateOpponentCandidates(
            OpponentSelectionOffer offer,
            string focusedProfileKey,
            bool usesFormalRewards)
        {
            var candidates = new List<OpponentCandidateViewModel>(offer.Candidates.Count);
            GoldRewardCatalog goldCatalog = usesFormalRewards
                ? GoldRewardCatalog.CreatePrototype()
                : null;
            foreach (OpponentSelectionCandidate candidate in offer.Candidates)
            {
                candidates.Add(CreateOpponentCandidate(
                    candidate.ProfileKey,
                    candidate.Preview,
                    focusedProfileKey,
                    usesFormalRewards,
                    goldCatalog));
            }

            return candidates;
        }

        /// <summary>
        /// The final boss stage never generates an <see cref="OpponentSelectionOffer"/>
        /// (it has one fixed enemy, not a choice among several), so it needs its own
        /// single-candidate builder for the boss "wanted poster" reveal shown right
        /// before combat starts. Returns null when the run's active stage isn't the
        /// final boss, so the caller can hide the reveal poster.
        /// </summary>
        public static OpponentCandidateViewModel CreateFinalBossRevealCandidate(
            FormalRunSession session)
        {
            StageDefinition stage = session?.CombatSession?.ActiveStage;
            if (stage == null ||
                stage.Kind != StageKind.FinalBossCombat ||
                stage.BattleProfileKey == null)
            {
                return null;
            }

            EnemyProfilePreview preview = EnemyCombatProfileCatalog.Default
                .GetPreviewByKey(stage.BattleProfileKey);
            return CreateOpponentCandidate(
                stage.BattleProfileKey,
                preview,
                focusedProfileKey: null,
                usesFormalRewards: true,
                GoldRewardCatalog.CreatePrototype());
        }

        private static OpponentCandidateViewModel CreateOpponentCandidate(
            string profileKey,
            EnemyProfilePreview preview,
            string focusedProfileKey,
            bool usesFormalRewards,
            GoldRewardCatalog goldCatalog)
        {
            return new OpponentCandidateViewModel(
                profileKey,
                preview.DisplayName,
                preview.Grade.ToString().ToUpperInvariant(),
                $"SOUL {preview.MaximumSoul}",
                preview.Summary,
                usesFormalRewards
                    ? $"VICTORY GOLD {goldCatalog.GetAmount(preview.ProfileKey)}"
                    : GetRewardTier(preview.ExpectedRewardTier),
                $"×{preview.MaximumSoul}",
                usesFormalRewards
                    ? $"×{goldCatalog.GetAmount(preview.ProfileKey)}"
                    : string.Empty,
                StringComparer.Ordinal.Equals(profileKey, focusedProfileKey));
        }

        private static IReadOnlyList<ShopCardOptionViewModel> CreateShopOptions(
            ShopVisit shop,
            PlayerRunState player)
        {
            var options = new List<ShopCardOptionViewModel>(
                shop.Offer.CardOptions.Count);
            foreach (ShopCardOption option in shop.Offer.CardOptions)
            {
                bool isSold = ContainsOptionId(
                    shop.PurchasedOptionIds,
                    option.OptionId);
                string displayName;
                string summary;
                string category;
                if (option.DeckKind == ShopCardDeckKind.Normal)
                {
                    CardDefinition definition = CardDefinitionCatalog.GetByKey(
                        option.DefinitionKey);
                    displayName = $"{definition.Rank} {definition.DisplayName}";
                    summary = definition.Description;
                    category = "CARD";
                }
                else
                {
                    DemonContractDefinition definition =
                        DemonContractCatalog.Default.GetByKey(option.DefinitionKey);
                    displayName = definition.DisplayName;
                    summary = definition.Summary;
                    category = "DEMON";
                }

                options.Add(new ShopCardOptionViewModel(
                    option.OptionId,
                    option.DefinitionKey,
                    displayName,
                    category,
                    summary,
                    $"{option.Price} GOLD",
                    option.Price,
                    !isSold && player.CurrentGold >= option.Price,
                    isSold));
            }

            return options;
        }

        private static IReadOnlyList<ShopOwnedCardViewModel> CreateOwnedCards(
            ShopVisit shop,
            PlayerRunState player)
        {
            var cards = new List<ShopOwnedCardViewModel>(player.Deck.Count);
            foreach (RunCardDefinition card in player.Deck)
            {
                CardDefinition definition = CardDefinitionCatalog.GetByKey(
                    card.DefinitionKey);
                cards.Add(new ShopOwnedCardViewModel(
                    card.Id,
                    definition.Key,
                    definition.Rank,
                    $"{definition.Rank} {definition.DisplayName}",
                    definition.Description,
                    card.Suit,
                    !shop.HasRemovedCard &&
                        player.CurrentGold >= shop.Offer.LighterPrice &&
                        player.CanRemoveCard(card.Id)));
            }

            return cards;
        }

        private static bool ContainsOptionId(
            IReadOnlyCollection<int> optionIds,
            int optionId)
        {
            foreach (int candidate in optionIds)
            {
                if (candidate == optionId)
                {
                    return true;
                }
            }

            return false;
        }

        private static string CreateUtilityLabel(
            string name,
            int price,
            bool wasUsed)
        {
            return wasUsed ? $"{name}  USED" : $"{name}  {price} GOLD";
        }

        private static string CreateShopTransactionResult(ShopVisit shop)
        {
            ShopTransaction transaction = shop.LastTransaction;
            if (transaction == null)
            {
                return string.Empty;
            }

            switch (transaction.Kind)
            {
                case ShopTransactionKind.CardPurchase:
                    return $"PURCHASED  {transaction.DefinitionKey}";
                case ShopTransactionKind.CardRemoval:
                    return $"REMOVED CARD  {transaction.AffectedCardId}";
                case ShopTransactionKind.SoulRecovery:
                    return $"RECOVERED {transaction.SoulRecovered} SOUL";
                case ShopTransactionKind.Leave:
                    return "SHOP CLOSED";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(transaction.Kind));
            }
        }

        private static bool ContainsProfileKey(
            OpponentSelectionOffer offer,
            string profileKey)
        {
            if (offer == null || string.IsNullOrEmpty(profileKey))
            {
                return false;
            }

            foreach (OpponentSelectionCandidate candidate in offer.Candidates)
            {
                if (StringComparer.Ordinal.Equals(candidate.ProfileKey, profileKey))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<BattleRewardOptionViewModel> CreateRewardOptions(
            BattleRewardOffer offer)
        {
            var options = new List<BattleRewardOptionViewModel>(offer.Options.Count);
            foreach (BattleRewardOption option in offer.Options)
            {
                CardDefinition definition = CardDefinitionCatalog.GetByKey(
                    option.DefinitionKey);
                options.Add(new BattleRewardOptionViewModel(
                    option.OptionId,
                    option.DefinitionKey,
                    definition.DisplayName,
                    definition.Rank,
                    GetEffectSummary(definition)));
            }

            return options;
        }

        private static string GetRewardTier(BattleRewardTier tier)
        {
            switch (tier)
            {
                case BattleRewardTier.Normal:
                    return "NORMAL REWARD";
                case BattleRewardTier.HighGrade:
                    return "HIGH-GRADE REWARD";
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), tier, null);
            }
        }

        private static string GetRewardCompletionMessage(
            BattleRewardCompletionTarget completionTarget)
        {
            switch (completionTarget)
            {
                case BattleRewardCompletionTarget.StageCleared:
                    return "REWARD COMPLETION WILL CLEAR THIS STAGE";
                case BattleRewardCompletionTarget.RunVictory:
                    return "REWARD COMPLETION WILL END THE RUN";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(completionTarget),
                        completionTarget,
                        null);
            }
        }

        private static string GetEffectSummary(CardDefinition definition)
        {
            switch (definition.Effect)
            {
                case CardEffectKind.None:
                    return definition.Activation == CardActivationKind.Passive
                        ? "PASSIVE VALUE CARD"
                        : "STANDARD VALUE CARD";
                case CardEffectKind.CrystalOrb:
                    return "PEEK AT 2 DECK CARDS";
                case CardEffectKind.ThreatHammer:
                    return "DISCARD 1 FACE-UP CARD";
                case CardEffectKind.AutoPistol:
                    return "GUESS 1 HIDDEN CARD";
                case CardEffectKind.MilitaryKnife:
                    return "FORCE A DRAW";
                case CardEffectKind.Poison:
                    return "PAY SOUL OR STAND; WIN RESTORES SOUL";
                case CardEffectKind.ResurrectionHerb:
                    return "OPTIONAL FREE HAND REDEAL";
                case CardEffectKind.LieDetector:
                    return "COMPARE AN ENEMY HIDDEN CARD";
                case CardEffectKind.Flamethrower:
                    return "BOTH SIDES MAY DISCARD";
                case CardEffectKind.PocketWatch:
                    return "REACTIVATE A USED MANUAL CARD";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(definition),
                        definition.Effect,
                        null);
            }
        }

        private static string GetRewardResult(RunProgress progress)
        {
            BattleRewardResolution resolution = progress.LastRewardResolution;
            if (resolution == null)
            {
                return string.Empty;
            }

            if (resolution.WasSkipped)
            {
                return $"REWARD SKIPPED  |  DECK {progress.Player.Deck.Count}";
            }

            CardDefinition definition = CardDefinitionCatalog.GetByKey(
                resolution.SelectedDefinitionKey);
            return $"ADDED  {definition.Rank} {definition.DisplayName}  |  " +
                $"DECK {progress.Player.Deck.Count}";
        }

        private static string GetMessage(StageProgressionState state)
        {
            switch (state)
            {
                case StageProgressionState.NotStarted:
                    return "READY TO START RUN";
                case StageProgressionState.OpponentSelection:
                    return "CHOOSE OPPONENT";
                case StageProgressionState.InBattle:
                    return "BATTLE IN PROGRESS";
                case StageProgressionState.RewardSelection:
                    return "SELECT BATTLE REWARD";
                case StageProgressionState.StageCleared:
                    return "STAGE CLEARED";
                case StageProgressionState.RunVictory:
                    return "RUN VICTORY";
                case StageProgressionState.RunDefeat:
                    return "RUN DEFEAT";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }
        }
    }
}
