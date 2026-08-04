#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(CodexOverlayView))]
    internal sealed class CodexOverlayViewEditor : UnityEditor.Editor
    {
        private string _lastError;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "CODEX PREVIEW",
                EditorStyles.boldLabel);

            CodexOverlayView view = target as CodexOverlayView;
            if (view == null || !view.gameObject.scene.IsValid())
            {
                EditorGUILayout.HelpBox(
                    "Open the prefab in Prefab Mode or select a scene instance.",
                    MessageType.Info);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "Codex preview is available only in Edit Mode.",
                    MessageType.Info);
                return;
            }

            CodexOverlayPreviewSession session =
                CodexOverlayPreviewSession.GetActive(view);
            EditorGUILayout.LabelField(
                "Current",
                session == null
                    ? "Off"
                    : session.CurrentLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Enemy"))
                {
                    RunPreviewAction(() =>
                        CodexOverlayPreviewSession.ShowCategory(
                            view,
                            CodexCategory.Enemy));
                }

                if (GUILayout.Button("Demon"))
                {
                    RunPreviewAction(() =>
                        CodexOverlayPreviewSession.ShowCategory(
                            view,
                            CodexCategory.DemonCard));
                }
            }

            session = CodexOverlayPreviewSession.GetActive(view);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMovePrevious))
                {
                    if (GUILayout.Button("Previous"))
                    {
                        RunPreviewAction(() =>
                            CodexOverlayPreviewSession.MovePrevious(view));
                    }
                }

                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMoveNext))
                {
                    if (GUILayout.Button("Next"))
                    {
                        RunPreviewAction(() =>
                            CodexOverlayPreviewSession.MoveNext(view));
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(session == null))
                {
                    if (GUILayout.Button("Refresh"))
                    {
                        RunPreviewAction(() =>
                            CodexOverlayPreviewSession.Refresh(view));
                    }

                    if (GUILayout.Button("Preview Off"))
                    {
                        CodexOverlayPreviewSession.StopActive();
                        _lastError = null;
                        RepaintPreview();
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Preview fills the existing ContractTemplate and DeckTemplate. " +
                "No preview objects are created. Saving restores authored values " +
                "for serialization, then resumes the current preview.",
                MessageType.None);

            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
            }
        }

        private void RunPreviewAction(Func<string> action)
        {
            _lastError = action();
            RepaintPreview();
        }

        private void RepaintPreview()
        {
            SceneView.RepaintAll();
            Repaint();
        }
    }

    internal sealed class CodexOverlayPreviewSession
    {
        internal const string CardCatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";
        internal const string EnemyCatalogPath =
            "Assets/02. ScriptableObjects/Enemies/EnemyContentCatalog.asset";

        private static CodexOverlayPreviewSession _active;

        private readonly CodexOverlayView _view;
        private readonly CodexOverlayPreviewSnapshot _snapshot;
        private IReadOnlyList<EnemyCodexPageViewModel> _enemyPages;
        private IReadOnlyList<DemonCodexPageViewModel> _demonPages;
        private CodexNavigationState _navigation;
        private CodexCategory _category = CodexCategory.Enemy;
        private int _enemyPageIndex;
        private int _demonPageIndex;

        private CodexOverlayPreviewSession(CodexOverlayView view)
        {
            _view = view;
            _snapshot = CodexOverlayPreviewSnapshot.Capture(view);
        }

        internal string CurrentLabel =>
            $"{(_category == CodexCategory.Enemy ? "Enemy" : "Demon")} " +
            $"{_navigation.CurrentPageIndex + 1} / {_navigation.CurrentPageCount}";

        internal bool CanMovePrevious =>
            _navigation != null &&
            (_navigation.CurrentPageIndex > 0 ||
             _navigation.Category == CodexCategory.DemonCard);

        internal bool CanMoveNext =>
            _navigation != null &&
            (_navigation.CurrentPageIndex + 1 < _navigation.CurrentPageCount ||
             _navigation.Category == CodexCategory.Enemy);

        internal CodexCategory CurrentCategory => _category;

        internal int CurrentPageIndex =>
            _navigation == null ? 0 : _navigation.CurrentPageIndex;

        internal static CodexOverlayPreviewSession GetActive(
            CodexOverlayView view)
        {
            return _active != null && _active._view == view
                ? _active
                : null;
        }

        internal static string ShowCategory(
            CodexOverlayView view,
            CodexCategory category)
        {
            string error = EnsureActive(view);
            if (error != null)
            {
                return error;
            }

            try
            {
                _active._navigation.TryShowCategory(category);
                _active.SyncNavigationState();
                _active.Render();
                return null;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        internal static string MovePrevious(CodexOverlayView view)
        {
            CodexOverlayPreviewSession session = GetActive(view);
            if (session == null)
            {
                return "Start Enemy or Demon preview first.";
            }

            try
            {
                if (session._navigation.TryMovePrevious())
                {
                    session.SyncNavigationState();
                    session.Render();
                }

                return null;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        internal static string MoveNext(CodexOverlayView view)
        {
            CodexOverlayPreviewSession session = GetActive(view);
            if (session == null)
            {
                return "Start Enemy or Demon preview first.";
            }

            try
            {
                if (session._navigation.TryMoveNext())
                {
                    session.SyncNavigationState();
                    session.Render();
                }

                return null;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        internal static string Refresh(CodexOverlayView view)
        {
            CodexOverlayPreviewSession session = GetActive(view);
            if (session == null)
            {
                return "Start Enemy or Demon preview first.";
            }

            try
            {
                session.RebuildModels();
                session.Render();
                return null;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        internal static void StopActive()
        {
            CodexOverlayPreviewSession session = _active;
            _active = null;
            session?._snapshot.Restore();
            SceneView.RepaintAll();
        }

        internal static CodexOverlayPreviewResumeState CaptureResumeState()
        {
            CodexOverlayPreviewSession session = _active;
            return session == null
                ? null
                : new CodexOverlayPreviewResumeState(
                    session._view,
                    session._category,
                    session._enemyPageIndex,
                    session._demonPageIndex);
        }

        internal static string Resume(
            CodexOverlayPreviewResumeState state)
        {
            if (state == null || state.View == null)
            {
                return null;
            }

            StopActive();
            CodexOverlayPreviewSession session =
                new CodexOverlayPreviewSession(state.View)
                {
                    _category = state.Category,
                    _enemyPageIndex = state.EnemyPageIndex,
                    _demonPageIndex = state.DemonPageIndex
                };

            try
            {
                session.RebuildModels();
                _active = session;
                session.Render();
                return null;
            }
            catch (Exception exception)
            {
                _active = null;
                session._snapshot.Restore();
                return exception.Message;
            }
        }

        private static string EnsureActive(CodexOverlayView view)
        {
            if (view == null)
            {
                return "CodexOverlayView is missing.";
            }

            if (_active != null && _active._view == view)
            {
                return null;
            }

            StopActive();
            CodexOverlayPreviewSession session =
                new CodexOverlayPreviewSession(view);
            try
            {
                session.RebuildModels();
                _active = session;
                session.Render();
                return null;
            }
            catch (Exception exception)
            {
                _active = null;
                session._snapshot.Restore();
                return exception.Message;
            }
        }

        private void RebuildModels()
        {
            CardContentCatalogSO cardContentCatalog =
                AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(
                    CardCatalogPath);
            EnemyContentCatalogSO enemyContentCatalog =
                AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                    EnemyCatalogPath);
            if (cardContentCatalog == null)
            {
                throw new MissingReferenceException(
                    $"Card content catalog is missing at '{CardCatalogPath}'.");
            }

            if (enemyContentCatalog == null)
            {
                throw new MissingReferenceException(
                    $"Enemy content catalog is missing at '{EnemyCatalogPath}'.");
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
            MoveToIndex(_enemyPageIndex);
            _navigation.TryShowCategory(CodexCategory.DemonCard);
            MoveToIndex(_demonPageIndex);
            if (_category == CodexCategory.Enemy)
            {
                _navigation.TryShowCategory(CodexCategory.Enemy);
            }

            SyncNavigationState();
            _view.Configure(cardContentCatalog, enemyContentCatalog);
        }

        private void MoveToIndex(int requestedIndex)
        {
            int targetIndex = Mathf.Clamp(
                requestedIndex,
                0,
                _navigation.CurrentPageCount - 1);
            for (int index = 0; index < targetIndex; index++)
            {
                _navigation.TryMoveNext();
            }
        }

        private void SyncNavigationState()
        {
            _category = _navigation.Category;
            if (_category == CodexCategory.Enemy)
            {
                _enemyPageIndex = _navigation.CurrentPageIndex;
            }
            else
            {
                _demonPageIndex = _navigation.CurrentPageIndex;
            }
        }

        private void Render()
        {
            if (_view == null)
            {
                throw new MissingReferenceException(
                    "CodexOverlayView was destroyed during preview.");
            }

            CodexBookViewModel model = CodexPresenter.CreateBook(
                _navigation,
                _enemyPages,
                _demonPages);
            _view.RenderEditorPreview(model);
        }
    }

    internal sealed class CodexOverlayPreviewResumeState
    {
        internal CodexOverlayPreviewResumeState(
            CodexOverlayView view,
            CodexCategory category,
            int enemyPageIndex,
            int demonPageIndex)
        {
            View = view;
            Category = category;
            EnemyPageIndex = enemyPageIndex;
            DemonPageIndex = demonPageIndex;
        }

        internal CodexCategory Category { get; }

        internal int DemonPageIndex { get; }

        internal int EnemyPageIndex { get; }

        internal CodexOverlayView View { get; }

        internal bool BelongsTo(GameObject root)
        {
            return View != null &&
                   root != null &&
                   (View.transform == root.transform ||
                    View.transform.IsChildOf(root.transform));
        }

        internal bool BelongsTo(Scene scene)
        {
            return View != null && View.gameObject.scene == scene;
        }
    }

    internal sealed class CodexOverlayPreviewSnapshot
    {
        private readonly List<GameObjectState> _gameObjects =
            new List<GameObjectState>();
        private readonly List<TextState> _texts = new List<TextState>();
        private readonly List<ImageState> _images = new List<ImageState>();
        private readonly List<ButtonState> _buttons = new List<ButtonState>();
        private readonly HashSet<int> _capturedObjects = new HashSet<int>();
        private readonly HashSet<int> _capturedTexts = new HashSet<int>();
        private readonly HashSet<int> _capturedImages = new HashSet<int>();
        private readonly HashSet<int> _capturedButtons = new HashSet<int>();

        private CodexOverlayPreviewSnapshot()
        {
        }

        internal static CodexOverlayPreviewSnapshot Capture(
            CodexOverlayView view)
        {
            var snapshot = new CodexOverlayPreviewSnapshot();
            var serialized = new SerializedObject(view);

            snapshot.CaptureGameObject(serialized, "enemyPageRoot");
            snapshot.CaptureGameObject(serialized, "demonPageRoot");

            string[] textProperties =
            {
                "enemyTabText",
                "demonTabText",
                "previousPageText",
                "nextPageText",
                "enemyNameText",
                "enemySoulText",
                "enemyGoldText",
                "enemyDescriptionText",
                "noContractText",
                "demonNameText",
                "demonGoldText",
                "demonSoulText",
                "demonLoreText",
                "demonActiveSkillText",
                "demonCostText"
            };
            foreach (string propertyName in textProperties)
            {
                TMP_Text text = GetReference<TMP_Text>(
                    serialized,
                    propertyName);
                snapshot.CaptureText(text);
                if (propertyName == "noContractText")
                {
                    snapshot.CaptureGameObject(text?.gameObject);
                }
            }

            string[] imageProperties =
            {
                "enemyTabImage",
                "demonTabImage",
                "enemyPortraitImage",
                "demonCardImage"
            };
            foreach (string propertyName in imageProperties)
            {
                snapshot.CaptureImage(GetReference<Image>(
                    serialized,
                    propertyName));
            }

            snapshot.CaptureButton(GetReference<Button>(
                serialized,
                "enemyTabButton"));
            snapshot.CaptureButton(GetReference<Button>(
                serialized,
                "demonTabButton"));
            snapshot.CaptureDeckCard(GetReference<DeckPreviewCardView>(
                serialized,
                "contractTemplate"));
            snapshot.CaptureDeckCard(GetReference<DeckPreviewCardView>(
                serialized,
                "deckTemplate"));
            return snapshot;
        }

        internal void Restore()
        {
            foreach (TextState state in _texts)
            {
                state.Restore();
            }

            foreach (ImageState state in _images)
            {
                state.Restore();
            }

            foreach (ButtonState state in _buttons)
            {
                state.Restore();
            }

            foreach (GameObjectState state in _gameObjects)
            {
                state.Restore();
            }
        }

        private void CaptureDeckCard(DeckPreviewCardView card)
        {
            if (card == null)
            {
                return;
            }

            CaptureGameObject(card.gameObject);
            var serialized = new SerializedObject(card);
            CaptureImage(GetReference<Image>(serialized, "faceImage"));
            TMP_Text fallback = GetReference<TMP_Text>(
                serialized,
                "fallbackText");
            CaptureText(fallback);
            CaptureGameObject(fallback?.gameObject);
            TMP_Text count = GetReference<TMP_Text>(serialized, "countText");
            CaptureText(count);
            CaptureGameObject(count?.gameObject);
            CaptureGameObject(GetReference<GameObject>(
                serialized,
                "hoverFrame"));
            CaptureGameObject(GetReference<GameObject>(
                serialized,
                "selectedFrame"));
        }

        private void CaptureGameObject(
            SerializedObject serialized,
            string propertyName)
        {
            CaptureGameObject(GetReference<GameObject>(
                serialized,
                propertyName));
        }

        private void CaptureGameObject(GameObject gameObject)
        {
            if (gameObject != null &&
                _capturedObjects.Add(gameObject.GetInstanceID()))
            {
                _gameObjects.Add(new GameObjectState(gameObject));
            }
        }

        private void CaptureText(TMP_Text text)
        {
            if (text != null && _capturedTexts.Add(text.GetInstanceID()))
            {
                _texts.Add(new TextState(text));
            }
        }

        private void CaptureImage(Image image)
        {
            if (image != null && _capturedImages.Add(image.GetInstanceID()))
            {
                _images.Add(new ImageState(image));
            }
        }

        private void CaptureButton(Button button)
        {
            if (button != null &&
                _capturedButtons.Add(button.GetInstanceID()))
            {
                _buttons.Add(new ButtonState(button));
            }
        }

        private static T GetReference<T>(
            SerializedObject serialized,
            string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            return property?.objectReferenceValue as T;
        }

        private readonly struct GameObjectState
        {
            private readonly GameObject _target;
            private readonly bool _active;

            internal GameObjectState(GameObject target)
            {
                _target = target;
                _active = target.activeSelf;
            }

            internal void Restore()
            {
                if (_target != null)
                {
                    _target.SetActive(_active);
                }
            }
        }

        private readonly struct TextState
        {
            private readonly TMP_Text _target;
            private readonly string _text;

            internal TextState(TMP_Text target)
            {
                _target = target;
                _text = target.text;
            }

            internal void Restore()
            {
                if (_target != null)
                {
                    _target.text = _text;
                }
            }
        }

        private readonly struct ImageState
        {
            private readonly Image _target;
            private readonly Sprite _sprite;
            private readonly Color _color;
            private readonly bool _enabled;

            internal ImageState(Image target)
            {
                _target = target;
                _sprite = target.sprite;
                _color = target.color;
                _enabled = target.enabled;
            }

            internal void Restore()
            {
                if (_target != null)
                {
                    _target.sprite = _sprite;
                    _target.color = _color;
                    _target.enabled = _enabled;
                }
            }
        }

        private readonly struct ButtonState
        {
            private readonly Button _target;
            private readonly bool _interactable;

            internal ButtonState(Button target)
            {
                _target = target;
                _interactable = target.interactable;
            }

            internal void Restore()
            {
                if (_target != null)
                {
                    _target.interactable = _interactable;
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class CodexOverlayPreviewLifecycle
    {
        private static CodexOverlayPreviewResumeState _prefabResumeState;
        private static CodexOverlayPreviewResumeState _sceneResumeState;

        static CodexOverlayPreviewLifecycle()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.quitting -= StopPreview;
            EditorApplication.quitting += StopPreview;
            AssemblyReloadEvents.beforeAssemblyReload -= StopPreview;
            AssemblyReloadEvents.beforeAssemblyReload += StopPreview;
            PrefabStage.prefabSaving -= HandlePrefabSaving;
            PrefabStage.prefabSaving += HandlePrefabSaving;
            PrefabStage.prefabSaved -= HandlePrefabSaved;
            PrefabStage.prefabSaved += HandlePrefabSaved;
            PrefabStage.prefabStageClosing -= HandlePrefabStageClosing;
            PrefabStage.prefabStageClosing += HandlePrefabStageClosing;
            EditorSceneManager.sceneSaving -= HandleSceneSaving;
            EditorSceneManager.sceneSaving += HandleSceneSaving;
            EditorSceneManager.sceneSaved -= HandleSceneSaved;
            EditorSceneManager.sceneSaved += HandleSceneSaved;
        }

        private static void HandlePlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                StopPreview();
            }
        }

        private static void HandlePrefabSaving(GameObject root)
        {
            SuspendForPrefabSave(root);
        }

        private static void HandlePrefabSaved(GameObject root)
        {
            ResumeAfterPrefabSave(root);
        }

        private static void HandlePrefabStageClosing(PrefabStage stage)
        {
            ClearPendingResume();
            StopPreview();
        }

        private static void HandleSceneSaving(Scene scene, string path)
        {
            SuspendForSceneSave(scene);
        }

        private static void HandleSceneSaved(Scene scene)
        {
            ResumeAfterSceneSave(scene);
        }

        internal static void SuspendForPrefabSave(GameObject root)
        {
            CodexOverlayPreviewResumeState state =
                CodexOverlayPreviewSession.CaptureResumeState();
            if (state == null || !state.BelongsTo(root))
            {
                return;
            }

            _prefabResumeState = state;
            CodexOverlayPreviewSession.StopActive();
        }

        internal static void ResumeAfterPrefabSave(GameObject root)
        {
            CodexOverlayPreviewResumeState state = _prefabResumeState;
            _prefabResumeState = null;
            if (state == null || !state.BelongsTo(root))
            {
                return;
            }

            CodexOverlayPreviewSession.Resume(state);
        }

        internal static void SuspendForSceneSave(Scene scene)
        {
            CodexOverlayPreviewResumeState state =
                CodexOverlayPreviewSession.CaptureResumeState();
            if (state == null || !state.BelongsTo(scene))
            {
                return;
            }

            _sceneResumeState = state;
            CodexOverlayPreviewSession.StopActive();
        }

        internal static void ResumeAfterSceneSave(Scene scene)
        {
            CodexOverlayPreviewResumeState state = _sceneResumeState;
            _sceneResumeState = null;
            if (state == null || !state.BelongsTo(scene))
            {
                return;
            }

            CodexOverlayPreviewSession.Resume(state);
        }

        private static void ClearPendingResume()
        {
            _prefabResumeState = null;
            _sceneResumeState = null;
        }

        private static void StopPreview()
        {
            ClearPendingResume();
            CodexOverlayPreviewSession.StopActive();
        }
    }
}
#endif
