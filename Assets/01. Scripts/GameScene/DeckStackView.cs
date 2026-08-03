using System.Collections.Generic;
using Border.Audio;
using DiaBlackJack.Rendering;
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
        [SerializeField] private string drawSfxId = "cardDraw";
        [SerializeField] private bool useMaterialHoverOutlineSettings = true;
        [SerializeField] private Color hoverOutlineColor =
            new Color(1f, 0.72f, 0.08f, 1f);
        [SerializeField] private float hoverOutlineWidth = 0.025f;
        [SerializeField] private float hoverOutlineWidthPixels = 4f;

        private const string StencilOutlineKeyword = "_STENCIL_OUTLINE_ON";
        private const string StencilOutlineOnlyShaderName =
            "Hidden/NHN/Stencil Outline Only";
        private const int StencilOutlineRenderQueue = 3000;
        private static readonly int StencilOutlineEnabledId =
            Shader.PropertyToID("_StencilOutlineEnabled");
        private static readonly int StencilOutlineColorId =
            Shader.PropertyToID("_StencilOutlineColor");
        private static readonly int StencilOutlineWidthId =
            Shader.PropertyToID("_StencilOutlineWidth");

        private Vector3 _baseLocalScale;
        private Vector3 _baseLocalPosition;
        private bool _initialized;
        private bool _isHovered;
        private bool _hasRenderedCount;
        private int _displayedCardCount;
        private int _targetCardCount;
        private Coroutine _animationQueueRoutine;
        private readonly Queue<CardStackAnimationKind> _animationQueue =
            new Queue<CardStackAnimationKind>();
        private readonly List<GameObject> _animationPool = new List<GameObject>();
        private readonly List<Material> _hoverMaterialInstances =
            new List<Material>();
        private readonly List<Mesh> _outlineMeshes = new List<Mesh>();
        private readonly List<OutlineRendererBinding> _outlineBindings =
            new List<OutlineRendererBinding>();

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
            hoverOutlineWidth = Mathf.Max(0f, hoverOutlineWidth);
            hoverOutlineWidthPixels = Mathf.Max(0f, hoverOutlineWidthPixels);
            AutoBindMissingReferences();
        }

        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered)
            {
                return;
            }

            _isHovered = hovered;
            ApplyHoverOutline(hovered);
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

        public void ResetView()
        {
            StopAnimationQueue();
            _displayedCardCount = 0;
            _targetCardCount = 0;
            _hasRenderedCount = false;
            ApplyStackCount(0);
        }

        public void PlayReinsertAnimation()
        {
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                return;
            }

            _animationQueue.Enqueue(CardStackAnimationKind.Reinsert);
            if (_animationQueueRoutine == null)
            {
                _animationQueueRoutine = StartCoroutine(PlayAnimationQueue());
            }
        }

        private void ApplyStackCount(int cardCount)
        {
            bool visible = cardCount > 0;
            SetVisible(visible);
            transform.localPosition = _baseLocalPosition;
            if (!visible)
            {
                SetHovered(false);
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

            if (!isActiveAndEnabled)
            {
                StopAnimationQueue();
                _displayedCardCount = _targetCardCount;
                ApplyStackCount(_displayedCardCount);
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
                    PlayDrawSfx();
                    PlayCardAnimation(drawTrigger, null);
                }
                else if (kind == CardStackAnimationKind.Insert)
                {
                    PlayCardAnimation(insertTrigger, ApplyCompletedInsert);
                }
                else
                {
                    PlayCardAnimation(
                        insertTrigger,
                        null,
                        playAtDeckBottom: true);
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

        private void PlayDrawSfx()
        {
            if (string.IsNullOrWhiteSpace(drawSfxId))
            {
                return;
            }

            SoundManager.Current?.PlaySfx(drawSfxId);
        }

        private void PlayCardAnimation(
            string triggerName,
            System.Action onCompleted,
            bool playAtDeckBottom = false)
        {
            GameObject instance = GetAnimationInstance();
            if (instance == null)
            {
                onCompleted?.Invoke();
                return;
            }

            Transform animationTransform = instance.transform;
            animationTransform.SetParent(
                playAtDeckBottom || cardCase == null ? transform : cardCase,
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

        private void ApplyHoverOutline(bool visible)
        {
            AutoBindMissingReferences();
            if (renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                ApplyHoverOutline(renderers[i], visible);
            }
        }

        private void ApplyHoverOutline(Renderer renderer, bool visible)
        {
            if (renderer == null)
            {
                return;
            }

            if (!visible)
            {
                PostProcessOutlineRegistry.Unregister(renderer);
                return;
            }

            Material material = renderer.sharedMaterial;
            PostProcessOutlineRegistry.Register(
                renderer,
                ResolveOutlineColor(material),
                ResolvePostProcessOutlineWidthPixels());
        }

        private OutlineRendererBinding EnsureOutlineBinding(Renderer renderer)
        {
            for (int i = 0; i < _outlineBindings.Count; i++)
            {
                if (_outlineBindings[i].SourceRenderer == renderer)
                {
                    return _outlineBindings[i];
                }
            }

            Material baseMaterial = EnsureStencilWriterMaterialInstance(renderer);
            if (baseMaterial == null ||
                !baseMaterial.HasProperty(StencilOutlineEnabledId))
            {
                return null;
            }

            MeshFilter sourceFilter = renderer.GetComponent<MeshFilter>();
            if (sourceFilter == null || sourceFilter.sharedMesh == null)
            {
                return null;
            }

            Shader outlineShader = Shader.Find(StencilOutlineOnlyShaderName);
            if (outlineShader == null)
            {
                return null;
            }

            Mesh outlineMesh = CreateSmoothedOutlineMesh(sourceFilter.sharedMesh);
            if (outlineMesh == null)
            {
                return null;
            }

            _outlineMeshes.Add(outlineMesh);

            GameObject outlineObject = new GameObject(renderer.gameObject.name + " Outline");
            outlineObject.transform.SetParent(renderer.transform, false);
            outlineObject.layer = renderer.gameObject.layer;

            MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
            outlineFilter.sharedMesh = outlineMesh;

            Material outlineMaterial = new Material(outlineShader)
            {
                name = baseMaterial.name + " Outline",
                renderQueue = StencilOutlineRenderQueue
            };
            _hoverMaterialInstances.Add(outlineMaterial);

            MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
            outlineRenderer.sharedMaterial = outlineMaterial;
            outlineRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            outlineRenderer.receiveShadows = false;
            outlineRenderer.enabled = false;
            outlineRenderer.sortingLayerID = renderer.sortingLayerID;
            outlineRenderer.sortingOrder = renderer.sortingOrder;

            var binding = new OutlineRendererBinding(
                renderer,
                outlineRenderer,
                baseMaterial,
                outlineMaterial);
            _outlineBindings.Add(binding);
            return binding;
        }

        private Material EnsureStencilWriterMaterialInstance(Renderer renderer)
        {
            Material material = renderer.sharedMaterial;
            if (material == null ||
                _hoverMaterialInstances.Contains(material))
            {
                return material;
            }

            if (!material.HasProperty(StencilOutlineEnabledId))
            {
                return material;
            }

            Material instance = new Material(material)
            {
                name = material.name + " (DeckStack Hover)"
            };
            renderer.sharedMaterial = instance;
            _hoverMaterialInstances.Add(instance);
            return instance;
        }

        private Mesh CreateSmoothedOutlineMesh(Mesh sourceMesh)
        {
            if (!sourceMesh.isReadable)
            {
                return null;
            }

            Mesh outlineMesh = Instantiate(sourceMesh);
            outlineMesh.name = sourceMesh.name + " Smooth Outline";

            Vector3[] vertices = outlineMesh.vertices;
            Vector3[] normals = outlineMesh.normals;
            if (normals == null || normals.Length != vertices.Length)
            {
                outlineMesh.RecalculateNormals();
                normals = outlineMesh.normals;
            }

            outlineMesh.normals = CreateSmoothedNormals(vertices, normals);
            return outlineMesh;
        }

        private static Vector3[] CreateSmoothedNormals(
            Vector3[] vertices,
            Vector3[] normals)
        {
            var normalSums = new Dictionary<Vector3Key, Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3Key key = new Vector3Key(vertices[i]);
                normalSums.TryGetValue(key, out Vector3 sum);
                normalSums[key] = sum + normals[i];
            }

            Vector3[] smoothedNormals = new Vector3[normals.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 sum = normalSums[new Vector3Key(vertices[i])];
                smoothedNormals[i] = sum.sqrMagnitude > 0.000001f
                    ? sum.normalized
                    : normals[i];
            }

            return smoothedNormals;
        }

        private Color ResolveOutlineColor(Material material)
        {
            if (useMaterialHoverOutlineSettings &&
                material != null &&
                material.HasProperty(StencilOutlineColorId))
            {
                Color color = material.GetColor(StencilOutlineColorId);
                if (color.a <= 0f)
                {
                    color.a = 1f;
                }

                return color;
            }

            return hoverOutlineColor;
        }

        private float ResolveOutlineWidth(Material material)
        {
            if (useMaterialHoverOutlineSettings &&
                material != null &&
                material.HasProperty(StencilOutlineWidthId))
            {
                return material.GetFloat(StencilOutlineWidthId);
            }

            return hoverOutlineWidth;
        }

        private float ResolvePostProcessOutlineWidthPixels()
        {
            return hoverOutlineWidthPixels;
        }

        private void OnDestroy()
        {
            if (renderers != null)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    PostProcessOutlineRegistry.Unregister(renderers[i]);
                }
            }

            for (int i = 0; i < _hoverMaterialInstances.Count; i++)
            {
                Material material = _hoverMaterialInstances[i];
                if (material != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(material);
                    }
                    else
                    {
                        DestroyImmediate(material);
                    }
                }
            }

            _hoverMaterialInstances.Clear();

            for (int i = 0; i < _outlineMeshes.Count; i++)
            {
                Mesh mesh = _outlineMeshes[i];
                if (mesh != null)
                {
                    if (Application.isPlaying)
                    {
                        Destroy(mesh);
                    }
                    else
                    {
                        DestroyImmediate(mesh);
                    }
                }
            }

            _outlineMeshes.Clear();
            _outlineBindings.Clear();
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
            Insert,
            Reinsert
        }

        private sealed class OutlineRendererBinding
        {
            public OutlineRendererBinding(
                Renderer sourceRenderer,
                MeshRenderer outlineRenderer,
                Material baseMaterial,
                Material outlineMaterial)
            {
                SourceRenderer = sourceRenderer;
                OutlineRenderer = outlineRenderer;
                BaseMaterial = baseMaterial;
                OutlineMaterial = outlineMaterial;
            }

            public Renderer SourceRenderer { get; }
            public MeshRenderer OutlineRenderer { get; }
            public Material BaseMaterial { get; }
            public Material OutlineMaterial { get; }
        }

        private readonly struct Vector3Key : System.IEquatable<Vector3Key>
        {
            private const float Scale = 100000f;
            private readonly int _x;
            private readonly int _y;
            private readonly int _z;

            public Vector3Key(Vector3 value)
            {
                _x = Mathf.RoundToInt(value.x * Scale);
                _y = Mathf.RoundToInt(value.y * Scale);
                _z = Mathf.RoundToInt(value.z * Scale);
            }

            public bool Equals(Vector3Key other)
            {
                return _x == other._x &&
                    _y == other._y &&
                    _z == other._z;
            }

            public override bool Equals(object obj)
            {
                return obj is Vector3Key other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = _x;
                    hashCode = (hashCode * 397) ^ _y;
                    hashCode = (hashCode * 397) ^ _z;
                    return hashCode;
                }
            }
        }
    }
}
