using System;
using System.Collections;
using DG.Tweening;
using DiaBlackJack.Content;
using DiaBlackJack.StageProgression.UI;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class OpponentSelectionView : MonoBehaviour
    {
        private enum PresentationMode
        {
            None,
            OpponentSelection,
            FinalBossReveal
        }

        [SerializeField] private GameObject contentRoot;
        [SerializeField] private EnemyContentCatalogSO enemyContentCatalog;
        [SerializeField] private OpponentWantedPosterView[] posterSlots =
            Array.Empty<OpponentWantedPosterView>();

        [Header("World entrance")]
        [SerializeField] private bool playEntranceAnimation;
        [SerializeField] private GameSceneCameraViewController cameraViewController;
        [SerializeField] private float offTableAnchoredY = 1100f;
        [Min(0f)]
        [SerializeField] private float slideDuration = 0.9f;

        private Vector2[] _authoredPosterPositions = Array.Empty<Vector2>();
        private Vector2[] _restingPosterPositions = Array.Empty<Vector2>();
        private CanvasGroup[] _posterCanvasGroups = Array.Empty<CanvasGroup>();
        private Coroutine _presentationRoutine;
        private Sequence _slideTween;
        private PresentationMode _presentationMode;
        private int _presentationVersion;
        private int? _presentedOfferId;
        private string _presentedFinalBossStageId;
        private string _pendingProfileKey;
        private bool _cameraInputLocked;
        private bool _modelAllowsInteraction;
        private bool _selectionCommitReady;

        public event Action<string> OpponentSelected;

        public bool IsVisible { get; private set; }

        internal bool IsReadyForSelection { get; private set; }

        internal bool IsEntrancePlaying => _presentationRoutine != null;

        private void OnEnable()
        {
            SetSlotSubscriptions(true);
        }

        private void OnDisable()
        {
            CancelPresentation(resetPosterPositions: true);
            IsReadyForSelection = false;
            SetSlotSubscriptions(false);
        }

        private void OnValidate()
        {
            slideDuration = Mathf.Max(0f, slideDuration);
        }

        public void Render(StageProgressionViewModel model)
        {
            if (model == null || model.OpponentCandidates.Count == 0)
            {
                Hide();
                return;
            }

            if (enemyContentCatalog == null)
            {
                throw new MissingReferenceException(
                    "OpponentSelectionView requires EnemyContentCatalogSO.");
            }

            if (posterSlots == null ||
                model.OpponentCandidates.Count > posterSlots.Length)
            {
                throw new InvalidOperationException(
                    "OpponentSelectionView does not have enough poster slots.");
            }

            SetSlotSubscriptions(true);
            CapturePosterLayout();
            int? offerId = model.OpponentOfferId;
            bool isNewOffer =
                _presentationMode != PresentationMode.OpponentSelection ||
                _presentedOfferId != offerId;
            _modelAllowsInteraction = model.CanFocusOpponent;
            if (isNewOffer)
            {
                CancelPresentation(resetPosterPositions: true);
                _presentationVersion++;
                _presentationMode = PresentationMode.OpponentSelection;
                _presentedOfferId = offerId;
                _presentedFinalBossStageId = null;
                ApplyOpponentSelectionLayout();
                BeginEntranceState();
            }

            IsVisible = true;
            EnsureWorldCanvasCamera();
            if (contentRoot != null)
            {
                contentRoot.SetActive(true);
            }

            for (int index = 0; index < posterSlots.Length; index++)
            {
                OpponentWantedPosterView slot = posterSlots[index];
                if (slot == null)
                {
                    throw new MissingReferenceException(
                        $"OpponentSelectionView poster slot {index} is missing.");
                }

                if (index >= model.OpponentCandidates.Count)
                {
                    slot.Hide();
                    continue;
                }

                OpponentCandidateViewModel candidate =
                    model.OpponentCandidates[index];
                slot.Render(
                    candidate,
                    enemyContentCatalog.GetPortrait(candidate.ProfileKey),
                    IsReadyForSelection && _modelAllowsInteraction);
            }

            if (!isNewOffer)
            {
                return;
            }

            if (!playEntranceAnimation || !Application.isPlaying)
            {
                ResetPosterPositions();
                CompleteEntranceState();
                return;
            }

            MovePostersOffTable();
            _presentationRoutine = StartCoroutine(
                PlayEntranceSequence(_presentationVersion));
        }

        internal void RenderFinalBossReveal(
            OpponentCandidateViewModel candidate,
            string stageId)
        {
            if (candidate == null)
            {
                Hide();
                return;
            }

            if (string.IsNullOrWhiteSpace(stageId))
            {
                throw new ArgumentException(
                    "Final boss reveal requires a stage id.",
                    nameof(stageId));
            }

            if (enemyContentCatalog == null)
            {
                throw new MissingReferenceException(
                    "OpponentSelectionView requires EnemyContentCatalogSO.");
            }

            if (posterSlots == null || posterSlots.Length == 0)
            {
                throw new InvalidOperationException(
                    "OpponentSelectionView requires at least one poster slot.");
            }

            SetSlotSubscriptions(true);
            CapturePosterLayout();
            bool isNewReveal =
                _presentationMode != PresentationMode.FinalBossReveal ||
                !StringComparer.Ordinal.Equals(
                    _presentedFinalBossStageId,
                    stageId);
            _modelAllowsInteraction = true;
            if (isNewReveal)
            {
                CancelPresentation(resetPosterPositions: true);
                _presentationVersion++;
                _presentationMode = PresentationMode.FinalBossReveal;
                _presentedOfferId = null;
                _presentedFinalBossStageId = stageId;
                ApplyFinalBossLayout();
                BeginEntranceState();
            }

            IsVisible = true;
            EnsureWorldCanvasCamera();
            if (contentRoot != null)
            {
                contentRoot.SetActive(true);
            }

            for (int index = 0; index < posterSlots.Length; index++)
            {
                OpponentWantedPosterView slot = posterSlots[index];
                if (slot == null)
                {
                    throw new MissingReferenceException(
                        $"OpponentSelectionView poster slot {index} is missing.");
                }

                if (index > 0)
                {
                    slot.Hide();
                    continue;
                }

                slot.Render(
                    candidate,
                    enemyContentCatalog.GetPortrait(candidate.ProfileKey),
                    IsReadyForSelection && _modelAllowsInteraction);
            }

            if (!isNewReveal)
            {
                return;
            }

            if (!playEntranceAnimation || !Application.isPlaying)
            {
                ResetPosterPositions();
                CompleteEntranceState();
                return;
            }

            MovePostersOffTable();
            _presentationRoutine = StartCoroutine(
                PlayEntranceSequence(_presentationVersion));
        }

        public void Hide()
        {
            CancelPresentation(resetPosterPositions: true);
            _presentationVersion++;
            IsVisible = false;
            IsReadyForSelection = false;
            _modelAllowsInteraction = false;
            _presentationMode = PresentationMode.None;
            _presentedOfferId = null;
            _presentedFinalBossStageId = null;
            ApplyOpponentSelectionLayout();
            if (posterSlots != null)
            {
                foreach (OpponentWantedPosterView slot in posterSlots)
                {
                    slot?.Hide();
                }
            }

            if (contentRoot != null)
            {
                contentRoot.SetActive(false);
            }
        }

        private void HandlePosterSelected(string profileKey)
        {
            if (!IsVisible || !IsReadyForSelection)
            {
                return;
            }

            BeginSelectionExit(profileKey);
        }

        internal bool CanCommitSelection(
            int offerId,
            string profileKey)
        {
            return _selectionCommitReady &&
                _presentationMode == PresentationMode.OpponentSelection &&
                IsVisible &&
                !IsReadyForSelection &&
                _presentedOfferId == offerId &&
                StringComparer.Ordinal.Equals(
                    _pendingProfileKey,
                    profileKey);
        }

        internal bool RestoreSelectionAfterRejectedCommit(string profileKey)
        {
            if (!_presentedOfferId.HasValue ||
                !CanCommitSelection(
                    _presentedOfferId.Value,
                    profileKey))
            {
                return false;
            }

            return RestoreRejectedCommit();
        }

        internal bool CanCommitFinalBossReveal(
            string stageId,
            string profileKey)
        {
            return _selectionCommitReady &&
                _presentationMode == PresentationMode.FinalBossReveal &&
                IsVisible &&
                !IsReadyForSelection &&
                StringComparer.Ordinal.Equals(
                    _presentedFinalBossStageId,
                    stageId) &&
                StringComparer.Ordinal.Equals(
                    _pendingProfileKey,
                    profileKey);
        }

        internal bool RestoreFinalBossRevealAfterRejectedCommit(
            string stageId,
            string profileKey)
        {
            if (!CanCommitFinalBossReveal(stageId, profileKey))
            {
                return false;
            }

            return RestoreRejectedCommit();
        }

        private bool RestoreRejectedCommit()
        {
            _selectionCommitReady = false;
            _pendingProfileKey = null;
            BeginEntranceState();

            if (!playEntranceAnimation || !Application.isPlaying)
            {
                SetPosterAlpha(1f);
                ResetPosterPositions();
                CompleteEntranceState();
                return true;
            }

            MovePostersOffTable();
            _presentationRoutine = StartCoroutine(
                PlayEntranceSequence(_presentationVersion));
            return true;
        }

        private void BeginSelectionExit(string profileKey)
        {
            IsReadyForSelection = false;
            _pendingProfileKey = profileKey;
            _selectionCommitReady = false;
            LockCameraInput(ResolveCameraViewController());
            foreach (OpponentWantedPosterView slot in posterSlots)
            {
                slot?.SetInteractable(false);
            }

            if (!playEntranceAnimation || !Application.isPlaying)
            {
                SetPosterAlpha(0f);
                CompleteSelectionExit(profileKey);
                return;
            }

            _presentationRoutine = StartCoroutine(
                PlayExitSequence(_presentationVersion, profileKey));
        }

        internal void BeginEntranceState()
        {
            IsReadyForSelection = false;
            if (playEntranceAnimation)
            {
                LockCameraInput(ResolveCameraViewController());
            }

            SetSlotsInteractable(false);
        }

        internal void CompleteEntranceState()
        {
            _pendingProfileKey = null;
            _selectionCommitReady = false;
            IsReadyForSelection = true;
            SetSlotsInteractable(_modelAllowsInteraction);
        }

        private IEnumerator PlayEntranceSequence(int presentationVersion)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            LockCameraInput(controller);

            if (controller != null &&
                controller.SetView(GameSceneCameraView.Current))
            {
                yield return WaitForCameraTransition(controller);
            }

            if (!IsCurrentPresentation(presentationVersion))
            {
                yield break;
            }

            yield return PlayPosterSlide(
                useRestingPosition: true,
                Ease.OutCubic);
            if (!IsCurrentPresentation(presentationVersion))
            {
                yield break;
            }

            if (controller != null &&
                controller.SetView(GameSceneCameraView.TableTop))
            {
                yield return WaitForCameraTransition(controller);
            }

            if (!IsCurrentPresentation(presentationVersion))
            {
                yield break;
            }

            _presentationRoutine = null;
            CompleteEntranceState();
        }

        private IEnumerator PlayExitSequence(
            int presentationVersion,
            string profileKey)
        {
            yield return PlayPosterSlide(
                useRestingPosition: false,
                Ease.InCubic);
            if (!IsCurrentSelection(presentationVersion, profileKey))
            {
                yield break;
            }

            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            if (controller != null &&
                controller.SetView(GameSceneCameraView.Current))
            {
                yield return WaitForCameraTransition(controller);
            }

            if (!IsCurrentSelection(presentationVersion, profileKey))
            {
                yield break;
            }

            _presentationRoutine = null;
            CompleteSelectionExit(profileKey);
        }

        private IEnumerator PlayPosterSlide(
            bool useRestingPosition,
            Ease ease)
        {
            Border.Audio.SoundManager.Current?.PlaySfx("paperSlide");

            _slideTween = DOTween.Sequence()
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            for (int index = 0; index < posterSlots.Length; index++)
            {
                RectTransform poster = ResolvePosterTransform(index);
                if (poster == null)
                {
                    continue;
                }

                float targetY = useRestingPosition
                    ? _restingPosterPositions[index].y
                    : offTableAnchoredY;
                _slideTween.Join(
                    poster.DOLocalMove(
                            new Vector3(
                                _restingPosterPositions[index].x,
                                targetY,
                                poster.localPosition.z),
                            slideDuration)
                        .SetEase(ease));

                CanvasGroup canvasGroup = ResolvePosterCanvasGroup(index);
                if (canvasGroup != null)
                {
                    float targetAlpha = useRestingPosition ? 1f : 0f;
                    _slideTween.Join(
                        DOTween.To(
                                () => canvasGroup.alpha,
                                value => canvasGroup.alpha = value,
                                targetAlpha,
                                slideDuration)
                            .SetEase(ease));
                }
            }

            yield return _slideTween.WaitForCompletion();
            _slideTween = null;
        }

        private void CompleteSelectionExit(string profileKey)
        {
            if (!IsCurrentSelection(_presentationVersion, profileKey))
            {
                return;
            }

            _selectionCommitReady = true;
            OpponentSelected?.Invoke(profileKey);
        }

        private static IEnumerator WaitForCameraTransition(
            GameSceneCameraViewController controller)
        {
            yield return null;
            while (controller != null && controller.IsTransitioning)
            {
                yield return null;
            }
        }

        private bool IsCurrentPresentation(int presentationVersion)
        {
            return IsVisible && _presentationVersion == presentationVersion;
        }

        private bool IsCurrentSelection(
            int presentationVersion,
            string profileKey)
        {
            return IsCurrentPresentation(presentationVersion) &&
                StringComparer.Ordinal.Equals(
                    _pendingProfileKey,
                    profileKey);
        }

        private void CapturePosterLayout()
        {
            if (posterSlots == null ||
                _authoredPosterPositions.Length == posterSlots.Length)
            {
                return;
            }

            _authoredPosterPositions = new Vector2[posterSlots.Length];
            _restingPosterPositions = new Vector2[posterSlots.Length];
            _posterCanvasGroups = new CanvasGroup[posterSlots.Length];
            for (int index = 0; index < posterSlots.Length; index++)
            {
                RectTransform poster = ResolvePosterTransform(index);
                _authoredPosterPositions[index] = poster == null
                    ? Vector2.zero
                    : poster.anchoredPosition;
                _restingPosterPositions[index] =
                    _authoredPosterPositions[index];
                _posterCanvasGroups[index] = ResolvePosterCanvasGroup(index);
            }
        }

        private void ApplyOpponentSelectionLayout()
        {
            ApplyPosterLayout(singleCenteredPoster: false);
        }

        private void ApplyFinalBossLayout()
        {
            ApplyPosterLayout(singleCenteredPoster: true);
        }

        private void ApplyPosterLayout(bool singleCenteredPoster)
        {
            if (_authoredPosterPositions.Length == 0 ||
                _authoredPosterPositions.Length !=
                _restingPosterPositions.Length)
            {
                return;
            }

            for (int index = 0;
                index < _authoredPosterPositions.Length;
                index++)
            {
                Vector2 resting = _authoredPosterPositions[index];
                if (singleCenteredPoster && index == 0)
                {
                    resting.x = 0f;
                }

                _restingPosterPositions[index] = resting;
                RectTransform poster = ResolvePosterTransform(index);
                if (poster != null)
                {
                    poster.anchoredPosition = resting;
                }
            }
        }

        private void MovePostersOffTable()
        {
            SetPosterAlpha(1f);
            for (int index = 0; index < posterSlots.Length; index++)
            {
                RectTransform poster = ResolvePosterTransform(index);
                if (poster != null)
                {
                    Vector2 resting = _restingPosterPositions[index];
                    poster.anchoredPosition = new Vector2(
                        resting.x,
                        offTableAnchoredY);
                }
            }
        }

        private void ResetPosterPositions()
        {
            SetPosterAlpha(1f);
            for (int index = 0; index < _restingPosterPositions.Length; index++)
            {
                RectTransform poster = ResolvePosterTransform(index);
                if (poster != null)
                {
                    poster.anchoredPosition =
                        _restingPosterPositions[index];
                }
            }
        }

        private RectTransform ResolvePosterTransform(int index)
        {
            if (posterSlots == null ||
                index < 0 ||
                index >= posterSlots.Length ||
                posterSlots[index] == null)
            {
                return null;
            }

            return posterSlots[index].transform as RectTransform;
        }

        private CanvasGroup ResolvePosterCanvasGroup(int index)
        {
            if (posterSlots == null ||
                index < 0 ||
                index >= posterSlots.Length ||
                posterSlots[index] == null)
            {
                return null;
            }

            if (_posterCanvasGroups.Length == posterSlots.Length &&
                _posterCanvasGroups[index] != null)
            {
                return _posterCanvasGroups[index];
            }

            CanvasGroup canvasGroup =
                posterSlots[index].GetComponent<CanvasGroup>();
            if (canvasGroup == null && playEntranceAnimation)
            {
                canvasGroup = posterSlots[index].gameObject
                    .AddComponent<CanvasGroup>();
            }

            return canvasGroup;
        }

        private void SetPosterAlpha(float alpha)
        {
            for (int index = 0; index < posterSlots.Length; index++)
            {
                CanvasGroup canvasGroup = ResolvePosterCanvasGroup(index);
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = alpha;
                }
            }
        }

        private GameSceneCameraViewController ResolveCameraViewController()
        {
            cameraViewController ??=
                FindFirstObjectByType<GameSceneCameraViewController>(
                    FindObjectsInactive.Include);
            return cameraViewController;
        }

        private void EnsureWorldCanvasCamera()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null &&
                canvas.renderMode == RenderMode.WorldSpace &&
                canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }
        }

        private void LockCameraInput(
            GameSceneCameraViewController controller)
        {
            if (_cameraInputLocked || controller == null)
            {
                return;
            }

            controller.LockSwitchInput();
            _cameraInputLocked = true;
        }

        private void UnlockCameraInput(
            GameSceneCameraViewController controller = null)
        {
            if (!_cameraInputLocked)
            {
                return;
            }

            controller ??= ResolveCameraViewController();
            controller?.UnlockSwitchInput();
            _cameraInputLocked = false;
        }

        private void CancelPresentation(bool resetPosterPositions)
        {
            if (_presentationRoutine != null)
            {
                StopCoroutine(_presentationRoutine);
                _presentationRoutine = null;
            }

            _slideTween?.Kill(complete: false);
            _slideTween = null;
            _pendingProfileKey = null;
            _selectionCommitReady = false;
            UnlockCameraInput();
            if (resetPosterPositions)
            {
                ResetPosterPositions();
            }
        }

        private void SetSlotsInteractable(bool interactable)
        {
            if (posterSlots == null)
            {
                return;
            }

            foreach (OpponentWantedPosterView slot in posterSlots)
            {
                slot?.SetInteractable(interactable);
            }
        }

        private void SetSlotSubscriptions(bool subscribe)
        {
            if (posterSlots == null)
            {
                return;
            }

            foreach (OpponentWantedPosterView slot in posterSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                slot.Selected -= HandlePosterSelected;
                if (subscribe)
                {
                    slot.Selected += HandlePosterSelected;
                }
            }
        }
    }
}
