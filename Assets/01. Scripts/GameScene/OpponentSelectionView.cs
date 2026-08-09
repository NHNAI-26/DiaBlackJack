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

        private Vector2[] _restingPosterPositions = Array.Empty<Vector2>();
        private CanvasGroup[] _posterCanvasGroups = Array.Empty<CanvasGroup>();
        private Coroutine _presentationRoutine;
        private Sequence _slideTween;
        private int? _presentedOfferId;
        private string _pendingProfileKey;
        private bool _cameraInputLocked;
        private bool _modelAllowsInteraction;
        private bool _selectionCommitReady;

        public event Action<string> OpponentSelected;

        public bool IsVisible { get; private set; }

        internal bool IsReadyForSelection { get; private set; }

        internal bool IsEntrancePlaying => _presentationRoutine != null;

        /// <summary>Exposed so other flow-driven posters (the final boss reveal) can resolve portraits without a duplicate serialized reference.</summary>
        internal EnemyContentCatalogSO ContentCatalog => enemyContentCatalog;

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

            CaptureRestingPosterPositions();
            int? offerId = model.OpponentOfferId;
            bool isNewOffer = _presentedOfferId != offerId;
            _modelAllowsInteraction = model.CanFocusOpponent;
            if (isNewOffer)
            {
                CancelPresentation(resetPosterPositions: true);
                _presentedOfferId = offerId;
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
                PlayEntranceSequence(offerId));
        }

        public void Hide()
        {
            CancelPresentation(resetPosterPositions: true);
            IsVisible = false;
            IsReadyForSelection = false;
            _modelAllowsInteraction = false;
            _presentedOfferId = null;
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

            _selectionCommitReady = false;
            _pendingProfileKey = null;
            BeginEntranceState();

            if (!playEntranceAnimation || !Application.isPlaying)
            {
                ResetPosterPositions();
                CompleteEntranceState();
                return true;
            }

            MovePostersOffTable();
            _presentationRoutine = StartCoroutine(
                PlayEntranceSequence(_presentedOfferId));
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
                PlayExitSequence(_presentedOfferId, profileKey));
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

        private IEnumerator PlayEntranceSequence(int? offerId)
        {
            GameSceneCameraViewController controller =
                ResolveCameraViewController();
            LockCameraInput(controller);

            if (controller != null &&
                controller.SetView(GameSceneCameraView.Current))
            {
                yield return WaitForCameraTransition(controller);
            }

            if (!IsCurrentOffer(offerId))
            {
                yield break;
            }

            yield return PlayPosterSlide(
                useRestingPosition: true,
                Ease.OutCubic);
            if (!IsCurrentOffer(offerId))
            {
                yield break;
            }

            if (controller != null &&
                controller.SetView(GameSceneCameraView.TableTop))
            {
                yield return WaitForCameraTransition(controller);
            }

            if (!IsCurrentOffer(offerId))
            {
                yield break;
            }

            _presentationRoutine = null;
            CompleteEntranceState();
        }

        private IEnumerator PlayExitSequence(
            int? offerId,
            string profileKey)
        {
            yield return PlayPosterSlide(
                useRestingPosition: false,
                Ease.InCubic);
            if (!IsCurrentSelection(offerId, profileKey))
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

            if (!IsCurrentSelection(offerId, profileKey))
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
            if (!IsCurrentSelection(_presentedOfferId, profileKey))
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

        private bool IsCurrentOffer(int? offerId)
        {
            return IsVisible && _presentedOfferId == offerId;
        }

        private bool IsCurrentSelection(
            int? offerId,
            string profileKey)
        {
            return IsCurrentOffer(offerId) &&
                StringComparer.Ordinal.Equals(
                    _pendingProfileKey,
                    profileKey);
        }

        private void CaptureRestingPosterPositions()
        {
            if (posterSlots == null ||
                _restingPosterPositions.Length == posterSlots.Length)
            {
                return;
            }

            _restingPosterPositions = new Vector2[posterSlots.Length];
            _posterCanvasGroups = new CanvasGroup[posterSlots.Length];
            for (int index = 0; index < posterSlots.Length; index++)
            {
                RectTransform poster = ResolvePosterTransform(index);
                _restingPosterPositions[index] = poster == null
                    ? Vector2.zero
                    : poster.anchoredPosition;
                _posterCanvasGroups[index] = ResolvePosterCanvasGroup(index);
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
