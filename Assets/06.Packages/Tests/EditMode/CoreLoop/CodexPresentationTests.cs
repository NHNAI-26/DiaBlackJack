using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.GameScene;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CodexPresentationTests
    {
        [Test]
        public void DX01_U01_EnemyPagesExposeSoulGoldContractsAndFullStartingDeck()
        {
            CardContentCatalog cards = CreateCardCatalog();
            IReadOnlyList<EnemyCodexPageViewModel> pages =
                CodexPresenter.CreateEnemyPages(
                    EnemyCombatProfileCatalog.Default,
                    GoldRewardCatalog.CreatePrototype(),
                    cards);

            Assert.That(pages.Count, Is.EqualTo(6));
            EnemyCodexPageViewModel gunslinger = pages.Single(
                page => page.ProfileKey ==
                    EnemyCombatProfileCatalog.GunslingerKey);
            EnemyCombatProfile profile =
                EnemyCombatProfileCatalog.Default.GetByKey(
                    EnemyCombatProfileCatalog.GunslingerKey);
            Assert.That(gunslinger.MaximumSoul, Is.EqualTo(profile.MaximumSoul));
            Assert.That(gunslinger.DefeatGold, Is.EqualTo(4));
            Assert.That(gunslinger.Description, Is.EqualTo(profile.Summary));
            Assert.That(gunslinger.ContractableDemons, Is.Empty);
            Assert.That(
                gunslinger.StartingDeckCardCount,
                Is.EqualTo(profile.DeckDefinitionKeys.Count));
        }

        [Test]
        public void DX01_U02_DuplicateRanksAlternateSpadeAndCloverLikeBattleDeck()
        {
            IReadOnlyList<EnemyCodexPageViewModel> pages =
                CodexPresenter.CreateEnemyPages(
                    EnemyCombatProfileCatalog.Default,
                    GoldRewardCatalog.CreatePrototype(),
                    CreateCardCatalog());
            EnemyCodexPageViewModel gunslinger = pages.Single(
                page => page.ProfileKey ==
                    EnemyCombatProfileCatalog.GunslingerKey);
            CodexDeckCardViewModel[] sevens = gunslinger.StartingDeck
                .Where(card => card.Rank == 7)
                .ToArray();

            Assert.That(sevens.Length, Is.EqualTo(2));
            Assert.That(sevens[0].Suit, Is.EqualTo(CardSuit.Spade));
            Assert.That(sevens[1].Suit, Is.EqualTo(CardSuit.Clover));
            Assert.That(sevens[0].Count, Is.EqualTo(2));
            Assert.That(sevens[1].Count, Is.EqualTo(2));
        }

        [Test]
        public void DX01_U03_DemonPagesExposePricesSkillCostAndLore()
        {
            CardContentCatalog cards = CreateCardCatalog();
            Dictionary<string, string> lore = CreateLore(cards);
            IReadOnlyList<DemonCodexPageViewModel> pages =
                CodexPresenter.CreateDemonPages(cards, lore);

            Assert.That(
                pages.Select(page => page.DefinitionKey),
                Is.EquivalentTo(DemonContractCatalog.PrototypeEnabledDemonKeys));
            Assert.That(pages.Count, Is.EqualTo(6));
            DemonCodexPageViewModel satan = pages.Single(
                page => page.DefinitionKey == DemonContractCatalog.SatanKey);
            DemonContractDefinition definition =
                cards.GetDemonByKey(DemonContractCatalog.SatanKey);
            Assert.That(satan.PurchaseGold, Is.EqualTo(definition.BasePurchasePrice));
            Assert.That(satan.SoulPrice, Is.EqualTo(definition.BaseSoulCost));
            Assert.That(satan.ActiveSkill, Is.EqualTo(definition.Summary));
            Assert.That(satan.Cost, Is.EqualTo(definition.CostSummary));
            Assert.That(satan.LoreDescription, Is.EqualTo("LORE:satan"));
        }

        [Test]
        public void DX01_U04_DemonPagesRejectMissingLoreWithoutPartialResult()
        {
            CardContentCatalog cards = CreateCardCatalog();
            Dictionary<string, string> lore = CreateLore(cards);
            lore.Remove(DemonContractCatalog.SatanKey);

            Assert.Throws<KeyNotFoundException>(() =>
                CodexPresenter.CreateDemonPages(cards, lore));
        }

        [Test]
        public void DXM05_U01_DemonPagesIgnoreNonPrototypeDefinitionsAndLore()
        {
            CardContentCatalog cards = CreateCardCatalog();
            Dictionary<string, string> lore = CreateLore(cards);
            lore.Remove(DemonContractCatalog.BaphometKey);

            IReadOnlyList<DemonCodexPageViewModel> pages =
                CodexPresenter.CreateDemonPages(cards, lore);

            Assert.That(
                pages.Select(page => page.DefinitionKey),
                Is.EqualTo(DemonContractCatalog.PrototypeEnabledDemonKeys));
            Assert.That(pages.Any(page =>
                page.DefinitionKey == DemonContractCatalog.BaphometKey),
                Is.False);
        }

        [Test]
        public void DX01_U05_NavigationCrossesCategoryBoundaryAndClampsBookEnds()
        {
            CodexNavigationState navigation =
                new CodexNavigationState(2, 3);

            Assert.That(navigation.TryMovePrevious(), Is.False);
            Assert.That(navigation.TryMoveNext(), Is.True);
            Assert.That(navigation.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(navigation.TryMoveNext(), Is.True);
            Assert.That(navigation.Category, Is.EqualTo(CodexCategory.DemonCard));
            Assert.That(navigation.CurrentPageIndex, Is.Zero);

            Assert.That(navigation.TryMovePrevious(), Is.True);
            Assert.That(navigation.Category, Is.EqualTo(CodexCategory.Enemy));
            Assert.That(navigation.CurrentPageIndex, Is.EqualTo(1));

            Assert.That(
                navigation.TryShowCategory(CodexCategory.DemonCard),
                Is.True);
            Assert.That(navigation.CurrentPageIndex, Is.Zero);
            Assert.That(navigation.TryMoveNext(), Is.True);
            Assert.That(navigation.TryMoveNext(), Is.True);
            Assert.That(navigation.TryMoveNext(), Is.False);

            Assert.That(
                navigation.TryShowCategory(CodexCategory.Enemy),
                Is.True);
            Assert.That(navigation.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(
                navigation.TryShowCategory(CodexCategory.DemonCard),
                Is.True);
            Assert.That(navigation.CurrentPageIndex, Is.EqualTo(2));
        }

        [Test]
        public void DX01_U06_BookModelKeepsBoundaryArrowsAcrossCategories()
        {
            var enemyLast = new CodexBookViewModel(
                CodexCategory.Enemy,
                pageIndex: 1,
                pageCount: 2,
                enemyPage: null,
                demonPage: null);
            var demonFirst = new CodexBookViewModel(
                CodexCategory.DemonCard,
                pageIndex: 0,
                pageCount: 3,
                enemyPage: null,
                demonPage: null);
            var enemyFirst = new CodexBookViewModel(
                CodexCategory.Enemy,
                pageIndex: 0,
                pageCount: 2,
                enemyPage: null,
                demonPage: null);
            var demonLast = new CodexBookViewModel(
                CodexCategory.DemonCard,
                pageIndex: 2,
                pageCount: 3,
                enemyPage: null,
                demonPage: null);

            Assert.That(enemyLast.CanMoveNext, Is.True);
            Assert.That(demonFirst.CanMovePrevious, Is.True);
            Assert.That(enemyFirst.CanMovePrevious, Is.False);
            Assert.That(demonLast.CanMoveNext, Is.False);
        }

        private static CardContentCatalog CreateCardCatalog()
        {
            return new CardContentCatalog(
                CardDefinitionCatalog.All,
                DemonContractCatalog.Default.Definitions);
        }

        private static Dictionary<string, string> CreateLore(
            CardContentCatalog cards)
        {
            Dictionary<string, string> lore =
                new Dictionary<string, string>();
            foreach (DemonContractDefinition definition in
                cards.DemonDefinitions)
            {
                lore.Add(definition.Key, "LORE:" + definition.Key);
            }

            return lore;
        }
    }
}
