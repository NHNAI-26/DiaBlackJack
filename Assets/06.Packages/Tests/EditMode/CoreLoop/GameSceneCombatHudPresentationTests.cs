using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class GameSceneCombatHudPresentationTests
    {
        private const string HudPrefabPath = "Assets/03. Prefabs/UI/HUD.prefab";
        private const string CardHoverTooltipPrefabPath =
            "Assets/03. Prefabs/UI/CardHoverTooltip.prefab";
        private const string ManagerPrefabPath =
            "Assets/03. Prefabs/Manager/GameManager.prefab";
        private const string CardPrefabPath =
            "Assets/03. Prefabs/Card/Card.prefab";
        private const string DemonCardPrefabPath =
            "Assets/03. Prefabs/Card/DemonCard.prefab";
        private const string TablePrefabPath =
            "Assets/03. Prefabs/TableObjects/Table Controller.prefab";
        private const string ContractPaperSpritePath =
            "Assets/05. Arts/Texture/ContractPaper/ContractPaper.png";
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
            Assert.That(model.PrimaryActions[2].Tooltip, Does.Contain(core.ChangeActionText));
            Assert.That(model.PrimaryActions.Any(action =>
                action.Command.Kind == GameSceneCombatHudCommandKind.BeginContract),
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
                ContractPaperClickable first = CreateContractPaper(root, "PaperA");
                ContractPaperClickable second = CreateContractPaper(root, "PaperB");

                view.Render(new ContractPaperViewModel(2, true));
                Assert.That(view.HasRequiredReferences, Is.True);
                Assert.That(view.VisibleCount, Is.EqualTo(2));
                Assert.That(first.gameObject.activeSelf, Is.True);
                Assert.That(second.gameObject.activeSelf, Is.True);
                Assert.That(first.IsInteractable, Is.False);
                Assert.That(second.IsInteractable, Is.True);

                view.Render(new ContractPaperViewModel(1, false));
                Assert.That(view.VisibleCount, Is.EqualTo(1));
                Assert.That(first.gameObject.activeSelf, Is.True);
                Assert.That(second.gameObject.activeSelf, Is.False);
                Assert.That(first.IsInteractable, Is.False);
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
            Assert.That(
                papers[0].transform.localPosition.y,
                Is.LessThan(papers[1].transform.localPosition.y));
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
        public void GSH01_U02_ChangeCandidatesKeepActualCandidateIndicesAndRespectInputLock()
        {
            CoreLoopBattle battle = CreateStartedBattle(10, 2, 4, 9);
            Assert.That(battle.TryBeginPlayerChange(), Is.True);

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle);
            GameSceneCombatHudViewModel unlocked = GameSceneCombatHudPresenter.Create(
                core, false, false, false);
            GameSceneCombatHudViewModel locked = GameSceneCombatHudPresenter.Create(
                core, false, false, true);

            Assert.That(unlocked.Mode, Is.EqualTo(GameSceneCombatHudMode.Options));
            Assert.That(unlocked.OptionActions.Select(action => action.Command.OptionId),
                Is.EqualTo(new[] { 0, 1 }));
            Assert.That(unlocked.OptionActions.All(action =>
                action.Command.Kind == GameSceneCombatHudCommandKind.SelectChangedCard &&
                action.IsInteractable), Is.True);
            Assert.That(locked.OptionActions.All(action => !action.IsInteractable), Is.True);
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
            Assert.That(candidates.Prompt, Is.Empty);
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
            Assert.That(hud.Prompt, Is.Empty);
            Assert.That(hud.OptionActions, Is.Empty);
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
                Is.EqualTo(pending.Options.Select(option => option.CardId.Value)));
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
            Assert.That(hud.Prompt, Is.EqualTo(scene.Core.CardEffectPrompt));
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
                Component prompt = serialized.FindProperty("combatPromptText")
                    .objectReferenceValue as Component;

                Assert.That(optionPanel, Is.Not.Null);
                Assert.That(optionPanel.activeSelf, Is.True);
                Assert.That(optionPanel.GetComponent<Graphic>().enabled, Is.False);
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

            ActiveDemonContract active =
                battle.ActivePlayerDemonContracts.Single();
            Assert.That(battle.TryBeginPlayerActiveDemonContractAction(
                active.SourceCardId), Is.True);

            GameSceneViewModel first = GameScenePresenter.Create(battle);
            GameSceneCombatHudViewModel firstHud =
                GameSceneCombatHudPresenter.Create(
                    first.Core,
                    isStageBattle: false,
                    isShopOpen: false,
                    inputLocked: false);
            Assert.That(firstHud.Mode,
                Is.EqualTo(GameSceneCombatHudMode.SatanNumberSelection));
            Assert.That(first.SatanNumberCandidates, Has.Count.EqualTo(10));
            Assert.That(first.SatanNumberCandidates.All(card =>
                card.DirectSelectionCommand.HasValue), Is.True);
            Assert.That(first.SatanNumberCandidates.All(card => !card.IsUsed),
                Is.True);

            PendingDemonContractInteraction declaration =
                battle.PendingPlayerDemonContractInteraction;
            DemonContractOption three = declaration.Options.Single(option =>
                option.NumericValue == 3);
            Assert.That(battle.TryResolvePlayerDemonContract(
                declaration.InteractionId,
                three.OptionId), Is.True);

            GameSceneViewModel second = GameScenePresenter.Create(battle);
            GameSceneCardViewModel branded = second.SatanNumberCandidates.Single(
                card => card.IsUsed);
            Assert.That(branded.Rank, Is.EqualTo(3));
            Assert.That(branded.DirectSelectionCommand.HasValue, Is.False);
            Assert.That(second.SatanNumberCandidates.Count(card =>
                card.DirectSelectionCommand.HasValue), Is.EqualTo(9));
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
                Is.EqualTo("추가하지 않기"));
            Assert.That(hud.OptionActions.Single().Command.OptionId, Is.EqualTo(0));
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
                SerializedObject crystalLayout = new SerializedObject(view);
                SerializedObject contractLayout = new SerializedObject(contractView);
                string[] sharedLayoutFields =
                {
                    "cameraDistance",
                    "viewportCenterY",
                    "viewportSpacing",
                    "hoverViewportLift",
                    "hoverCameraPull",
                    "fanAngle",
                    "cardScale",
                    "poseLerp",
                    "baseSortingOrder"
                };
                foreach (string field in sharedLayoutFields)
                {
                    SerializedProperty crystalProperty =
                        crystalLayout.FindProperty(field);
                    SerializedProperty contractProperty =
                        contractLayout.FindProperty(field);
                    Assert.That(crystalProperty, Is.Not.Null, field);
                    Assert.That(contractProperty, Is.Not.Null, field);
                    Assert.That(
                        crystalProperty.propertyType,
                        Is.EqualTo(contractProperty.propertyType),
                        field);
                    if (crystalProperty.propertyType ==
                        SerializedPropertyType.Integer)
                    {
                        Assert.That(
                            crystalProperty.intValue,
                            Is.EqualTo(contractProperty.intValue),
                            field);
                    }
                    else
                    {
                        Assert.That(
                            crystalProperty.floatValue,
                            Is.EqualTo(contractProperty.floatValue),
                            field);
                    }
                }

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
            Assert.That(ownerHud.Prompt, Does.EndWith("(1/2)"));
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
            Assert.That(opponentHud.Prompt, Does.EndWith("(2/2)"));
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
                    string.Empty,
                    actions,
                    null,
                    null,
                    string.Empty);

                group.Render(model);
                TableCombatCommandView hit = group
                    .GetComponentsInChildren<TableCombatCommandView>(true)
                    .Single(command => command.Kind == GameSceneCombatHudCommandKind.Hit);
                Assert.That(hit.GetComponent<Collider>().enabled, Is.False);
                Assert.That(hit.GetComponent<SpriteRenderer>().color,
                    Is.EqualTo(new Color(0.35f, 0.35f, 0.35f, 0.35f)));
                Assert.That(hit.TryGetCommand(out _), Is.False);

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
        public void GSH01_U07_ContractDetailLayoutIsAuthoredInHudPrefab()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            Transform panel = prefab.transform.Find(
                "CombatControls/ContractDetailPanel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.Find("Prompt"), Is.Null);

            Transform layout = panel.Find("DetailLayout");
            Assert.That(layout, Is.Not.Null);
            Transform detail = layout.Find("ContractDetail");
            Assert.That(detail, Is.Not.Null);
            Assert.That(layout.Find("CandidateSlot_2"), Is.Null);
            Assert.That(detail.GetComponent<Button>(), Is.Null);
            Assert.That(detail.GetComponent<GameHudChoiceButton>(), Is.Null);

            GameHudContractDetailView detailView =
                detail.GetComponent<GameHudContractDetailView>();
            Assert.That(detailView, Is.Not.Null);
            Assert.That(detailView.HasRequiredReferences, Is.True);
            Assert.That(detail.Find("Face"), Is.Not.Null);
            Assert.That(detail.Find("Title/txtTitle"), Is.Not.Null);
            Assert.That(detail.Find("Ability/txtAbility"), Is.Not.Null);
            Assert.That(detail.Find("Cost/txtCost"), Is.Not.Null);
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

                positionMethod.Invoke(hud, new object[] { anchorPoint, false });
                Assert.That(
                    tooltipRoot.localPosition,
                    Is.EqualTo(new Vector3(anchorPoint.x, anchorPoint.y, 0f)));
                Assert.That(tooltipRoot.pivot.y, Is.EqualTo(0f));

                positionMethod.Invoke(hud, new object[] { anchorPoint, true });
                Assert.That(
                    tooltipRoot.localPosition,
                    Is.EqualTo(new Vector3(anchorPoint.x, anchorPoint.y, 0f)));
                Assert.That(tooltipRoot.pivot.y, Is.EqualTo(1f));
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void GSH01_U12_ShopDemonReusesContractDetailPanel()
        {
            GameObject hudPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            GameObject demonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/03. Prefabs/Card/DemonCard.prefab");
            GameObject hudInstance = Object.Instantiate(hudPrefab);
            GameObject demonInstance = Object.Instantiate(demonPrefab);
            try
            {
                GameHudView hud = hudInstance.GetComponent<GameHudView>();
                DemonCardView demon = demonInstance.GetComponent<DemonCardView>();
                demon.Bind(new GameSceneDemonCardViewModel(
                    cardId: 7,
                    definitionKey: DemonContractCatalog.SatanKey,
                    isFaceUp: true,
                    canUse: true,
                    displayName: "사탄",
                    summary: "공개 카드 한 장을 사용한다.",
                    costSummary: "PRICE 5 GOLD"));

                hud.Render((CoreLoopViewModel)null);
                hud.ShowDemonContractDetail(demon.BoundCard);

                Transform panel = hudInstance.transform.Find(
                    "CombatControls/ContractDetailPanel");
                Assert.That(panel.gameObject.activeSelf, Is.True);
                Assert.That(panel.gameObject.activeInHierarchy, Is.True);
                Assert.That(hud.IsDemonContractDetailVisible, Is.True);
                Assert.That(
                    GetRenderedText(panel.Find(
                        "DetailLayout/ContractDetail/Title/txtTitle")),
                    Is.EqualTo("사탄"));
                Assert.That(
                    GetRenderedText(panel.Find(
                        "DetailLayout/ContractDetail/Ability/txtAbility")),
                    Does.Contain("공개 카드 한 장을 사용한다."));
                Assert.That(
                    GetRenderedText(panel.Find(
                        "DetailLayout/ContractDetail/Cost/txtCost")),
                    Does.Contain(
                        $"PRICE 5 {CurrencyIconMarkup.GoldTag}"));

                hud.HideDemonContractDetail();
                Assert.That(panel.gameObject.activeSelf, Is.False);
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
            string name)
        {
            var paper = new GameObject(name);
            paper.transform.SetParent(parent.transform, false);
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
