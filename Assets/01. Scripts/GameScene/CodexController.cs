using System;
using System.Collections.Generic;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class CodexController : MonoBehaviour
    {
        [SerializeField] private CodexOverlayView view;
        [SerializeField] private CardContentCatalogSO cardContentCatalog;
        [SerializeField] private EnemyContentCatalogSO enemyContentCatalog;
        [SerializeField] private GameObject tableBookRoot;

        private IReadOnlyList<EnemyCodexPageViewModel> _enemyPages;
        private IReadOnlyList<DemonCodexPageViewModel> _demonPages;
        private CodexNavigationState _navigation;
        private bool _available = true;

        public event Action<bool> OpenStateChanged;

        public event Action<CardHoverBadgeRequest> HoverBadgeRequested;

        public event Action HoverBadgeCleared;

        public CodexCategory CurrentCategory =>
            _navigation == null
                ? CodexCategory.Enemy
                : _navigation.Category;

        public bool IsAvailable => _available;

        public bool IsOpen => view != null && view.IsOpen;

        private void Awake()
        {
            EnsureInitialized();
            view.Close();
            SetBookVisible(_available);
        }

        private void OnEnable()
        {
            if (view == null)
            {
                return;
            }

            view.CloseRequested += Close;
            view.CategoryRequested += ShowCategory;
            view.DemonPageRequested += HandleDemonPageRequested;
            view.HoverBadgeRequested += HandleHoverBadgeRequested;
            view.HoverBadgeCleared += HandleHoverBadgeCleared;
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.CloseRequested -= Close;
                view.CategoryRequested -= ShowCategory;
                view.DemonPageRequested -= HandleDemonPageRequested;
                view.HoverBadgeRequested -= HandleHoverBadgeRequested;
                view.HoverBadgeCleared -= HandleHoverBadgeCleared;
            }

            Close();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                PreviousPage();
            }
            else if (keyboard.eKey.wasPressedThisFrame)
            {
                NextPage();
            }
        }

        public bool Open()
        {
            if (!_available || IsOpen)
            {
                return false;
            }

            EnsureInitialized();
            view.Open(CreateCurrentBook());
            SetBookVisible(false);
            OpenStateChanged?.Invoke(true);
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            view.Close();
            SetBookVisible(_available);
            OpenStateChanged?.Invoke(false);
        }

        public bool PreviousPage()
        {
            if (!IsOpen ||
                view.IsTransitioning ||
                !_navigation.TryMovePrevious())
            {
                return false;
            }

            return view.TryRenderTransition(
                CreateCurrentBook(),
                CodexPageTurnDirection.Previous);
        }

        public bool NextPage()
        {
            if (!IsOpen ||
                view.IsTransitioning ||
                !_navigation.TryMoveNext())
            {
                return false;
            }

            return view.TryRenderTransition(
                CreateCurrentBook(),
                CodexPageTurnDirection.Next);
        }

        public void ShowCategory(CodexCategory category)
        {
            if (!IsOpen ||
                view.IsTransitioning ||
                !_navigation.TryShowCategory(category))
            {
                return;
            }

            CodexPageTurnDirection direction =
                category == CodexCategory.DemonCard
                    ? CodexPageTurnDirection.Next
                    : CodexPageTurnDirection.Previous;
            view.TryRenderTransition(CreateCurrentBook(), direction);
        }

        public void SetAvailable(bool available)
        {
            _available = available;
            if (!available)
            {
                Close();
            }

            SetBookVisible(available);
        }

        internal bool TryShowDemonPage(string definitionKey)
        {
            if (!IsOpen ||
                view.IsTransitioning ||
                string.IsNullOrEmpty(definitionKey))
            {
                return false;
            }

            int pageIndex = -1;
            for (int index = 0; index < _demonPages.Count; index++)
            {
                if (_demonPages[index].DefinitionKey == definitionKey)
                {
                    pageIndex = index;
                    break;
                }
            }

            if (pageIndex < 0 || !_navigation.TryShowDemonPage(pageIndex))
            {
                return false;
            }

            return view.TryRenderTransition(
                CreateCurrentBook(),
                CodexPageTurnDirection.Next);
        }

        private void BuildModels()
        {
            if (cardContentCatalog == null)
            {
                throw new MissingReferenceException(
                    "CodexController requires CardContentCatalogSO.");
            }

            if (enemyContentCatalog == null)
            {
                throw new MissingReferenceException(
                    "CodexController requires EnemyContentCatalogSO.");
            }

            if (view == null)
            {
                throw new MissingReferenceException(
                    "CodexController requires CodexOverlayView.");
            }

            CardContentCatalog runtimeCards =
                cardContentCatalog.BuildRuntimeCatalog();
            IReadOnlyDictionary<string, string> lore =
                cardContentCatalog.BuildDemonLoreCatalog();
            EnemyCombatProfileCatalog runtimeEnemies =
                enemyContentCatalog.BuildRuntimeCatalog();
            GoldRewardCatalog runtimeGold =
                enemyContentCatalog.BuildGoldRewardCatalog();
            _enemyPages = CodexPresenter.CreateEnemyPages(
                runtimeEnemies,
                runtimeGold,
                runtimeCards);
            _demonPages = CodexPresenter.CreateDemonPages(
                runtimeCards,
                lore);
            _navigation = new CodexNavigationState(
                _enemyPages.Count,
                _demonPages.Count);
        }

        private void EnsureInitialized()
        {
            if (_navigation != null)
            {
                return;
            }

            view ??= FindFirstObjectByType<CodexOverlayView>(
                FindObjectsInactive.Include);
            BuildModels();
            view.Configure(cardContentCatalog, enemyContentCatalog);
        }

        private CodexBookViewModel CreateCurrentBook()
        {
            return CodexPresenter.CreateBook(
                _navigation,
                _enemyPages,
                _demonPages);
        }

        private void HandleHoverBadgeRequested(CardHoverBadgeRequest request)
        {
            HoverBadgeRequested?.Invoke(request);
        }

        private void HandleDemonPageRequested(string definitionKey)
        {
            TryShowDemonPage(definitionKey);
        }

        private void HandleHoverBadgeCleared()
        {
            HoverBadgeCleared?.Invoke();
        }

        private void SetBookVisible(bool visible)
        {
            if (tableBookRoot != null)
            {
                tableBookRoot.SetActive(visible);
            }
        }
    }
}
