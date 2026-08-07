using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Border.UI;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.GameScene;
using DiaBlackJack.GameScene.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class GameSceneCombatHudPresentationTests
    {
        private const string HudPrefabPath = "Assets/03. Prefabs/UI/HUD.prefab";
        private const string RevolverNumberSelectorPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/RevolverNumberSelector.prefab";
        private const string DefaultButtonPrefabPath =
            "Assets/03. Prefabs/UI/DefaultButton.prefab";
        private const string CardHoverTooltipPrefabPath =
            "Assets/03. Prefabs/UI/CardHoverTooltip.prefab";
        private const string ManagerPrefabPath =
            "Assets/03. Prefabs/Manager/GameManager.prefab";
        private const string CardPrefabPath =
            "Assets/03. Prefabs/Card/Card.prefab";
        private const string DemonCardPrefabPath =
            "Assets/03. Prefabs/Card/DemonCard.prefab";
        private const string DemonCardHoverDetailPrefabPath =
            "Assets/03. Prefabs/UI/DemonCardHoverDetail.prefab";
        private const string TablePrefabPath =
            "Assets/03. Prefabs/TableObjects/Table Controller.prefab";
        private const string ContractPaperSpritePath =
            "Assets/05. Arts/Texture/ContractPaper/ContractPaper.png";
        private const string TableCommandFrameSpritePath =
            "Assets/05. Arts/UI/button.png";
        private const string GoldSpriteAssetPath =
            "Assets/TextMesh Pro/Resources/Sprite Assets/GoldIcon.asset";
        private const string SoulSpriteAssetPath =
            "Assets/TextMesh Pro/Resources/Sprite Assets/SoulIcon-v5.asset";

        [Test]
        public void GSH01_U01_PlayerTurnProjectsThreeButtonsAndTableContractEntry()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle);
            GameSceneCombatHudViewModel model = GameSceneCombatHudPresenter.Create(
                core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false);

            Assert.That(model.Mode, Is.EqualTo(GameSceneCombatHudMode.Actions));
            Assert.That(model.PrimaryActions.Select(action => action.Command.Kind), Is.EqualTo(new[]
            {
                GameSceneCombatHudCommandKind.Hit,
                GameSceneCombatHudCommandKind.Stand,
                GameSceneCombatHudCommandKind.BeginChange
            }));
            Assert.That(model.PrimaryActions.All(action => action.IsInteractable),
                Is.True);
            Assert.That(
                model.PrimaryActions[2].Label,
                Is.EqualTo($"CHANGE {CurrencyIconMarkup.SoulTag} 0"));
            Assert.That(model.PrimaryActions.All(action => action.Tooltip == string.Empty),
                Is.True);
            Assert.That(model.PrimaryActions.Any(action =>
                action.Command.Kind == GameSceneCombatHudCommandKind.BeginContract),
                Is.False);
        }

        [Test]
        public void TU_U09_RestrictedPrimaryActionOnlyKeepsAllowedActionInteractable()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);
            CoreLoopViewModel core = CoreLoopPresenter.Create(battle);

            GameSceneCombatHudViewModel restricted = GameSceneCombatHudPresenter.Create(
                core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                restrictedPrimaryAction: GameSceneCombatHudCommandKind.Stand);

            Assert.That(
                restricted.PrimaryActions
                    .Single(action => action.Command.Kind ==
                        GameSceneCombatHudCommandKind.Stand)
                    .IsInteractable,
                Is.True);
            Assert.That(
                restricted.PrimaryActions
                    .Single(action => action.Command.Kind ==
                        GameSceneCombatHudCommandKind.Hit)
                    .IsInteractable,
                Is.False);
            Assert.That(
                restricted.PrimaryActions
                    .Single(action => action.Command.Kind ==
                        GameSceneCombatHudCommandKind.BeginChange)
                    .IsInteractable,
                Is.False);
        }

        [Test]
        public void TU_U10_RestrictedPrimaryActionStaysDisabledIfUnderlyingRuleForbidsIt()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);
            CoreLoopViewModel core = CoreLoopPresenter.Create(battle);

            GameSceneCombatHudViewModel restricted = GameSceneCombatHudPresenter.Create(
                core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: true,
                restrictedPrimaryAction: GameSceneCombatHudCommandKind.Hit);

            Assert.That(
                restricted.PrimaryActions
                    .Single(action => action.Command.Kind ==
                        GameSceneCombatHudCommandKind.Hit)
                    .IsInteractable,
                Is.False);
        }

        [Test]
        public void DCUI01_U01_PlayerClickImmediatelyConsumesTopPaper()
        {
            CoreLoopBattle battle = CreateStartedContractBattle(
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon);

            ContractPaperViewModel initial = ContractPaperPresenter.Create(battle);
            Assert.That(initial.VisibleCount, Is.EqualTo(2));
            Assert.That(initial.CanPlayerBegin, Is.True);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            ContractPaperViewModel choosing = ContractPaperPresenter.Create(battle);
            Assert.That(choosing.VisibleCount, Is.EqualTo(1));
            Assert.That(choosing.CanPlayerBegin, Is.False);

            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(
                battle.TryResolvePlayerDemonContract(
                    pending.InteractionId,
                    pending.Options[0].OptionId),
                Is.True);

            ContractPaperViewModel committed = ContractPaperPresenter.Create(battle);
            Assert.That(committed.VisibleCount, Is.EqualTo(1));
            Assert.That(committed.CanPlayerBegin, Is.False);
        }

        [Test]
        public void DCUI01_U02_EnemyContractAutomaticallyConsumesOnePaper()
        {
            CoreLoopBattle battle = CreateEnemyContractBattle();

            Assert.That(ContractPaperPresenter.Create(battle).VisibleCount,
                Is.EqualTo(2));
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.EqualTo(1));
            Assert.That(ContractPaperPresenter.Create(battle).VisibleCount,
                Is.EqualTo(1));
        }

        [Test]
        public void DCUI01_U03_ViewShowsExactPaperCountAndClickability()
        {
            var root = new GameObject("ContractPapers");
            try
            {
                ContractPaperView view = root.AddComponent<ContractPaperView>();
                // Higher sortingOrder renders in front — "first" is the visually
                // front-most paper here, which is what ContractPaperView now keys
                // "top of stack" off of (see ContractPaperClickable.SortingOrder).
                ContractPaperClickable first =
                    CreateContractPaper(root, "PaperA", sortingOrder: 11);
                ContractPaperClickable second =
                    CreateContractPaper(root, "PaperB", sortingOrder: 10);

                view.Render(new ContractPaperViewModel(2, true));
                Assert.That(view.HasRequiredReferences, Is.True);
                Assert.That(view.VisibleCount, Is.EqualTo(2));
                Assert.That(first.gameObject.activeSelf, Is.True);
                Assert.That(second.gameObject.activeSelf, Is.True);
                // Only the top-most visible paper is hoverable/clickable — the rest of
                // the stack is purely visual filler.
                Assert.That(first.IsInteractable, Is.True);
                Assert.That(second.IsInteractable, Is.False);

                view.Render(new ContractPaperViewModel(1, false));
                Assert.That(view.VisibleCount, Is.EqualTo(1));
                // The top paper (first, index 0) is always the one that disappears as
                // the stack shrinks, regardless of which paper was actually clicked.
                Assert.That(first.gameObject.activeSelf, Is.False);
                Assert.That(second.gameObject.activeSelf, Is.True);
                Assert.That(second.IsInteractable, Is.False);

                view.Render(new ContractPaperViewModel(1, true));
                // Once it's the only (and therefore top-most) paper left, it becomes
                // clickable in its own right.
                Assert.That(second.IsInteractable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DCUI01_U05_TablePrefabHasTwoStackedContractPaperSprites()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            Sprite expectedSprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(ContractPaperSpritePath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(expectedSprite, Is.Not.Null);

            ContractPaperView view =
                prefab.GetComponentInChildren<ContractPaperView>(true);
            ContractPaperClickable[] papers = prefab
                .GetComponentsInChildren<ContractPaperClickable>(true)
                .OrderBy(paper => paper.name)
                .ToArray();

            Assert.That(view, Is.Not.Null);
            Assert.That(view.HasRequiredReferences, Is.True);
            Assert.That(papers, Has.Length.EqualTo(2));
            Assert.That(
                papers.Select(paper => paper.name),
                Is.EqualTo(new[] { "ContractPaperA", "ContractPaperB" }));
            Assert.That(
                papers.All(paper =>
                    paper.GetComponent<SpriteRenderer>()?.sprite == expectedSprite),
                Is.True);
            Assert.That(
                papers.All(paper => paper.GetComponent<BoxCollider>() != null),
                Is.True);
            Assert.That(
                papers[0].GetComponent<SpriteRenderer>().sortingOrder,
                Is.LessThan(papers[1].GetComponent<SpriteRenderer>().sortingOrder));
        }

        [Test]
        public void DCUI01_U04_NonCombatHidesBothPapers()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);

            ContractPaperViewModel model = ContractPaperPresenter.Create(
                battle,
                isCombatVisible: false);

            Assert.That(model.VisibleCount, Is.Zero);
            Assert.That(model.CanPlayerBegin, Is.False);
        }

        [Test]
        public void DCUI04_U01_ActiveContractsProjectFaceUpDemonCardsForBothSides()
        {
            CoreLoopBattle playerBattle = CreateStartedContractBattle(
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon);
            Assert.That(playerBattle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction playerOffer =
                playerBattle.PendingPlayerDemonContractInteraction;
            DemonContractOption belphegor = playerOffer.Options.Single(option =>
                option.ContractDefinitionKey == DemonContractCatalog.BelphegorKey);
            Assert.That(playerBattle.TryResolvePlayerDemonContract(
                playerOffer.InteractionId,
                belphegor.OptionId), Is.True);

            GameSceneViewModel playerScene =
                GameScenePresenter.Create(playerBattle);
            GameSceneDemonCardViewModel playerCard =
                playerScene.PlayerDemonCards.Single();
            DemonContractDefinition playerDefinition =
                DemonContractCatalog.Default.GetByKey(
                    DemonContractCatalog.BelphegorKey);
            Assert.That(playerCard.DefinitionKey,
                Is.EqualTo(playerDefinition.Key));
            Assert.That(playerCard.IsFaceUp, Is.True);
            Assert.That(playerCard.CanUse, Is.False);
            Assert.That(playerCard.ShowHoverBadgeWhenUnavailable, Is.True);
            Assert.That(playerCard.Summary, Is.EqualTo(playerDefinition.Summary));
            Assert.That(playerCard.CostSummary,
                Is.EqualTo(playerDefinition.CostSummary));
            Assert.That(playerScene.EnemyDemonCards, Is.Empty);

            CoreLoopBattle enemyBattle = CreateEnemyContractBattle();
            Assert.That(enemyBattle.TryPlayerHit(), Is.True);
            Assert.That(enemyBattle.ActiveEnemyDemonContracts, Has.Count.EqualTo(1));

            GameSceneViewModel enemyScene = GameScenePresenter.Create(enemyBattle);
            GameSceneDemonCardViewModel enemyCard =
                enemyScene.EnemyDemonCards.Single();
            Assert.That(enemyCard.DefinitionKey,
                Is.EqualTo(DemonContractCatalog.BelphegorKey));
            Assert.That(enemyCard.IsFaceUp, Is.True);
            Assert.That(enemyCard.ShowHoverBadgeWhenUnavailable, Is.True);
        }

        [Test]
        public void DCUI04_U02_MultipleContractsPreserveAcquisitionOrderForLayout()
        {
            CoreLoopBattle battle = CreateStartedContractBattle(
                DemonContractKind.Lucifer,
                DemonContractKind.Belphegor);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction baseOffer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption lucifer = baseOffer.Options.Single(option =>
                option.ContractDefinitionKey == DemonContractCatalog.LuciferKey);
            Assert.That(battle.TryResolvePlayerDemonContract(
                baseOffer.InteractionId,
                lucifer.OptionId), Is.True);

            PendingDemonContractInteraction additionalOffer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption additional = additionalOffer.Options.Single(option =>
                option.ContractDefinitionKey == DemonContractCatalog.BelphegorKey);
            Assert.That(battle.TryResolvePlayerDemonContract(
                additionalOffer.InteractionId,
                additional.OptionId), Is.True);

            GameSceneViewModel scene = GameScenePresenter.Create(battle);
            Assert.That(
                scene.PlayerDemonCards.Select(card => card.DefinitionKey),
                Is.EqualTo(new[]
                {
                    DemonContractCatalog.LuciferKey,
                    DemonContractCatalog.BelphegorKey
                }));
        }

        [Test]
        public void DCUI04_U03_CardHandPlacesNewestDemonNearestHiddenAtScreenLeft()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            IReadOnlyList<GameSceneCardViewModel> normalCards =
                CreateSceneCardsForHand();
            IReadOnlyList<GameSceneDemonCardViewModel> demonCards =
                CreateSceneDemonCardsForHand();

            try
            {
                CardHand[] hands = instance
                    .GetComponentsInChildren<CardHand>(true)
                    .OrderBy(hand => hand.name)
                    .ToArray();
                Assert.That(hands, Has.Length.EqualTo(2));

                foreach (CardHand hand in hands)
                {
                    hand.gameObject.SetActive(true);
                    hand.Render(normalCards, demonCards);
                    Assert.That(hand.TryGetCardWorldPosition(
                        normalCards[normalCards.Count - 1].CardId,
                        out Vector3 hiddenWorld), Is.True);
                    Assert.That(hand.TryGetDemonCardWorldPosition(
                        demonCards[1].CardId,
                        out Vector3 newestWorld), Is.True);
                    Assert.That(hand.TryGetDemonCardWorldPosition(
                        demonCards[0].CardId,
                        out Vector3 oldestWorld), Is.True);

                    float hiddenX = hand.transform
                        .InverseTransformPoint(hiddenWorld).x;
                    float newestX = hand.transform
                        .InverseTransformPoint(newestWorld).x;
                    float oldestX = hand.transform
                        .InverseTransformPoint(oldestWorld).x;
                    Assert.That(newestX, Is.GreaterThan(hiddenX));
                    Assert.That(oldestX, Is.GreaterThan(newestX));
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void DCUI04_U04_CardHandRemovesEndedDemonAndKeepsRemaining()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            CardHand hand = instance.GetComponentsInChildren<CardHand>(true)
                .Single(candidate => candidate.name == "PlayerHand");
            IReadOnlyList<GameSceneCardViewModel> normalCards =
                CreateSceneCardsForHand();
            IReadOnlyList<GameSceneDemonCardViewModel> demonCards =
                CreateSceneDemonCardsForHand();

            try
            {
                hand.gameObject.SetActive(true);
                hand.Render(normalCards, demonCards);
                hand.Render(normalCards, new[] { demonCards[0] });

                Assert.That(hand.TryGetDemonCardWorldPosition(
                    demonCards[1].CardId,
                    out _), Is.False);
                Assert.That(hand.TryGetDemonCardWorldPosition(
                    demonCards[0].CardId,
                    out Vector3 remainingWorld), Is.True);
                Assert.That(hand.TryGetCardWorldPosition(
                    normalCards[normalCards.Count - 1].CardId,
                    out Vector3 hiddenWorld), Is.True);
                Assert.That(
                    hand.transform.InverseTransformPoint(remainingWorld).x,
                    Is.GreaterThan(
                        hand.transform.InverseTransformPoint(hiddenWorld).x));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void DCUI04_U05_ActiveDemonUsesDedicatedHoverDetail()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(DemonCardPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                DemonCardView view = instance.GetComponent<DemonCardView>();
                GameSceneDemonCardViewModel model =
                    CreateSceneDemonCardsForHand()[0];
                view.Bind(model);
                view.SetHovered(true);

                Assert.That(view.CanUse, Is.False);
                Assert.That(view.ShouldShowHoverBadge, Is.True);
                Assert.That(view.BoundCard, Is.SameAs(model));
                Assert.That(view.HoverBadgeDescription,
                    Does.Contain(model.Summary));
                Assert.That(view.HoverBadgeDescription,
                    Does.Contain(model.CostSummary));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void DCUI04_U06_TablePrefabWiresDemonPrefabToBothHands()
        {
            GameObject table =
                AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            GameObject demon =
                AssetDatabase.LoadAssetAtPath<GameObject>(DemonCardPrefabPath);
            DemonCardView expected = demon.GetComponent<DemonCardView>();
            CardHand[] hands = table.GetComponentsInChildren<CardHand>(true);

            Assert.That(hands, Has.Length.EqualTo(2));
            Assert.That(hands.All(hand => hand.DemonCardPrefab == expected), Is.True);
        }

        [Test]
        public void GF06_U02_ChangeActionLabelShowsCurrentPaidCost()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);
            Assert.That(battle.TryBeginPlayerChange(), Is.True);
            Assert.That(battle.TrySelectChangedCard(0), Is.True);

            GameSceneCombatHudViewModel model = GameSceneCombatHudPresenter.Create(
                CoreLoopPresenter.Create(battle),
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false);

            Assert.That(
                model.PrimaryActions[2].Label,
                Is.EqualTo($"CHANGE {CurrencyIconMarkup.SoulTag} -1"));
        }

        [Test]
        public void CUI01_U01_CurrencyWordsBecomeInlineSpriteTags()
        {
            string formatted = CurrencyIconMarkup.FormatForTmp(
                "PRICE 5 GOLD | 영혼 2 | SOULS 3 | 골드 4");

            Assert.That(
                formatted,
                Is.EqualTo(
                    $"PRICE 5 {CurrencyIconMarkup.GoldTag} | " +
                    $"{CurrencyIconMarkup.SoulTag} 2 | " +
                    $"{CurrencyIconMarkup.SoulTag} 3 | " +
                    $"{CurrencyIconMarkup.GoldTag} 4"));
        }

        [TestCase(GoldSpriteAssetPath)]
        [TestCase(SoulSpriteAssetPath)]
        public void CUI01_U02_CurrencySpriteAssetContainsOneRenderableGlyph(
            string assetPath)
        {
            Object spriteAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            Object[] embeddedAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            Assert.That(spriteAsset, Is.Not.Null);
            Assert.That(spriteAsset.GetType().Name, Is.EqualTo("TMP_SpriteAsset"));
            Assert.That(embeddedAssets.OfType<Material>(), Is.Not.Empty);
        }

        [Test]
        public void CUI01_U03_ImguiCurrencyIconUsesBoundedThumbnail()
        {
            GUIContent content = CurrencyIconGui.Soul("ELITE  ·  5");

            Assert.That(content.image, Is.Not.Null);
            Assert.That(content.image.width, Is.EqualTo(CurrencyIconGui.IconTextureSize));
            Assert.That(content.image.height, Is.EqualTo(CurrencyIconGui.IconTextureSize));
        }

        [Test]
        public void CUI01_U04_HudCurrencyCountersUseMirroredSafeAreaLayout()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);

            RectTransform playerSoul = prefab.transform
                .Find("PlayerSoul")
                .GetComponent<RectTransform>();
            RectTransform enemySoul = prefab.transform
                .Find("EnemySoul")
                .GetComponent<RectTransform>();
            RectTransform gold = prefab.transform
                .Find("Gold")
                .GetComponent<RectTransform>();

            Assert.That(playerSoul.gameObject.activeSelf, Is.True);
            Assert.That(enemySoul.gameObject.activeSelf, Is.True);
            Assert.That(gold.gameObject.activeSelf, Is.True);
            Assert.That(playerSoul.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(enemySoul.anchorMin, Is.EqualTo(new Vector2(1f, 1f)));
            Assert.That(gold.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(
                playerSoul.anchoredPosition,
                Is.EqualTo(new Vector2(24f, -20f)));
            Assert.That(
                enemySoul.anchoredPosition,
                Is.EqualTo(new Vector2(-24f, -20f)));
            Assert.That(
                gold.anchoredPosition,
                Is.EqualTo(new Vector2(24f, -120f)));
        }

        [Test]
        public void CUV06_U01_HammerAndRevolverPresentationHideCombatHud()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);

            GameSceneCombatHudViewModel model = GameSceneCombatHudPresenter.Create(
                CoreLoopPresenter.Create(battle),
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: true,
                hideForPresentation: true);

            Assert.That(model.Mode, Is.EqualTo(GameSceneCombatHudMode.Hidden));
            Assert.That(model.PrimaryActions, Is.Empty);
            Assert.That(model.OptionActions, Is.Empty);
        }

        [Test]
        public void GSH01_U02_ChangeCandidatesUseWorldCardsWithoutCentralOptions()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);
            Assert.That(battle.TryBeginPlayerChange(), Is.True);

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle);
            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                core, false, false, false);
            GameSceneViewModel scene = GameScenePresenter.Create(battle);

            Assert.That(
                hud.Mode,
                Is.EqualTo(GameSceneCombatHudMode.DiegeticSelection));
            Assert.That(
                hud.SelectionPrompt?.Id,
                Is.EqualTo(CombatPromptId.ChangeCard));
            Assert.That(hud.OptionActions, Is.Empty);
            Assert.That(scene.CrystalOrbCandidates, Has.Count.EqualTo(2));
            Assert.That(
                scene.CrystalOrbCandidates.Select(candidate =>
                    candidate.DirectSelectionCommand.Value.OptionId),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(scene.CrystalOrbCandidates.All(candidate =>
                candidate.DirectSelectionCommand.HasValue &&
                candidate.DirectSelectionCommand.Value.Kind ==
                    GameSceneCombatHudCommandKind.SelectChangedCard), Is.True);
        }

        [Test]
        public void GSH01_U03_ContractCandidatesUseWorldCardModeAndKeepInteractionIds()
        {
            CoreLoopBattle battle = CreateStartedContractBattle(
                DemonContractKind.Mammon,
                DemonContractKind.Belphegor);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            CoreLoopViewModel resolving = CoreLoopPresenter.Create(battle);
            GameSceneCombatHudViewModel candidates =
                GameSceneCombatHudPresenter.Create(
                    resolving,
                    false,
                    false,
                    false);
            GameSceneCombatHudViewModel locked =
                GameSceneCombatHudPresenter.Create(
                    resolving,
                    false,
                    false,
                    true);

            Assert.That(
                candidates.Mode,
                Is.EqualTo(GameSceneCombatHudMode.ContractCandidates));
            Assert.That(
                candidates.SelectionPrompt?.Id,
                Is.EqualTo(CombatPromptId.DemonChooseContract));
            Assert.That(
                candidates.ContractCandidates.Select(
                    candidate => candidate.Command.OptionId),
                Is.EqualTo(resolving.DemonContract.Choices.Select(choice => choice.OptionId)));
            Assert.That(candidates.ContractCandidates.All(candidate =>
                candidate.Command.InteractionId ==
                    resolving.DemonContract.InteractionId), Is.True);
            Assert.That(candidates.ContractCandidates.All(candidate =>
                !string.IsNullOrEmpty(candidate.DefinitionKey) &&
                !string.IsNullOrEmpty(candidate.Ability) &&
                !string.IsNullOrEmpty(candidate.Cost)), Is.True);
            Assert.That(locked.ContractCandidates.All(
                candidate => !candidate.IsInteractable), Is.True);
        }

        [Test]
        public void GSH01_U04_AutomaticChoiceAndResultAreProjectedToHud()
        {
            CoreLoopSession session = new CoreLoopSession(() => new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0,
                    2,
                    3,
                    CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey),
                    4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 4, 5, 2)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy()));
            Assert.That(session.TryPlayerHit(), Is.True);

            CoreLoopViewModel pending = CoreLoopPresenter.Create(session.Battle);
            GameSceneCombatHudViewModel choices = GameSceneCombatHudPresenter.Create(
                pending, false, false, false);
            AutomaticCardInteractionViewModel interaction = pending.AutomaticCardInteraction;

            Assert.That(choices.Mode, Is.EqualTo(GameSceneCombatHudMode.Options));
            Assert.That(choices.OptionActions.All(action =>
                action.Command.Kind == GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice &&
                action.Command.InteractionId == interaction.InteractionId), Is.True);

            Assert.That(session.TryResolvePlayerAutomaticCardChoice(
                interaction.InteractionId,
                PoisonEffectHandler.PaySoulOptionId), Is.True);
            GameSceneCombatHudViewModel result = GameSceneCombatHudPresenter.Create(
                CoreLoopPresenter.Create(session.Battle), false, false, false);
            Assert.That(result.AutomaticCardResult, Does.StartWith("AUTOMATIC CARD"));
        }

        [Test]
        public void ACRV04_U04_FlamethrowerResultAppearsOnlyAfterBothCommits()
        {
            CardDefinition flamethrower = CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.FlamethrowerKey);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 2, 3, flamethrower, 4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 4, 5, 6)),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy(),
                cardEffectResolver: CardEffectResolver.CreateDefault(),
                enemyAutomaticCardDecisionPolicy: null);
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingAutomaticCardInteraction playerChoice =
                battle.PendingPlayerAutomaticInteraction;
            AutomaticCardChoiceOption playerCard = playerChoice.Options
                .Single(option => option.CardId == 0);

            Assert.That(battle.TryResolvePlayerAutomaticCardChoice(
                playerChoice.InteractionId,
                playerCard.OptionId), Is.True);
            GameSceneCombatHudViewModel committedPlayer =
                GameSceneCombatHudPresenter.Create(
                    CoreLoopPresenter.Create(battle), false, false, false);
            Assert.That(committedPlayer.AutomaticCardResult, Is.Empty);
            Assert.That(committedPlayer.Mode,
                Is.EqualTo(GameSceneCombatHudMode.Hidden));

            PendingAutomaticCardInteraction enemyChoice =
                battle.PendingAutomaticInteraction;
            AutomaticCardChoiceOption enemySkip = enemyChoice.Options
                .Single(option => option.OptionId ==
                    FlamethrowerEffectHandler.SkipOptionId);
            Assert.That(battle.TryResolveAutomaticCardChoice(
                CombatantSide.Enemy,
                enemyChoice.InteractionId,
                enemySkip.OptionId), Is.True);
            GameSceneCombatHudViewModel revealed =
                GameSceneCombatHudPresenter.Create(
                    CoreLoopPresenter.Create(battle), false, false, false);

            Assert.That(revealed.AutomaticCardResult,
                Does.Contain("PLAYER DISCARDED"));
            Assert.That(revealed.AutomaticCardResult,
                Does.Contain("ENEMY SKIPPED"));
        }

        [Test]
        public void GSH01_U13_EnemyLieDetectorDecisionDoesNotOpenPlayerChoiceHud()
        {
            CardDefinition lieDetector = CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.LieDetectorKey);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 10, 10, 2, 3)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100,
                    2,
                    3,
                    lieDetector,
                    4)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new HitThenStandPolicy());
            CoreLoopViewModel enemyDecisionFrame = null;
            battle.Stepped += () =>
            {
                PendingAutomaticCardInteraction pending =
                    battle.PendingAutomaticInteraction;
                if (pending?.DecisionSide == CombatantSide.Enemy)
                {
                    enemyDecisionFrame = CoreLoopPresenter.Create(battle);
                }
            };
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(enemyDecisionFrame, Is.Not.Null);
            Assert.That(enemyDecisionFrame.IsResolvingAutomaticCardEffect, Is.True);
            Assert.That(enemyDecisionFrame.AutomaticCardInteraction, Is.Null);
            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                enemyDecisionFrame,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false);
            Assert.That(hud.Mode, Is.EqualTo(GameSceneCombatHudMode.Hidden));
            Assert.That(hud.SelectionPrompt, Is.Null);
            Assert.That(hud.OptionActions, Is.Empty);
        }

        [Test]
        public void ACF01_U03_PlayerLieDetectorUsesFocusedNumberSelector()
        {
            CardDefinition lieDetector = CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.LieDetectorKey);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0,
                    2,
                    3,
                    lieDetector)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerHit(), Is.True);

            GameSceneViewModel scene = GameScenePresenter.Create(battle);
            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);

            Assert.That(
                hud.Mode,
                Is.EqualTo(GameSceneCombatHudMode.RevolverNumberSelection));
            Assert.That(hud.OptionActions, Has.Count.EqualTo(10));
            Assert.That(
                hud.OptionActions.Select(action => action.Command.OptionId),
                Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(hud.OptionActions.All(action =>
                action.Command.Kind ==
                    GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice),
                Is.True);
        }

        [Test]
        public void GSH01_U05_BattleEndDifferentiatesStageReturnAndStandaloneRestart()
        {
            CoreLoopSession session = new CoreLoopSession(() => new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 10, 1)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 10)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1,
                enemyPolicy: new StandPolicy()));
            Assert.That(session.TryPlayerStand(), Is.True);
            CoreLoopViewModel ended = CoreLoopPresenter.Create(session.Battle);

            GameSceneCombatHudViewModel standalone = GameSceneCombatHudPresenter.Create(
                ended, false, false, false);
            GameSceneCombatHudViewModel stage = GameSceneCombatHudPresenter.Create(
                ended, true, false, false);

            Assert.That(standalone.Mode, Is.EqualTo(GameSceneCombatHudMode.Restart));
            Assert.That(standalone.OptionActions.Single().Command.Kind,
                Is.EqualTo(GameSceneCombatHudCommandKind.Restart));
            Assert.That(stage.Mode, Is.EqualTo(GameSceneCombatHudMode.ReturningToRun));
            Assert.That(stage.OptionActions, Is.Empty);
        }

        [Test]
        public void CUM09_U01_HammerProjectsOnlyLegalEnemyCardsAsDiegeticTargets()
        {
            CoreLoopBattle battle = CreateStartedHammerBattle();
            BlackjackCard revealedHiddenCard = battle.Enemy.Hand.Cards[1];
            revealedHiddenCard.Reveal();
            battle.Enemy.Draw(faceUp: true);
            BlackjackCard hammer = battle.Player.Draw(faceUp: true);
            Assert.That(battle.TryBeginPlayerCardUse(hammer.Id), Is.True);

            GameSceneViewModel model = GameScenePresenter.Create(battle);
            PendingCardEffect pending = battle.PendingPlayerCardEffect;

            Assert.That(model.UsesDiegeticCardEffectSelection, Is.True);
            Assert.That(
                model.EnemyCards
                    .Where(card => card.CardEffectChoiceOptionId.HasValue)
                    .Select(card => card.CardId),
                Is.EquivalentTo(
                    pending.Options.Select(option => option.CardId.Value)));
            foreach (CardEffectChoiceOption option in pending.Options)
            {
                GameSceneCardViewModel target = model.EnemyCards.Single(
                    card => card.CardId == option.CardId.Value);
                Assert.That(target.CardEffectChoiceOptionId, Is.EqualTo(option.Id));
            }

            GameSceneCardViewModel hiddenRoleCard = model.EnemyCards.Single(
                card => card.CardId == revealedHiddenCard.Id);
            Assert.That(hiddenRoleCard.RevealRank, Is.True);
            Assert.That(hiddenRoleCard.CardEffectChoiceOptionId, Is.Null);
        }

        [Test]
        public void CUM09_U02_HammerKeepsPromptWithoutBlackOptionPanel()
        {
            CoreLoopBattle battle = CreateStartedHammerBattle();
            BlackjackCard hammer = battle.Player.Draw(faceUp: true);
            Assert.That(battle.TryBeginPlayerCardUse(hammer.Id), Is.True);
            GameSceneViewModel scene = GameScenePresenter.Create(battle);

            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);

            Assert.That(
                hud.Mode,
                Is.EqualTo(GameSceneCombatHudMode.DiegeticSelection));
            Assert.That(hud.SelectionPrompt, Is.EqualTo(scene.Core.SelectionPrompt));
            Assert.That(hud.OptionActions, Is.Empty);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                GameHudView view = instance.GetComponent<GameHudView>();
                view.Render(scene.Core, hud);

                SerializedObject serialized = new SerializedObject(view);
                GameObject optionPanel = serialized.FindProperty("optionPanel")
                    .objectReferenceValue as GameObject;
                ScrollRect optionScroll = serialized.FindProperty("optionScrollRect")
                    .objectReferenceValue as ScrollRect;
                Component prompt = serialized.FindProperty("combatPromptView")
                    .objectReferenceValue as Component;

                Assert.That(optionPanel, Is.Not.Null);
                Assert.That(optionPanel.activeSelf, Is.False);
                Assert.That(optionScroll, Is.Not.Null);
                Assert.That(optionScroll.gameObject.activeSelf, Is.False);
                Assert.That(prompt, Is.Not.Null);
                Assert.That(prompt.gameObject.activeInHierarchy, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CUM09_U03_CardViewRetainsProjectedCardEffectChoiceOption()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Card/Card.prefab");
            Assert.That(cardPrefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(cardPrefab);

            try
            {
                CardView view = instance.GetComponent<CardView>();
                Assert.That(view, Is.Not.Null);
                view.Bind(new GameSceneCardViewModel(
                    cardId: 21,
                    rank: 10,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    displayName: "Target",
                    cardEffectChoiceOptionId: 42));

                Assert.That(view.CanUse, Is.False);
                Assert.That(view.CardEffectChoiceOptionId, Is.EqualTo(42));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CUM13_U01_RevolverUsesFocusedNumberSelector()
        {
            CardDefinition revolver = CardDefinitionCatalog.GetDefaultForRank(7);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 2, revolver, 3)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            BlackjackCard source = battle.Player.Hand.Cards.Single(
                card => card.Definition.Effect == CardEffectKind.AutoPistol);
            Assert.That(battle.TryBeginPlayerCardUse(source.Id), Is.True);
            GameSceneViewModel scene = GameScenePresenter.Create(battle);

            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);

            Assert.That(scene.UsesDiegeticCardEffectSelection, Is.False);
            Assert.That(
                hud.Mode,
                Is.EqualTo(GameSceneCombatHudMode.RevolverNumberSelection));
            Assert.That(hud.OptionActions, Has.Count.EqualTo(10));
            Assert.That(hud.OptionActions.All(action =>
                action.Command.Kind ==
                    GameSceneCombatHudCommandKind.ResolveCardEffectChoice), Is.True);
        }

        [Test]
        [Category("CUM06")]
        public void CUM06_U03_PlayerRevolverSelectionStaysHiddenWhileInputLocked()
        {
            CardDefinition revolver = CardDefinitionCatalog.GetDefaultForRank(7);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 2, revolver, 3)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            BlackjackCard source = battle.Player.Hand.Cards.Single(
                card => card.Definition.Effect == CardEffectKind.AutoPistol);
            Assert.That(battle.TryBeginPlayerCardUse(source.Id), Is.True);

            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                CoreLoopPresenter.Create(battle),
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: true);

            Assert.That(hud.Mode, Is.EqualTo(GameSceneCombatHudMode.Hidden));
            Assert.That(hud.OptionActions, Is.Empty);
        }

        [Test]
        [Category("CP04")]
        public void DCUI03_U01_SatanShowsTenCardsAndBrandsFirstDeclaration()
        {
            CoreLoopBattle battle = CreateStartedContractBattle(
                DemonContractKind.Satan,
                DemonContractKind.Belphegor);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction offer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption satan = offer.Options.Single(option =>
                option.ContractDefinitionKey == DemonContractCatalog.SatanKey);
            Assert.That(battle.TryResolvePlayerDemonContract(
                offer.InteractionId,
                satan.OptionId), Is.True);

            // Signing a contract is itself a full player action; since the newly-signed
            // Satan contract can't stand and the enemy immediately stands too (this
            // file's StandPolicy), that cascades straight into a fresh player-turn
            // start where Satan's own once-per-turn "use ability?" choice is already
            // offered — its table card is no longer directly pressable at all.
            PendingDemonContractInteraction turnStart =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(turnStart.Kind,
                Is.EqualTo(DemonContractInteractionKind.SatanTurnStartChoice));

            GameSceneViewModel activeScene = GameScenePresenter.Create(battle);
            GameSceneDemonCardViewModel activeSatan =
                activeScene.PlayerDemonCards.Single();
            Assert.That(activeSatan.CanUse, Is.False);
            Assert.That(activeSatan.IsUpsideDown, Is.False);
            Assert.That(activeSatan.SatanDoomCount, Is.EqualTo(3));

            Assert.That(battle.TryResolvePlayerDemonContract(
                turnStart.InteractionId,
                SatanDemonContractHandler.UseAbilityOptionId), Is.True);

            GameSceneViewModel first = GameScenePresenter.Create(battle);
            GameSceneCombatHudViewModel firstHud =
                GameSceneCombatHudPresenter.Create(
                    first.Core,
                    isStageBattle: false,
                    isShopOpen: false,
                    inputLocked: false);
            GameSceneCombatHudViewModel lockedHud =
                GameSceneCombatHudPresenter.Create(
                    first.Core,
                    isStageBattle: false,
                    isShopOpen: false,
                    inputLocked: true);
            Assert.That(lockedHud.Mode,
                Is.EqualTo(GameSceneCombatHudMode.Hidden));
            Assert.That(lockedHud.SelectionPrompt, Is.Null);
            Assert.That(lockedHud.OptionActions, Is.Empty);
            Assert.That(GameManager.ShouldShowPromptSelection(lockedHud),
                Is.False);
            Assert.That(firstHud.Mode,
                Is.EqualTo(GameSceneCombatHudMode.SatanNumberSelection));
            Assert.That(GameManager.ShouldShowPromptSelection(firstHud),
                Is.True);
            Assert.That(firstHud.SelectionPrompt?.CurrentCount, Is.EqualTo(0));
            Assert.That(firstHud.SelectionPrompt?.RequiredCount, Is.EqualTo(2));
            Assert.That(firstHud.OptionActions.Count, Is.EqualTo(1));
            Assert.That(firstHud.OptionActions[0].Command.Kind,
                Is.EqualTo(GameSceneCombatHudCommandKind
                    .ConfirmSatanNumberSelection));
            Assert.That(firstHud.OptionActions[0].Command.InteractionId,
                Is.EqualTo(first.Core.DemonContract.InteractionId));
            Assert.That(firstHud.OptionActions[0].IsInteractable, Is.False);
            // Satan's forward-facing ability confirm button is centered on screen,
            // unlike the usual bottom-right corner placement.
            Assert.That(firstHud.OptionActions[0].Placement,
                Is.EqualTo(GameSceneCombatHudActionPlacement.Center));

            GameSceneCombatHudViewModel oneSelectedHud =
                GameSceneCombatHudPresenter.Create(
                    first.Core,
                    isStageBattle: false,
                    isShopOpen: false,
                    inputLocked: false,
                    satanSelectedNumberCount: 1);
            GameSceneCombatHudViewModel twoSelectedHud =
                GameSceneCombatHudPresenter.Create(
                    first.Core,
                    isStageBattle: false,
                    isShopOpen: false,
                    inputLocked: false,
                    satanSelectedNumberCount: 2);
            Assert.That(oneSelectedHud.SelectionPrompt?.CurrentCount, Is.EqualTo(1));
            Assert.That(oneSelectedHud.OptionActions[0].IsInteractable,
                Is.False);
            Assert.That(twoSelectedHud.SelectionPrompt?.CurrentCount, Is.EqualTo(2));
            Assert.That(twoSelectedHud.OptionActions[0].IsInteractable,
                Is.True);
            Assert.That(first.SatanNumberCandidates, Has.Count.EqualTo(10));
            Assert.That(first.SatanNumberCandidates.All(card =>
                card.DirectSelectionCommand.HasValue), Is.True);
            Assert.That(first.SatanNumberCandidates.All(card => !card.IsUsed),
                Is.True);
            Assert.That(first.SatanNumberCandidates.All(card =>
                !card.ShowHoverBadgeWhenUnavailable), Is.True);

            PendingDemonContractInteraction declaration =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption three = declaration.Options.Single(option =>
                option.NumericValue == 3);
            Assert.That(battle.TryResolvePlayerDemonContract(
                declaration.InteractionId,
                three.OptionId), Is.True);

            GameSceneViewModel second = GameScenePresenter.Create(battle);
            GameSceneCardViewModel branded = second.SatanNumberCandidates.Single(
                card => card.IsSatanBranded);
            Assert.That(branded.Rank, Is.EqualTo(3));
            Assert.That(branded.IsUsed, Is.False);
            Assert.That(branded.DirectSelectionCommand.HasValue, Is.False);
            Assert.That(second.SatanNumberCandidates.Count(card =>
                card.DirectSelectionCommand.HasValue), Is.EqualTo(9));

            PendingDemonContractInteraction secondDeclaration =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption four = secondDeclaration.Options.Single(option =>
                option.NumericValue == 4);
            Assert.That(battle.TryResolvePlayerDemonContract(
                secondDeclaration.InteractionId,
                four.OptionId), Is.True);
            Assert.That(
                GameScenePresenter.Create(battle)
                    .PlayerDemonCards.Single().IsUpsideDown,
                Is.True);
        }

        [Test]
        public void DCUI09_U01_SatanDoomCounterRendersBloodRedOnCardFace()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonCardPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                DemonCardView view = instance.GetComponent<DemonCardView>();
                view.Bind(new GameSceneDemonCardViewModel(
                    cardId: 1,
                    definitionKey: DemonContractCatalog.SatanKey,
                    isFaceUp: true,
                    canUse: true,
                    displayName: "사탄",
                    satanDoomCount: 4));

                Assert.That(view.IsSatanDoomCountVisible, Is.True);
                Assert.That(view.SatanDoomCountLabel, Is.EqualTo("4"));
                Assert.That(
                    view.SatanDoomCountTextColor.r,
                    Is.GreaterThan(view.SatanDoomCountTextColor.g * 10f));
                Assert.That(view.SatanDoomCountOutlineWidth, Is.GreaterThan(0f));
                Assert.That(view.SatanDoomCountFontSize, Is.EqualTo(11f));
                Assert.That(view.SatanDoomCountLocalScale.x, Is.EqualTo(0.4f));
                Assert.That(
                    view.SatanDoomCountRectSize.x *
                    view.SatanDoomCountLocalScale.x,
                    Is.LessThanOrEqualTo(0.65f));
                Assert.That(
                    view.SatanDoomCountLocalPosition.y +
                    view.SatanDoomCountRectSize.y *
                    view.SatanDoomCountLocalScale.y * 0.5f,
                    Is.LessThanOrEqualTo(0.35f));
                Assert.That(
                    view.SatanDoomCountLocalPosition.y,
                    Is.GreaterThan(0f));
                Assert.That(
                    view.SatanDoomCountLocalPosition.y,
                    Is.LessThan(0.4f));
                Assert.That(
                    view.SatanDoomCountLocalPosition.z,
                    Is.GreaterThan(0f));

                view.Bind(new GameSceneDemonCardViewModel(
                    cardId: 1,
                    definitionKey: DemonContractCatalog.SatanKey,
                    isFaceUp: true,
                    canUse: true,
                    displayName: "사탄",
                    isUpsideDown: true,
                    satanDoomCount: 0));
                view.AlignSatanDoomCount();

                Assert.That(view.SatanDoomCountLabel, Is.EqualTo("0"));
                Assert.That(
                    view.SatanDoomCountLocalPosition.y,
                    Is.GreaterThan(0f));
                float worldFacingAngle = instance.transform.localEulerAngles.z +
                    view.SatanDoomCountLocalEulerAngles.z;
                Assert.That(
                    Mathf.Abs(Mathf.DeltaAngle(worldFacingAngle, -4f)),
                    Is.EqualTo(180f).Within(0.1f));
                Assert.That(
                    Mathf.DeltaAngle(
                        view.SatanDoomCountLocalEulerAngles.z,
                        -4f),
                    Is.EqualTo(0f).Within(0.1f));

                view.Bind(new GameSceneDemonCardViewModel(
                    cardId: 2,
                    definitionKey: DemonContractCatalog.BelphegorKey,
                    isFaceUp: true,
                    canUse: false,
                    displayName: "벨페고르"));
                Assert.That(view.IsSatanDoomCountVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void CUM12_U01_CrystalOrbProjectsTwoWorldCandidatesAndSkipOnlyHud()
        {
            CoreLoopBattle battle = CreateStartedBattle(2, 5, 7, 8, 9);
            BlackjackCard crystalOrb = battle.Player.Hand.Cards.Single(card =>
                card.Definition.Effect == CardEffectKind.CrystalOrb);
            Assert.That(battle.TryBeginPlayerCardUse(crystalOrb.Id), Is.True);

            PendingCardEffect pending = battle.PendingPlayerCardEffect;
            GameSceneViewModel scene = GameScenePresenter.Create(battle);
            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);

            Assert.That(scene.UsesDiegeticCardEffectSelection, Is.True);
            Assert.That(scene.CrystalOrbCandidates, Has.Count.EqualTo(2));
            Assert.That(
                scene.CrystalOrbCandidates.Select(card => card.CardId),
                Is.EqualTo(pending.TemporaryCards.Select(card => card.Id)));
            foreach (GameSceneCardViewModel candidate in scene.CrystalOrbCandidates)
            {
                CardEffectChoiceOption option = pending.Options.Single(choice =>
                    choice.CardId == candidate.CardId);
                Assert.That(candidate.DirectSelectionCommand.HasValue, Is.True);
                Assert.That(candidate.DirectSelectionCommand.Value.Kind, Is.EqualTo(
                    GameSceneCombatHudCommandKind.ResolveCardEffectChoice));
                Assert.That(candidate.DirectSelectionCommand.Value.OptionId,
                    Is.EqualTo(option.Id));
            }

            Assert.That(hud.Mode, Is.EqualTo(
                GameSceneCombatHudMode.DiegeticSelection));
            Assert.That(hud.OptionActions, Has.Count.EqualTo(1));
            Assert.That(hud.OptionActions.Single().Label,
                Is.EqualTo("선택하지 않기"));
            Assert.That(hud.OptionActions.Single().Command.OptionId, Is.EqualTo(0));
            Assert.That(
                hud.OptionActions.Single().Placement,
                Is.EqualTo(GameSceneCombatHudActionPlacement.BottomRight));
        }

        [Test]
        public void GSH01_U14_BottomRightChoiceAvoidsFullscreenOptionChrome()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                HudPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                GameHudView hud = instance.GetComponent<GameHudView>();
                CoreLoopViewModel core = CoreLoopPresenter.Create(
                    CreateStartedBattle(2, 5, 7, 8, 9));
                var action = new GameSceneCombatHudActionViewModel(
                    new GameSceneCombatHudCommand(
                        GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                        optionId: 0),
                    "선택하지 않기",
                    true,
                    placement: GameSceneCombatHudActionPlacement.BottomRight);
                var model = new GameSceneCombatHudViewModel(
                    GameSceneCombatHudMode.DiegeticSelection,
                    new CombatPromptRequest(CombatPromptId.ChangeCard),
                    string.Empty,
                    null,
                    new[] { action },
                    null,
                    string.Empty);

                hud.Render(core, model);

                Transform optionPanel = instance
                    .GetComponentsInChildren<Transform>(true)
                    .Single(child => child.name == "OptionPanel");
                Transform optionScroll = instance
                    .GetComponentsInChildren<Transform>(true)
                    .Single(child => child.name == "OptionScroll");
                RectTransform slot = instance
                    .GetComponentsInChildren<RectTransform>(true)
                    .Single(child => child.name == "OptionSlot_001");
                Assert.That(optionPanel.GetComponent<Graphic>().enabled, Is.False);
                Assert.That(optionScroll.gameObject.activeSelf, Is.False);
                Assert.That(slot.parent, Is.EqualTo(optionPanel));
                Assert.That(slot.anchorMin, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(slot.anchorMax, Is.EqualTo(new Vector2(1f, 0f)));
                Assert.That(slot.anchoredPosition, Is.EqualTo(new Vector2(-48f, 48f)));
                Assert.That(slot.sizeDelta, Is.EqualTo(new Vector2(380f, 64f)));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [TestCase(
            DemonContractInteractionKind.BelphegorTopCard,
            BelphegorDemonContractHandler.MoveTopCardToBottomOptionId,
            true)]
        [TestCase(
            DemonContractInteractionKind.BelphegorTopCard,
            BelphegorDemonContractHandler.KeepTopCardOptionId,
            true)]
        [TestCase(
            DemonContractInteractionKind.AsmodeusForceOpponentHit,
            AsmodeusDemonContractHandler.SkipForcedHitOptionId,
            true)]
        [TestCase(
            DemonContractInteractionKind.AsmodeusForceOpponentHit,
            AsmodeusDemonContractHandler.ForceHitOptionId,
            true)]
        [TestCase(
            DemonContractInteractionKind.MammonReroll,
            MammonDemonContractHandler.KeepDieOptionId,
            true)]
        [TestCase(
            DemonContractInteractionKind.MammonReroll,
            MammonDemonContractHandler.RerollDieOptionId,
            false)]
        [TestCase(
            DemonContractInteractionKind.MammonApplyDie,
            MammonDemonContractHandler.DoNotApplyDieOptionId,
            true)]
        [TestCase(
            DemonContractInteractionKind.MammonApplyDie,
            MammonDemonContractHandler.ApplyDieOptionId,
            true)]
        public void GSH01_U15_OnlySpecifiedContractChoicesUseBottomRight(
            DemonContractInteractionKind interactionKind,
            int optionId,
            bool expected)
        {
            Assert.That(
                GameSceneCombatHudPresenter.IsBottomRightContractAction(
                    interactionKind,
                    optionId),
                Is.EqualTo(expected));
        }

        [TestCase(AutomaticCardChoiceKind.PoisonDecision, 0, true)]
        [TestCase(AutomaticCardChoiceKind.PoisonDecision, 1, true)]
        [TestCase(AutomaticCardChoiceKind.ResurrectionHerbDecision, 0, true)]
        [TestCase(AutomaticCardChoiceKind.ResurrectionHerbDecision, 1, true)]
        [TestCase(AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision, 0, true)]
        [TestCase(AutomaticCardChoiceKind.ResurrectionHerbOpponentDecision, 1, true)]
        [TestCase(AutomaticCardChoiceKind.FlamethrowerOwnerDiscard,
            FlamethrowerEffectHandler.SkipOptionId, true)]
        [TestCase(AutomaticCardChoiceKind.FlamethrowerOwnerDiscard, 100, false)]
        [TestCase(AutomaticCardChoiceKind.FlamethrowerOpponentDiscard,
            FlamethrowerEffectHandler.SkipOptionId, true)]
        [TestCase(AutomaticCardChoiceKind.PocketWatchManualCard,
            PocketWatchEffectHandler.SkipManualCardOptionId, true)]
        [TestCase(AutomaticCardChoiceKind.PocketWatchManualCard, 100, false)]
        [TestCase(AutomaticCardChoiceKind.PocketWatchSourceDisposition, 0, true)]
        [TestCase(AutomaticCardChoiceKind.PocketWatchSourceDisposition, 1, true)]
        [TestCase(AutomaticCardChoiceKind.LieDetectorNumber, 1, false)]
        public void GSH01_U20_GeneralAutomaticChoicesUseBottomRight(
            AutomaticCardChoiceKind choiceKind,
            int optionId,
            bool expected)
        {
            Assert.That(
                GameSceneCombatHudPresenter.IsBottomRightAutomaticCardAction(
                    choiceKind,
                    optionId),
                Is.EqualTo(expected));
        }

        [Test]
        public void GSH01_U16_EmptyOptionListDoesNotShowTemporaryPanel()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                HudPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                GameHudView hud = instance.GetComponent<GameHudView>();
                CoreLoopViewModel core = CoreLoopPresenter.Create(
                    CreateStartedBattle(2, 5, 7, 8, 9));
                var model = new GameSceneCombatHudViewModel(
                    GameSceneCombatHudMode.Options,
                    selectionPrompt: null,
                    string.Empty,
                    null,
                    System.Array.Empty<GameSceneCombatHudActionViewModel>(),
                    null,
                    string.Empty);

                hud.Render(core, model);

                Transform optionPanel = instance
                    .GetComponentsInChildren<Transform>(true)
                    .Single(child => child.name == "OptionPanel");
                Assert.That(optionPanel.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSH01_U17_BelphegorShowsOnePreviewCardAndTwoBottomRightActions()
        {
            CoreLoopBattle battle = CreateStartedContractBattle(
                DemonContractKind.Belphegor,
                DemonContractKind.Mammon);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction offer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption belphegor = offer.Options.Single(option =>
                option.ContractDefinitionKey == DemonContractCatalog.BelphegorKey);
            Assert.That(battle.TryResolvePlayerDemonContract(
                offer.InteractionId,
                belphegor.OptionId), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            PlayerDemonContractPreview preview = battle.PlayerDemonContractPreview;
            GameSceneViewModel scene = GameScenePresenter.Create(battle);
            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);

            Assert.That(preview, Is.Not.Null);
            Assert.That(scene.CrystalOrbCandidates, Has.Count.EqualTo(1));
            GameSceneCardViewModel card = scene.CrystalOrbCandidates.Single();
            Assert.That(card.CardId, Is.EqualTo(preview.CardId));
            Assert.That(card.Rank, Is.EqualTo(preview.Rank));
            Assert.That(card.Suit, Is.EqualTo(preview.Suit));
            Assert.That(card.DirectSelectionCommand.HasValue, Is.False);
            Assert.That(hud.OptionActions, Has.Count.EqualTo(2));
            Assert.That(hud.OptionActions.Select(action => action.Label),
                Is.EqualTo(new[] { "히트하기", "히트하지 않기" }));
            Assert.That(hud.OptionActions.All(action =>
                action.Placement ==
                    GameSceneCombatHudActionPlacement.BottomRight), Is.True);
        }

        [TestCase("HIT", "HIT")]
        [TestCase("STAND", "STAND")]
        [TestCase("CHANGE", "CHANGE")]
        [TestCase("CONTRACT", "CONTRACT")]
        [TestCase("DISCARD", "")]
        [TestCase("MISS", "")]
        [TestCase("WIN", "")]
        public void GSH01_U18_EnemyActionLabelAllowsOnlyFourActionTokens(
            string label,
            string expected)
        {
            Assert.That(
                GameScenePresenter.FilterEnemyActionLabel(label),
                Is.EqualTo(expected));
        }

        [Test]
        public void GSH01_U19_AsmodeusShowsTwoBottomRightActionsBeforeOpponentStand()
        {
            CoreLoopBattle battle = CreateStartedAsmodeusContractBattle();
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction offer =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption asmodeus = offer.Options.Single(option =>
                option.ContractDefinitionKey == DemonContractCatalog.AsmodeusKey);

            Assert.That(battle.TryResolvePlayerDemonContract(
                offer.InteractionId,
                asmodeus.OptionId), Is.True);

            Assert.That(battle.Enemy.IsStanding, Is.False);
            Assert.That(
                battle.PendingPlayerDemonContractInteraction.Kind,
                Is.EqualTo(
                    DemonContractInteractionKind.AsmodeusForceOpponentHit));
            GameSceneViewModel scene = GameScenePresenter.Create(battle);
            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);
            Assert.That(hud.OptionActions, Has.Count.EqualTo(2));
            Assert.That(hud.OptionActions.Select(action => action.Label),
                Is.EqualTo(new[] { "능력 사용하지 않기", "능력 사용하기" }));
            Assert.That(hud.OptionActions.All(action =>
                action.Placement ==
                    GameSceneCombatHudActionPlacement.BottomRight), Is.True);
        }

        [Test]
        public void CUM12_U02_CrystalOrbSelectionViewRendersTwoClickableWorldCards()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                CardPrefabPath);
            Assert.That(cardPrefab, Is.Not.Null);
            var root = new GameObject("CrystalOrbSelectionTest");
            try
            {
                CrystalOrbSelectionView view =
                    root.AddComponent<CrystalOrbSelectionView>();
                view.Initialize(cardPrefab.GetComponent<CardView>());
                var candidates = new[]
                {
                    new GameSceneCardViewModel(
                        31, 7, true, true, false, "리볼버",
                        directSelectionCommand: new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                            1)),
                    new GameSceneCardViewModel(
                        32, 8, true, true, false, "나이프",
                        directSelectionCommand: new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                            2))
                };

                view.Render(candidates, null);

                Assert.That(view.HasCandidatePrefab, Is.True);
                Assert.That(view.Capacity, Is.EqualTo(2));
                Assert.That(view.VisibleCandidateCount, Is.EqualTo(2));
                Assert.That(view.IsOpen, Is.True);
                CardView[] cards = root.GetComponentsInChildren<CardView>(true);
                Assert.That(cards, Has.Length.EqualTo(2));
                Assert.That(cards.Count(card => card.gameObject.activeInHierarchy),
                    Is.EqualTo(2));
                Assert.That(cards.All(card =>
                    card.DirectSelectionCommand.HasValue), Is.True);

                CardView firstCard = cards.Single(card => card.CardId == 31);
                Assert.That(view.Contains(firstCard), Is.True);
                Assert.That(view.GetCandidate(firstCard), Is.SameAs(candidates[0]));
                view.SetHovered(firstCard);
                Assert.That(view.HoveredCandidateIndex, Is.EqualTo(0));

                GameObject managerPrefab =
                    AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
                DemonContractSelectionView contractView =
                    managerPrefab.GetComponent<DemonContractSelectionView>();
                Assert.That(view.FanLayout, Is.Not.Null);
                Assert.That(contractView.FanLayout, Is.Not.Null);

                view.Hide();
                Assert.That(view.IsOpen, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CUM11_U01_AutomaticCardTargetUsesTableCardAndKeepsSkipAction()
        {
            CardDefinition flamethrower = CardDefinitionCatalog.GetByKey(
                CardDefinitionCatalog.FlamethrowerKey);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 2, 3, flamethrower, 4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            GameSceneViewModel scene = GameScenePresenter.Create(battle);
            PendingAutomaticCardInteraction interaction =
                battle.PendingPlayerAutomaticInteraction;
            GameSceneCardViewModel target = scene.PlayerCards.Single(card =>
                card.DirectSelectionCommand.HasValue);
            GameSceneCombatHudCommand command = target.DirectSelectionCommand.Value;

            Assert.That(scene.UsesDiegeticCardEffectSelection, Is.True);
            Assert.That(command.Kind, Is.EqualTo(
                GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice));
            Assert.That(command.InteractionId, Is.EqualTo(interaction.InteractionId));
            Assert.That(scene.PlayerCards.Single(card =>
                card.CardId == interaction.SourceCardId).IsEffectSource, Is.True);

            GameSceneCombatHudViewModel hud = GameSceneCombatHudPresenter.Create(
                scene.Core,
                isStageBattle: false,
                isShopOpen: false,
                inputLocked: false,
                scene.UsesDiegeticCardEffectSelection);
            Assert.That(hud.Mode, Is.EqualTo(
                GameSceneCombatHudMode.DiegeticSelection));
            Assert.That(hud.OptionActions, Has.Count.EqualTo(1));
            Assert.That(hud.OptionActions.Single().Command.OptionId,
                Is.EqualTo(FlamethrowerEffectHandler.SkipOptionId));
            Assert.That(
                hud.OptionActions.Single().Placement,
                Is.EqualTo(GameSceneCombatHudActionPlacement.BottomRight));
        }

        [Test]
        public void CUM11_U02_BeelzebubProjectsTwoStepTableTargets()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .GetByKey(DemonContractCatalog.BeelzebubKey);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 10, 2, 10, 10, 2, 2)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100, 8, 7, 2, 2, 2, 2)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy(),
                playerDemonDeck: new DemonContractDeck(
                    new[] { new DemonContractCard(500, definition) },
                    seed: 73));
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction offer =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                offer.InteractionId,
                offer.Options[0].OptionId), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            GameSceneViewModel ownerScene = GameScenePresenter.Create(battle);
            GameSceneCardViewModel ownerTarget = ownerScene.PlayerCards.First(card =>
                card.DirectSelectionCommand.HasValue);
            GameSceneCombatHudCommand ownerCommand =
                ownerTarget.DirectSelectionCommand.Value;
            GameSceneCombatHudViewModel ownerHud =
                GameSceneCombatHudPresenter.Create(
                    ownerScene.Core, false, false, false,
                    ownerScene.UsesDiegeticCardEffectSelection);
            Assert.That(ownerScene.FocusesEnemyCardsForSelection, Is.False);
            Assert.That(ownerCommand.Kind, Is.EqualTo(
                GameSceneCombatHudCommandKind.ResolveDemonContractChoice));
            Assert.That(ownerHud.SelectionPrompt?.CurrentCount, Is.EqualTo(1));
            Assert.That(ownerHud.SelectionPrompt?.RequiredCount, Is.EqualTo(2));
            Assert.That(battle.TryResolvePlayerDemonContract(
                ownerCommand.InteractionId,
                ownerCommand.OptionId), Is.True);

            GameSceneViewModel opponentScene = GameScenePresenter.Create(battle);
            GameSceneCardViewModel opponentTarget = opponentScene.EnemyCards.First(card =>
                card.DirectSelectionCommand.HasValue);
            GameSceneCombatHudCommand opponentCommand =
                opponentTarget.DirectSelectionCommand.Value;
            GameSceneCombatHudViewModel opponentHud =
                GameSceneCombatHudPresenter.Create(
                    opponentScene.Core, false, false, false,
                    opponentScene.UsesDiegeticCardEffectSelection);
            Assert.That(opponentScene.FocusesEnemyCardsForSelection, Is.True);
            Assert.That(opponentCommand.Kind, Is.EqualTo(
                GameSceneCombatHudCommandKind.ResolveDemonContractChoice));
            Assert.That(opponentHud.SelectionPrompt?.CurrentCount, Is.EqualTo(2));
            Assert.That(opponentHud.SelectionPrompt?.RequiredCount, Is.EqualTo(2));
        }

        [Test]
        public void CUM11_U03_CardViewRetainsDirectSelectionCommand()
        {
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Card/Card.prefab");
            GameObject instance = Object.Instantiate(cardPrefab);
            try
            {
                CardView view = instance.GetComponent<CardView>();
                var command = new GameSceneCombatHudCommand(
                    GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice,
                    optionId: 7,
                    interactionId: 11);
                view.Bind(new GameSceneCardViewModel(
                    cardId: 21,
                    rank: 4,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    displayName: "Target",
                    directSelectionCommand: command));

                Assert.That(view.DirectSelectionCommand.HasValue, Is.True);
                Assert.That(view.DirectSelectionCommand.Value.Kind,
                    Is.EqualTo(command.Kind));
                Assert.That(view.DirectSelectionCommand.Value.OptionId,
                    Is.EqualTo(7));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("CP04")]
        public void CP04_U03_CardHandHidesAndRestoresDirectSelectionCommand()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                CardHand hand = instance.GetComponentsInChildren<CardHand>(true)
                    .Single(candidate => candidate.name == "PlayerHand");
                hand.gameObject.SetActive(true);
                var command = new GameSceneCombatHudCommand(
                    GameSceneCombatHudCommandKind.ResolveAutomaticCardChoice,
                    optionId: 7,
                    interactionId: 11);
                var card = new GameSceneCardViewModel(
                    cardId: 21,
                    rank: 4,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    displayName: "Target",
                    directSelectionCommand: command);
                var cards = new[] { card };
                var demonCards = new GameSceneDemonCardViewModel[0];

                hand.Render(
                    cards,
                    demonCards,
                    showTransientEffectSources: true,
                    showDirectSelectionCommands: false);
                Assert.That(hand.TryGetCard(card.CardId, out CardView view),
                    Is.True);
                Assert.That(view.DirectSelectionCommand, Is.Null);
                FieldInfo highlightedField = typeof(CardView).GetField(
                    "_isEffectHighlighted",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(highlightedField, Is.Not.Null);
                Assert.That(highlightedField.GetValue(view), Is.False);

                hand.Render(
                    cards,
                    demonCards,
                    showTransientEffectSources: true,
                    showDirectSelectionCommands: true);
                Assert.That(view.DirectSelectionCommand.HasValue, Is.True);
                Assert.That(view.DirectSelectionCommand.Value.Kind,
                    Is.EqualTo(command.Kind));
                Assert.That(view.DirectSelectionCommand.Value.OptionId,
                    Is.EqualTo(command.OptionId));
                Assert.That(highlightedField.GetValue(view), Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSH01_U06_HudPrefabOmitsFixedActionsAndKeepsSelectionControls()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameHudView hud = prefab.GetComponent<GameHudView>();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.CombatOptionSlotCount, Is.EqualTo(100));
            Assert.That(hud.HasCombatTooltipReference, Is.True);
            Assert.That(hud.HasCombatCandidateContentReference, Is.True);
            Assert.That(hud.HasCombatContractDetailReference, Is.True);
            Assert.That(prefab.GetComponentsInChildren<GameHudChoiceButton>(true).Length,
                Is.EqualTo(100));
            Assert.That(prefab.GetComponentInChildren<ScrollRect>(true), Is.Not.Null);
            Assert.That(prefab.transform.Find("CombatControls/ActionRow"), Is.Null);
            Assert.That(
                prefab.GetComponentsInChildren<GameHudActionButton>(true),
                Is.Empty);

            GameObject managerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
            Assert.That(managerPrefab, Is.Not.Null);
            DemonContractSelectionView selection =
                managerPrefab.GetComponent<DemonContractSelectionView>();
            Assert.That(selection, Is.Not.Null);
            Assert.That(selection.HasCandidatePrefab, Is.True);
            Assert.That(selection.Capacity, Is.EqualTo(2));

        }

        [Test]
        [Category("CUM13")]
        public void CUM13_U03_HudAuthorsRevolverSelectorPrefabAndBrushButtons()
        {
            GameObject selectorPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RevolverNumberSelectorPrefabPath);
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(selectorPrefab, Is.Not.Null);
            Assert.That(hudPrefab, Is.Not.Null);

            GameHudView hud = hudPrefab.GetComponent<GameHudView>();
            RevolverNumberSelectorView selector = hudPrefab
                .GetComponentsInChildren<RevolverNumberSelectorView>(true)
                .Single();
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.HasRevolverNumberSelectorReference, Is.True);
            Assert.That(selector.HasRequiredReferences, Is.True);
            Assert.That(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    selector.gameObject),
                Is.EqualTo(RevolverNumberSelectorPrefabPath));

            RectTransform circle = selector.transform
                .Find("NumberCircle")
                .GetComponent<RectTransform>();
            RectTransform previous = selector.transform
                .Find("PreviousButton")
                .GetComponent<RectTransform>();
            RectTransform next = selector.transform
                .Find("NextButton")
                .GetComponent<RectTransform>();
            RectTransform confirm = selector.transform
                .Find("ConfirmButton")
                .GetComponent<RectTransform>();
            Assert.That(circle.sizeDelta, Is.EqualTo(new Vector2(400f, 400f)));
            Assert.That(previous.sizeDelta, Is.EqualTo(new Vector2(170f, 170f)));
            Assert.That(next.sizeDelta, Is.EqualTo(new Vector2(170f, 170f)));
            Assert.That(confirm.sizeDelta, Is.EqualTo(new Vector2(234f, 66f)));

            Assert.That(circle.GetComponent<Image>().sprite.name,
                Is.EqualTo("brush_select_circle"));
            Assert.That(previous.GetComponent<Image>().sprite.name,
                Is.EqualTo("brush_select_left"));
            Assert.That(next.GetComponent<Image>().sprite.name,
                Is.EqualTo("brush_select_right"));
            Assert.That(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    confirm.gameObject),
                Is.EqualTo(DefaultButtonPrefabPath));

            UIButtonScaleFeedback[] feedbacks = selector
                .GetComponentsInChildren<UIButtonScaleFeedback>(true);
            Assert.That(feedbacks, Has.Length.EqualTo(3));
            foreach (UIButtonScaleFeedback feedback in feedbacks)
            {
                SerializedObject data = new SerializedObject(feedback);
                Assert.That(data.FindProperty("hoverScale").floatValue,
                    Is.EqualTo(1.08f));
                Assert.That(data.FindProperty("pressedScale").floatValue,
                    Is.EqualTo(0.92f));
                Assert.That(data.FindProperty("animationDuration").floatValue,
                    Is.EqualTo(0.12f));
            }
        }

        [Test]
        [Category("CUM13")]
        public void CUM13_U04_RevolverSelectorCyclesAndConfirmsAuthoredButtons()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                RevolverNumberSelectorPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                RevolverNumberSelectorView selector =
                    instance.GetComponent<RevolverNumberSelectorView>();
                typeof(RevolverNumberSelectorView)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(selector, null);
                var actions = Enumerable.Range(1, 10)
                    .Select(number => new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(
                            GameSceneCombatHudCommandKind.ResolveCardEffectChoice,
                            optionId: number),
                        number.ToString(),
                        true))
                    .ToArray();
                GameSceneCombatHudCommand confirmed = default;
                bool commandReceived = false;
                selector.CommandRequested += command =>
                {
                    commandReceived = true;
                    confirmed = command;
                };

                selector.Render(actions);

                Assert.That(selector.IsOpen, Is.True);
                Assert.That(instance.activeSelf, Is.True);
                Assert.That(selector.SelectedNumber, Is.EqualTo(1));
                Assert.That(instance.transform.Find("Prompt"), Is.Null);
                Assert.That(ReadText(instance.transform.Find("NumberCircle/Number")),
                    Is.EqualTo("1"));

                selector.PreviousButton.onClick.Invoke();
                Assert.That(selector.SelectedNumber, Is.EqualTo(10));
                selector.NextButton.onClick.Invoke();
                selector.NextButton.onClick.Invoke();
                Assert.That(selector.SelectedNumber, Is.EqualTo(2));

                selector.ConfirmButton.onClick.Invoke();
                Assert.That(commandReceived, Is.True);
                Assert.That(confirmed.Kind, Is.EqualTo(
                    GameSceneCombatHudCommandKind.ResolveCardEffectChoice));
                Assert.That(confirmed.OptionId, Is.EqualTo(2));

                selector.Hide();
                Assert.That(selector.IsOpen, Is.False);
                Assert.That(instance.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        private static string ReadText(Transform target)
        {
            Component textComponent = target
                .GetComponents<Component>()
                .Single(component => component.GetType().Name == "TextMeshProUGUI");
            PropertyInfo textProperty = textComponent.GetType().GetProperty("text");
            Assert.That(textProperty, Is.Not.Null);
            return (string)textProperty.GetValue(textComponent);
        }

        [Test]
        public void GSH01_U10_TablePrefabAuthorsThreeWorldCommands()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            Assert.That(prefab, Is.Not.Null);
            TableCombatCommandGroup group =
                prefab.GetComponentInChildren<TableCombatCommandGroup>(true);
            Assert.That(group, Is.Not.Null);
            Assert.That(group.CommandViewCount, Is.EqualTo(3));
            Assert.That(group.HasRequiredReferences, Is.True);

            TableCombatCommandView[] commands =
                group.GetComponentsInChildren<TableCombatCommandView>(true);
            Assert.That(commands, Has.Length.EqualTo(3));
            Assert.That(commands.Select(command => command.Kind), Is.EquivalentTo(new[]
            {
                GameSceneCombatHudCommandKind.Hit,
                GameSceneCombatHudCommandKind.Stand,
                GameSceneCombatHudCommandKind.BeginChange
            }));
            Assert.That(commands.All(command => command.HasRequiredReferences), Is.True);
            Assert.That(commands.All(command =>
                command.GetComponent<Collider>() != null), Is.True);
            Sprite commandFrame = AssetDatabase.LoadAssetAtPath<Sprite>(
                TableCommandFrameSpritePath);
            Assert.That(commandFrame, Is.Not.Null);
            Assert.That(commands.All(command =>
                command.GetComponent<SpriteRenderer>().sprite == commandFrame),
                Is.True);
            Assert.That(commands.All(command =>
                command.GetComponent<SpriteRenderer>().drawMode ==
                    SpriteDrawMode.Simple), Is.True);
            Assert.That(commands.All(command =>
                command.GetComponent<SpriteRenderer>().size ==
                    new Vector2(3.5f, 3.5f)), Is.True);
            Assert.That(commands.All(command =>
                command.transform.localScale ==
                    new Vector3(0.2f, 0.2f, 0.82f)), Is.True);
            Assert.That(commands.All(command =>
                ((BoxCollider)command.GetComponent<Collider>()).size ==
                    new Vector3(3.5f, 3.5f, 0.1f)), Is.True);
        }

        [Test]
        public void GSH01_U11_DisabledWorldCommandIsGrayAndRejectsClicks()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(TablePrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                TableCombatCommandGroup group =
                    instance.GetComponentInChildren<TableCombatCommandGroup>(true);
                var actions = new List<GameSceneCombatHudActionViewModel>
                {
                    new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(GameSceneCombatHudCommandKind.Hit),
                        "HIT",
                        false),
                    new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(GameSceneCombatHudCommandKind.Stand),
                        "STAND",
                        true),
                    new GameSceneCombatHudActionViewModel(
                        new GameSceneCombatHudCommand(GameSceneCombatHudCommandKind.BeginChange),
                        "CHANGE -2",
                        true)
                };
                var model = new GameSceneCombatHudViewModel(
                    GameSceneCombatHudMode.Actions,
                    selectionPrompt: null,
                    string.Empty,
                    actions,
                    null,
                    null,
                    string.Empty);

                group.Render(model);
                TableCombatCommandView hit = group
                    .GetComponentsInChildren<TableCombatCommandView>(true)
                    .Single(command => command.Kind == GameSceneCombatHudCommandKind.Hit);
                Assert.That(hit.GetComponent<Collider>().enabled, Is.True);
                Assert.That(hit.GetComponent<SpriteRenderer>().color,
                    Is.EqualTo(new Color(0.35f, 0.35f, 0.35f, 0.35f)));
                Assert.That(hit.TryGetCommand(out _), Is.False);
                Assert.That(
                    hit.GetComponent<HoverDescriptionTarget>(),
                    Is.Not.Null);

                TableCombatCommandView stand = group
                    .GetComponentsInChildren<TableCombatCommandView>(true)
                    .Single(command => command.Kind == GameSceneCombatHudCommandKind.Stand);
                Assert.That(stand.GetComponent<Collider>().enabled, Is.True);
                Assert.That(stand.TryGetCommand(out GameSceneCombatHudCommand command), Is.True);
                Assert.That(command.Kind, Is.EqualTo(GameSceneCombatHudCommandKind.Stand));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GF03_U01_EnemyStatusVisibilityCanBeHiddenBeforeCombat()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                GameHudView hud = instance.GetComponent<GameHudView>();
                Transform enemyStatus = instance
                    .GetComponentsInChildren<Transform>(true)
                    .Single(child => child.name == "EnemySoul");

                hud.SetEnemyStatusVisible(false);
                Assert.That(enemyStatus.gameObject.activeSelf, Is.False);

                hud.SetEnemyStatusVisible(true);
                Assert.That(enemyStatus.gameObject.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSH01_U07_ContractHoverUsesDedicatedPrefabOnly()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Assert.That(
                prefab.transform.Find("CombatControls/ContractDetailPanel"),
                Is.Null);
            Assert.That(
                prefab.GetComponentsInChildren<
                    GameHudContractDetailView>(true),
                Has.Length.EqualTo(1));

            DemonCardHoverDetailView authoredHoverDetail =
                prefab.GetComponentInChildren<
                    DemonCardHoverDetailView>(true);
            GameObject hoverDetailPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    DemonCardHoverDetailPrefabPath);
            Assert.That(authoredHoverDetail, Is.Not.Null);
            Assert.That(authoredHoverDetail.HasRequiredReferences, Is.True);
            Assert.That(
                PrefabUtility.GetCorrespondingObjectFromSource(
                    authoredHoverDetail.gameObject),
                Is.SameAs(hoverDetailPrefab));

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                CoreLoopBattle battle = CreateStartedContractBattle(
                    DemonContractKind.Mammon,
                    DemonContractKind.Belphegor);
                Assert.That(
                    battle.TryBeginPlayerDemonContract(),
                    Is.True);
                GameSceneCombatHudContractCandidateViewModel candidate =
                    GameSceneCombatHudPresenter.Create(
                        CoreLoopPresenter.Create(battle),
                        false,
                        false,
                        false).ContractCandidates[0];
                GameHudView hud = instance.GetComponent<GameHudView>();
                DemonCardHoverDetailView hoverDetail =
                    instance.GetComponentInChildren<
                        DemonCardHoverDetailView>(true);

                hud.Render((CoreLoopViewModel)null);
                hud.ShowDemonContractDetail(candidate);

                Transform detail = hoverDetail.DetailView.transform;
                Transform title = GetSerializedTransform(
                    hoverDetail.DetailView,
                    "titleText");
                Assert.That(hoverDetail.gameObject.activeInHierarchy, Is.True);
                Assert.That(hud.IsDemonContractDetailVisible, Is.True);
                Assert.That(
                    GetRenderedText(title),
                    Is.EqualTo(candidate.Title));
                Assert.That(
                    GetRenderedText(detail.Find("Face/txtEnglishName")),
                    Is.EqualTo(candidate.EnglishName));
                Assert.That(
                    GetRenderedText(detail.Find("Ability/txtAbility")),
                    Is.EqualTo(
                        CurrencyIconMarkup.FormatForTmp(candidate.Ability)));
                Assert.That(
                    GetRenderedText(detail.Find("Cost/txtCost")),
                    Is.EqualTo(
                        CurrencyIconMarkup.FormatForTmp(candidate.Cost)));

                hud.HideDemonContractDetail();
                Assert.That(hoverDetail.gameObject.activeSelf, Is.False);
                Assert.That(hud.IsDemonContractDetailVisible, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSH01_U08_HudUsesSharedCardHoverTooltipPrefab()
        {
            GameObject tooltipPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(CardHoverTooltipPrefabPath);
            Assert.That(tooltipPrefab, Is.Not.Null);
            Assert.That(tooltipPrefab.transform.Find("CardHoverHeader/Title"), Is.Not.Null);
            Assert.That(tooltipPrefab.transform.Find("CardHoverBadge/Text"), Is.Not.Null);

            GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(hudPrefab, Is.Not.Null);
            Transform tooltipInstance = hudPrefab.transform.Find("CardHoverTooltipRoot");
            Assert.That(tooltipInstance, Is.Not.Null);
            Assert.That(
                PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
                    tooltipInstance.gameObject),
                Is.EqualTo(CardHoverTooltipPrefabPath));

            GameHudView hud = hudPrefab.GetComponent<GameHudView>();
            Assert.That(hud, Is.Not.Null);
            SerializedObject serialized = new SerializedObject(hud);
            Assert.That(
                serialized.FindProperty("cardHoverTooltipRoot").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serialized.FindProperty("cardHoverBadge").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serialized.FindProperty("cardHoverBadgeText").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serialized.FindProperty("cardHoverHeaderBadge").objectReferenceValue,
                Is.Not.Null);
            Assert.That(
                serialized.FindProperty("cardHoverHeaderText").objectReferenceValue,
                Is.Not.Null);
        }

        [Test]
        public void GSH01_U09_HoverTooltipUsesExactCardAnchorWithoutOffset()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            GameObject instance = Object.Instantiate(prefab);

            try
            {
                GameHudView hud = instance.GetComponent<GameHudView>();
                Assert.That(hud, Is.Not.Null);
                SerializedObject serialized = new SerializedObject(hud);
                RectTransform tooltipRoot = serialized
                    .FindProperty("cardHoverTooltipRoot")
                    .objectReferenceValue as RectTransform;
                Assert.That(tooltipRoot, Is.Not.Null);
                Assert.That(
                    serialized.FindProperty("cardHoverBadgeScreenOffset"),
                    Is.Null);

                MethodInfo positionMethod = typeof(GameHudView).GetMethod(
                    "PositionCardHoverTooltip",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(positionMethod, Is.Not.Null);
                Vector2 anchorPoint = new Vector2(123f, -45f);
                Vector2 bottomPivot = new Vector2(0.5f, 0f);
                Vector2 topPivot = new Vector2(0.5f, 1f);

                positionMethod.Invoke(
                    hud,
                    new object[] { anchorPoint, bottomPivot });
                Assert.That(
                    tooltipRoot.localPosition,
                    Is.EqualTo(new Vector3(anchorPoint.x, anchorPoint.y, 0f)));
                Assert.That(tooltipRoot.pivot, Is.EqualTo(bottomPivot));

                positionMethod.Invoke(
                    hud,
                    new object[] { anchorPoint, topPivot });
                Assert.That(
                    tooltipRoot.localPosition,
                    Is.EqualTo(new Vector3(anchorPoint.x, anchorPoint.y, 0f)));
                Assert.That(tooltipRoot.pivot, Is.EqualTo(topPivot));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        [Category("GSH02")]
        public void GSH02_U07_OverlayOwnedTooltipSurvivesGenericHoverClear()
        {
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject managerPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(ManagerPrefabPath);
            Assert.That(hudPrefab, Is.Not.Null);
            Assert.That(managerPrefab, Is.Not.Null);

            GameObject hudInstance = Object.Instantiate(hudPrefab);
            GameObject managerInstance = Object.Instantiate(managerPrefab);
            try
            {
                GameHudView hud = hudInstance.GetComponent<GameHudView>();
                GameManager manager = managerInstance.GetComponent<GameManager>();
                Assert.That(hud, Is.Not.Null);
                Assert.That(manager, Is.Not.Null);

                SerializedObject managerData = new SerializedObject(manager);
                managerData.FindProperty("hud").objectReferenceValue = hud;
                managerData.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject hudData = new SerializedObject(hud);
                RectTransform header = hudData
                    .FindProperty("cardHoverHeaderBadge")
                    .objectReferenceValue as RectTransform;
                RectTransform body = hudData
                    .FindProperty("cardHoverBadge")
                    .objectReferenceValue as RectTransform;
                Assert.That(header, Is.Not.Null);
                Assert.That(body, Is.Not.Null);

                MethodInfo showOverlayBadge = typeof(GameManager).GetMethod(
                    "ShowOverlayHoverBadge",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo updateDescriptionTarget = typeof(GameManager).GetMethod(
                    "UpdateHoverDescriptionTarget",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo clearOverlayBadge = typeof(GameManager).GetMethod(
                    "ClearOverlayHoverBadge",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(showOverlayBadge, Is.Not.Null);
                Assert.That(updateDescriptionTarget, Is.Not.Null);
                Assert.That(clearOverlayBadge, Is.Not.Null);

                object owner = new object();
                showOverlayBadge.Invoke(
                    manager,
                    new object[]
                    {
                        owner,
                        new CardHoverBadgeRequest(
                            "7. Revolver",
                            "Test description",
                            new Vector2(960f, 540f),
                            showBelow: false)
                    });
                Assert.That(header.gameObject.activeSelf, Is.True);
                Assert.That(body.gameObject.activeSelf, Is.True);

                updateDescriptionTarget.Invoke(manager, new object[] { null });
                Assert.That(header.gameObject.activeSelf, Is.True);
                Assert.That(body.gameObject.activeSelf, Is.True);

                clearOverlayBadge.Invoke(manager, new[] { new object() });
                Assert.That(header.gameObject.activeSelf, Is.True);
                Assert.That(body.gameObject.activeSelf, Is.True);

                clearOverlayBadge.Invoke(manager, new[] { owner });
                Assert.That(header.gameObject.activeSelf, Is.False);
                Assert.That(body.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(managerInstance);
                Object.DestroyImmediate(hudInstance);
            }
        }

        [Test]
        public void GSH01_U12_ShopDemonUsesDedicatedHoverDetailPrefab()
        {
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject demonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Card/DemonCard.prefab");
            GameObject hoverDetailPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    DemonCardHoverDetailPrefabPath);
            DemonCardHoverDetailView authoredHoverDetail =
                hudPrefab.GetComponentInChildren<
                    DemonCardHoverDetailView>(true);
            Assert.That(authoredHoverDetail, Is.Not.Null);
            Assert.That(
                PrefabUtility.GetCorrespondingObjectFromSource(
                    authoredHoverDetail.gameObject),
                Is.SameAs(hoverDetailPrefab));
            GameObject hudInstance = Object.Instantiate(hudPrefab);
            GameObject demonInstance = Object.Instantiate(demonPrefab);
            try
            {
                GameHudView hud = hudInstance.GetComponent<GameHudView>();
                DemonCardView demon = demonInstance.GetComponent<DemonCardView>();
                DemonCardHoverDetailView hoverDetail =
                    hudInstance.GetComponentInChildren<
                        DemonCardHoverDetailView>(true);
                Assert.That(hoverDetail, Is.Not.Null);
                DemonContractDefinition definition =
                    DemonContractCatalog.Default.GetByKey(
                        DemonContractCatalog.SatanKey);
                demon.Bind(new GameSceneDemonCardViewModel(
                    cardId: 7,
                    definitionKey: definition.Key,
                    isFaceUp: true,
                    canUse: true,
                    displayName: definition.DisplayName,
                    summary: definition.Summary,
                    costSummary: definition.CostSummary));

                hud.Render((CoreLoopViewModel)null);
                hud.ShowDemonContractDetail(demon.BoundCard);

                Transform detail = hoverDetail.DetailView.transform;
                Transform title = GetSerializedTransform(
                    hoverDetail.DetailView,
                    "titleText");
                Assert.That(hoverDetail.gameObject.activeSelf, Is.True);
                Assert.That(hoverDetail.gameObject.activeInHierarchy, Is.True);
                Assert.That(
                    hudInstance.transform.Find(
                        "CombatControls/ContractDetailPanel"),
                    Is.Null);
                Assert.That(hud.IsDemonContractDetailVisible, Is.True);
                Assert.That(
                    GetRenderedText(title),
                    Is.EqualTo(definition.DisplayName));
                Assert.That(
                    GetRenderedText(detail.Find("Face/txtEnglishName")),
                    Is.EqualTo(definition.Key.ToUpperInvariant()));
                Assert.That(
                    GetRenderedText(detail.Find("Ability/txtAbilityLabel")),
                    Is.EqualTo("ACTIVE"));
                Assert.That(
                    GetRenderedText(detail.Find("Ability/txtAbility")),
                    Does.Contain(definition.Summary));
                Assert.That(
                    GetRenderedText(detail.Find("Ability/txtAbility")),
                    Does.Not.Contain("ACTIVE"));
                Assert.That(
                    GetRenderedText(detail.Find("Cost/txtCostLabel")),
                    Is.EqualTo("COST"));
                Assert.That(
                    GetRenderedText(detail.Find("Cost/txtCost")),
                    Is.EqualTo(
                        CurrencyIconMarkup.FormatForTmp(
                            definition.CostSummary)));
                Assert.That(
                    GetRenderedText(detail.Find("Cost/txtCost")),
                    Does.Not.Contain(CurrencyIconMarkup.GoldTag));

                hud.HideDemonContractDetail();
                Assert.That(hoverDetail.gameObject.activeSelf, Is.False);
                Assert.That(hud.IsDemonContractDetailVisible, Is.False);
                Assert.That(
                    hudInstance.transform.Find("CombatControls").gameObject.activeSelf,
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(hudInstance);
                Object.DestroyImmediate(demonInstance);
            }
        }

        [Test]
        public void GSH01_U17_DemonHoverDetailPrefabPreviewRendersAndRestores()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonCardHoverDetailPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                DemonCardHoverDetailView view =
                    instance.GetComponent<DemonCardHoverDetailView>();
                Assert.That(view, Is.Not.Null);
                Transform englishName = GetSerializedTransform(
                    view.DetailView,
                    "englishNameText");
                Assert.That(englishName, Is.Not.Null);
                SerializedObject detailSerialized = new SerializedObject(
                    view.DetailView);
                Assert.That(
                    detailSerialized.FindProperty("englishNameText")
                        .objectReferenceValue,
                    Is.SameAs(englishName.GetComponent<Graphic>()));
                Assert.That(view.HasRequiredReferences, Is.True);
                Assert.That(instance.GetComponent<Canvas>(), Is.Null);
                Transform title = GetSerializedTransform(
                    view.DetailView,
                    "titleText");
                string originalTitle = GetRenderedText(title);

                Assert.That(
                    DemonCardHoverDetailPreviewSession.Show(view),
                    Is.Null);
                Canvas previewCanvas = instance.GetComponent<Canvas>();
                Assert.That(previewCanvas, Is.Not.Null);
                Assert.That(
                    previewCanvas.renderMode,
                    Is.EqualTo(RenderMode.ScreenSpaceOverlay));
                Assert.That(
                    (int)previewCanvas.additionalShaderChannels,
                    Is.EqualTo(25));
                Assert.That(instance.GetComponent<CanvasScaler>(), Is.Not.Null);
                string firstTitle = GetRenderedText(title);
                Assert.That(firstTitle, Is.Not.Empty);
                string firstEnglishName = GetRenderedText(englishName);
                Assert.That(firstEnglishName, Is.EqualTo("SATAN"));
                Assert.That(
                    firstEnglishName,
                    Is.EqualTo(firstEnglishName.ToUpperInvariant()));
                Transform cost = GetSerializedTransform(
                    view.DetailView,
                    "costText");
                Assert.That(
                    GetRenderedText(cost),
                    Does.Contain(CurrencyIconMarkup.SoulTag));
                Graphic soulIcon = cost
                    .GetComponentsInChildren<Graphic>(true)
                    .FirstOrDefault(component =>
                        component.GetType().Name == "TMP_SubMeshUI");
                Assert.That(soulIcon, Is.Not.Null);
                Assert.That(soulIcon.material, Is.Not.Null);
                Material costTextMaterial = new SerializedObject(
                        cost.GetComponent<Graphic>())
                    .FindProperty("m_sharedMaterial")
                    .objectReferenceValue as Material;
                Assert.That(costTextMaterial, Is.Not.Null);
                Assert.That(
                    soulIcon.material.renderQueue,
                    Is.EqualTo(costTextMaterial.renderQueue));

                DemonCardHoverDetailPreviewSession session =
                    DemonCardHoverDetailPreviewSession.GetActive(view);
                Assert.That(session, Is.Not.Null);
                Assert.That(session.CanMoveNext, Is.True);
                Assert.That(
                    DemonCardHoverDetailPreviewSession.MoveNext(view),
                    Is.Null);
                Assert.That(
                    GetRenderedText(title),
                    Is.Not.EqualTo(firstTitle));
                Assert.That(
                    GetRenderedText(englishName),
                    Is.Not.EqualTo(firstEnglishName));
                Assert.That(
                    GetRenderedText(englishName),
                    Is.EqualTo(GetRenderedText(englishName)
                        .ToUpperInvariant()));

                DemonCardHoverDetailPreviewSession.StopActive();
                Assert.That(instance.GetComponent<Canvas>(), Is.Null);
                Assert.That(instance.GetComponent<CanvasScaler>(), Is.Null);
                Assert.That(
                    GetRenderedText(title),
                    Is.EqualTo(originalTitle));
            }
            finally
            {
                DemonCardHoverDetailPreviewSession.StopActive();
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSH01_U18_DemonHoverDetailPreviewRestoresForSaveAndResumes()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                DemonCardHoverDetailPrefabPath);
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                DemonCardHoverDetailView view =
                    instance.GetComponent<DemonCardHoverDetailView>();
                Transform title = GetSerializedTransform(
                    view.DetailView,
                    "titleText");
                string originalTitle = GetRenderedText(title);

                Assert.That(
                    DemonCardHoverDetailPreviewSession.Show(view),
                    Is.Null);
                Assert.That(
                    DemonCardHoverDetailPreviewSession.MoveNext(view),
                    Is.Null);
                int previewIndex = DemonCardHoverDetailPreviewSession
                    .GetActive(view)
                    .CurrentIndex;

                DemonCardHoverDetailPreviewLifecycle.SuspendForPrefabSave(
                    instance);
                Assert.That(
                    DemonCardHoverDetailPreviewSession.GetActive(view),
                    Is.Null);
                Assert.That(
                    GetRenderedText(title),
                    Is.EqualTo(originalTitle));
                Assert.That(instance.GetComponent<Canvas>(), Is.Null);

                DemonCardHoverDetailPreviewLifecycle.ResumeAfterPrefabSave(
                    instance);
                DemonCardHoverDetailPreviewSession resumed =
                    DemonCardHoverDetailPreviewSession.GetActive(view);
                Assert.That(resumed, Is.Not.Null);
                Assert.That(resumed.CurrentIndex, Is.EqualTo(previewIndex));
                Assert.That(instance.GetComponent<Canvas>(), Is.Not.Null);
                Assert.That(GetRenderedText(title), Is.Not.EqualTo(originalTitle));
            }
            finally
            {
                DemonCardHoverDetailPreviewSession.StopActive();
                Object.DestroyImmediate(instance);
            }
        }

        private static string GetRenderedText(Transform target)
        {
            Assert.That(target, Is.Not.Null);
            Component textComponent = target
                .GetComponents<Component>()
                .First(component =>
                    component != null &&
                    component.GetType().GetProperty("text") != null);
            return (string)textComponent.GetType()
                .GetProperty("text")
                .GetValue(textComponent);
        }

        private static Transform GetSerializedTransform(
            UnityEngine.Object target,
            string propertyName)
        {
            SerializedProperty property = new SerializedObject(target)
                .FindProperty(propertyName);
            Assert.That(property, Is.Not.Null);
            Component component = property.objectReferenceValue as Component;
            Assert.That(component, Is.Not.Null);
            return component.transform;
        }

        private static CoreLoopBattle CreateStartedBattle(params int[] playerRanks)
        {
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateRankCards(0, playerRanks)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateStartedHammerBattle()
        {
            CardDefinition hammer = CardDefinitionCatalog.GetDefaultForRank(6);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 2, 3, hammer, 4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5, 4)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateStartedContractBattle(
            params DemonContractKind[] kinds)
        {
            var cards = new List<DemonContractCard>();
            for (int i = 0; i < kinds.Length; i++)
            {
                cards.Add(new DemonContractCard(
                    i,
                    DemonContractCatalog.Default.GetByKey(GetDefinitionKey(kinds[i]))));
            }

            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 5, 2, 3, 4)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(100, 10, 7, 5)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new StandPolicy(),
                playerDemonDeck: new DemonContractDeck(cards, seed: 73));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateStartedAsmodeusContractBattle()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .GetByKey(DemonContractCatalog.AsmodeusKey);
            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(0, 7, 2, 3, 4, 5)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100,
                    4,
                    5,
                    2,
                    3,
                    4)),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 4,
                enemyPolicy: new HitThenStandPolicy(),
                playerDemonDeck: new DemonContractDeck(
                    new[] { new DemonContractCard(0, definition) },
                    seed: 73));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static CoreLoopBattle CreateEnemyContractBattle()
        {
            DemonContractDefinition definition = DemonContractCatalog.Default
                .GetByKey(DemonContractCatalog.BelphegorKey);
            var enemyContracts = new List<DemonContractCard>();
            for (int i = 0; i < 4; i++)
            {
                enemyContracts.Add(new DemonContractCard(1000 + i, definition));
            }

            var battle = new CoreLoopBattle(
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    0, 2, 2, 2, 2, 2, 2, 2, 2)),
                BlackjackDeck.CreateInDrawOrder(CreateCards(
                    100, 10, 2, 2, 2, 2, 2, 2, 2)),
                playerMaximumSoul: 12,
                playerCurrentSoul: 12,
                enemyMaximumSoul: 3,
                enemyPolicy: new CultistEnemyPolicy(),
                cardEffectResolver: CardEffectResolver.CreateDefault(),
                playerDemonDeck: new DemonContractDeck(
                    System.Array.Empty<DemonContractCard>(),
                    seed: 0),
                demonContractResolver: DemonContractResolver.CreateDefault(),
                enemyDemonDeck: new DemonContractDeck(enemyContracts, seed: 17));
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static ContractPaperClickable CreateContractPaper(
            GameObject parent,
            string name,
            int sortingOrder)
        {
            var paper = new GameObject(name);
            paper.transform.SetParent(parent.transform, false);
            paper.AddComponent<SpriteRenderer>().sortingOrder = sortingOrder;
            return paper.AddComponent<ContractPaperClickable>();
        }

        private static IReadOnlyList<BlackjackCard> CreateCards(
            int firstId,
            params object[] values)
        {
            var cards = new List<BlackjackCard>();
            for (int i = 0; i < values.Length; i++)
            {
                CardDefinition definition = values[i] as CardDefinition ??
                    CardDefinitionCatalog.GetDefaultForRank((int)values[i]);
                cards.Add(new BlackjackCard(firstId + i, definition));
            }

            return cards;
        }

        private static IReadOnlyList<GameSceneCardViewModel>
            CreateSceneCardsForHand()
        {
            return new[]
            {
                new GameSceneCardViewModel(
                    500,
                    2,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    "Two"),
                new GameSceneCardViewModel(
                    501,
                    5,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    "Five"),
                new GameSceneCardViewModel(
                    502,
                    7,
                    isFaceUp: false,
                    revealRank: true,
                    canUse: false,
                    "Hidden")
            };
        }

        private static IReadOnlyList<GameSceneDemonCardViewModel>
            CreateSceneDemonCardsForHand()
        {
            DemonContractDefinition oldest = DemonContractCatalog.Default.GetByKey(
                DemonContractCatalog.MammonKey);
            DemonContractDefinition newest = DemonContractCatalog.Default.GetByKey(
                DemonContractCatalog.BelphegorKey);
            return new[]
            {
                new GameSceneDemonCardViewModel(
                    700,
                    oldest.Key,
                    isFaceUp: true,
                    canUse: false,
                    oldest.DisplayName,
                    oldest.Summary,
                    oldest.CostSummary,
                    showHoverBadgeWhenUnavailable: true),
                new GameSceneDemonCardViewModel(
                    701,
                    newest.Key,
                    isFaceUp: true,
                    canUse: false,
                    newest.DisplayName,
                    newest.Summary,
                    newest.CostSummary,
                    showHoverBadgeWhenUnavailable: true)
            };
        }

        private static IReadOnlyList<BlackjackCard> CreateRankCards(
            int firstId,
            IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>();
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(firstId + i, ranks[i]));
            }

            return cards;
        }

        private static string GetDefinitionKey(DemonContractKind kind)
        {
            switch (kind)
            {
                case DemonContractKind.Belphegor:
                    return DemonContractCatalog.BelphegorKey;
                case DemonContractKind.Mammon:
                    return DemonContractCatalog.MammonKey;
                case DemonContractKind.Satan:
                    return DemonContractCatalog.SatanKey;
                case DemonContractKind.Lucifer:
                    return DemonContractCatalog.LuciferKey;
                case DemonContractKind.Asmodeus:
                    return DemonContractCatalog.AsmodeusKey;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(kind));
            }
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "gsh01-stand");
            }
        }

        private sealed class HitThenStandPolicy : IEnemyBehaviorPolicy
        {
            private bool _hasHit;

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionType action = _hasHit
                    ? EnemyActionType.Stand
                    : EnemyActionType.Hit;
                _hasHit = true;
                return new EnemyDecision(action, "gsh01-hit-then-stand");
            }
        }
    }
}
