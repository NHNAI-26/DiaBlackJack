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
        [SerializeField] private string baseStateName = "Hammer_Basic";
        [SerializeField] private string readyTrigger = "PlayerTurnStart";
        [SerializeField] private string playerSmashTrigger = "PlayerChoose";
        [SerializeField] private string enemySmashTrigger = "EnemyAttack";
        [SerializeField] private string readyAttackStateName = "Hammer_ReadyAttack";
        [SerializeField] private string playerSmashStateName = "Hammer_Smash";
        [SerializeField] private string enemySmashStateName = "Hammer_EnemySmash";
        [SerializeField] private float smashStateWaitTimeoutSeconds = 8f;

        private bool _hasLastCue;
        private int _lastRoundNumber;
        private int _lastSourceCardId;
        private CombatantSide _lastActorSide;
        private GameSceneHammerAnimationPhase _lastPhase;
        private int _lastActionOrdinal;
        private int? _lastTargetCardId;
        private Coroutine _hideRoutine;
        private bool _isSmashAnimationPlaying;
        private GameSceneHammerAnimationCue _queuedCue;
        private CardHand _queuedPlayerHand;
        private CardHand _queuedEnemyHand;
        private bool _hasQueuedTargetPosition;
        private Vector3 _queuedTargetPosition;

        public float AnimationSeconds => Mathf.Max(0f, animationSeconds);

        public bool IsSmashAnimationPlaying => _isSmashAnimationPlaying;

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

            _queuedCue = cue;
            _queuedPlayerHand = playerHand;
            _queuedEnemyHand = enemyHand;
            _hasQueuedTargetPosition = false;

            GameObject resolvedRoot = ResolveRoot();
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

            if (!CaptureQueuedTargetPosition())
            {
                _queuedCue = null;
                return false;
            }

            if (cue.ActorSide == CombatantSide.Enemy)
            {
                ResetAnimatorToBase();
                if (!ApplyQueuedTargetPosition())
                {
                    _queuedCue = null;
                    return false;
                }
            }

            resolvedAnimator.SetTrigger(
                cue.ActorSide == CombatantSide.Player
                    ? playerSmashTrigger
                    : enemySmashTrigger);
            resolvedAnimator.Update(0f);
            RememberCue(cue);

            if (Application.isPlaying)
            {
                _isSmashAnimationPlaying = true;
                _hideRoutine = StartCoroutine(HideWhenSmashStateEnds(
                    resolvedAnimator,
                    cue.ActorSide));
            }

            return true;
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
            if (!_hasQueuedTargetPosition)
            {
                Log.W("[HammerAnimationController] Cannot apply a hammer target position before one is captured.", this);
                return false;
            }

            Transform resolvedTarget = ResolveTarget();
            if (resolvedTarget == null)
            {
                Log.W("[HammerAnimationController] Cannot move target because no target transform is assigned.", this);
                return false;
            }

            resolvedTarget.position = _queuedTargetPosition;
            return true;
        }

        public void Hide()
        {
            bool wasSmashAnimationPlaying = _isSmashAnimationPlaying;
            StopHideRoutine();
            _isSmashAnimationPlaying = false;
            _hasQueuedTargetPosition = false;
            ResetAnimatorToBase();

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

            yield return null;

            if (actorSide == CombatantSide.Player)
            {
                yield return WaitForStateToEnd(resolvedAnimator, readyAttackStateName);
                if (!ApplyQueuedTargetPosition())
                {
                    _hideRoutine = null;
                    Hide();
                    yield break;
                }
            }

            float waitedSeconds = 0f;
            bool enteredSmashState = IsCurrentState(resolvedAnimator, smashStateName);
            while (!enteredSmashState &&
                waitedSeconds < Mathf.Max(0f, smashStateWaitTimeoutSeconds))
            {
                waitedSeconds += Time.deltaTime;
                enteredSmashState = IsCurrentState(resolvedAnimator, smashStateName);
                yield return null;
            }

            if (!enteredSmashState)
            {
                yield return new WaitForSeconds(AnimationSeconds);
                _hideRoutine = null;
                Hide();
                yield break;
            }

            if (actorSide == CombatantSide.Enemy && !ApplyQueuedTargetPosition())
            {
                _hideRoutine = null;
                Hide();
                yield break;
            }

            while (resolvedAnimator != null &&
                resolvedAnimator.gameObject.activeInHierarchy)
            {
                AnimatorStateInfo state = resolvedAnimator.GetCurrentAnimatorStateInfo(0);
                if (!resolvedAnimator.IsInTransition(0) &&
                    state.IsName(smashStateName) &&
                    state.normalizedTime >= 1f)
                {
                    break;
                }

                if (!resolvedAnimator.IsInTransition(0) &&
                    !state.IsName(smashStateName))
                {
                    break;
                }

                yield return null;
            }

            _hideRoutine = null;
            Hide();
        }

        private IEnumerator WaitForStateToEnd(
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
                resolvedAnimator.gameObject.activeInHierarchy)
            {
                AnimatorStateInfo state = resolvedAnimator.GetCurrentAnimatorStateInfo(0);
                if (!resolvedAnimator.IsInTransition(0) &&
                    !state.IsName(stateName))
                {
                    break;
                }

                yield return null;
            }
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

    }
}
