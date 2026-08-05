using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;

namespace DiaBlackJack.GameScene
{
    public enum CodexCategory
    {
        Enemy,
        DemonCard
    }

    public sealed class CodexDeckCardViewModel
    {
        public CodexDeckCardViewModel(
            string definitionKey,
            int rank,
            string displayName,
            string description,
            CardSuit suit,
            int count)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            DefinitionKey = definitionKey ??
                throw new ArgumentNullException(nameof(definitionKey));
            DisplayName = displayName ??
                throw new ArgumentNullException(nameof(displayName));
            Description = description ?? string.Empty;
            Rank = rank;
            Suit = suit;
            Count = count;
        }

        public int Count { get; }

        public string DefinitionKey { get; }

        public string Description { get; }

        public string DisplayName { get; }

        public int Rank { get; }

        public CardSuit Suit { get; }
    }

    public sealed class CodexDemonReferenceViewModel
    {
        public CodexDemonReferenceViewModel(
            string definitionKey,
            string displayName)
        {
            DefinitionKey = definitionKey ??
                throw new ArgumentNullException(nameof(definitionKey));
            DisplayName = displayName ??
                throw new ArgumentNullException(nameof(displayName));
            EnglishName = DefinitionKey.ToUpperInvariant();
        }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public string EnglishName { get; }
    }

    public sealed class EnemyCodexPageViewModel
    {
        public EnemyCodexPageViewModel(
            string profileKey,
            string displayName,
            int maximumSoul,
            int defeatGold,
            string description,
            IReadOnlyList<CodexDemonReferenceViewModel> contractableDemons,
            IReadOnlyList<CodexDeckCardViewModel> startingDeck)
        {
            ProfileKey = profileKey ??
                throw new ArgumentNullException(nameof(profileKey));
            DisplayName = displayName ??
                throw new ArgumentNullException(nameof(displayName));
            Description = description ??
                throw new ArgumentNullException(nameof(description));
            MaximumSoul = maximumSoul;
            DefeatGold = defeatGold;
            ContractableDemons = contractableDemons ??
                throw new ArgumentNullException(nameof(contractableDemons));
            StartingDeck = startingDeck ??
                throw new ArgumentNullException(nameof(startingDeck));

            int startingDeckCardCount = 0;
            foreach (CodexDeckCardViewModel card in StartingDeck)
            {
                if (card == null)
                {
                    throw new ArgumentException(
                        "Starting deck cannot contain null.",
                        nameof(startingDeck));
                }

                startingDeckCardCount += card.Count;
            }

            StartingDeckCardCount = startingDeckCardCount;
        }

        public IReadOnlyList<CodexDemonReferenceViewModel>
            ContractableDemons { get; }

        public int DefeatGold { get; }

        public string Description { get; }

        public string DisplayName { get; }

        public int MaximumSoul { get; }

        public string ProfileKey { get; }

        public IReadOnlyList<CodexDeckCardViewModel> StartingDeck { get; }

        public int StartingDeckCardCount { get; }
    }

    public sealed class DemonCodexPageViewModel
    {
        public DemonCodexPageViewModel(
            string definitionKey,
            string displayName,
            int purchaseGold,
            int soulPrice,
            string loreDescription,
            string activeSkill,
            string cost)
        {
            DefinitionKey = definitionKey ??
                throw new ArgumentNullException(nameof(definitionKey));
            DisplayName = displayName ??
                throw new ArgumentNullException(nameof(displayName));
            EnglishName = DefinitionKey.ToUpperInvariant();
            LoreDescription = loreDescription ??
                throw new ArgumentNullException(nameof(loreDescription));
            ActiveSkill = activeSkill ??
                throw new ArgumentNullException(nameof(activeSkill));
            Cost = cost ?? throw new ArgumentNullException(nameof(cost));
            PurchaseGold = purchaseGold;
            SoulPrice = soulPrice;
        }

        public string ActiveSkill { get; }

        public string Cost { get; }

        public string DefinitionKey { get; }

        public string DisplayName { get; }

        public string EnglishName { get; }

        public string LoreDescription { get; }

        public int PurchaseGold { get; }

        public int SoulPrice { get; }
    }

    public sealed class CodexBookViewModel
    {
        public CodexBookViewModel(
            CodexCategory category,
            int pageIndex,
            int pageCount,
            EnemyCodexPageViewModel enemyPage,
            DemonCodexPageViewModel demonPage)
        {
            if (pageCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageCount));
            }

            if (pageIndex < 0 || pageIndex >= pageCount)
            {
                throw new ArgumentOutOfRangeException(nameof(pageIndex));
            }

            Category = category;
            PageIndex = pageIndex;
            PageCount = pageCount;
            EnemyPage = enemyPage;
            DemonPage = demonPage;
        }

        public bool CanMoveNext =>
            PageIndex + 1 < PageCount || Category == CodexCategory.Enemy;

        public bool CanMovePrevious =>
            PageIndex > 0 || Category == CodexCategory.DemonCard;

        public CodexCategory Category { get; }

        public DemonCodexPageViewModel DemonPage { get; }

        public EnemyCodexPageViewModel EnemyPage { get; }

        public int PageCount { get; }

        public int PageIndex { get; }
    }

    public sealed class CodexNavigationState
    {
        private readonly int _enemyPageCount;
        private readonly int _demonPageCount;
        private int _enemyPageIndex;
        private int _demonPageIndex;

        public CodexNavigationState(int enemyPageCount, int demonPageCount)
        {
            if (enemyPageCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyPageCount));
            }

            if (demonPageCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(demonPageCount));
            }

            _enemyPageCount = enemyPageCount;
            _demonPageCount = demonPageCount;
            Category = CodexCategory.Enemy;
        }

        public CodexCategory Category { get; private set; }

        public int CurrentPageCount =>
            Category == CodexCategory.Enemy
                ? _enemyPageCount
                : _demonPageCount;

        public int CurrentPageIndex =>
            Category == CodexCategory.Enemy
                ? _enemyPageIndex
                : _demonPageIndex;

        public bool TryMoveNext()
        {
            if (CurrentPageIndex + 1 >= CurrentPageCount)
            {
                if (Category != CodexCategory.Enemy)
                {
                    return false;
                }

                Category = CodexCategory.DemonCard;
                _demonPageIndex = 0;
                return true;
            }

            if (Category == CodexCategory.Enemy)
            {
                _enemyPageIndex++;
            }
            else
            {
                _demonPageIndex++;
            }

            return true;
        }

        public bool TryMovePrevious()
        {
            if (CurrentPageIndex <= 0)
            {
                if (Category != CodexCategory.DemonCard)
                {
                    return false;
                }

                Category = CodexCategory.Enemy;
                _enemyPageIndex = _enemyPageCount - 1;
                return true;
            }

            if (Category == CodexCategory.Enemy)
            {
                _enemyPageIndex--;
            }
            else
            {
                _demonPageIndex--;
            }

            return true;
        }

        public bool TryShowCategory(CodexCategory category)
        {
            if (!Enum.IsDefined(typeof(CodexCategory), category) ||
                Category == category)
            {
                return false;
            }

            Category = category;
            return true;
        }

        public bool TryShowDemonPage(int pageIndex)
        {
            if (pageIndex < 0 ||
                pageIndex >= _demonPageCount ||
                (Category == CodexCategory.DemonCard &&
                    _demonPageIndex == pageIndex))
            {
                return false;
            }

            Category = CodexCategory.DemonCard;
            _demonPageIndex = pageIndex;
            return true;
        }
    }

    public static class CodexPresenter
    {
        public static IReadOnlyList<EnemyCodexPageViewModel> CreateEnemyPages(
            EnemyCombatProfileCatalog enemyCatalog,
            GoldRewardCatalog goldCatalog,
            CardContentCatalog cardCatalog)
        {
            if (enemyCatalog == null)
            {
                throw new ArgumentNullException(nameof(enemyCatalog));
            }

            if (goldCatalog == null)
            {
                throw new ArgumentNullException(nameof(goldCatalog));
            }

            if (cardCatalog == null)
            {
                throw new ArgumentNullException(nameof(cardCatalog));
            }

            var pages = new List<EnemyCodexPageViewModel>(
                enemyCatalog.Profiles.Count);
            foreach (EnemyCombatProfile profile in enemyCatalog.Profiles)
            {
                pages.Add(CreateEnemyPage(profile, goldCatalog, cardCatalog));
            }

            return new ReadOnlyCollection<EnemyCodexPageViewModel>(pages);
        }

        public static IReadOnlyList<DemonCodexPageViewModel> CreateDemonPages(
            CardContentCatalog cardCatalog,
            IReadOnlyDictionary<string, string> loreByDefinitionKey)
        {
            if (cardCatalog == null)
            {
                throw new ArgumentNullException(nameof(cardCatalog));
            }

            if (loreByDefinitionKey == null)
            {
                throw new ArgumentNullException(nameof(loreByDefinitionKey));
            }

            IReadOnlyList<string> prototypeKeys =
                DemonContractCatalog.PrototypeEnabledDemonKeys;
            var pages = new List<DemonCodexPageViewModel>(
                prototypeKeys.Count);
            foreach (string definitionKey in prototypeKeys)
            {
                DemonContractDefinition definition =
                    cardCatalog.GetDemonByKey(definitionKey);

                if (!loreByDefinitionKey.TryGetValue(
                        definition.Key,
                        out string lore) ||
                    string.IsNullOrWhiteSpace(lore))
                {
                    throw new KeyNotFoundException(
                        $"Codex lore for demon '{definition.Key}' does not exist.");
                }

                pages.Add(new DemonCodexPageViewModel(
                    definition.Key,
                    definition.DisplayName,
                    definition.BasePurchasePrice,
                    definition.BaseSoulCost,
                    lore.Trim(),
                    definition.Summary,
                    definition.CostSummary));
            }

            return new ReadOnlyCollection<DemonCodexPageViewModel>(pages);
        }

        public static CodexBookViewModel CreateBook(
            CodexNavigationState navigation,
            IReadOnlyList<EnemyCodexPageViewModel> enemyPages,
            IReadOnlyList<DemonCodexPageViewModel> demonPages)
        {
            if (navigation == null)
            {
                throw new ArgumentNullException(nameof(navigation));
            }

            if (enemyPages == null)
            {
                throw new ArgumentNullException(nameof(enemyPages));
            }

            if (demonPages == null)
            {
                throw new ArgumentNullException(nameof(demonPages));
            }

            int index = navigation.CurrentPageIndex;
            if (navigation.Category == CodexCategory.Enemy)
            {
                return new CodexBookViewModel(
                    CodexCategory.Enemy,
                    index,
                    enemyPages.Count,
                    enemyPages[index],
                    null);
            }

            return new CodexBookViewModel(
                CodexCategory.DemonCard,
                index,
                demonPages.Count,
                null,
                demonPages[index]);
        }

        private static EnemyCodexPageViewModel CreateEnemyPage(
            EnemyCombatProfile profile,
            GoldRewardCatalog goldCatalog,
            CardContentCatalog cardCatalog)
        {
            var demons = new List<CodexDemonReferenceViewModel>(
                profile.DemonContractDefinitionKeys.Count);
            foreach (string definitionKey in
                profile.DemonContractDefinitionKeys)
            {
                DemonContractDefinition definition =
                    cardCatalog.GetDemonByKey(definitionKey);
                demons.Add(new CodexDemonReferenceViewModel(
                    definition.Key,
                    definition.DisplayName));
            }

            var deck = new List<CodexDeckCardViewModel>(
                profile.DeckDefinitionKeys.Count);
            var groupIndices =
                new Dictionary<(string DefinitionKey, CardSuit Suit), int>();
            int[] rankOccurrences = new int[11];
            foreach (string definitionKey in profile.DeckDefinitionKeys)
            {
                CardDefinition definition =
                    cardCatalog.GetNormalByKey(definitionKey);
                CardSuit suit = rankOccurrences[definition.Rank] % 2 == 0
                    ? CardSuit.Spade
                    : CardSuit.Clover;
                rankOccurrences[definition.Rank]++;
                (string DefinitionKey, CardSuit Suit) groupKey =
                    (definition.Key, suit);
                if (groupIndices.TryGetValue(groupKey, out int groupIndex))
                {
                    CodexDeckCardViewModel existing = deck[groupIndex];
                    deck[groupIndex] = new CodexDeckCardViewModel(
                        existing.DefinitionKey,
                        existing.Rank,
                        existing.DisplayName,
                        existing.Description,
                        existing.Suit,
                        existing.Count + 1);
                    continue;
                }

                groupIndices.Add(groupKey, deck.Count);
                deck.Add(new CodexDeckCardViewModel(
                    definition.Key,
                    definition.Rank,
                    definition.DisplayName,
                    definition.Description,
                    suit,
                    count: 1));
            }

            return new EnemyCodexPageViewModel(
                profile.Key,
                profile.DisplayName,
                profile.MaximumSoul,
                goldCatalog.GetAmount(profile.Key),
                profile.Summary,
                new ReadOnlyCollection<CodexDemonReferenceViewModel>(demons),
                new ReadOnlyCollection<CodexDeckCardViewModel>(deck));
        }
    }
}
