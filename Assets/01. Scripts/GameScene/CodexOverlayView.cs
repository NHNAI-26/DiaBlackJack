using System;
using System.Collections;
using System.Collections.Generic;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    internal enum CodexPageTurnDirection
    {
        Previous,
        Next
    }

    internal static class CodexPageTurnSequence
    {
        private static readonly int[] PreviousFrames = { 4, 3, 2, 1, 0 };
        private static readonly int[] NextFrames = { 0, 1, 2, 3, 4 };

        internal static IReadOnlyList<int> GetFrames(
            CodexPageTurnDirection direction)
        {
            return direction switch
            {
                CodexPageTurnDirection.Previous => PreviousFrames,
                CodexPageTurnDirection.Next => NextFrames,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }
    }

    [DisallowMultipleComponent]
    public sealed class CodexOverlayView : MonoBehaviour
    {
        private const int RequiredPageTurnFrameCount = 5;

        [Header("Overlay")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private GraphicRaycaster overlayRaycaster;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button enemyTabButton;
        [SerializeField] private Button demonTabButton;
        [SerializeField] private Image enemyTabImage;
        [SerializeField] private Image demonTabImage;
        [SerializeField] private TMP_Text enemyTabText;
        [SerializeField] private TMP_Text demonTabText;
        [SerializeField] private TMP_Text previousPageText;
        [SerializeField] private TMP_Text nextPageText;

        [Header("Page turn")]
        [SerializeField] private Image openBookImage;
        [SerializeField] private CanvasGroup bookContentGroup;
        [SerializeField] private Sprite[] pageTurnFrames = Array.Empty<Sprite>();
        [Min(0f)]
        [SerializeField] private float contentFadeDuration = 0.12f;
        [Min(0f)]
        [SerializeField] private float pageTurnFrameDuration = 0.08f;

        [Header("Enemy page")]
        [SerializeField] private GameObject enemyPageRoot;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private Image enemyPortraitImage;
        [SerializeField] private TMP_Text enemySoulText;
        [SerializeField] private TMP_Text enemyGoldText;
        [SerializeField] private TMP_Text enemyDescriptionText;
        [SerializeField] private TMP_Text noContractText;
        [SerializeField] private Transform contractGrid;
        [SerializeField] private CodexDemonCardPreviewView contractTemplate;
        [SerializeField] private ScrollRect deckScrollRect;
        [SerializeField] private Transform deckGrid;
        [SerializeField] private DeckPreviewCardView deckTemplate;

        [Header("Demon page")]
        [SerializeField] private GameObject demonPageRoot;
        [SerializeField] private TMP_Text demonNameText;
        [SerializeField] private Image demonCardImage;
        [SerializeField] private TMP_Text demonGoldText;
        [SerializeField] private TMP_Text demonSoulText;
        [SerializeField] private TMP_Text demonLoreText;
        [SerializeField] private TMP_Text demonActiveSkillText;
        [SerializeField] private TMP_Text demonCostText;

        [Header("Tab colors")]
        [SerializeField] private Color activeTabColor = Color.white;
        [SerializeField] private Color inactiveTabColor =
            new Color(0.55f, 0.42f, 0.38f, 1f);

        private readonly List<CodexDemonCardPreviewView> _contractItems =
            new List<CodexDemonCardPreviewView>();
        private readonly List<DeckPreviewCardView> _deckItems =
            new List<DeckPreviewCardView>();
        private CardContentCatalogSO _cardContentCatalog;
        private EnemyContentCatalogSO _enemyContentCatalog;
        private CardContentCatalog _runtimeCardCatalog;
        private DeckPreviewCardView _hoveredDeckItem;
        private Coroutine _pageTransition;
        private bool _controlsBound;

        public event Action CloseRequested;

        public event Action<CodexCategory> CategoryRequested;

        public event Action<CardHoverBadgeRequest> HoverBadgeRequested;

        public event Action HoverBadgeCleared;

        public bool IsOpen { get; private set; }

        internal bool IsTransitioning => _pageTransition != null;

        private void Awake()
        {
            BindControls();
            ApplyVisibility(false);
            SetTemplatesVisible(false);
            ResetTransitionVisuals();
        }

        private void OnDisable()
        {
            CancelPageTransition();
            ClearHoveredDeckItem();
        }

        private void OnDestroy()
        {
            CancelPageTransition();
            ClearHoveredDeckItem();
            UnbindControls();
        }

        public void Configure(
            CardContentCatalogSO cardContentCatalog,
            EnemyContentCatalogSO enemyContentCatalog)
        {
            _cardContentCatalog = cardContentCatalog ??
                throw new ArgumentNullException(nameof(cardContentCatalog));
            _enemyContentCatalog = enemyContentCatalog ??
                throw new ArgumentNullException(nameof(enemyContentCatalog));
            _runtimeCardCatalog = _cardContentCatalog.BuildRuntimeCatalog();
            _enemyContentCatalog.ValidateOrThrow();
        }

        public void Open(CodexBookViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            EnsureConfigured();
            CancelPageTransition();
            IsOpen = true;
            ApplyVisibility(true);
            RenderImmediate(model);
        }

        public void Close()
        {
            CancelPageTransition();
            IsOpen = false;
            ClearSpawnedItems(_contractItems);
            ClearDeckItems();
            ApplyVisibility(false);
        }

        public void Render(CodexBookViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            EnsureConfigured();
            CancelPageTransition();
            RenderImmediate(model);
        }

        internal bool TryRenderTransition(
            CodexBookViewModel model,
            CodexPageTurnDirection direction)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            EnsureConfigured();
            if (!IsOpen || IsTransitioning)
            {
                return false;
            }

            ClearHoveredDeckItem();
            _pageTransition = StartCoroutine(
                RenderTransition(model, direction));
            return true;
        }

#if UNITY_EDITOR
        internal void RenderEditorPreview(CodexBookViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            EnsureConfigured();
            CancelPageTransition();
            ClearSpawnedItems(_contractItems);
            ClearDeckItems();
            bool showEnemy = RenderBookFrame(model);
            if (showEnemy)
            {
                RenderEnemyPreview(model.EnemyPage);
            }
            else
            {
                SetTemplatesVisible(false);
                RenderDemon(model.DemonPage);
            }
        }
#endif

        private IEnumerator RenderTransition(
            CodexBookViewModel model,
            CodexPageTurnDirection direction)
        {
            SetContentInteraction(false);
            yield return FadeContent(1f, 0f);

            IReadOnlyList<int> frameIndices =
                CodexPageTurnSequence.GetFrames(direction);
            foreach (int frameIndex in frameIndices)
            {
                openBookImage.sprite = pageTurnFrames[frameIndex];
                yield return WaitUnscaled(pageTurnFrameDuration);
            }

            RenderImmediate(model);
            SetRestingBookFrame();
            yield return FadeContent(0f, 1f);
            _pageTransition = null;
            SetContentInteraction(true);
        }

        private IEnumerator FadeContent(float from, float to)
        {
            if (contentFadeDuration <= 0f)
            {
                bookContentGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;
            bookContentGroup.alpha = from;
            while (elapsed < contentFadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                bookContentGroup.alpha = Mathf.Lerp(
                    from,
                    to,
                    Mathf.Clamp01(elapsed / contentFadeDuration));
                yield return null;
            }

            bookContentGroup.alpha = to;
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void RenderImmediate(CodexBookViewModel model)
        {
            ClearHoveredDeckItem();
            bool showEnemy = RenderBookFrame(model);
            if (showEnemy)
            {
                RenderEnemy(model.EnemyPage);
            }
            else
            {
                RenderDemon(model.DemonPage);
            }
        }

        private bool RenderBookFrame(CodexBookViewModel model)
        {
            bool showEnemy = model.Category == CodexCategory.Enemy;
            enemyPageRoot?.SetActive(showEnemy);
            demonPageRoot?.SetActive(!showEnemy);

            SetGraphicColor(
                enemyTabImage,
                showEnemy ? activeTabColor : inactiveTabColor);
            SetGraphicColor(
                demonTabImage,
                showEnemy ? inactiveTabColor : activeTabColor);
            SetGraphicColor(
                enemyTabText,
                showEnemy ? activeTabColor : inactiveTabColor);
            SetGraphicColor(
                demonTabText,
                showEnemy ? inactiveTabColor : activeTabColor);

            ApplyTabInteraction(showEnemy, !IsTransitioning);

            SetText(previousPageText, "Q Previous");
            SetText(
                nextPageText,
                $"{model.PageIndex + 1}/{model.PageCount} Next E");
            return showEnemy;
        }

        private void RenderEnemy(EnemyCodexPageViewModel page)
        {
            RenderEnemyDetails(page);

            ClearSpawnedItems(_contractItems);
            bool hasContracts = page.ContractableDemons.Count > 0;
            RenderNoContractMessage(hasContracts);
            foreach (CodexDemonReferenceViewModel demon in
                page.ContractableDemons)
            {
                CodexDemonCardPreviewView item = CreateItem(
                    contractTemplate,
                    contractGrid,
                    _contractItems);
                RenderDemonThumbnail(item, demon);
            }

            ClearDeckItems();
            foreach (CodexDeckCardViewModel card in page.StartingDeck)
            {
                DeckPreviewCardView item = CreateItem(
                    deckTemplate,
                    deckGrid,
                    _deckItems);
                item.HoverChanged += HandleDeckItemHoverChanged;
                RenderDeckThumbnail(item, card);
            }

            ResetDeckScroll();
        }

        private void RenderEnemyDetails(EnemyCodexPageViewModel page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            SetText(enemyNameText, page.DisplayName);
            SetText(enemySoulText, page.MaximumSoul.ToString());
            SetText(enemyGoldText, page.DefeatGold.ToString());
            SetText(enemyDescriptionText, page.Description);

            if (enemyPortraitImage != null)
            {
                enemyPortraitImage.sprite =
                    _enemyContentCatalog.GetPortrait(page.ProfileKey);
                enemyPortraitImage.enabled =
                    enemyPortraitImage.sprite != null;
            }
        }

        private void RenderNoContractMessage(bool hasContracts)
        {
            if (noContractText == null)
            {
                return;
            }

            noContractText.gameObject.SetActive(!hasContracts);
            noContractText.text = "계약 가능한 악마 없음";
        }

        private void RenderDemonThumbnail(
            CodexDemonCardPreviewView target,
            CodexDemonReferenceViewModel demon)
        {
            target.Render(
                _cardContentCatalog.GetDemonFaceSprite(
                    demon.DefinitionKey),
                demon.EnglishName);
        }

        private void RenderDeckThumbnail(
            DeckPreviewCardView target,
            CodexDeckCardViewModel card)
        {
            target.RenderCodex(
                _cardContentCatalog.GetNormalFaceSprite(
                    card.DefinitionKey,
                    card.Suit),
                card.Count,
                $"{card.Rank}. {card.DisplayName}",
                card.Description,
                GameSceneCardViewModel.ResolveHoverOutlineState(
                    card.DefinitionKey,
                    canUse: false,
                    isUsed: false));
        }

#if UNITY_EDITOR
        private void RenderEnemyPreview(EnemyCodexPageViewModel page)
        {
            RenderEnemyDetails(page);

            bool hasContracts = page.ContractableDemons.Count > 0;
            RenderNoContractMessage(hasContracts);
            if (contractTemplate != null)
            {
                contractTemplate.gameObject.SetActive(hasContracts);
                if (hasContracts)
                {
                    RenderDemonThumbnail(
                        contractTemplate,
                        page.ContractableDemons[0]);
                }
            }

            bool hasDeck = page.StartingDeck.Count > 0;
            if (deckTemplate != null)
            {
                deckTemplate.gameObject.SetActive(hasDeck);
                if (hasDeck)
                {
                    RenderDeckThumbnail(
                        deckTemplate,
                        page.StartingDeck[0]);
                }
            }
        }
#endif

        private void RenderDemon(DemonCodexPageViewModel page)
        {
            if (page == null)
            {
                throw new ArgumentNullException(nameof(page));
            }

            SetText(demonNameText, page.DisplayName);
            SetText(demonGoldText, $"구매 골드  {page.PurchaseGold}");
            SetText(demonSoulText, $"영혼 가격  {page.SoulPrice}");
            SetText(demonLoreText, page.LoreDescription);
            SetText(
                demonActiveSkillText,
                $"액티브 스킬\n\n{page.ActiveSkill}");
            SetText(demonCostText, $"대가\n\n{page.Cost}");

            if (demonCardImage != null)
            {
                demonCardImage.sprite =
                    _cardContentCatalog.GetDemonFaceSprite(
                        page.DefinitionKey);
                demonCardImage.enabled = demonCardImage.sprite != null;
            }
        }

        private void BindControls()
        {
            if (_controlsBound)
            {
                return;
            }

            _controlsBound = true;
            closeButton?.onClick.AddListener(HandleCloseRequested);
            enemyTabButton?.onClick.AddListener(HandleEnemyTabRequested);
            demonTabButton?.onClick.AddListener(HandleDemonTabRequested);
        }

        private void UnbindControls()
        {
            if (!_controlsBound)
            {
                return;
            }

            _controlsBound = false;
            closeButton?.onClick.RemoveListener(HandleCloseRequested);
            enemyTabButton?.onClick.RemoveListener(HandleEnemyTabRequested);
            demonTabButton?.onClick.RemoveListener(HandleDemonTabRequested);
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleEnemyTabRequested()
        {
            CategoryRequested?.Invoke(CodexCategory.Enemy);
        }

        private void HandleDemonTabRequested()
        {
            CategoryRequested?.Invoke(CodexCategory.DemonCard);
        }

        private void HandleDeckItemHoverChanged(
            DeckPreviewCardView item,
            bool hovered)
        {
            if (!IsOpen)
            {
                return;
            }

            if (hovered)
            {
                _hoveredDeckItem = item;
                CardHoverBadgeRequest request =
                    item.CreateHoverBadgeRequest();
                if (request != null)
                {
                    HoverBadgeRequested?.Invoke(request);
                }

                return;
            }

            if (_hoveredDeckItem == item)
            {
                _hoveredDeckItem = null;
                HoverBadgeCleared?.Invoke();
            }
        }

        private void ClearHoveredDeckItem()
        {
            if (_hoveredDeckItem == null)
            {
                return;
            }

            _hoveredDeckItem = null;
            HoverBadgeCleared?.Invoke();
        }

        private void ClearDeckItems()
        {
            ClearHoveredDeckItem();
            foreach (DeckPreviewCardView item in _deckItems)
            {
                if (item != null)
                {
                    item.HoverChanged -= HandleDeckItemHoverChanged;
                }
            }

            ClearSpawnedItems(_deckItems);
        }

        private void ApplyVisibility(bool visible)
        {
            if (overlayCanvas != null)
            {
                overlayCanvas.enabled = visible;
            }

            if (overlayRaycaster != null)
            {
                overlayRaycaster.enabled = visible;
            }
        }

        private void SetTemplatesVisible(bool visible)
        {
            contractTemplate?.gameObject.SetActive(visible);
            deckTemplate?.gameObject.SetActive(visible);
        }

        private void EnsureConfigured()
        {
            if (_cardContentCatalog == null ||
                _enemyContentCatalog == null ||
                _runtimeCardCatalog == null)
            {
                throw new InvalidOperationException(
                    "CodexOverlayView must be configured before rendering.");
            }

            if (openBookImage == null ||
                bookContentGroup == null ||
                pageTurnFrames == null ||
                pageTurnFrames.Length != RequiredPageTurnFrameCount)
            {
                throw new MissingReferenceException(
                    "Codex page-turn references must contain the book image, " +
                    "content group, and exactly five frames.");
            }

            foreach (Sprite frame in pageTurnFrames)
            {
                if (frame == null)
                {
                    throw new MissingReferenceException(
                        "Codex page-turn frames cannot contain null.");
                }
            }
        }

        private void ResetDeckScroll()
        {
            if (deckScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            deckScrollRect.verticalNormalizedPosition = 1f;
            deckScrollRect.velocity = Vector2.zero;
        }

        private void CancelPageTransition()
        {
            if (_pageTransition != null)
            {
                StopCoroutine(_pageTransition);
                _pageTransition = null;
            }

            ResetTransitionVisuals();
        }

        private void ResetTransitionVisuals()
        {
            SetRestingBookFrame();
            if (bookContentGroup != null)
            {
                bookContentGroup.alpha = 1f;
                SetContentInteraction(true);
            }
        }

        private void SetRestingBookFrame()
        {
            if (openBookImage != null &&
                pageTurnFrames != null &&
                pageTurnFrames.Length > 0 &&
                pageTurnFrames[0] != null)
            {
                openBookImage.sprite = pageTurnFrames[0];
            }
        }

        private void SetContentInteraction(bool enabled)
        {
            if (bookContentGroup != null)
            {
                bookContentGroup.interactable = enabled;
                bookContentGroup.blocksRaycasts = enabled;
            }

            bool showEnemy = enemyPageRoot == null ||
                enemyPageRoot.activeSelf;
            ApplyTabInteraction(showEnemy, enabled);
        }

        private void ApplyTabInteraction(bool showEnemy, bool enabled)
        {
            if (enemyTabButton != null)
            {
                enemyTabButton.interactable = enabled && !showEnemy;
            }

            if (demonTabButton != null)
            {
                demonTabButton.interactable = enabled && showEnemy;
            }
        }

        private static T CreateItem<T>(
            T template,
            Transform parent,
            ICollection<T> items)
            where T : Component
        {
            if (template == null || parent == null)
            {
                throw new MissingReferenceException(
                    "Codex card template or parent is missing.");
            }

            T item =
                Instantiate(template, parent, false);
            item.gameObject.SetActive(true);
            items.Add(item);
            return item;
        }

        private static void ClearSpawnedItems<T>(ICollection<T> items)
            where T : Component
        {
            foreach (T item in items)
            {
                if (item == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(item.gameObject);
                }
                else
                {
                    DestroyImmediate(item.gameObject);
                }
            }

            items.Clear();
        }

        private static void SetGraphicColor(Graphic target, Color value)
        {
            if (target != null)
            {
                target.color = value;
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            CurrencyIconText.Set(target, value);
        }
    }
}
