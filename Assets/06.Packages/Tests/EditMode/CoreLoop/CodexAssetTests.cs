using System;
using System.Collections.Generic;
using System.Reflection;
using DiaBlackJack.Content;
using DiaBlackJack.GameScene;
using DiaBlackJack.GameScene.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CodexAssetTests
    {
        private const string CardCatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";
        private const string EnemyCatalogPath =
            "Assets/02. ScriptableObjects/Enemies/EnemyContentCatalog.asset";
        private const string OverlayPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/CodexOverlay.prefab";
        private const string BookPrefabPath =
            "Assets/03. Prefabs/Props/CodexBook.prefab";
        private const string CodexFramePath =
            "Assets/05. Arts/Texture/Codex/CodexFrame.png";
        private const string CodexOutlinePath =
            "Assets/05. Arts/Texture/Codex/CodexOutline.png";
        private const string SoulIconPath =
            "Assets/05. Arts/UI/Icons/SoulIcon.png";
        private const string GoldIconPath =
            "Assets/05. Arts/UI/Icons/GoldIcon.png";
        private const string DeckPreviewCardPrefabPath =
            "Assets/03. Prefabs/UI/GameScene/DeckPreviewCard.prefab";
        private const string OpenBookRemakePathPrefix =
            "Assets/05. Arts/Texture/Codex/Codex_OpenBook_Remake_";

        [Test]
        public void DX02_U01_ContentCatalogCoversEveryEnemyAndDemon()
        {
            CardContentCatalogSO cardCatalog =
                AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(
                    CardCatalogPath);
            EnemyContentCatalogSO enemyCatalog = LoadEnemyCatalog();

            Assert.That(cardCatalog, Is.Not.Null);
            Assert.That(enemyCatalog, Is.Not.Null);
            enemyCatalog.ValidateOrThrow();
            Assert.That(enemyCatalog.EnemyCount, Is.EqualTo(6));
            Assert.That(cardCatalog.DemonCardCount, Is.EqualTo(12));
            Assert.That(
                cardCatalog.BuildDemonLoreCatalog().Count,
                Is.EqualTo(12));

            EnemyCombatProfileCatalog authoredProfiles =
                enemyCatalog.BuildRuntimeCatalog();
            DiaBlackJack.StageProgression.GoldRewardCatalog authoredGold =
                enemyCatalog.BuildGoldRewardCatalog();
            foreach (EnemyCombatProfile expected in
                EnemyCombatProfileCatalog.Default.Profiles)
            {
                EnemyCombatProfile actual = authoredProfiles.GetByKey(
                    expected.Key);
                Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName));
                Assert.That(actual.Grade, Is.EqualTo(expected.Grade));
                Assert.That(actual.MaximumSoul, Is.EqualTo(expected.MaximumSoul));
                Assert.That(
                    actual.BehaviorPolicyKey,
                    Is.EqualTo(expected.BehaviorPolicyKey));
                Assert.That(
                    actual.DeckDefinitionKeys,
                    Is.EqualTo(expected.DeckDefinitionKeys));
                Assert.That(actual.Summary, Is.EqualTo(expected.Summary));
                Assert.That(
                    actual.PlayerInformationMode,
                    Is.EqualTo(expected.PlayerInformationMode));
                Assert.That(
                    actual.ChangeCostMode,
                    Is.EqualTo(expected.ChangeCostMode));
                Assert.That(
                    actual.DemonContractDefinitionKeys,
                    Is.EqualTo(expected.DemonContractDefinitionKeys));
                Assert.That(
                    actual.DemonContractCandidateCount,
                    Is.EqualTo(expected.DemonContractCandidateCount));
                Assert.That(
                    actual.InjectsPoisonIntoPlayerDeckEachRound,
                    Is.EqualTo(
                        expected.InjectsPoisonIntoPlayerDeckEachRound));
                Assert.That(
                    actual.FixedDemonContractPhases.Count,
                    Is.EqualTo(expected.FixedDemonContractPhases.Count));
                for (int index = 0;
                    index < actual.FixedDemonContractPhases.Count;
                    index++)
                {
                    FixedDemonContractPhaseDefinition actualPhase =
                        actual.FixedDemonContractPhases[index];
                    FixedDemonContractPhaseDefinition expectedPhase =
                        expected.FixedDemonContractPhases[index];
                    Assert.That(
                        actualPhase.ActivationSoulThreshold,
                        Is.EqualTo(expectedPhase.ActivationSoulThreshold));
                    Assert.That(
                        actualPhase.ActiveDefinitionKey,
                        Is.EqualTo(expectedPhase.ActiveDefinitionKey));
                    Assert.That(
                        actualPhase.DiscardedDefinitionKey,
                        Is.EqualTo(expectedPhase.DiscardedDefinitionKey));
                }

                Assert.That(
                    authoredGold.GetAmount(expected.Key),
                    Is.EqualTo(
                        DiaBlackJack.StageProgression.GoldRewardCatalog
                            .CreatePrototype()
                            .GetAmount(expected.Key)));
            }
        }

        [Test]
        public void DX02_U02_OverlayPrefabHasTabsCloseAndScrollableDeck()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            CodexOverlayView view = prefab.GetComponent<CodexOverlayView>();
            Assert.That(view, Is.Not.Null);
            Assert.That(
                prefab.GetComponentsInChildren<Button>(true).Length,
                Is.GreaterThanOrEqualTo(3));
            Assert.That(
                prefab.GetComponentInChildren<ScrollRect>(true),
                Is.Not.Null);
            Assert.That(
                prefab.GetComponentsInChildren<DeckPreviewCardView>(true)
                    .Length,
                Is.EqualTo(2));
            Assert.That(
                prefab.GetComponentInChildren<EventSystem>(true),
                Is.Null);

            RectTransform[] rectTransforms =
                prefab.GetComponentsInChildren<RectTransform>(true);
            foreach (RectTransform rectTransform in rectTransforms)
            {
                Assert.That(
                    IsAnchorPreset(rectTransform),
                    Is.True,
                    $"'{rectTransform.name}' must use a standard anchor preset.");

                bool usesFixedAnchors =
                    Mathf.Approximately(
                        rectTransform.anchorMin.x,
                        rectTransform.anchorMax.x) &&
                    Mathf.Approximately(
                        rectTransform.anchorMin.y,
                        rectTransform.anchorMax.y);
                if (rectTransform != prefab.transform && usesFixedAnchors)
                {
                    Assert.That(
                        rectTransform.sizeDelta.x,
                        Is.GreaterThan(0f),
                        $"'{rectTransform.name}' must expose a positive width.");
                    Assert.That(
                        rectTransform.sizeDelta.y,
                        Is.GreaterThan(0f),
                        $"'{rectTransform.name}' must expose a positive height.");
                }
            }

            DeckPreviewCardView deckTemplate =
                GetReference<DeckPreviewCardView>(view, "deckTemplate");
            RectTransform deckRect = deckTemplate.transform as RectTransform;
            Assert.That(deckRect, Is.Not.Null);
            Assert.That(deckRect.anchorMin, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(deckRect.anchorMax, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(deckRect.sizeDelta, Is.EqualTo(new Vector2(116f, 184f)));
            Graphic countText = GetReference<Graphic>(deckTemplate, "countText");
            Assert.That(countText, Is.Not.Null);
            Assert.That(countText.transform.parent, Is.EqualTo(deckTemplate.transform));
            Assert.That(countText.raycastTarget, Is.False);
        }

        [Test]
        public void DX02_U06_EnemyLayoutUsesCodexArtAndVerticalFourColumnGrid()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            Assert.That(prefab, Is.Not.Null);
            CodexOverlayView view = prefab.GetComponent<CodexOverlayView>();
            Assert.That(view, Is.Not.Null);

            Image portraitFrame = GetImageAtPath(
                prefab,
                "Book/EnemyPage/LeftPage/PortraitPanel/PortraitFrame");
            Image soulOutline = GetImageAtPath(
                prefab,
                "Book/EnemyPage/LeftPage/SoulPanel/Outline");
            Image soulIcon = GetImageAtPath(
                prefab,
                "Book/EnemyPage/LeftPage/SoulPanel/Icon");
            Image goldIcon = GetImageAtPath(
                prefab,
                "Book/EnemyPage/LeftPage/GoldPanel/Icon");
            Image deckBanner = GetImageAtPath(
                prefab,
                "Book/EnemyPage/RightPage/DeckTitleBanner");
            Image deckOutline = GetImageAtPath(
                prefab,
                "Book/EnemyPage/RightPage/DeckPanel/Outline");

            Assert.That(
                portraitFrame.sprite,
                Is.EqualTo(LoadSprite(CodexFramePath, "CodexFrame_0")));
            Assert.That(portraitFrame.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(
                deckBanner.sprite,
                Is.EqualTo(LoadSprite(CodexFramePath, "CodexFrame_2")));
            Assert.That(
                soulOutline.sprite,
                Is.EqualTo(LoadSprite(CodexOutlinePath, "CodexOutline_0")));
            Assert.That(
                deckOutline.sprite,
                Is.EqualTo(LoadSprite(CodexOutlinePath, "CodexOutline_0")));
            Assert.That(soulOutline.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(deckOutline.type, Is.EqualTo(Image.Type.Sliced));
            Assert.That(
                soulIcon.sprite,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(SoulIconPath)));
            Assert.That(
                goldIcon.sprite,
                Is.EqualTo(AssetDatabase.LoadAssetAtPath<Sprite>(GoldIconPath)));

            ScrollRect scrollRect = GetReference<ScrollRect>(
                view,
                "deckScrollRect");
            Assert.That(scrollRect.horizontal, Is.False);
            Assert.That(scrollRect.vertical, Is.True);
            Assert.That(
                scrollRect.movementType,
                Is.EqualTo(ScrollRect.MovementType.Clamped));
            Assert.That(scrollRect.horizontalScrollbar, Is.Null);
            Assert.That(scrollRect.verticalScrollbar, Is.Null);
            Assert.That(
                scrollRect.viewport.GetComponent<RectMask2D>(),
                Is.Not.Null);

            GridLayoutGroup grid =
                scrollRect.content.GetComponent<GridLayoutGroup>();
            ContentSizeFitter fitter =
                scrollRect.content.GetComponent<ContentSizeFitter>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(fitter, Is.Not.Null);
            Assert.That(
                grid.constraint,
                Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(4));
            Assert.That(grid.cellSize, Is.EqualTo(new Vector2(116f, 184f)));
            Assert.That(grid.spacing, Is.EqualTo(new Vector2(8f, 12f)));
            Assert.That(grid.padding.left, Is.EqualTo(8));
            Assert.That(grid.padding.right, Is.EqualTo(8));
            Assert.That(
                fitter.verticalFit,
                Is.EqualTo(ContentSizeFitter.FitMode.PreferredSize));
        }

        [Test]
        public void DX02_U03_TableBookPrefabIsClickable()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(BookPrefabPath);

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<CodexClickable>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Collider>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Not.Null);
        }

        [Test]
        public void DX02_U04_EditorPreviewUsesTemplatesWithoutCloningAndRestores()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(prefab);
            CodexOverlayView view = overlay.GetComponent<CodexOverlayView>();
            DeckPreviewCardView contractTemplate =
                GetReference<DeckPreviewCardView>(
                    view,
                    "contractTemplate");
            DeckPreviewCardView deckTemplate =
                GetReference<DeckPreviewCardView>(view, "deckTemplate");
            Component noContractText = GetReference<Component>(
                view,
                "noContractText");
            Component enemySoulText = GetReference<Component>(
                view,
                "enemySoulText");
            Component enemyGoldText = GetReference<Component>(
                view,
                "enemyGoldText");
            Component enemyDescriptionText = GetReference<Component>(
                view,
                "enemyDescriptionText");
            Image enemyPortraitImage = GetReference<Image>(
                view,
                "enemyPortraitImage");
            Image deckFace = GetReference<Image>(
                deckTemplate,
                "faceImage");
            Component deckCount = GetReference<Component>(
                deckTemplate,
                "countText");
            int cardViewCount = overlay
                .GetComponentsInChildren<DeckPreviewCardView>(true)
                .Length;
            bool contractActive = contractTemplate.gameObject.activeSelf;
            bool deckActive = deckTemplate.gameObject.activeSelf;
            bool noContractActive = noContractText.gameObject.activeSelf;
            Sprite deckSprite = deckFace.sprite;
            string deckCountLabel = GetText(deckCount);
            string soulLabel = GetText(enemySoulText);
            string goldLabel = GetText(enemyGoldText);
            string descriptionLabel = GetText(enemyDescriptionText);
            Sprite portraitSprite = enemyPortraitImage.sprite;

            CardContentCatalogSO cardCatalog = LoadCardCatalog();
            EnemyContentCatalogSO enemyCatalog = LoadEnemyCatalog();
            IReadOnlyList<EnemyCodexPageViewModel> pages =
                CreateEnemyPages(cardCatalog);
            int emptyContractIndex = FindEnemyPageIndex(
                pages,
                hasContracts: false);
            int longestDescriptionIndex =
                FindLongestDescriptionIndex(pages);

            try
            {
                Assert.That(
                    CodexOverlayPreviewSession.ShowCategory(
                        view,
                        CodexCategory.Enemy),
                    Is.Null);
                MovePreviewToIndex(view, emptyContractIndex);

                EnemyCodexPageViewModel page = pages[emptyContractIndex];
                CodexDeckCardViewModel firstCard = page.StartingDeck[0];
                Assert.That(
                    overlay.GetComponentsInChildren<DeckPreviewCardView>(true)
                        .Length,
                    Is.EqualTo(cardViewCount));
                Assert.That(contractTemplate.gameObject.activeSelf, Is.False);
                Assert.That(deckTemplate.gameObject.activeSelf, Is.True);
                Assert.That(noContractText.gameObject.activeSelf, Is.True);
                Assert.That(
                    GetText(enemySoulText),
                    Is.EqualTo(page.MaximumSoul.ToString()));
                Assert.That(
                    GetText(enemyGoldText),
                    Is.EqualTo(page.DefeatGold.ToString()));
                Assert.That(
                    GetText(enemyDescriptionText),
                    Is.EqualTo(page.Description));
                Assert.That(
                    enemyPortraitImage.sprite,
                    Is.EqualTo(enemyCatalog.GetPortrait(page.ProfileKey)));
                Assert.That(
                    deckFace.sprite,
                    Is.EqualTo(cardCatalog.GetNormalFaceSprite(
                        firstCard.DefinitionKey,
                        firstCard.Suit)));
                Assert.That(
                    GetText(deckCount),
                    Is.EqualTo($"x{firstCard.Count}"));

                MovePreviewToIndex(view, longestDescriptionIndex);
                EnemyCodexPageViewModel longestDescriptionPage =
                    pages[longestDescriptionIndex];
                Assert.That(
                    GetText(enemyDescriptionText),
                    Is.EqualTo(longestDescriptionPage.Description));
                Assert.That(
                    enemyPortraitImage.sprite,
                    Is.EqualTo(enemyCatalog.GetPortrait(
                        longestDescriptionPage.ProfileKey)));

                CodexOverlayPreviewSession.StopActive();
                Assert.That(
                    contractTemplate.gameObject.activeSelf,
                    Is.EqualTo(contractActive));
                Assert.That(
                    deckTemplate.gameObject.activeSelf,
                    Is.EqualTo(deckActive));
                Assert.That(
                    noContractText.gameObject.activeSelf,
                    Is.EqualTo(noContractActive));
                Assert.That(deckFace.sprite, Is.EqualTo(deckSprite));
                Assert.That(GetText(deckCount), Is.EqualTo(deckCountLabel));
                Assert.That(GetText(enemySoulText), Is.EqualTo(soulLabel));
                Assert.That(GetText(enemyGoldText), Is.EqualTo(goldLabel));
                Assert.That(
                    GetText(enemyDescriptionText),
                    Is.EqualTo(descriptionLabel));
                Assert.That(enemyPortraitImage.sprite, Is.EqualTo(portraitSprite));
            }
            finally
            {
                CodexOverlayPreviewSession.StopActive();
                UnityEngine.Object.DestroyImmediate(overlay);
            }
        }

        [Test]
        public void DX02_U05_EditorPreviewShowsContractAndDemonAtBoundaries()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(prefab);
            CodexOverlayView view = overlay.GetComponent<CodexOverlayView>();
            DeckPreviewCardView contractTemplate =
                GetReference<DeckPreviewCardView>(
                    view,
                    "contractTemplate");
            DeckPreviewCardView deckTemplate =
                GetReference<DeckPreviewCardView>(view, "deckTemplate");
            Image contractFace = GetReference<Image>(
                contractTemplate,
                "faceImage");
            Image demonCard = GetReference<Image>(view, "demonCardImage");
            GameObject enemyPage = GetReference<GameObject>(
                view,
                "enemyPageRoot");
            GameObject demonPage = GetReference<GameObject>(
                view,
                "demonPageRoot");

            CardContentCatalogSO cardCatalog = LoadCardCatalog();
            IReadOnlyList<EnemyCodexPageViewModel> enemyPages =
                CreateEnemyPages(cardCatalog);
            IReadOnlyList<DemonCodexPageViewModel> demonPages =
                CodexPresenter.CreateDemonPages(
                    cardCatalog.BuildRuntimeCatalog(),
                    cardCatalog.BuildDemonLoreCatalog());
            int contractIndex = FindEnemyPageIndex(
                enemyPages,
                hasContracts: true);

            try
            {
                Assert.That(
                    CodexOverlayPreviewSession.ShowCategory(
                        view,
                        CodexCategory.Enemy),
                    Is.Null);
                MovePreviewToIndex(view, contractIndex);
                CodexDemonReferenceViewModel firstContract =
                    enemyPages[contractIndex].ContractableDemons[0];
                Assert.That(contractTemplate.gameObject.activeSelf, Is.True);
                Assert.That(
                    contractFace.sprite,
                    Is.EqualTo(cardCatalog.GetDemonFaceSprite(
                        firstContract.DefinitionKey)));

                Assert.That(
                    CodexOverlayPreviewSession.ShowCategory(
                        view,
                        CodexCategory.DemonCard),
                    Is.Null);
                CodexOverlayPreviewSession session =
                    CodexOverlayPreviewSession.GetActive(view);
                Assert.That(session.CurrentCategory, Is.EqualTo(
                    CodexCategory.DemonCard));
                Assert.That(enemyPage.activeSelf, Is.False);
                Assert.That(demonPage.activeSelf, Is.True);
                Assert.That(contractTemplate.gameObject.activeSelf, Is.False);
                Assert.That(deckTemplate.gameObject.activeSelf, Is.False);
                Assert.That(
                    demonCard.sprite,
                    Is.EqualTo(cardCatalog.GetDemonFaceSprite(
                        demonPages[0].DefinitionKey)));

                while (session.CanMoveNext)
                {
                    Assert.That(
                        CodexOverlayPreviewSession.MoveNext(view),
                        Is.Null);
                }

                int lastIndex = session.CurrentPageIndex;
                Assert.That(
                    CodexOverlayPreviewSession.MoveNext(view),
                    Is.Null);
                Assert.That(session.CurrentPageIndex, Is.EqualTo(lastIndex));
                Assert.That(session.CanMoveNext, Is.False);
            }
            finally
            {
                CodexOverlayPreviewSession.StopActive();
                UnityEngine.Object.DestroyImmediate(overlay);
            }
        }

        [Test]
        public void DX02_U07_EditorPreviewResumesAfterSaveAndKeepsLayoutEdit()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(prefab);
            CodexOverlayView view = overlay.GetComponent<CodexOverlayView>();
            RectTransform leftPage = overlay.transform.Find(
                "Book/EnemyPage/LeftPage") as RectTransform;
            Assert.That(leftPage, Is.Not.Null);

            try
            {
                Assert.That(
                    CodexOverlayPreviewSession.ShowCategory(
                        view,
                        CodexCategory.DemonCard),
                    Is.Null);
                Assert.That(
                    CodexOverlayPreviewSession.MoveNext(view),
                    Is.Null);
                CodexOverlayPreviewSession beforeSave =
                    CodexOverlayPreviewSession.GetActive(view);
                int pageIndex = beforeSave.CurrentPageIndex;
                Vector2 editedPosition =
                    leftPage.anchoredPosition + new Vector2(7f, -9f);
                leftPage.anchoredPosition = editedPosition;

                CodexOverlayPreviewLifecycle.SuspendForPrefabSave(overlay);
                Assert.That(
                    CodexOverlayPreviewSession.GetActive(view),
                    Is.Null);
                Assert.That(
                    leftPage.anchoredPosition,
                    Is.EqualTo(editedPosition));

                CodexOverlayPreviewLifecycle.ResumeAfterPrefabSave(overlay);
                CodexOverlayPreviewSession afterSave =
                    CodexOverlayPreviewSession.GetActive(view);
                Assert.That(afterSave, Is.Not.Null);
                Assert.That(
                    afterSave.CurrentCategory,
                    Is.EqualTo(CodexCategory.DemonCard));
                Assert.That(afterSave.CurrentPageIndex, Is.EqualTo(pageIndex));
                Assert.That(
                    leftPage.anchoredPosition,
                    Is.EqualTo(editedPosition));
            }
            finally
            {
                CodexOverlayPreviewSession.StopActive();
                UnityEngine.Object.DestroyImmediate(overlay);
            }
        }

        [Test]
        public void DXM06_U01_PageLabelsCloseAlphaAndInactiveTabTextAreAuthored()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(prefab);
            CodexOverlayView view = overlay.GetComponent<CodexOverlayView>();
            Component previousText = GetReference<Component>(
                view,
                "previousPageText");
            Component nextText = GetReference<Component>(view, "nextPageText");
            Graphic enemyTabText = GetReference<Graphic>(
                view,
                "enemyTabText");
            Graphic demonTabText = GetReference<Graphic>(
                view,
                "demonTabText");
            Button closeButton = GetReference<Button>(view, "closeButton");
            SerializedObject serialized = new SerializedObject(view);
            Color activeColor = serialized.FindProperty("activeTabColor")
                .colorValue;
            Color inactiveColor = serialized.FindProperty("inactiveTabColor")
                .colorValue;

            try
            {
                Assert.That(
                    CodexOverlayPreviewSession.ShowCategory(
                        view,
                        CodexCategory.Enemy),
                    Is.Null);
                Assert.That(GetText(previousText), Is.EqualTo("Q Previous"));
                Assert.That(GetText(nextText), Is.EqualTo("1/6 Next E"));
                Assert.That(enemyTabText.color, Is.EqualTo(activeColor));
                Assert.That(demonTabText.color, Is.EqualTo(inactiveColor));

                Assert.That(closeButton.colors.normalColor.a, Is.EqualTo(0.5f));
                Assert.That(
                    closeButton.colors.highlightedColor.a,
                    Is.EqualTo(1f));
                Assert.That(closeButton.colors.pressedColor.a, Is.EqualTo(0.8f));

                Assert.That(
                    CodexOverlayPreviewSession.MoveNext(view),
                    Is.Null);
                Assert.That(GetText(nextText), Is.EqualTo("2/6 Next E"));

                Assert.That(
                    CodexOverlayPreviewSession.ShowCategory(
                        view,
                        CodexCategory.DemonCard),
                    Is.Null);
                Assert.That(enemyTabText.color, Is.EqualTo(inactiveColor));
                Assert.That(demonTabText.color, Is.EqualTo(activeColor));
            }
            finally
            {
                CodexOverlayPreviewSession.StopActive();
                UnityEngine.Object.DestroyImmediate(overlay);
            }
        }

        [Test]
        public void DXM06_U02_CodexUsesDeckPreviewTemplatesAndSixColumnGrid()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            CodexOverlayView view = prefab.GetComponent<CodexOverlayView>();
            DeckPreviewCardView contractTemplate =
                GetReference<DeckPreviewCardView>(
                    view,
                    "contractTemplate");
            DeckPreviewCardView deckTemplate =
                GetReference<DeckPreviewCardView>(view, "deckTemplate");

            Assert.That(
                AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        contractTemplate.gameObject)),
                Is.EqualTo(DeckPreviewCardPrefabPath));
            Assert.That(
                AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromSource(
                        deckTemplate.gameObject)),
                Is.EqualTo(DeckPreviewCardPrefabPath));

            Transform contractGrid = GetReference<Transform>(
                view,
                "contractGrid");
            GridLayoutGroup grid = contractGrid.GetComponent<GridLayoutGroup>();
            Assert.That(grid, Is.Not.Null);
            Assert.That(
                grid.constraint,
                Is.EqualTo(GridLayoutGroup.Constraint.FixedColumnCount));
            Assert.That(grid.constraintCount, Is.EqualTo(6));
            Assert.That(grid.cellSize, Is.EqualTo(new Vector2(70f, 112f)));
            Assert.That(grid.spacing, Is.EqualTo(new Vector2(7f, 0f)));
            Assert.That(grid.padding.left, Is.EqualTo(4));
            Assert.That(grid.padding.right, Is.EqualTo(4));
        }

        [Test]
        public void DXM06_U03_CodexCardRenderingKeepsDeckCountAndHidesContractChrome()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(
                DeckPreviewCardPrefabPath);
            GameObject instance = UnityEngine.Object.Instantiate(source);
            DeckPreviewCardView card = instance.GetComponent<DeckPreviewCardView>();
            Component fallback = GetReference<Component>(card, "fallbackText");
            Component count = GetReference<Component>(card, "countText");
            GameObject hover = GetReference<GameObject>(card, "hoverFrame");
            GameObject selected = GetReference<GameObject>(card, "selectedFrame");

            try
            {
                card.RenderCodex(
                    null,
                    3,
                    "3. Test Card",
                    "Test description");
                Assert.That(count.gameObject.activeSelf, Is.True);
                Assert.That(GetText(count), Is.EqualTo("x3"));
                Assert.That(fallback.gameObject.activeSelf, Is.False);
                Assert.That(GetText(fallback), Is.Empty);
                Assert.That(card.CreateHoverBadgeRequest(), Is.Not.Null);
                Assert.That(hover.activeSelf, Is.False);
                Assert.That(selected.activeSelf, Is.False);

                card.RenderCodex(null, null, null, null);
                Assert.That(count.gameObject.activeSelf, Is.False);
                Assert.That(GetText(count), Is.Empty);
                Assert.That(fallback.gameObject.activeSelf, Is.False);
                Assert.That(card.CreateHoverBadgeRequest(), Is.Null);
                Assert.That(card.CanSelect, Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void DXM06_U04_PageTurnSpritesAndFrameDirectionsAreStable()
        {
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            CodexOverlayView view = prefab.GetComponent<CodexOverlayView>();
            CanvasGroup group = GetReference<CanvasGroup>(
                view,
                "bookContentGroup");
            Image openBook = GetReference<Image>(view, "openBookImage");
            SerializedObject serialized = new SerializedObject(view);
            SerializedProperty frames = serialized.FindProperty(
                "pageTurnFrames");

            Assert.That(group.transform.name, Is.EqualTo("Book"));
            Assert.That(openBook.transform.name, Is.EqualTo("OpenBook"));
            Assert.That(openBook.transform.IsChildOf(group.transform), Is.False);
            Assert.That(frames.arraySize, Is.EqualTo(5));
            Assert.That(
                serialized.FindProperty("contentFadeDuration").floatValue,
                Is.EqualTo(0.12f));
            Assert.That(
                serialized.FindProperty("pageTurnFrameDuration").floatValue,
                Is.EqualTo(0.08f));
            for (int index = 0; index < frames.arraySize; index++)
            {
                Sprite sprite = frames.GetArrayElementAtIndex(index)
                    .objectReferenceValue as Sprite;
                Assert.That(sprite, Is.Not.Null);
                Assert.That(
                    AssetDatabase.GetAssetPath(sprite),
                    Is.EqualTo($"{OpenBookRemakePathPrefix}{index}.png"));
            }

            Assert.That(
                CodexPageTurnSequence.GetFrames(
                    CodexPageTurnDirection.Next),
                Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(
                CodexPageTurnSequence.GetFrames(
                    CodexPageTurnDirection.Previous),
                Is.EqualTo(new[] { 4, 3, 2, 1, 0 }));
        }

        [Test]
        public void DXM06_U05_BossSixContractsFitOneGridRow()
        {
            CardContentCatalogSO cardCatalog = LoadCardCatalog();
            IReadOnlyList<EnemyCodexPageViewModel> pages =
                CreateEnemyPages(cardCatalog);
            EnemyCodexPageViewModel bossPage = pages[pages.Count - 1];
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            CodexOverlayView view = prefab.GetComponent<CodexOverlayView>();
            Transform contractGrid = GetReference<Transform>(
                view,
                "contractGrid");
            GridLayoutGroup grid = contractGrid.GetComponent<GridLayoutGroup>();

            Assert.That(bossPage.ContractableDemons.Count, Is.EqualTo(6));
            Assert.That(grid.constraintCount, Is.EqualTo(6));
            Assert.That(
                Mathf.CeilToInt(
                    bossPage.ContractableDemons.Count /
                    (float)grid.constraintCount),
                Is.EqualTo(1));
        }

        [Test]
        public void DX00_U01_ContentCatalogRejectsNullEnemy()
        {
            AssertInvalidEnemyCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "enemies");
                entries.GetArrayElementAtIndex(0).objectReferenceValue = null;
            });
        }

        [Test]
        public void DX00_U02_ContentCatalogRejectsDuplicateEnemyKey()
        {
            AssertInvalidEnemyCatalog(catalog =>
            {
                SerializedProperty entries = catalog.FindProperty(
                    "enemies");
                entries.GetArrayElementAtIndex(1).objectReferenceValue =
                    entries.GetArrayElementAtIndex(0).objectReferenceValue;
            });
        }

        [Test]
        public void DX00_U03_ContentCatalogRejectsEmptyLore()
        {
            CardContentCatalogSO catalog = UnityEngine.Object.Instantiate(
                LoadCardCatalog());
            catalog.hideFlags = HideFlags.HideAndDontSave;
            DemonCardDefinitionSO demon = null;
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                SerializedProperty entries = serialized.FindProperty(
                    "demonCards");
                demon = UnityEngine.Object.Instantiate(
                    entries.GetArrayElementAtIndex(0).objectReferenceValue as
                        DemonCardDefinitionSO);
                demon.hideFlags = HideFlags.HideAndDontSave;
                SerializedObject demonData = new SerializedObject(demon);
                demonData.FindProperty("codexLoreDescription").stringValue =
                    " ";
                demonData.ApplyModifiedPropertiesWithoutUndo();
                entries.GetArrayElementAtIndex(0).objectReferenceValue = demon;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.Throws<InvalidOperationException>(() =>
                    catalog.BuildDemonLoreCatalog());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(demon);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void DX00_U04_ContentCatalogRejectsMissingPortrait()
        {
            EnemyContentCatalogSO catalog = UnityEngine.Object.Instantiate(
                LoadEnemyCatalog());
            catalog.hideFlags = HideFlags.HideAndDontSave;
            EnemyCombatProfileDefinitionSO enemy = null;
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                SerializedProperty entries = serialized.FindProperty(
                    "enemies");
                enemy = UnityEngine.Object.Instantiate(
                    entries.GetArrayElementAtIndex(0).objectReferenceValue as
                        EnemyCombatProfileDefinitionSO);
                enemy.hideFlags = HideFlags.HideAndDontSave;
                SerializedObject enemyData = new SerializedObject(enemy);
                enemyData.FindProperty("portrait").objectReferenceValue = null;
                enemyData.ApplyModifiedPropertiesWithoutUndo();
                entries.GetArrayElementAtIndex(0).objectReferenceValue = enemy;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(
                    () => catalog.ValidateOrThrow(),
                    Throws.Exception);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy);
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void DX03_U01_ControllerOpensClosesAndCleansState()
        {
            GameObject overlayPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(overlayPrefab);
            InvokeLifecycle(
                overlay.GetComponent<CodexOverlayView>(),
                "Awake");
            GameObject controllerObject = new GameObject("CodexControllerTest");
            controllerObject.SetActive(false);
            GameObject book = new GameObject("CodexBookTest");
            CodexController controller =
                controllerObject.AddComponent<CodexController>();
            SerializedObject serialized = new SerializedObject(controller);
            serialized.FindProperty("view").objectReferenceValue =
                overlay.GetComponent<CodexOverlayView>();
            serialized.FindProperty("cardContentCatalog")
                .objectReferenceValue = LoadCardCatalog();
            serialized.FindProperty("enemyContentCatalog")
                .objectReferenceValue = LoadEnemyCatalog();
            serialized.FindProperty("tableBookRoot").objectReferenceValue =
                book;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            controllerObject.SetActive(true);
            InvokeLifecycle(controller, "Awake");
            InvokeLifecycle(controller, "OnEnable");

            Assert.That(controller.Open(), Is.True);
            Assert.That(controller.IsOpen, Is.True);
            Assert.That(
                overlay.GetComponent<Canvas>().enabled,
                Is.True);
            Assert.That(book.activeSelf, Is.False);

            controller.Close();
            Assert.That(book.activeSelf, Is.True);
            Assert.That(controller.Open(), Is.True);

            controller.SetAvailable(false);
            Assert.That(controller.IsOpen, Is.False);
            Assert.That(controller.IsAvailable, Is.False);
            Assert.That(book.activeSelf, Is.False);

            UnityEngine.Object.DestroyImmediate(controllerObject);
            UnityEngine.Object.DestroyImmediate(overlay);
            UnityEngine.Object.DestroyImmediate(book);
        }

        [Test]
        public void DX03_U02_TransientCloseConsumesCodexBeforePause()
        {
            GameObject overlayPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(OverlayPrefabPath);
            GameObject overlay = UnityEngine.Object.Instantiate(overlayPrefab);
            InvokeLifecycle(
                overlay.GetComponent<CodexOverlayView>(),
                "Awake");
            GameObject controllerObject = new GameObject(
                "CodexControllerTest");
            controllerObject.SetActive(false);
            GameObject managerObject = new GameObject("GameManagerTest");
            managerObject.SetActive(false);
            GameObject book = new GameObject("CodexBookTest");
            CodexController controller =
                controllerObject.AddComponent<CodexController>();
            GameManager manager = managerObject.AddComponent<GameManager>();
            SerializedObject controllerData = new SerializedObject(controller);
            controllerData.FindProperty("view").objectReferenceValue =
                overlay.GetComponent<CodexOverlayView>();
            controllerData.FindProperty("cardContentCatalog")
                .objectReferenceValue = LoadCardCatalog();
            controllerData.FindProperty("enemyContentCatalog")
                .objectReferenceValue = LoadEnemyCatalog();
            controllerData.FindProperty("tableBookRoot")
                .objectReferenceValue = book;
            controllerData.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject managerData = new SerializedObject(manager);
            managerData.FindProperty("codex").objectReferenceValue =
                controller;
            managerData.ApplyModifiedPropertiesWithoutUndo();

            controllerObject.SetActive(true);
            InvokeLifecycle(controller, "Awake");
            InvokeLifecycle(controller, "OnEnable");
            Assert.That(controller.Open(), Is.True);
            Assert.That(manager.TryCloseTransientOverlay(), Is.True);
            Assert.That(controller.IsOpen, Is.False);

            UnityEngine.Object.DestroyImmediate(controllerObject);
            UnityEngine.Object.DestroyImmediate(managerObject);
            UnityEngine.Object.DestroyImmediate(overlay);
            UnityEngine.Object.DestroyImmediate(book);
        }

        private static void AssertInvalidEnemyCatalog(
            Action<SerializedObject> mutate)
        {
            EnemyContentCatalogSO catalog = UnityEngine.Object.Instantiate(
                LoadEnemyCatalog());
            catalog.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                SerializedObject serialized = new SerializedObject(catalog);
                mutate(serialized);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(
                    () => catalog.ValidateOrThrow(),
                    Throws.Exception);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(catalog);
            }
        }

        private static CardContentCatalogSO LoadCardCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(
                CardCatalogPath);
        }

        private static EnemyContentCatalogSO LoadEnemyCatalog()
        {
            return AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                EnemyCatalogPath);
        }

        private static IReadOnlyList<EnemyCodexPageViewModel> CreateEnemyPages(
            CardContentCatalogSO cardCatalog)
        {
            EnemyContentCatalogSO enemyCatalog = LoadEnemyCatalog();
            return CodexPresenter.CreateEnemyPages(
                enemyCatalog.BuildRuntimeCatalog(),
                enemyCatalog.BuildGoldRewardCatalog(),
                cardCatalog.BuildRuntimeCatalog());
        }

        private static int FindEnemyPageIndex(
            IReadOnlyList<EnemyCodexPageViewModel> pages,
            bool hasContracts)
        {
            for (int index = 0; index < pages.Count; index++)
            {
                if ((pages[index].ContractableDemons.Count > 0) ==
                    hasContracts)
                {
                    return index;
                }
            }

            Assert.Fail(
                hasContracts
                    ? "No enemy page has contractable demons."
                    : "No enemy page has an empty contract list.");
            return -1;
        }

        private static int FindLongestDescriptionIndex(
            IReadOnlyList<EnemyCodexPageViewModel> pages)
        {
            int longestIndex = 0;
            for (int index = 1; index < pages.Count; index++)
            {
                if (pages[index].Description.Length >
                    pages[longestIndex].Description.Length)
                {
                    longestIndex = index;
                }
            }

            return longestIndex;
        }

        private static void MovePreviewToIndex(
            CodexOverlayView view,
            int targetIndex)
        {
            CodexOverlayPreviewSession session =
                CodexOverlayPreviewSession.GetActive(view);
            while (session.CurrentPageIndex < targetIndex)
            {
                Assert.That(
                    CodexOverlayPreviewSession.MoveNext(view),
                    Is.Null);
            }

            while (session.CurrentPageIndex > targetIndex)
            {
                Assert.That(
                    CodexOverlayPreviewSession.MovePrevious(view),
                    Is.Null);
            }
        }

        private static T GetReference<T>(
            UnityEngine.Object target,
            string propertyName)
            where T : UnityEngine.Object
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            Assert.That(
                property,
                Is.Not.Null,
                $"Serialized property '{propertyName}' is missing.");
            T value = property.objectReferenceValue as T;
            Assert.That(
                value,
                Is.Not.Null,
                $"Serialized reference '{propertyName}' is missing.");
            return value;
        }

        private static string GetText(Component textComponent)
        {
            SerializedObject serialized = new SerializedObject(textComponent);
            SerializedProperty text = serialized.FindProperty("m_text");
            Assert.That(text, Is.Not.Null);
            return text.stringValue;
        }

        private static Image GetImageAtPath(
            GameObject root,
            string path)
        {
            Transform target = root.transform.Find(path);
            Assert.That(target, Is.Not.Null, $"'{path}' is missing.");
            Image image = target.GetComponent<Image>();
            Assert.That(image, Is.Not.Null, $"'{path}' requires an Image.");
            return image;
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            foreach (UnityEngine.Object asset in
                AssetDatabase.LoadAllAssetsAtPath(path))
            {
                Sprite sprite = asset as Sprite;
                if (sprite != null && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            Assert.Fail($"Sprite '{spriteName}' is missing at '{path}'.");
            return null;
        }

        private static bool IsAnchorPreset(RectTransform rectTransform)
        {
            return IsAnchorPresetAxis(
                    rectTransform.anchorMin.x,
                    rectTransform.anchorMax.x) &&
                IsAnchorPresetAxis(
                    rectTransform.anchorMin.y,
                    rectTransform.anchorMax.y);
        }

        private static bool IsAnchorPresetAxis(float minimum, float maximum)
        {
            bool fixedAnchor = Mathf.Approximately(minimum, maximum) &&
                (Mathf.Approximately(minimum, 0f) ||
                    Mathf.Approximately(minimum, 0.5f) ||
                    Mathf.Approximately(minimum, 1f));
            bool stretchAnchor = Mathf.Approximately(minimum, 0f) &&
                Mathf.Approximately(maximum, 1f);
            return fixedAnchor || stretchAnchor;
        }

        private static void InvokeLifecycle(
            MonoBehaviour target,
            string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(target, null);
        }
    }
}
