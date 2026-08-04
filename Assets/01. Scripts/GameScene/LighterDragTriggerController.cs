using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DiaBlackJack.GameScene
{
    [DisallowMultipleComponent]
    public sealed class LighterDragTriggerController : MonoBehaviour
    {
        private enum DragTarget
        {
            None,
            Cover,
            Wheel
        }

        private enum InteractionState
        {
            WaitingForStart,
            ReadyForCover,
            ReadyForFire,
            Fired
        }

        [Header("Bindings")]
        [SerializeField] private Animator animator;
        [SerializeField] private LighterAnimationEventReceiver animationEvents;
        [SerializeField] private Renderer coverRenderer;
        [SerializeField] private Renderer wheelRenderer;
        [SerializeField] private SpriteRenderer burnCardRenderer;

        [Header("Child lookup")]
        [SerializeField] private string coverRendererName = "cover";
        [SerializeField] private string wheelRendererName = "wheel";
        [SerializeField] private string burnCardRendererName = "Square";

        [Header("Animator")]
        [SerializeField] private int baseLayerIndex;
        [SerializeField] private int startLayerIndex = 1;
        [SerializeField] private string startStateName = "Lighter_Start";
        [SerializeField] private string readyStateName = "Lighter_Loop";
        [SerializeField] private string coverCloseStateName = "Lighter_CoverClose";
        [SerializeField] private string coverOpenTrigger = "CoverOpen";
        [SerializeField] private string fireOnTrigger = "FireOn";

        [Header("Drag")]
        [Min(1f)]
        [SerializeField] private float dragThresholdPixels = 45f;
        [Min(0f)]
        [SerializeField] private float screenBoundsPaddingPixels = 16f;
        [Min(1f)]
        [SerializeField] private float verticalBias = 1.2f;

        [Header("Hover Fresnel")]
        [SerializeField] private string fresnelIntensityProperty = "_RimIntensity";
        [Min(0f)]
        [SerializeField] private float hoverFresnelIntensity = 10f;
        [Min(0f)]
        [SerializeField] private float idleFresnelIntensity;

        private static readonly int DefaultFresnelIntensityId =
            Shader.PropertyToID("_RimIntensity");
        private static readonly int DissolveAmountId =
            Shader.PropertyToID("_DissolveAmount");
        private static readonly int DissolveEnabledId =
            Shader.PropertyToID("_DissolveEnabled");
        private static readonly int BaseSpriteUvRectId =
            Shader.PropertyToID("_BaseSpriteUVRect");
        private const string DissolveKeyword = "_DISSOLVE_ON";
        private static int _backgroundBlockCount;
        private InteractionState _state;
        private DragTarget _dragTarget;
        private Camera _camera;
        private Vector2 _dragStartPosition;
        private int _startStateHash;
        private int _readyStateHash;
        private int _coverCloseStateHash;
        private bool _startSequenceSeen;
        private bool _blocksBackground;
        private MaterialPropertyBlock _coverPropertyBlock;
        private MaterialPropertyBlock _wheelPropertyBlock;
        private int _fresnelIntensityId;
        private Sprite _authoredBurnCardSprite;
        private Vector3 _authoredBurnCardScale = Vector3.one;
        private Vector2 _authoredBurnCardSize = Vector2.one;
        private bool _burnCardVisualCached;
        private Material _burnCardMaterial;

        public static bool BlocksBackgroundInteraction => _backgroundBlockCount > 0;

        public bool HasCompletedInteraction { get; private set; }

        internal bool SetBurnCardSprite(Sprite sprite)
        {
            ResolveBurnCardRenderer();
            if (burnCardRenderer == null || sprite == null)
            {
                return false;
            }

            CacheBurnCardVisual();
            burnCardRenderer.sprite = sprite;
            burnCardRenderer.enabled = true;
            burnCardRenderer.color = Color.white;
            NormalizeBurnCardSize(sprite);
            ResetBurnCardMaterial(sprite);
            return true;
        }

        internal void EnableBurnCardDissolve()
        {
            Material material = EnsureBurnCardMaterialInstance();
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(DissolveEnabledId))
            {
                material.SetFloat(DissolveEnabledId, 1f);
            }

            material.EnableKeyword(DissolveKeyword);
        }

        internal void CompleteBurnCardDissolve()
        {
            ResolveBurnCardRenderer();
            if (burnCardRenderer != null)
            {
                burnCardRenderer.enabled = false;
            }
        }

        private void Awake()
        {
            ResolveBindings();
            CacheStateHashes();
            CacheShaderProperties();
            CacheBurnCardVisual();
        }

        private void OnEnable()
        {
            _state = InteractionState.WaitingForStart;
            _dragTarget = DragTarget.None;
            _startSequenceSeen = false;
            _blocksBackground = false;
            HasCompletedInteraction = false;
            ResetBurnCardVisual();
        }

        private void OnDisable()
        {
            _dragTarget = DragTarget.None;
            ResetHoverHighlights();
            EndBackgroundBlock();
        }

        private void OnDestroy()
        {
            if (_burnCardMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_burnCardMaterial);
            }
            else
            {
                DestroyImmediate(_burnCardMaterial);
            }
        }

        private void Update()
        {
            ResolveBindings();
            UpdateAnimatorGate();

            if (_state != InteractionState.ReadyForCover &&
                _state != InteractionState.ReadyForFire)
            {
                _dragTarget = DragTarget.None;
                ResetHoverHighlights();
                return;
            }

            if (!TryReadPointer(
                    out Vector2 pointerPosition,
                    out bool pressedThisFrame,
                    out bool isPressed,
                    out bool releasedThisFrame))
            {
                _dragTarget = DragTarget.None;
                ResetHoverHighlights();
                return;
            }

            UpdateHoverHighlights(pointerPosition);

            if (pressedThisFrame)
            {
                BeginDrag(pointerPosition);
            }

            if (_dragTarget != DragTarget.None && isPressed)
            {
                UpdateDrag(pointerPosition);
            }

            if (releasedThisFrame)
            {
                _dragTarget = DragTarget.None;
            }
        }

        private void ResolveBindings()
        {
            animator ??= GetComponent<Animator>();
            animationEvents ??= GetComponent<LighterAnimationEventReceiver>();
            coverRenderer ??= FindChildRenderer(coverRendererName);
            wheelRenderer ??= FindChildRenderer(wheelRendererName);
            ResolveBurnCardRenderer();

            if (_camera == null)
            {
                _camera = Camera.main;
            }
        }

        private void CacheStateHashes()
        {
            _startStateHash = Animator.StringToHash(startStateName);
            _readyStateHash = Animator.StringToHash(readyStateName);
            _coverCloseStateHash = Animator.StringToHash(coverCloseStateName);
        }

        private void CacheShaderProperties()
        {
            _fresnelIntensityId = string.IsNullOrWhiteSpace(fresnelIntensityProperty)
                ? DefaultFresnelIntensityId
                : Shader.PropertyToID(fresnelIntensityProperty);
        }

        private void UpdateAnimatorGate()
        {
            UpdateStartGate();
            UpdateReleaseGate();
        }

        private void UpdateStartGate()
        {
            if (_state != InteractionState.WaitingForStart || animator == null)
            {
                return;
            }

            if (startLayerIndex < 0 || startLayerIndex >= animator.layerCount)
            {
                _state = InteractionState.ReadyForCover;
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(startLayerIndex);
            if (stateInfo.shortNameHash == _startStateHash)
            {
                BeginBackgroundBlock();
                _startSequenceSeen = true;
                return;
            }

            if (stateInfo.shortNameHash == _readyStateHash && _startSequenceSeen)
            {
                _state = InteractionState.ReadyForCover;
            }
        }

        private void UpdateReleaseGate()
        {
            if (_state != InteractionState.Fired ||
                animator == null ||
                baseLayerIndex < 0 ||
                baseLayerIndex >= animator.layerCount)
            {
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);
            if (stateInfo.shortNameHash == _coverCloseStateHash &&
                stateInfo.normalizedTime >= 1f &&
                !animator.IsInTransition(baseLayerIndex))
            {
                EndBackgroundBlock();
                HasCompletedInteraction = true;
                _state = InteractionState.WaitingForStart;
                _startSequenceSeen = false;
                _dragTarget = DragTarget.None;
                ResetHoverHighlights();
            }
        }

        private void BeginBackgroundBlock()
        {
            if (_blocksBackground)
            {
                return;
            }

            _blocksBackground = true;
            _backgroundBlockCount++;
        }

        private void EndBackgroundBlock()
        {
            if (!_blocksBackground)
            {
                return;
            }

            _blocksBackground = false;
            _backgroundBlockCount = Mathf.Max(0, _backgroundBlockCount - 1);
        }

        private void BeginDrag(Vector2 pointerPosition)
        {
            if (_state == InteractionState.ReadyForCover &&
                ContainsPointer(coverRenderer, pointerPosition))
            {
                _dragTarget = DragTarget.Cover;
                _dragStartPosition = pointerPosition;
                return;
            }

            if (_state == InteractionState.ReadyForFire &&
                ContainsPointer(wheelRenderer, pointerPosition))
            {
                _dragTarget = DragTarget.Wheel;
                _dragStartPosition = pointerPosition;
                return;
            }

            _dragTarget = DragTarget.None;
        }

        private void UpdateHoverHighlights(Vector2 pointerPosition)
        {
            bool coverHovered = _state == InteractionState.ReadyForCover &&
                ContainsPointer(coverRenderer, pointerPosition);
            bool wheelHovered = _state == InteractionState.ReadyForFire &&
                ContainsPointer(wheelRenderer, pointerPosition);

            SetFresnelIntensity(
                coverRenderer,
                ref _coverPropertyBlock,
                coverHovered ? hoverFresnelIntensity : idleFresnelIntensity);
            SetFresnelIntensity(
                wheelRenderer,
                ref _wheelPropertyBlock,
                wheelHovered ? hoverFresnelIntensity : idleFresnelIntensity);
        }

        private void ResetHoverHighlights()
        {
            SetFresnelIntensity(coverRenderer, ref _coverPropertyBlock, idleFresnelIntensity);
            SetFresnelIntensity(wheelRenderer, ref _wheelPropertyBlock, idleFresnelIntensity);
        }

        private void SetFresnelIntensity(
            Renderer targetRenderer,
            ref MaterialPropertyBlock propertyBlock,
            float intensity)
        {
            if (targetRenderer == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(_fresnelIntensityId, intensity);
            targetRenderer.SetPropertyBlock(propertyBlock);
        }

        private void UpdateDrag(Vector2 pointerPosition)
        {
            Vector2 delta = pointerPosition - _dragStartPosition;
            if (!IsVerticalDrag(delta))
            {
                return;
            }

            if (_dragTarget == DragTarget.Cover && delta.y >= dragThresholdPixels)
            {
                SetTrigger(coverOpenTrigger);
                _state = InteractionState.ReadyForFire;
                _dragTarget = DragTarget.None;
            }
            else if (_dragTarget == DragTarget.Wheel && delta.y <= -dragThresholdPixels)
            {
                SetTrigger(fireOnTrigger);
                _state = InteractionState.Fired;
                _dragTarget = DragTarget.None;
            }
        }

        private bool IsVerticalDrag(Vector2 delta) =>
            Mathf.Abs(delta.y) >= dragThresholdPixels &&
            Mathf.Abs(delta.y) >= Mathf.Abs(delta.x) * verticalBias;

        private void SetTrigger(string triggerName)
        {
            if (animationEvents != null)
            {
                animationEvents.SetAnimatorTrigger(triggerName);
                return;
            }

            animator?.SetTrigger(triggerName);
        }

        private bool ContainsPointer(Renderer targetRenderer, Vector2 pointerPosition)
        {
            if (_camera == null || targetRenderer == null)
            {
                return false;
            }

            Bounds bounds = targetRenderer.bounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            bool hasPointInFront = false;
            Vector2 screenMin = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 screenMax = new Vector2(float.NegativeInfinity, float.NegativeInfinity);

            for (int x = 0; x <= 1; x++)
            {
                for (int y = 0; y <= 1; y++)
                {
                    for (int z = 0; z <= 1; z++)
                    {
                        Vector3 world = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        Vector3 screen = _camera.WorldToScreenPoint(world);
                        if (screen.z <= 0f)
                        {
                            continue;
                        }

                        hasPointInFront = true;
                        screenMin = Vector2.Min(screenMin, screen);
                        screenMax = Vector2.Max(screenMax, screen);
                    }
                }
            }

            return hasPointInFront &&
                pointerPosition.x >= screenMin.x - screenBoundsPaddingPixels &&
                pointerPosition.x <= screenMax.x + screenBoundsPaddingPixels &&
                pointerPosition.y >= screenMin.y - screenBoundsPaddingPixels &&
                pointerPosition.y <= screenMax.y + screenBoundsPaddingPixels;
        }

        private Renderer FindChildRenderer(string childName)
        {
            if (string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer candidate = renderers[i];
                if (candidate != null && candidate.name == childName)
                {
                    return candidate;
                }
            }

            return null;
        }

        private void ResolveBurnCardRenderer()
        {
            if (burnCardRenderer != null ||
                string.IsNullOrWhiteSpace(burnCardRendererName))
            {
                return;
            }

            SpriteRenderer[] renderers =
                GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer candidate = renderers[i];
                if (candidate != null && candidate.name == burnCardRendererName)
                {
                    burnCardRenderer = candidate;
                    return;
                }
            }
        }

        private void CacheBurnCardVisual()
        {
            if (_burnCardVisualCached || burnCardRenderer == null)
            {
                return;
            }

            _authoredBurnCardSprite = burnCardRenderer.sprite;
            _authoredBurnCardScale = burnCardRenderer.transform.localScale;
            _authoredBurnCardSize = GetSpriteSize(_authoredBurnCardSprite);
            _burnCardVisualCached = true;
        }

        private void ResetBurnCardVisual()
        {
            ResolveBurnCardRenderer();
            CacheBurnCardVisual();
            if (burnCardRenderer == null)
            {
                return;
            }

            burnCardRenderer.sprite = _authoredBurnCardSprite;
            burnCardRenderer.transform.localScale = _authoredBurnCardScale;
            burnCardRenderer.enabled = true;
            burnCardRenderer.color = Color.white;
            ResetBurnCardMaterial(_authoredBurnCardSprite);
        }

        private void NormalizeBurnCardSize(Sprite sprite)
        {
            Vector2 selectedSize = GetSpriteSize(sprite);
            Vector3 scale = _authoredBurnCardScale;
            if (_authoredBurnCardSize.x > 0f && selectedSize.x > 0f)
            {
                scale.x *= _authoredBurnCardSize.x / selectedSize.x;
            }

            if (_authoredBurnCardSize.y > 0f && selectedSize.y > 0f)
            {
                scale.y *= _authoredBurnCardSize.y / selectedSize.y;
            }

            burnCardRenderer.transform.localScale = scale;
        }

        private void ResetBurnCardMaterial(Sprite sprite)
        {
            burnCardRenderer.SetPropertyBlock(null);
            Material material = EnsureBurnCardMaterialInstance();
            if (material == null)
            {
                return;
            }

            if (material.HasProperty(DissolveAmountId))
            {
                material.SetFloat(DissolveAmountId, 0f);
            }

            if (material.HasProperty(DissolveEnabledId))
            {
                material.SetFloat(DissolveEnabledId, 0f);
            }

            material.DisableKeyword(DissolveKeyword);

            if (material.HasProperty(BaseSpriteUvRectId))
            {
                material.SetVector(BaseSpriteUvRectId, GetSpriteUvRect(sprite));
            }
        }

        private Material EnsureBurnCardMaterialInstance()
        {
            if (_burnCardMaterial != null)
            {
                return _burnCardMaterial;
            }

            Material source = burnCardRenderer.sharedMaterial;
            if (source == null)
            {
                return null;
            }

            _burnCardMaterial = new Material(source)
            {
                name = source.name + " (Lighter Burn Card Instance)"
            };
            burnCardRenderer.sharedMaterial = _burnCardMaterial;
            return _burnCardMaterial;
        }

        private static Vector2 GetSpriteSize(Sprite sprite)
        {
            return sprite == null
                ? Vector2.one
                : new Vector2(sprite.bounds.size.x, sprite.bounds.size.y);
        }

        private static Vector4 GetSpriteUvRect(Sprite sprite)
        {
            if (sprite == null || sprite.uv == null || sprite.uv.Length == 0)
            {
                return new Vector4(0f, 0f, 1f, 1f);
            }

            Vector2[] uvs = sprite.uv;
            Vector2 minimum = uvs[0];
            Vector2 maximum = uvs[0];
            for (int i = 1; i < uvs.Length; i++)
            {
                minimum = Vector2.Min(minimum, uvs[i]);
                maximum = Vector2.Max(maximum, uvs[i]);
            }

            Vector2 size = maximum - minimum;
            return new Vector4(
                minimum.x,
                minimum.y,
                Mathf.Max(size.x, 0.00001f),
                Mathf.Max(size.y, 0.00001f));
        }

        private static bool TryReadPointer(
            out Vector2 position,
            out bool pressedThisFrame,
            out bool isPressed,
            out bool releasedThisFrame)
        {
            Touchscreen touch = Touchscreen.current;
            if (touch != null)
            {
                TouchControl primary = touch.primaryTouch;
                if (primary.press.isPressed ||
                    primary.press.wasPressedThisFrame ||
                    primary.press.wasReleasedThisFrame)
                {
                    position = primary.position.ReadValue();
                    pressedThisFrame = primary.press.wasPressedThisFrame;
                    isPressed = primary.press.isPressed;
                    releasedThisFrame = primary.press.wasReleasedThisFrame;
                    return true;
                }
            }

            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                position = mouse.position.ReadValue();
                pressedThisFrame = mouse.leftButton.wasPressedThisFrame;
                isPressed = mouse.leftButton.isPressed;
                releasedThisFrame = mouse.leftButton.wasReleasedThisFrame;
                return true;
            }

            position = default;
            pressedThisFrame = false;
            isPressed = false;
            releasedThisFrame = false;
            return false;
        }
    }
}
