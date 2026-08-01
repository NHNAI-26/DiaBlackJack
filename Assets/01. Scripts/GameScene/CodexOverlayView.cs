using System;
using System.Collections.Generic;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CodexOverlayView : MonoBehaviour
    {
        [Header("Overlay")]
        [SerializeField] private Canvas overlayCanvas;
        [SerializeField] private GraphicRaycaster overlayRaycaster;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button enemyTabButton;
        [SerializeField] private Button demonTabButton;
        [SerializeField] private Image enemyTabImage;
        [SerializeField] private Image demonTabImage;
        [SerializeField] private TMP_Text pageNumberText;

        [Header("Enemy page")]
        [SerializeField] private GameObject enemyPageRoot;
        [SerializeField] private TMP_Text enemyNameText;
        [SerializeField] private Image enemyPortraitImage;
        [SerializeField] private TMP_Text enemySoulText;
        [SerializeField] private TMP_Text enemyGoldText;
        [SerializeField] private TMP_Text enemyDescriptionText;
        [SerializeField] private TMP_Text noContractText;
        [SerializeField] private Transform contractGrid;
        [SerializeField] private CodexCardThumbnailView contractTemplate;
        [SerializeField] private ScrollRect deckScrollRect;
        [SerializeField] private Transform deckGrid;
        [SerializeField] private CodexCardThumbnailView deckTemplate;

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

        private readonly List<CodexCardThumbnailView> _contractItems =
            new List<CodexCardThumbnailView>();
        private readonly List<CodexCardThumbnailView> _deckItems =
            new List<CodexCardThumbnailView>();
        private CardContentCatalogSO _cardContentCatalog;
        private EnemyContentCatalogSO _enemyContentCatalog;
        private CardContentCatalog _runtimeCardCatalog;
        private bool _controlsBound;

        public event Action CloseRequested;

        public event Action<CodexCategory> CategoryRequested;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            BindControls();
            ApplyVisibility(false);
            SetTemplatesVisible(false);
        }

        private void OnDestroy()
        {
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
            IsOpen = true;
            ApplyVisibility(true);
            Render(model);
        }

        public void Close()
        {
            IsOpen = false;
            ClearSpawnedItems(_contractItems);
            ClearSpawnedItems(_deckItems);
            ApplyVisibility(false);
        }

        public void Render(CodexBookViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            EnsureConfigured();
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

#if UNITY_EDITOR
        internal void RenderEditorPreview(CodexBookViewModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            EnsureConfigured();
            ClearSpawnedItems(_contractItems);
            ClearSpawnedItems(_deckItems);
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

        private bool RenderBookFrame(CodexBookViewModel model)
        {
            bool showEnemy = model.Category == CodexCategory.Enemy;
            if (enemyPageRoot != null)
            {
                enemyPageRoot.SetActive(showEnemy);
            }

            if (demonPageRoot != null)
            {
                demonPageRoot.SetActive(!showEnemy);
            }

            if (enemyTabImage != null)
            {
                enemyTabImage.color =
                    showEnemy ? activeTabColor : inactiveTabColor;
            }

            if (demonTabImage != null)
            {
                demonTabImage.color =
                    showEnemy ? inactiveTabColor : activeTabColor;
            }

            if (enemyTabButton != null)
            {
                enemyTabButton.interactable = !showEnemy;
            }

            if (demonTabButton != null)
            {
                demonTabButton.interactable = showEnemy;
            }

            if (pageNumberText != null)
            {
                pageNumberText.text =
                    $"Q  이전    {model.PageIndex + 1} / {model.PageCount}    다음  E";
            }

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
                CodexCardThumbnailView item = CreateItem(
                    contractTemplate,
                    contractGrid,
                    _contractItems);
                RenderDemonThumbnail(item, demon);
            }

            ClearSpawnedItems(_deckItems);
            foreach (CodexDeckCardViewModel card in page.StartingDeck)
            {
                CodexCardThumbnailView item = CreateItem(
                    deckTemplate,
                    deckGrid,
                    _deckItems);
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
            SetText(enemySoulText, $"영혼  {page.MaximumSoul}");
            SetText(enemyGoldText, $"처치 골드  {page.DefeatGold}");
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
            if (noContractText != null)
            {
                noContractText.gameObject.SetActive(!hasContracts);
                noContractText.text = "계약 가능한 악마 없음";
            }
        }

        private void RenderDemonThumbnail(
            CodexCardThumbnailView target,
            CodexDemonReferenceViewModel demon)
        {
            target.Render(
                demon.DisplayName,
                _cardContentCatalog.GetDemonFaceSprite(
                    demon.DefinitionKey));
        }

        private void RenderDeckThumbnail(
            CodexCardThumbnailView target,
            CodexDeckCardViewModel card)
        {
            string cardName = $"{card.Rank}  {card.DisplayName}";
            target.Render(
                cardName,
                _cardContentCatalog.GetNormalFaceSprite(
                    card.DefinitionKey,
                    card.Suit));
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
            if (contractTemplate != null)
            {
                contractTemplate.gameObject.SetActive(visible);
            }

            if (deckTemplate != null)
            {
                deckTemplate.gameObject.SetActive(visible);
            }
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

        private static CodexCardThumbnailView CreateItem(
            CodexCardThumbnailView template,
            Transform parent,
            ICollection<CodexCardThumbnailView> items)
        {
            if (template == null || parent == null)
            {
                throw new MissingReferenceException(
                    "Codex card template or parent is missing.");
            }

            CodexCardThumbnailView item =
                Instantiate(template, parent, false);
            item.gameObject.SetActive(true);
            items.Add(item);
            return item;
        }

        private static void ClearSpawnedItems(
            ICollection<CodexCardThumbnailView> items)
        {
            foreach (CodexCardThumbnailView item in items)
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

        private static void SetText(TMP_Text target, string value)
        {
            CurrencyIconText.Set(target, value);
        }
    }
}
