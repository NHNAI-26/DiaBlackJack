using System.Collections.Generic;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class DeckStackView : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Collider[] colliders;
        [SerializeField] private float minimumHeight = 0.08f;
        [SerializeField] private float heightPerCard = 0.035f;
        [SerializeField] private float maximumHeight = 1.2f;
        [SerializeField] private Transform cardCase;
        [SerializeField] private GameObject cardAnimationPrefab;
        [SerializeField] private float cardAnimationIntervalSeconds = 0.08f;
        [SerializeField] private float cardAnimationSeconds = 0.55f;
        [SerializeField] private string drawTrigger = "Draw";
        [SerializeField] private string insertTrigger = "Insert";

        private Vector3 _baseLocalScale;
        private Vector3 _baseLocalPosition;
        private bool _initialized;
        private bool _hasRenderedCount;
        private int _displayedCardCount;
        private int _targetCardCount;
        private Coroutine _animationQueueRoutine;
        private readonly Queue<CardStackAnimationKind> _animationQueue =
            new Queue<CardStackAnimationKind>();
        private readonly List<GameObject> _animationPool = new List<GameObject>();

        private void Awake()
        {
            CaptureBaseTransform();
            AutoBindMissingReferences();
            PrepareAuthoredAnimationInstance();
        }

        private void Reset()
        {
            CaptureBaseTransform();
            AutoBindMissingReferences();
        }

        private void OnValidate()
        {
            minimumHeight = Mathf.Max(0.001f, minimumHeight);
            heightPerCard = Mathf.Max(0f, heightPerCard);
            maximumHeight = Mathf.Max(minimumHeight, maximumHeight);
            cardAnimationIntervalSeconds = Mathf.Max(0f, cardAnimationIntervalSeconds);
            cardAnimationSeconds = Mathf.Max(0f, cardAnimationSeconds);
            AutoBindMissingReferences();
        }

        public void Render(int cardCount)
        {
            CaptureBaseTransform();
            AutoBindMissingReferences();

            cardCount = Mathf.Max(0, cardCount);
            if (!_hasRenderedCount || !Application.isPlaying)
            {
                StopAnimationQueue();
                _displayedCardCount = cardCount;
                _targetCardCount = cardCount;
                _hasRenderedCount = true;
                ApplyStackCount(cardCount);
                return;
            }

            if (cardCount == _targetCardCount)
            {
                return;
            }

            int delta = cardCount - _targetCardCount;
            _targetCardCount = cardCount;
            EnqueueAnimations(delta);
        }

        private void ApplyStackCount(int cardCount)
        {
            bool visible = cardCount > 0;
            SetVisible(visible);
            transform.localPosition = _baseLocalPosition;
            if (!visible)
            {
                return;
            }

            float height = Mathf.Clamp(
                minimumHeight + (cardCount - 1) * heightPerCard,
                minimumHeight,
                maximumHeight);
            Vector3 scale = _baseLocalScale;
            scale.y = height;
            transform.localScale = scale;
        }

        private void EnqueueAnimations(int delta)
        {
            if (delta == 0)
            {
                return;
            }

            CardStackAnimationKind kind = delta < 0
                ? CardStackAnimationKind.Draw
                : CardStackAnimationKind.Insert;
            int count = Mathf.Abs(delta);
            for (int i = 0; i < count; i++)
            {
                _animationQueue.Enqueue(kind);
            }

            if (_animationQueueRoutine == null)
            {
                _animationQueueRoutine = StartCoroutine(PlayAnimationQueue());
            }
        }

        private System.Collections.IEnumerator PlayAnimationQueue()
        {
            while (_animationQueue.Count > 0)
            {
                CardStackAnimationKind kind = _animationQueue.Dequeue();
                if (kind == CardStackAnimationKind.Draw)
                {
                    _displayedCardCount = Mathf.Max(0, _displayedCardCount - 1);
                    ApplyStackCount(_displayedCardCount);
                    PlayCardAnimation(drawTrigger, null);
                }
                else
                {
                    PlayCardAnimation(insertTrigger, ApplyCompletedInsert);
                }

                if (_animationQueue.Count > 0 && cardAnimationIntervalSeconds > 0f)
                {
                    yield return new WaitForSeconds(cardAnimationIntervalSeconds);
                }
                else
                {
                    yield return null;
                }
            }

            _animationQueueRoutine = null;
        }

        private void ApplyCompletedInsert()
        {
            _displayedCardCount = Mathf.Min(_targetCardCount, _displayedCardCount + 1);
            ApplyStackCount(_displayedCardCount);
        }

        private void PlayCardAnimation(string triggerName, System.Action onCompleted)
        {
            GameObject instance = GetAnimationInstance();
            if (instance == null)
            {
                onCompleted?.Invoke();
                return;
            }

            Transform animationTransform = instance.transform;
            animationTransform.SetParent(
                cardCase != null ? cardCase : transform,
                worldPositionStays: false);
            instance.SetActive(true);

            Animator animator = instance.GetComponent<Animator>();
            if (animator != null && !string.IsNullOrWhiteSpace(triggerName))
            {
                animator.Rebind();
                animator.Update(0f);
                animator.ResetTrigger(drawTrigger);
                animator.ResetTrigger(insertTrigger);
                animator.SetTrigger(triggerName);
            }

            StartCoroutine(DeactivateAnimationAfterDelay(instance, onCompleted));
        }

        private System.Collections.IEnumerator DeactivateAnimationAfterDelay(
            GameObject instance,
            System.Action onCompleted)
        {
            if (cardAnimationSeconds > 0f)
            {
                yield return new WaitForSeconds(cardAnimationSeconds);
            }
            else
            {
                yield return null;
            }

            if (instance != null)
            {
                instance.SetActive(false);
            }

            onCompleted?.Invoke();
        }

        private GameObject GetAnimationInstance()
        {
            PrepareAuthoredAnimationInstance();
            for (int i = 0; i < _animationPool.Count; i++)
            {
                GameObject pooled = _animationPool[i];
                if (pooled != null && !pooled.activeSelf)
                {
                    return pooled;
                }
            }

            if (cardAnimationPrefab == null)
            {
                return null;
            }

            GameObject instance = Instantiate(
                cardAnimationPrefab,
                cardCase != null ? cardCase : transform);
            instance.name = cardAnimationPrefab.name;
            _animationPool.Add(instance);
            return instance;
        }

        private void PrepareAuthoredAnimationInstance()
        {
            if (cardAnimationPrefab == null || _animationPool.Contains(cardAnimationPrefab))
            {
                return;
            }

            Transform prefabTransform = cardAnimationPrefab.transform;
            Transform expectedParent = cardCase != null ? cardCase : transform;
            if (prefabTransform == expectedParent ||
                prefabTransform.IsChildOf(expectedParent))
            {
                cardAnimationPrefab.SetActive(false);
                _animationPool.Add(cardAnimationPrefab);
            }
        }

        private void StopAnimationQueue()
        {
            if (_animationQueueRoutine != null)
            {
                StopCoroutine(_animationQueueRoutine);
                _animationQueueRoutine = null;
            }

            _animationQueue.Clear();
            StopAllCoroutines();
            for (int i = 0; i < _animationPool.Count; i++)
            {
                if (_animationPool[i] != null)
                {
                    _animationPool[i].SetActive(false);
                }
            }
        }

        private void SetVisible(bool visible)
        {
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].enabled = visible;
                    }
                }
            }

            if (colliders != null)
            {
                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = visible;
                    }
                }
            }
        }

        private void CaptureBaseTransform()
        {
            if (_initialized)
            {
                return;
            }

            _baseLocalScale = transform.localScale;
            _baseLocalPosition = transform.localPosition;
            _initialized = true;
        }

        private void AutoBindMissingReferences()
        {
            if (cardCase == null)
            {
                Transform child = transform.Find("CardCase");
                if (child != null)
                {
                    cardCase = child;
                }
            }

            if (cardAnimationPrefab == null && cardCase != null)
            {
                Transform child = cardCase.Find("Card_Anim");
                if (child != null)
                {
                    cardAnimationPrefab = child.gameObject;
                }
            }

            if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            }

            if (colliders == null || colliders.Length == 0)
            {
                colliders = GetComponentsInChildren<Collider>(includeInactive: true);
            }
        }

        private enum CardStackAnimationKind
        {
            Draw,
            Insert
        }
    }
}
