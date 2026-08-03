#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DiaBlackJack.Content;
using DiaBlackJack.CoreLoop;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene.Editor
{
    [CustomEditor(typeof(DemonCardHoverDetailView))]
    internal sealed class DemonCardHoverDetailViewEditor : UnityEditor.Editor
    {
        private string _lastError;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField(
                "DEMON HOVER DETAIL PREVIEW",
                EditorStyles.boldLabel);

            DemonCardHoverDetailView view =
                target as DemonCardHoverDetailView;
            if (view == null || !view.gameObject.scene.IsValid())
            {
                EditorGUILayout.HelpBox(
                    "Open DemonCardHoverDetail in Prefab Mode or select a scene instance.",
                    MessageType.Info);
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorGUILayout.HelpBox(
                    "Preview is available only in Edit Mode.",
                    MessageType.Info);
                return;
            }

            DemonCardHoverDetailPreviewSession session =
                DemonCardHoverDetailPreviewSession.GetActive(view);
            EditorGUILayout.LabelField(
                "Current",
                session == null ? "Off" : session.CurrentLabel);

            if (GUILayout.Button("Preview"))
            {
                RunPreviewAction(() =>
                    DemonCardHoverDetailPreviewSession.Show(view));
            }

            session = DemonCardHoverDetailPreviewSession.GetActive(view);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMovePrevious))
                {
                    if (GUILayout.Button("Previous"))
                    {
                        RunPreviewAction(() =>
                            DemonCardHoverDetailPreviewSession.MovePrevious(view));
                    }
                }

                using (new EditorGUI.DisabledScope(
                    session == null || !session.CanMoveNext))
                {
                    if (GUILayout.Button("Next"))
                    {
                        RunPreviewAction(() =>
                            DemonCardHoverDetailPreviewSession.MoveNext(view));
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
                            DemonCardHoverDetailPreviewSession.Refresh(view));
                    }

                    if (GUILayout.Button("Preview Off"))
                    {
                        DemonCardHoverDetailPreviewSession.StopActive();
                        _lastError = null;
                        RepaintPreview();
                    }
                }
            }

            EditorGUILayout.HelpBox(
                "Preview fills this prefab directly. Saving restores authored " +
                "values for serialization, then resumes the current preview.",
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

    internal sealed class DemonCardHoverDetailPreviewSession
    {
        internal const string CardCatalogPath =
            "Assets/02. ScriptableObjects/Cards/CardContentCatalog.asset";

        private static DemonCardHoverDetailPreviewSession _active;

        private readonly DemonCardHoverDetailView _view;
        private readonly DemonCardHoverDetailPreviewSnapshot _snapshot;
        private readonly DemonCardHoverDetailPreviewCanvas _previewCanvas;
        private CardContentCatalogSO _catalog;
        private IReadOnlyList<DemonContractDefinition> _definitions;
        private int _index;

        private DemonCardHoverDetailPreviewSession(
            DemonCardHoverDetailView view)
        {
            _view = view;
            _snapshot = DemonCardHoverDetailPreviewSnapshot.Capture(view);
            _previewCanvas = DemonCardHoverDetailPreviewCanvas.Attach(view);
        }

        internal bool CanMovePrevious => _index > 0;

        internal bool CanMoveNext =>
            _definitions != null && _index + 1 < _definitions.Count;

        internal int CurrentIndex => _index;

        internal string CurrentLabel
        {
            get
            {
                if (_definitions == null || _definitions.Count == 0)
                {
                    return "Off";
                }

                DemonContractDefinition definition = _definitions[_index];
                return $"{definition.DisplayName}  {_index + 1} / {_definitions.Count}";
            }
        }

        internal static DemonCardHoverDetailPreviewSession GetActive(
            DemonCardHoverDetailView view)
        {
            return _active != null && _active._view == view
                ? _active
                : null;
        }

        internal static string Show(DemonCardHoverDetailView view)
        {
            if (view == null)
            {
                return "DemonCardHoverDetailView is missing.";
            }

            if (!view.HasRequiredReferences)
            {
                return "DemonCardHoverDetailView references are incomplete.";
            }

            if (_active != null && _active._view == view)
            {
                _active.Render();
                return null;
            }

            StopActive();
            DemonCardHoverDetailPreviewSession session =
                new DemonCardHoverDetailPreviewSession(view);
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
                session.Restore();
                return exception.Message;
            }
        }

        internal static string MovePrevious(DemonCardHoverDetailView view)
        {
            DemonCardHoverDetailPreviewSession session = GetActive(view);
            if (session == null)
            {
                return "Start preview first.";
            }

            if (session.CanMovePrevious)
            {
                session._index--;
                session.Render();
            }

            return null;
        }

        internal static string MoveNext(DemonCardHoverDetailView view)
        {
            DemonCardHoverDetailPreviewSession session = GetActive(view);
            if (session == null)
            {
                return "Start preview first.";
            }

            if (session.CanMoveNext)
            {
                session._index++;
                session.Render();
            }

            return null;
        }

        internal static string Refresh(DemonCardHoverDetailView view)
        {
            DemonCardHoverDetailPreviewSession session = GetActive(view);
            if (session == null)
            {
                return "Start preview first.";
            }

            try
            {
                int previousIndex = session._index;
                session.RebuildModels();
                session._index = Mathf.Clamp(
                    previousIndex,
                    0,
                    session._definitions.Count - 1);
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
            DemonCardHoverDetailPreviewSession session = _active;
            _active = null;
            session?.Restore();
            SceneView.RepaintAll();
        }

        internal static DemonCardHoverDetailPreviewResumeState
            CaptureResumeState()
        {
            DemonCardHoverDetailPreviewSession session = _active;
            return session == null
                ? null
                : new DemonCardHoverDetailPreviewResumeState(
                    session._view,
                    session._index);
        }

        internal static string Resume(
            DemonCardHoverDetailPreviewResumeState state)
        {
            if (state == null || state.View == null)
            {
                return null;
            }

            string error = Show(state.View);
            if (error != null)
            {
                return error;
            }

            _active._index = Mathf.Clamp(
                state.Index,
                0,
                _active._definitions.Count - 1);
            _active.Render();
            return null;
        }

        private void RebuildModels()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<CardContentCatalogSO>(
                CardCatalogPath);
            if (_catalog == null)
            {
                throw new MissingReferenceException(
                    $"Card content catalog is missing at '{CardCatalogPath}'.");
            }

            _definitions = _catalog.BuildRuntimeCatalog().DemonDefinitions;
            if (_definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    "Demon card catalog has no definitions.");
            }
        }

        private void Render()
        {
            if (_view == null)
            {
                throw new MissingReferenceException(
                    "Demon hover detail preview target was destroyed.");
            }

            DemonContractDefinition definition = _definitions[_index];
            GameSceneDemonCardViewModel model =
                new GameSceneDemonCardViewModel(
                    cardId: 0,
                    definitionKey: definition.Key,
                    isFaceUp: true,
                    canUse: false,
                    displayName: definition.DisplayName,
                    summary: definition.Summary,
                    costSummary: definition.CostSummary,
                    showHoverBadgeWhenUnavailable: true);
            _view.Render(
                model,
                _catalog.GetDemonFaceSprite(definition.Key));
            Canvas.ForceUpdateCanvases();
            _previewCanvas.MatchInlineSpriteMaterialQueues(
                _view.DetailView);
            Canvas.ForceUpdateCanvases();
            SceneView.RepaintAll();
        }

        private void Restore()
        {
            _previewCanvas.Restore();
            _snapshot.Restore();
        }
    }

    internal sealed class DemonCardHoverDetailPreviewCanvas
    {
        private readonly Canvas _canvas;
        private readonly CanvasScaler _scaler;
        private readonly List<InlineSpriteMaterialOverride>
            _inlineSpriteMaterialOverrides =
                new List<InlineSpriteMaterialOverride>();

        private DemonCardHoverDetailPreviewCanvas(
            Canvas canvas,
            CanvasScaler scaler)
        {
            _canvas = canvas;
            _scaler = scaler;
        }

        internal static DemonCardHoverDetailPreviewCanvas Attach(
            DemonCardHoverDetailView view)
        {
            Canvas parentCanvas = view.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                return new DemonCardHoverDetailPreviewCanvas(null, null);
            }

            Canvas canvas = view.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.additionalShaderChannels =
                AdditionalCanvasShaderChannels.TexCoord1 |
                AdditionalCanvasShaderChannels.Normal |
                AdditionalCanvasShaderChannels.Tangent;

            CanvasScaler scaler = view.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;
            return new DemonCardHoverDetailPreviewCanvas(canvas, scaler);
        }

        internal void MatchInlineSpriteMaterialQueues(
            GameHudContractDetailView detailView)
        {
            RestoreInlineSpriteMaterials();
            if (detailView == null)
            {
                return;
            }

            TMP_Text[] texts =
                detailView.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in texts)
            {
                Material textMaterial = text.fontSharedMaterial;
                if (textMaterial == null)
                {
                    continue;
                }

                text.ForceMeshUpdate(ignoreActiveState: true);
                TMP_SubMeshUI[] subMeshes =
                    text.GetComponentsInChildren<TMP_SubMeshUI>(true);
                foreach (TMP_SubMeshUI subMesh in subMeshes)
                {
                    Material sourceMaterial = subMesh.sharedMaterial;
                    if (subMesh.spriteAsset == null ||
                        sourceMaterial == null ||
                        sourceMaterial.renderQueue >= textMaterial.renderQueue)
                    {
                        continue;
                    }

                    Material previewMaterial = new Material(sourceMaterial)
                    {
                        name = sourceMaterial.name + " (Demon Hover Preview)",
                        hideFlags = HideFlags.HideAndDontSave
                    };
                    previewMaterial.renderQueue = textMaterial.renderQueue;
                    subMesh.sharedMaterial = previewMaterial;
                    _inlineSpriteMaterialOverrides.Add(
                        new InlineSpriteMaterialOverride(
                            subMesh,
                            sourceMaterial,
                            previewMaterial));
                }
            }
        }

        internal void Restore()
        {
            RestoreInlineSpriteMaterials();
            if (_scaler != null)
            {
                UnityEngine.Object.DestroyImmediate(_scaler);
            }

            if (_canvas != null)
            {
                UnityEngine.Object.DestroyImmediate(_canvas);
            }
        }

        private void RestoreInlineSpriteMaterials()
        {
            foreach (InlineSpriteMaterialOverride materialOverride in
                     _inlineSpriteMaterialOverrides)
            {
                materialOverride.Restore();
            }

            _inlineSpriteMaterialOverrides.Clear();
        }

        private sealed class InlineSpriteMaterialOverride
        {
            private readonly TMP_SubMeshUI _subMesh;
            private readonly Material _sourceMaterial;
            private readonly Material _previewMaterial;

            internal InlineSpriteMaterialOverride(
                TMP_SubMeshUI subMesh,
                Material sourceMaterial,
                Material previewMaterial)
            {
                _subMesh = subMesh;
                _sourceMaterial = sourceMaterial;
                _previewMaterial = previewMaterial;
            }

            internal void Restore()
            {
                if (_subMesh != null &&
                    _subMesh.sharedMaterial == _previewMaterial)
                {
                    _subMesh.sharedMaterial = _sourceMaterial;
                }

                if (_previewMaterial != null)
                {
                    UnityEngine.Object.DestroyImmediate(_previewMaterial);
                }
            }
        }
    }

    internal sealed class DemonCardHoverDetailPreviewResumeState
    {
        internal DemonCardHoverDetailPreviewResumeState(
            DemonCardHoverDetailView view,
            int index)
        {
            View = view;
            Index = index;
        }

        internal DemonCardHoverDetailView View { get; }

        internal int Index { get; }

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

    internal sealed class DemonCardHoverDetailPreviewSnapshot
    {
        private readonly Image _faceImage;
        private readonly Sprite _faceSprite;
        private readonly bool _faceEnabled;
        private readonly TMP_Text _titleText;
        private readonly string _title;
        private readonly TMP_Text _englishNameText;
        private readonly string _englishName;
        private readonly TMP_Text _abilityLabelText;
        private readonly string _abilityLabel;
        private readonly TMP_Text _abilityText;
        private readonly string _ability;
        private readonly TMP_Text _costLabelText;
        private readonly string _costLabel;
        private readonly TMP_Text _costText;
        private readonly string _cost;

        private DemonCardHoverDetailPreviewSnapshot(
            Image faceImage,
            TMP_Text titleText,
            TMP_Text englishNameText,
            TMP_Text abilityLabelText,
            TMP_Text abilityText,
            TMP_Text costLabelText,
            TMP_Text costText)
        {
            _faceImage = faceImage;
            _faceSprite = faceImage.sprite;
            _faceEnabled = faceImage.enabled;
            _titleText = titleText;
            _title = titleText.text;
            _englishNameText = englishNameText;
            _englishName = englishNameText.text;
            _abilityLabelText = abilityLabelText;
            _abilityLabel = abilityLabelText.text;
            _abilityText = abilityText;
            _ability = abilityText.text;
            _costLabelText = costLabelText;
            _costLabel = costLabelText.text;
            _costText = costText;
            _cost = costText.text;
        }

        internal static DemonCardHoverDetailPreviewSnapshot Capture(
            DemonCardHoverDetailView view)
        {
            GameHudContractDetailView detailView = view.DetailView;
            if (detailView == null)
            {
                throw new MissingReferenceException(
                    "Detail view reference is missing.");
            }

            SerializedObject detailSerialized =
                new SerializedObject(detailView);
            return new DemonCardHoverDetailPreviewSnapshot(
                GetRequiredReference<Image>(detailSerialized, "faceImage"),
                GetRequiredReference<TMP_Text>(detailSerialized, "titleText"),
                GetRequiredReference<TMP_Text>(
                    detailSerialized,
                    "englishNameText"),
                GetRequiredReference<TMP_Text>(
                    detailSerialized,
                    "abilityLabelText"),
                GetRequiredReference<TMP_Text>(detailSerialized, "abilityText"),
                GetRequiredReference<TMP_Text>(
                    detailSerialized,
                    "costLabelText"),
                GetRequiredReference<TMP_Text>(detailSerialized, "costText"));
        }

        internal void Restore()
        {
            if (_titleText != null)
            {
                _titleText.text = _title;
            }

            if (_englishNameText != null)
            {
                _englishNameText.text = _englishName;
            }

            if (_abilityText != null)
            {
                _abilityText.text = _ability;
            }

            if (_abilityLabelText != null)
            {
                _abilityLabelText.text = _abilityLabel;
            }

            if (_costText != null)
            {
                _costText.text = _cost;
            }

            if (_costLabelText != null)
            {
                _costLabelText.text = _costLabel;
            }

            if (_faceImage != null)
            {
                _faceImage.sprite = _faceSprite;
                _faceImage.enabled = _faceEnabled;
            }
        }

        private static T GetRequiredReference<T>(
            SerializedObject serialized,
            string propertyName)
            where T : UnityEngine.Object
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            T value = property?.objectReferenceValue as T;
            if (value == null)
            {
                throw new MissingReferenceException(
                    $"Preview reference '{propertyName}' is missing.");
            }

            return value;
        }
    }

    [InitializeOnLoad]
    internal static class DemonCardHoverDetailPreviewLifecycle
    {
        private static DemonCardHoverDetailPreviewResumeState
            _prefabResumeState;
        private static DemonCardHoverDetailPreviewResumeState
            _sceneResumeState;

        static DemonCardHoverDetailPreviewLifecycle()
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
            DemonCardHoverDetailPreviewSession.StopActive();
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
            DemonCardHoverDetailPreviewResumeState state =
                DemonCardHoverDetailPreviewSession.CaptureResumeState();
            if (state == null || !state.BelongsTo(root))
            {
                return;
            }

            _prefabResumeState = state;
            DemonCardHoverDetailPreviewSession.StopActive();
        }

        internal static void ResumeAfterPrefabSave(GameObject root)
        {
            DemonCardHoverDetailPreviewResumeState state =
                _prefabResumeState;
            _prefabResumeState = null;
            if (state == null || !state.BelongsTo(root))
            {
                return;
            }

            DemonCardHoverDetailPreviewSession.Resume(state);
        }

        internal static void SuspendForSceneSave(Scene scene)
        {
            DemonCardHoverDetailPreviewResumeState state =
                DemonCardHoverDetailPreviewSession.CaptureResumeState();
            if (state == null || !state.BelongsTo(scene))
            {
                return;
            }

            _sceneResumeState = state;
            DemonCardHoverDetailPreviewSession.StopActive();
        }

        internal static void ResumeAfterSceneSave(Scene scene)
        {
            DemonCardHoverDetailPreviewResumeState state = _sceneResumeState;
            _sceneResumeState = null;
            if (state == null || !state.BelongsTo(scene))
            {
                return;
            }

            DemonCardHoverDetailPreviewSession.Resume(state);
        }

        private static void ClearPendingResume()
        {
            _prefabResumeState = null;
            _sceneResumeState = null;
        }

        private static void StopPreview()
        {
            ClearPendingResume();
            DemonCardHoverDetailPreviewSession.StopActive();
        }
    }
}
#endif
