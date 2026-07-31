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
        [SerializeField] private CodexContentCatalogSO codexContentCatalog;
        [SerializeField] private GameObject tableBookRoot;

        private IReadOnlyList<EnemyCodexPageViewModel> _enemyPages;
        private IReadOnlyList<DemonCodexPageViewModel> _demonPages;
        private CodexNavigationState _navigation;
        private bool _available = true;

        public event Action<bool> OpenStateChanged;

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
        }

        private void OnDisable()
        {
            if (view != null)
            {
                view.CloseRequested -= Close;
                view.CategoryRequested -= ShowCategory;
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
            if (!IsOpen || !_navigation.TryMovePrevious())
            {
                return false;
            }

            view.Render(CreateCurrentBook());
            return true;
        }

        public bool NextPage()
        {
            if (!IsOpen || !_navigation.TryMoveNext())
            {
                return false;
            }

            view.Render(CreateCurrentBook());
            return true;
        }

        public void ShowCategory(CodexCategory category)
        {
            if (!IsOpen || !_navigation.TryShowCategory(category))
            {
                return;
            }

            view.Render(CreateCurrentBook());
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

        private void BuildModels()
        {
            if (cardContentCatalog == null)
            {
                throw new MissingReferenceException(
                    "CodexController requires CardContentCatalogSO.");
            }

            if (codexContentCatalog == null)
            {
                throw new MissingReferenceException(
                    "CodexController requires CodexContentCatalogSO.");
            }

            if (view == null)
            {
                throw new MissingReferenceException(
                    "CodexController requires CodexOverlayView.");
            }

            CardContentCatalog runtimeCards =
                cardContentCatalog.BuildRuntimeCatalog();
            IReadOnlyDictionary<string, string> lore =
                codexContentCatalog.BuildDemonLoreCatalog(runtimeCards);
            _enemyPages = CodexPresenter.CreateEnemyPages(
                EnemyCombatProfileCatalog.Default,
                GoldRewardCatalog.CreatePrototype(),
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
            view.Configure(cardContentCatalog, codexContentCatalog);
        }

        private CodexBookViewModel CreateCurrentBook()
        {
            return CodexPresenter.CreateBook(
                _navigation,
                _enemyPages,
                _demonPages);
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
