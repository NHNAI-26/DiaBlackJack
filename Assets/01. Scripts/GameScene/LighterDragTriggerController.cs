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

        [Header("Child lookup")]
        [SerializeField] private string coverRendererName = "cover";
        [SerializeField] private string wheelRendererName = "wheel";

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

        public static bool BlocksBackgroundInteraction => _backgroundBlockCount > 0;

        private void Awake()
        {
            ResolveBindings();
            CacheStateHashes();
            CacheShaderProperties();
        }

        private void OnEnable()
        {
            _state = InteractionState.WaitingForStart;
            _dragTarget = DragTarget.None;
            _startSequenceSeen = false;
            _blocksBackground = false;
        }

        private void OnDisable()
        {
            _dragTarget = DragTarget.None;
            ResetHoverHighlights();
            EndBackgroundBlock();
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
