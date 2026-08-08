using System;
using System.Collections;
using Border.Core;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class HammerAnimationController :
        PresentationAnimationEventReceiver
    {
        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject root;
        [SerializeField] private Transform target;
        [SerializeField] private float animationSeconds = 1.8f;
        [SerializeField] private string baseStateName =
            "Base Layer.Hammer_Basic";
        [SerializeField] private string readyTrigger = "PlayerTurnStart";
        [SerializeField] private string playerSmashTrigger = "PlayerChoose";
        [SerializeField] private string enemySmashTrigger = "EnemyAttack";
        [SerializeField] private string readyAttackStateName =
            "Base Layer.Hammer_ReadyAttack";
        [SerializeField] private string playerSmashStateName =
            "Base Layer.Hammer_Smash";
        [SerializeField] private string enemySmashStateName =
            "Base Layer.Hammer_EnemySmash";
        [SerializeField] private float smashStateWaitTimeoutSeconds = 8f;
        [SerializeField] private float targetResolveTimeoutSeconds = 0.5f;

        private bool _hasLastCue;
        private int _lastRoundNumber;
        private int _lastSourceCardId;
        private CombatantSide _lastActorSide;
        private GameSceneHammerAnimationPhase _lastPhase;
        private int _lastActionOrdinal;
        private int? _lastTargetCardId;
        private Coroutine _hideRoutine;
        private HammerPlaybackState _playbackState;
        private GameSceneHammerAnimationCue _queuedCue;
        private CardHand _queuedPlayerHand;
        private CardHand _queuedEnemyHand;
        private bool _hasQueuedTargetPosition;
        private Vector3 _queuedTargetPosition;
        private bool _hasRootBaseWorldPosition;
        private Vector3 _rootBaseWorldPosition;

        public float AnimationSeconds => Mathf.Max(0f, animationSeconds);

        public bool IsSmashAnimationPlaying =>
            _playbackState != HammerPlaybackState.Idle;

        public event Action SmashAnimationFinished;

        protected override void Awake()
        {
            base.Awake();
            animator ??= GetComponent<Animator>();
            root ??= gameObject;
            target ??= transform.Find("Hammer_Rigging/Target");
        }

        public bool TryPlay(
            GameSceneHammerAnimationCue cue,
            CardHand playerHand,
            CardHand enemyHand)
        {
            if (cue == null || ResolveAnimator() == null)
            {
                return false;
            }

            if (IsLastCue(cue))
            {
                return false;
            }

            if (_playbackState != HammerPlaybackState.Idle)
            {
                return false;
            }

            _queuedCue = cue;
            _queuedPlayerHand = playerHand;
            _queuedEnemyHand = enemyHand;
            _hasQueuedTargetPosition = false;

            GameObject resolvedRoot = ResolveRoot();
            CaptureRootBasePosition(resolvedRoot);
            RestoreRootBasePosition();
            if (resolvedRoot != null && !resolvedRoot.activeSelf)
            {
                resolvedRoot.SetActive(true);
            }

            Animator resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator == null || !resolvedAnimator.gameObject.activeInHierarchy)
            {
                return false;
            }

            StopHideRoutine();
            ResetTriggers();

            if (cue.Phase == GameSceneHammerAnimationPhase.Ready)
            {
                ResetAnimatorToBase();
                resolvedAnimator.SetTrigger(readyTrigger);
                resolvedAnimator.Update(0f);
                RememberCue(cue);
                return true;
            }

            if (cue.ActorSide == CombatantSide.Enemy)
            {
                ResetAnimatorToBase();
            }

            if (TryBeginQueuedSmash(resolvedAnimator, cue))
            {
                if (Application.isPlaying)
                {
                    _hideRoutine = StartCoroutine(HideWhenSmashStateEnds(
                        resolvedAnimator,
                        cue.ActorSide));
                }

                return true;
            }

            if (Application.isPlaying)
            {
                _playbackState = HammerPlaybackState.Preparing;
                _hideRoutine = StartCoroutine(PrepareAndPlaySmash(
                    resolvedAnimator,
                    cue));
                return true;
            }

            ClearQueuedPlayback();
            return false;
        }

        public bool MoveTargetToQueuedCard()
        {
            if (_queuedCue == null)
            {
                Log.W("[HammerAnimationController] Cannot move target because no hammer cue is queued.", this);
                return false;
            }

            if (!CaptureQueuedTargetPosition())
            {
                return false;
            }

            return ApplyQueuedTargetPosition();
        }

        private bool CaptureQueuedTargetPosition()
        {
            if (_queuedCue == null)
            {
                Log.W("[HammerAnimationController] Cannot capture a target position because no hammer cue is queued.", this);
                return false;
            }

            if (!TryResolveTargetPosition(_queuedCue, out Vector3 position))
            {
                Log.W("[HammerAnimationController] Cannot find a hammer target card position.", this);
                return false;
            }

            _queuedTargetPosition = position;
            _hasQueuedTargetPosition = true;
            return true;
        }

        private bool ApplyQueuedTargetPosition()
        {
            if (_queuedCue == null)
            {
                Log.W("[HammerAnimationController] Cannot apply a hammer target position before a cue is queued.", this);
                return false;
            }

            Transform resolvedTarget = ResolveTarget();
            if (resolvedTarget == null)
            {
                Log.W("[HammerAnimationController] Cannot move target because no target transform is assigned.", this);
                return false;
            }

            // Re-resolve fresh every time this is called rather than reusing the
            // position captured back when the cue first started: the target
            // hand's layout can shift between then and the animation actually
            // reaching its smash frame (other cards in that hand being drawn,
            // used, or discarded reflows it), which is what let the hammer land
            // on a stale spot instead of the card it's actually meant to hit.
            if (TryResolveTargetPosition(_queuedCue, out Vector3 freshPosition))
            {
                _queuedTargetPosition = freshPosition;
                _hasQueuedTargetPosition = true;
            }

            if (!_hasQueuedTargetPosition)
            {
                // Never resolved any position at all, not even back when the cue
                // first started — _queuedTargetPosition is still its zeroed-out
                // default. Applying that would snap the target (and the hammer
                // with it) to the world origin right as it's about to strike,
                // which is exactly what made the hammer disappear at impact.
                Log.W(
                    "[HammerAnimationController] No hammer target position has resolved.",
                    this);
                return false;
            }

            GameObject resolvedRoot = ResolveRoot();
            if (resolvedRoot == null)
            {
                Log.W(
                    "[HammerAnimationController] Cannot align hammer because no root is assigned.",
                    this);
                return false;
            }

            // Target is authored by every hammer Animator state. Writing its
            // position directly is lost on the next Animator update. Move the
            // non-animated root by the required delta instead, leaving the rig
            // free to animate while its target remains over the selected card.
            return TryAlignRootToTarget(
                resolvedRoot.transform,
                resolvedTarget,
                _queuedTargetPosition);
        }

        internal static bool TryAlignRootToTarget(
            Transform rootTransform,
            Transform animatedTarget,
            Vector3 desiredTargetPosition)
        {
            if (rootTransform == null || animatedTarget == null ||
                rootTransform == animatedTarget)
            {
                return false;
            }

            rootTransform.position +=
                desiredTargetPosition - animatedTarget.position;
            return true;
        }

        public void Hide()
        {
            bool wasSmashAnimationPlaying =
                _playbackState != HammerPlaybackState.Idle;
            StopHideRoutine();
            _playbackState = HammerPlaybackState.Idle;
            ResetAnimatorToBase();
            RestoreRootBasePosition();
            ClearQueuedPlayback();

            GameObject resolvedRoot = ResolveRoot();
            if (resolvedRoot != null)
            {
                resolvedRoot.SetActive(false);
            }

            if (wasSmashAnimationPlaying)
            {
                SmashAnimationFinished?.Invoke();
            }
        }

        public void ResetPresentationState()
        {
            Hide();
            _hasLastCue = false;
            _lastRoundNumber = 0;
            _lastSourceCardId = 0;
            _lastActorSide = CombatantSide.Player;
            _lastPhase = GameSceneHammerAnimationPhase.Ready;
            _lastActionOrdinal = 0;
            _lastTargetCardId = null;
            _queuedCue = null;
            _queuedPlayerHand = null;
            _queuedEnemyHand = null;
        }

        private bool TryBeginQueuedSmash(
            Animator resolvedAnimator,
            GameSceneHammerAnimationCue cue)
        {
            if (resolvedAnimator == null || cue == null ||
                !TryResolveTargetPosition(cue, out Vector3 position))
            {
                return false;
            }

            string initialStateName = cue.ActorSide == CombatantSide.Player
                ? readyAttackStateName
                : enemySmashStateName;
            if (!TryPlayState(resolvedAnimator, initialStateName))
            {
                return false;
            }

            _queuedTargetPosition = position;
            _hasQueuedTargetPosition = true;
            if (!ApplyQueuedTargetPosition())
            {
                return false;
            }

            RememberCue(cue);
            _playbackState = Application.isPlaying
                ? HammerPlaybackState.Playing
                : HammerPlaybackState.Idle;
            return true;
        }

        private IEnumerator PrepareAndPlaySmash(
            Animator resolvedAnimator,
            GameSceneHammerAnimationCue cue)
        {
            float waitedSeconds = 0f;
            float timeoutSeconds = Mathf.Max(0f, targetResolveTimeoutSeconds);
            while (resolvedAnimator != null &&
                resolvedAnimator.gameObject.activeInHierarchy &&
                waitedSeconds < timeoutSeconds)
            {
                yield return null;
                waitedSeconds += Time.deltaTime;
                if (TryBeginQueuedSmash(resolvedAnimator, cue))
                {
                    yield return HideWhenSmashStateEnds(
                        resolvedAnimator,
                        cue.ActorSide);
                    yield break;
                }
            }

            Log.E("[HammerAnimationController] Hammer smash did not start because its target card never became available.", this);
            _hideRoutine = null;
            Hide();
        }

        private void ClearQueuedPlayback()
        {
            _hasQueuedTargetPosition = false;
            _queuedCue = null;
            _queuedPlayerHand = null;
            _queuedEnemyHand = null;
        }

        private bool TryResolveTargetPosition(
            GameSceneHammerAnimationCue cue,
            out Vector3 position)
        {
            CardHand targetHand = cue.ActorSide == CombatantSide.Player
                ? _queuedEnemyHand
                : _queuedPlayerHand;

            if (targetHand != null &&
                cue.TargetCardId.HasValue &&
                targetHand.TryGetCardLayoutWorldPosition(
                    cue.TargetCardId.Value,
                    out position))
            {
                return true;
            }

            position = default;
            return false;
        }

        private IEnumerator HideWhenSmashStateEnds(
            Animator resolvedAnimator,
            CombatantSide actorSide)
        {
            string smashStateName = ResolveSmashStateName(actorSide);
            if (resolvedAnimator == null || string.IsNullOrWhiteSpace(smashStateName))
            {
                yield return new WaitForSeconds(AnimationSeconds);
                _hideRoutine = null;
                Hide();
                yield break;
            }

            if (actorSide == CombatantSide.Player)
            {
                yield return WaitForStateToComplete(
                    resolvedAnimator,
                    readyAttackStateName);
                if (!TryPlayState(resolvedAnimator, smashStateName))
                {
                    _hideRoutine = null;
                    Hide();
                    yield break;
                }

                if (!ApplyQueuedTargetPosition())
                {
                    _hideRoutine = null;
                    Hide();
                    yield break;
                }
            }
            else if (!ApplyQueuedTargetPosition())
            {
                _hideRoutine = null;
                Hide();
                yield break;
            }

            yield return WaitForStateToComplete(
                resolvedAnimator,
                smashStateName);

            _hideRoutine = null;
            Hide();
        }

        private IEnumerator WaitForStateToComplete(
            Animator resolvedAnimator,
            string stateName)
        {
            if (resolvedAnimator == null || string.IsNullOrWhiteSpace(stateName))
            {
                yield break;
            }

            float waitedSeconds = 0f;
            bool enteredState = IsCurrentState(resolvedAnimator, stateName);
            while (!enteredState &&
                waitedSeconds < Mathf.Max(0f, smashStateWaitTimeoutSeconds))
            {
                waitedSeconds += Time.deltaTime;
                enteredState = IsCurrentState(resolvedAnimator, stateName);
                yield return null;
            }

            if (!enteredState)
            {
                yield break;
            }

            while (resolvedAnimator != null &&
                resolvedAnimator.gameObject.activeInHierarchy &&
                waitedSeconds < Mathf.Max(0f, smashStateWaitTimeoutSeconds))
            {
                AnimatorStateInfo state = resolvedAnimator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(stateName) && state.normalizedTime >= 1f)
                {
                    break;
                }

                waitedSeconds += Time.deltaTime;
                yield return null;
            }
        }

        private static bool TryPlayState(
            Animator resolvedAnimator,
            string stateName)
        {
            if (resolvedAnimator == null ||
                !resolvedAnimator.gameObject.activeInHierarchy ||
                string.IsNullOrWhiteSpace(stateName))
            {
                return false;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (!resolvedAnimator.HasState(0, stateHash))
            {
                Log.E(
                    $"[HammerAnimationController] Animator state '{stateName}' does not exist.",
                    resolvedAnimator);
                return false;
            }

            resolvedAnimator.Play(stateHash, 0, 0f);
            resolvedAnimator.Update(0f);
            return IsCurrentState(resolvedAnimator, stateName);
        }

        private string ResolveSmashStateName(CombatantSide actorSide)
        {
            return actorSide == CombatantSide.Player
                ? playerSmashStateName
                : enemySmashStateName;
        }

        private static bool IsCurrentState(Animator animator, string stateName)
        {
            return animator != null &&
                !string.IsNullOrWhiteSpace(stateName) &&
                animator.gameObject.activeInHierarchy &&
                animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
        }

        private bool IsLastCue(GameSceneHammerAnimationCue cue)
        {
            return _hasLastCue &&
                _lastRoundNumber == cue.RoundNumber &&
                _lastSourceCardId == cue.SourceCardId &&
                _lastActorSide == cue.ActorSide &&
                _lastPhase == cue.Phase &&
                _lastActionOrdinal == cue.ActionOrdinal &&
                _lastTargetCardId == cue.TargetCardId;
        }

        private void RememberCue(GameSceneHammerAnimationCue cue)
        {
            _hasLastCue = true;
            _lastRoundNumber = cue.RoundNumber;
            _lastSourceCardId = cue.SourceCardId;
            _lastActorSide = cue.ActorSide;
            _lastPhase = cue.Phase;
            _lastActionOrdinal = cue.ActionOrdinal;
            _lastTargetCardId = cue.TargetCardId;
        }

        private void StopHideRoutine()
        {
            if (_hideRoutine == null)
            {
                return;
            }

            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }

        private void ResetAnimatorToBase()
        {
            Animator resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator == null ||
                string.IsNullOrWhiteSpace(baseStateName) ||
                !resolvedAnimator.gameObject.activeInHierarchy)
            {
                return;
            }

            resolvedAnimator.Play(baseStateName, 0, 0f);
            resolvedAnimator.Update(0f);
        }

        private void ResetTriggers()
        {
            ResetTrigger(readyTrigger);
            ResetTrigger(playerSmashTrigger);
            ResetTrigger(enemySmashTrigger);
        }

        private void ResetTrigger(string triggerName)
        {
            Animator resolvedAnimator = ResolveAnimator();
            if (resolvedAnimator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                resolvedAnimator.ResetTrigger(triggerName);
            }
        }

        private Animator ResolveAnimator()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            return animator;
        }

        private GameObject ResolveRoot()
        {
            if (root == null)
            {
                root = gameObject;
            }

            return root;
        }

        private Transform ResolveTarget()
        {
            if (target == null)
            {
                target = transform.Find("Hammer_Rigging/Target");
            }

            return target;
        }

        private void CaptureRootBasePosition(GameObject resolvedRoot)
        {
            if (_hasRootBaseWorldPosition || resolvedRoot == null)
            {
                return;
            }

            _rootBaseWorldPosition = resolvedRoot.transform.position;
            _hasRootBaseWorldPosition = true;
        }

        private void RestoreRootBasePosition()
        {
            GameObject resolvedRoot = ResolveRoot();
            if (!_hasRootBaseWorldPosition || resolvedRoot == null)
            {
                return;
            }

            resolvedRoot.transform.position = _rootBaseWorldPosition;
        }

        private enum HammerPlaybackState
        {
            Idle,
            Preparing,
            Playing,
        }

    }
}
