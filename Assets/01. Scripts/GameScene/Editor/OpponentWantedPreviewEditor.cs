#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.StageProgression;
using DiaBlackJack.StageProgression.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(OpponentSelectionView))]
    internal sealed class OpponentSelectionViewEditor : UnityEditor.Editor
    {
        private string _lastError;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "OPPONENT SELECTION PREVIEW",
                EditorStyles.boldLabel);

            OpponentSelectionView view = target as OpponentSelectionView;
            if (!CanPreview(view))
            {
                return;
            }

            OpponentWantedPreviewSession session =
                OpponentWantedPreviewSession.GetActive(view);
            EditorGUILayout.LabelField(
                "Current",
                session == null ? "Off" : session.CurrentLabel);

            if (GUILayout.Button("Preview Two Posters"))
            {
                RunPreviewAction(() =>
                    OpponentWantedPreviewSession.Show(view));
            }

            session = OpponentWantedPreviewSession.GetActive(view);
            DrawNavigation(view, session);
            DrawHoverControls(view, session);
            DrawFooter(view, session);

            EditorGUILayout.HelpBox(
                "Preview fills the two existing poster slots with Enemy Content " +
                "Catalog data. No preview objects are created. Saving restores " +
                "authored values, then resumes the current preview.",
                MessageType.None);
            DrawError();
        }

        private void DrawNavigation(
            OpponentSelectionView view,
            OpponentWantedPreviewSession session)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMovePrevious))
                {
                    if (GUILayout.Button("Previous Pair"))
                    {
                        RunPreviewAction(() =>
                            OpponentWantedPreviewSession.MovePrevious(view));
                    }
                }

                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMoveNext))
                {
                    if (GUILayout.Button("Next Pair"))
                    {
                        RunPreviewAction(() =>
                            OpponentWantedPreviewSession.MoveNext(view));
                    }
                }
            }
        }

        private void DrawHoverControls(
            OpponentSelectionView view,
            OpponentWantedPreviewSession session)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                    session == null || session.VisibleSlotCount < 1))
                {
                    if (GUILayout.Button("Hover Left"))
                    {
                        RunPreviewAction(() =>
                            OpponentWantedPreviewSession.SetHover(view, 0));
                    }
                }

                using (new EditorGUI.DisabledScope(
                    session == null || session.VisibleSlotCount < 2))
                {
                    if (GUILayout.Button("Hover Right"))
                    {
                        RunPreviewAction(() =>
                            OpponentWantedPreviewSession.SetHover(view, 1));
                    }
                }

                using (new EditorGUI.DisabledScope(session == null))
                {
                    if (GUILayout.Button("No Hover"))
                    {
                        RunPreviewAction(() =>
                            OpponentWantedPreviewSession.SetHover(view, -1));
                    }
                }
            }
        }

        private void DrawFooter(
            OpponentSelectionView view,
            OpponentWantedPreviewSession session)
        {
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(session == null))
            {
                if (GUILayout.Button("Refresh"))
                {
                    RunPreviewAction(() =>
                        OpponentWantedPreviewSession.Refresh(view));
                }

                if (GUILayout.Button("Preview Off"))
                {
                    OpponentWantedPreviewSession.StopActive();
                    _lastError = null;
                    RepaintPreview();
                }
            }
        }

        private bool CanPreview(Component view)
        {
            if (view == null || !view.gameObject.scene.IsValid())
            {
                EditorGUILayout.HelpBox(
                    "Open the prefab in Prefab Mode or select a scene instance.",
                    MessageType.Info);
                return false;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "WANTED preview is available only in Edit Mode.",
                    MessageType.Info);
                return false;
            }

            return true;
        }

        private void RunPreviewAction(Func<string> action)
        {
            _lastError = action();
            RepaintPreview();
        }

        private void DrawError()
        {
            if (!string.IsNullOrEmpty(_lastError))
            {
                EditorGUILayout.HelpBox(_lastError, MessageType.Error);
            }
        }

        private void RepaintPreview()
        {
            SceneView.RepaintAll();
            Repaint();
        }
    }

    [CustomEditor(typeof(OpponentWantedPosterView))]
    internal sealed class OpponentWantedPosterViewEditor : UnityEditor.Editor
    {
        private string _lastError;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "WANTED POSTER PREVIEW",
                EditorStyles.boldLabel);

            OpponentWantedPosterView view =
                target as OpponentWantedPosterView;
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
                    "WANTED preview is available only in Edit Mode.",
                    MessageType.Info);
                return;
            }

            OpponentWantedPreviewSession session =
                OpponentWantedPreviewSession.GetActive(view);
            EditorGUILayout.LabelField(
                "Current",
                session == null ? "Off" : session.CurrentLabel);

            if (GUILayout.Button("Preview Enemy"))
            {
                RunPreviewAction(() =>
                    OpponentWantedPreviewSession.Show(view));
            }

            session = OpponentWantedPreviewSession.GetActive(view);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMovePrevious))
                {
                    if (GUILayout.Button("Previous"))
                    {
                        RunPreviewAction(() =>
                            OpponentWantedPreviewSession.MovePrevious(view));
                    }
                }

                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMoveNext))
                {
                    if (GUILayout.Button("Next"))
                    {
                        RunPreviewAction(() =>
                            OpponentWantedPreviewSession.MoveNext(view));
                    }
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(session == null))
            {
                if (GUILayout.Button("Hover"))
                {
                    RunPreviewAction(() =>
                        OpponentWantedPreviewSession.SetHover(view, 0));
                }

                if (GUILayout.Button("No Hover"))
                {
                    RunPreviewAction(() =>
                        OpponentWantedPreviewSession.SetHover(view, -1));
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(session == null))
            {
                if (GUILayout.Button("Refresh"))
                {
                    RunPreviewAction(() =>
                        OpponentWantedPreviewSession.Refresh(view));
                }

                if (GUILayout.Button("Preview Off"))
                {
                    OpponentWantedPreviewSession.StopActive();
                    _lastError = null;
                    RepaintPreview();
                }
            }

            EditorGUILayout.HelpBox(
                "Preview fills this existing poster with Enemy Content Catalog " +
                "data. No preview object is created. Saving restores authored " +
                "values, then resumes the current preview.",
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

    internal sealed class OpponentWantedPreviewSession
    {
        internal const string EnemyCatalogPath =
            "Assets/02. ScriptableObjects/Enemies/EnemyContentCatalog.asset";

        private static OpponentWantedPreviewSession _active;

        private readonly Component _target;
        private readonly GameObject _contentRoot;
        private readonly OpponentWantedPosterView[] _slots;
        private readonly OpponentWantedPreviewSnapshot _snapshot;
        private EnemyContentCatalogSO _catalog;
        private IReadOnlyList<OpponentCandidateViewModel> _candidates;
        private int _pageIndex;
        private int _hoverSlotIndex = -1;

        private OpponentWantedPreviewSession(
            Component target,
            GameObject contentRoot,
            OpponentWantedPosterView[] slots)
        {
            _target = target;
            _contentRoot = contentRoot;
            _slots = slots;
            _snapshot = OpponentWantedPreviewSnapshot.Capture(
                contentRoot,
                slots);
        }

        internal bool CanMoveNext => _candidates != null &&
            _pageIndex + 1 < PageCount;

        internal bool CanMovePrevious => _pageIndex > 0;

        internal string CurrentLabel
        {
            get
            {
                if (_candidates == null || _candidates.Count == 0)
                {
                    return "No enemies";
                }

                int first = _pageIndex * _slots.Length + 1;
                int last = Mathf.Min(
                    first + _slots.Length - 1,
                    _candidates.Count);
                string hover = _hoverSlotIndex < 0
                    ? "No Hover"
                    : $"Hover {_hoverSlotIndex + 1}";
                return $"Enemies {first}-{last} / {_candidates.Count} · {hover}";
            }
        }

        internal int CurrentPageIndex => _pageIndex;

        internal int VisibleSlotCount => _candidates == null
            ? 0
            : Mathf.Min(
                _slots.Length,
                _candidates.Count - _pageIndex * _slots.Length);

        private int PageCount => _candidates == null || _slots.Length == 0
            ? 0
            : Mathf.CeilToInt((float)_candidates.Count / _slots.Length);

        internal static OpponentWantedPreviewSession GetActive(
            OpponentSelectionView view)
        {
            return GetActive((Component)view);
        }

        internal static OpponentWantedPreviewSession GetActive(
            OpponentWantedPosterView view)
        {
            return GetActive((Component)view);
        }

        internal static string Show(OpponentSelectionView view)
        {
            if (view == null)
            {
                return "OpponentSelectionView is missing.";
            }

            SerializedObject serialized = new SerializedObject(view);
            GameObject contentRoot = GetReference<GameObject>(
                serialized,
                "contentRoot");
            OpponentWantedPosterView[] slots = GetArrayReferences(
                serialized.FindProperty("posterSlots"));
            EnemyContentCatalogSO catalog =
                GetReference<EnemyContentCatalogSO>(
                    serialized,
                    "enemyContentCatalog");
            return Show(view, contentRoot, slots, catalog);
        }

        internal static string Show(OpponentWantedPosterView view)
        {
            if (view == null)
            {
                return "OpponentWantedPosterView is missing.";
            }

            return Show(
                view,
                null,
                new[] { view },
                null);
        }

        internal static string MovePrevious(OpponentSelectionView view)
        {
            return MovePrevious((Component)view);
        }

        internal static string MovePrevious(OpponentWantedPosterView view)
        {
            return MovePrevious((Component)view);
        }

        internal static string MoveNext(OpponentSelectionView view)
        {
            return MoveNext((Component)view);
        }

        internal static string MoveNext(OpponentWantedPosterView view)
        {
            return MoveNext((Component)view);
        }

        internal static string SetHover(
            OpponentSelectionView view,
            int slotIndex)
        {
            return SetHover((Component)view, slotIndex);
        }

        internal static string SetHover(
            OpponentWantedPosterView view,
            int slotIndex)
        {
            return SetHover((Component)view, slotIndex);
        }

        internal static string Refresh(OpponentSelectionView view)
        {
            return Refresh((Component)view);
        }

        internal static string Refresh(OpponentWantedPosterView view)
        {
            return Refresh((Component)view);
        }

        internal static void StopActive()
        {
            OpponentWantedPreviewSession session = _active;
            _active = null;
            session?._snapshot.Restore();
            SceneView.RepaintAll();
        }

        internal static OpponentWantedPreviewResumeState CaptureResumeState()
        {
            OpponentWantedPreviewSession session = _active;
            return session == null
                ? null
                : new OpponentWantedPreviewResumeState(
                    session._target,
                    session._pageIndex,
                    session._hoverSlotIndex);
        }

        internal static string Resume(OpponentWantedPreviewResumeState state)
        {
            if (state == null || state.Target == null)
            {
                return null;
            }

            string error;
            OpponentSelectionView selection =
                state.Target as OpponentSelectionView;
            if (selection != null)
            {
                error = Show(selection);
            }
            else
            {
                OpponentWantedPosterView poster =
                    state.Target as OpponentWantedPosterView;
                error = Show(poster);
            }

            if (error != null || _active == null)
            {
                return error;
            }

            _active._pageIndex = Mathf.Clamp(
                state.PageIndex,
                0,
                Mathf.Max(0, _active.PageCount - 1));
            _active._hoverSlotIndex = state.HoverSlotIndex;
            _active.Render();
            return null;
        }

        private static OpponentWantedPreviewSession GetActive(Component target)
        {
            return _active != null && _active._target == target
                ? _active
                : null;
        }

        private static string Show(
            Component target,
            GameObject contentRoot,
            OpponentWantedPosterView[] slots,
            EnemyContentCatalogSO catalog)
        {
            if (slots == null || slots.Length == 0)
            {
                return "WANTED preview requires at least one poster slot.";
            }

            foreach (OpponentWantedPosterView slot in slots)
            {
                if (slot == null)
                {
                    return "WANTED preview contains a missing poster slot.";
                }
            }

            if (_active != null && _active._target == target)
            {
                return null;
            }

            StopActive();
            OpponentWantedPreviewSession session =
                new OpponentWantedPreviewSession(
                    target,
                    contentRoot,
                    slots);
            try
            {
                session.RebuildModels(catalog);
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

        private static string MovePrevious(Component target)
        {
            OpponentWantedPreviewSession session = GetActive(target);
            if (session == null)
            {
                return "Start WANTED preview first.";
            }

            if (session.CanMovePrevious)
            {
                session._pageIndex--;
                session.Render();
            }

            return null;
        }

        private static string MoveNext(Component target)
        {
            OpponentWantedPreviewSession session = GetActive(target);
            if (session == null)
            {
                return "Start WANTED preview first.";
            }

            if (session.CanMoveNext)
            {
                session._pageIndex++;
                session.Render();
            }

            return null;
        }

        private static string SetHover(Component target, int slotIndex)
        {
            OpponentWantedPreviewSession session = GetActive(target);
            if (session == null)
            {
                return "Start WANTED preview first.";
            }

            if (slotIndex < -1 || slotIndex >= session.VisibleSlotCount)
            {
                return "Requested hover slot is not visible.";
            }

            session._hoverSlotIndex = slotIndex;
            session.Render();
            return null;
        }

        private static string Refresh(Component target)
        {
            OpponentWantedPreviewSession session = GetActive(target);
            if (session == null)
            {
                return "Start WANTED preview first.";
            }

            try
            {
                session.RebuildModels(session._catalog);
                session.Render();
                return null;
            }
            catch (Exception exception)
            {
                return exception.Message;
            }
        }

        private void RebuildModels(EnemyContentCatalogSO configuredCatalog)
        {
            _catalog = configuredCatalog != null
                ? configuredCatalog
                : AssetDatabase.LoadAssetAtPath<EnemyContentCatalogSO>(
                    EnemyCatalogPath);
            if (_catalog == null)
            {
                throw new MissingReferenceException(
                    $"Enemy content catalog is missing at '{EnemyCatalogPath}'.");
            }

            EnemyCombatProfileCatalog profiles =
                _catalog.BuildRuntimeCatalog();
            GoldRewardCatalog gold = _catalog.BuildGoldRewardCatalog();
            List<OpponentCandidateViewModel> candidates =
                new List<OpponentCandidateViewModel>(profiles.Profiles.Count);
            foreach (EnemyCombatProfile profile in profiles.Profiles)
            {
                int defeatGold = gold.GetAmount(profile.Key);
                candidates.Add(new OpponentCandidateViewModel(
                    profile.Key,
                    profile.DisplayName,
                    profile.Grade.ToString().ToUpperInvariant(),
                    $"SOUL {profile.MaximumSoul}",
                    profile.Summary,
                    $"VICTORY GOLD {defeatGold}",
                    $"×{profile.MaximumSoul}",
                    $"×{defeatGold}",
                    false));
            }

            _candidates = candidates;
            _pageIndex = Mathf.Clamp(
                _pageIndex,
                0,
                Mathf.Max(0, PageCount - 1));
            if (_hoverSlotIndex >= VisibleSlotCount)
            {
                _hoverSlotIndex = -1;
            }
        }

        private void Render()
        {
            if (_target == null)
            {
                throw new MissingReferenceException(
                    "WANTED preview target was destroyed.");
            }

            if (_contentRoot != null)
            {
                _contentRoot.SetActive(true);
            }

            int firstCandidateIndex = _pageIndex * _slots.Length;
            for (int slotIndex = 0; slotIndex < _slots.Length; slotIndex++)
            {
                OpponentWantedPosterView slot = _slots[slotIndex];
                int candidateIndex = firstCandidateIndex + slotIndex;
                if (candidateIndex >= _candidates.Count)
                {
                    slot.Hide();
                    continue;
                }

                OpponentCandidateViewModel candidate =
                    _candidates[candidateIndex];
                slot.Render(
                    candidate,
                    _catalog.GetPortrait(candidate.ProfileKey),
                    true);
                if (slotIndex == _hoverSlotIndex)
                {
                    slot.OnPointerEnter(null);
                }
            }
        }

        private static T GetReference<T>(
            SerializedObject serialized,
            string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property =
                serialized.FindProperty(propertyName);
            return property == null
                ? null
                : property.objectReferenceValue as T;
        }

        private static OpponentWantedPosterView[] GetArrayReferences(
            SerializedProperty property)
        {
            if (property == null || !property.isArray)
            {
                return Array.Empty<OpponentWantedPosterView>();
            }

            OpponentWantedPosterView[] result =
                new OpponentWantedPosterView[property.arraySize];
            for (int index = 0; index < property.arraySize; index++)
            {
                result[index] = property.GetArrayElementAtIndex(index)
                    .objectReferenceValue as OpponentWantedPosterView;
            }

            return result;
        }
    }

    internal sealed class OpponentWantedPreviewResumeState
    {
        internal OpponentWantedPreviewResumeState(
            Component target,
            int pageIndex,
            int hoverSlotIndex)
        {
            Target = target;
            PageIndex = pageIndex;
            HoverSlotIndex = hoverSlotIndex;
        }

        internal int HoverSlotIndex { get; }

        internal int PageIndex { get; }

        internal Component Target { get; }

        internal bool BelongsTo(GameObject root)
        {
            return Target != null &&
                   root != null &&
                   (Target.transform == root.transform ||
                    Target.transform.IsChildOf(root.transform));
        }

        internal bool BelongsTo(Scene scene)
        {
            return Target != null && Target.gameObject.scene == scene;
        }
    }

    internal sealed class OpponentWantedPreviewSnapshot
    {
        private readonly GameObject _contentRoot;
        private readonly bool _contentRootActive;
        private readonly PosterState[] _posterStates;

        private OpponentWantedPreviewSnapshot(
            GameObject contentRoot,
            PosterState[] posterStates)
        {
            _contentRoot = contentRoot;
            _contentRootActive = contentRoot != null && contentRoot.activeSelf;
            _posterStates = posterStates;
        }

        internal static OpponentWantedPreviewSnapshot Capture(
            GameObject contentRoot,
            OpponentWantedPosterView[] slots)
        {
            PosterState[] states = new PosterState[slots.Length];
            for (int index = 0; index < slots.Length; index++)
            {
                states[index] = PosterState.Capture(slots[index]);
            }

            return new OpponentWantedPreviewSnapshot(contentRoot, states);
        }

        internal void Restore()
        {
            foreach (PosterState state in _posterStates)
            {
                state.Restore();
            }

            if (_contentRoot != null)
            {
                _contentRoot.SetActive(_contentRootActive);
            }
        }

        private sealed class PosterState
        {
            private readonly OpponentWantedPosterView _view;
            private readonly bool _active;
            private readonly Vector3 _localScale;
            private readonly ImageState _portrait;
            private readonly TextState _enemyName;
            private readonly TextState _soulAmount;
            private readonly TextState _description;
            private readonly TextState _goldAmount;
            private readonly Outline _outline;
            private readonly bool _outlineEnabled;

            private PosterState(
                OpponentWantedPosterView view,
                ImageState portrait,
                TextState enemyName,
                TextState soulAmount,
                TextState description,
                TextState goldAmount,
                Outline outline)
            {
                _view = view;
                _active = view.gameObject.activeSelf;
                _localScale = view.transform.localScale;
                _portrait = portrait;
                _enemyName = enemyName;
                _soulAmount = soulAmount;
                _description = description;
                _goldAmount = goldAmount;
                _outline = outline;
                _outlineEnabled = outline != null && outline.enabled;
            }

            internal static PosterState Capture(
                OpponentWantedPosterView view)
            {
                SerializedObject serialized = new SerializedObject(view);
                return new PosterState(
                    view,
                    ImageState.Capture(GetReference<Image>(
                        serialized,
                        "portraitImage")),
                    TextState.Capture(GetReference<TMP_Text>(
                        serialized,
                        "enemyNameText")),
                    TextState.Capture(GetReference<TMP_Text>(
                        serialized,
                        "soulAmountText")),
                    TextState.Capture(GetReference<TMP_Text>(
                        serialized,
                        "descriptionText")),
                    TextState.Capture(GetReference<TMP_Text>(
                        serialized,
                        "defeatGoldAmountText")),
                    GetReference<Outline>(serialized, "hoverOutline"));
            }

            internal void Restore()
            {
                if (_view == null)
                {
                    return;
                }

                _view.Hide();
                _view.transform.localScale = _localScale;
                _portrait.Restore();
                _enemyName.Restore();
                _soulAmount.Restore();
                _description.Restore();
                _goldAmount.Restore();
                if (_outline != null)
                {
                    _outline.enabled = _outlineEnabled;
                }

                _view.gameObject.SetActive(_active);
            }

            private static T GetReference<T>(
                SerializedObject serialized,
                string propertyName)
                where T : UnityEngine.Object
            {
                SerializedProperty property =
                    serialized.FindProperty(propertyName);
                return property == null
                    ? null
                    : property.objectReferenceValue as T;
            }
        }

        private sealed class ImageState
        {
            private readonly Image _target;
            private readonly Sprite _sprite;
            private readonly bool _enabled;

            private ImageState(Image target)
            {
                _target = target;
                _sprite = target == null ? null : target.sprite;
                _enabled = target != null && target.enabled;
            }

            internal static ImageState Capture(Image target)
            {
                return new ImageState(target);
            }

            internal void Restore()
            {
                if (_target == null)
                {
                    return;
                }

                _target.sprite = _sprite;
                _target.enabled = _enabled;
            }
        }

        private sealed class TextState
        {
            private readonly TMP_Text _target;
            private readonly string _text;
            private readonly int _maxVisibleLines;
            private readonly TextOverflowModes _overflowMode;

            private TextState(TMP_Text target)
            {
                _target = target;
                _text = target == null ? null : target.text;
                _maxVisibleLines = target == null
                    ? 0
                    : target.maxVisibleLines;
                _overflowMode = target == null
                    ? TextOverflowModes.Overflow
                    : target.overflowMode;
            }

            internal static TextState Capture(TMP_Text target)
            {
                return new TextState(target);
            }

            internal void Restore()
            {
                if (_target != null)
                {
                    _target.text = _text;
                    _target.maxVisibleLines = _maxVisibleLines;
                    _target.overflowMode = _overflowMode;
                }
            }
        }
    }

    [InitializeOnLoad]
    internal static class OpponentWantedPreviewLifecycle
    {
        private static OpponentWantedPreviewResumeState _prefabResumeState;
        private static OpponentWantedPreviewResumeState _sceneResumeState;

        static OpponentWantedPreviewLifecycle()
        {
            EditorApplication.playModeStateChanged -=
                HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                HandlePlayModeStateChanged;
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
            OpponentWantedPreviewResumeState state =
                OpponentWantedPreviewSession.CaptureResumeState();
            if (state == null || !state.BelongsTo(root))
            {
                return;
            }

            _prefabResumeState = state;
            OpponentWantedPreviewSession.StopActive();
        }

        internal static void ResumeAfterPrefabSave(GameObject root)
        {
            OpponentWantedPreviewResumeState state = _prefabResumeState;
            _prefabResumeState = null;
            if (state == null || !state.BelongsTo(root))
            {
                return;
            }

            OpponentWantedPreviewSession.Resume(state);
        }

        internal static void SuspendForSceneSave(Scene scene)
        {
            OpponentWantedPreviewResumeState state =
                OpponentWantedPreviewSession.CaptureResumeState();
            if (state == null || !state.BelongsTo(scene))
            {
                return;
            }

            _sceneResumeState = state;
            OpponentWantedPreviewSession.StopActive();
        }

        internal static void ResumeAfterSceneSave(Scene scene)
        {
            OpponentWantedPreviewResumeState state = _sceneResumeState;
            _sceneResumeState = null;
            if (state == null || !state.BelongsTo(scene))
            {
                return;
            }

            OpponentWantedPreviewSession.Resume(state);
        }

        private static void ClearPendingResume()
        {
            _prefabResumeState = null;
            _sceneResumeState = null;
        }

        private static void StopPreview()
        {
            ClearPendingResume();
            OpponentWantedPreviewSession.StopActive();
        }
    }
}
#endif
