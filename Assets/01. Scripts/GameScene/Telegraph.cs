using System;
using System.Collections.Generic;
using Border.Audio;
using DG.Tweening;
using DiaBlackJack.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the diegetic telegraph handle and its three collider buttons.
    /// The signed angle is tweened directly so switching between the end points
    /// never takes DOTween's shorter wrapped path through the underside.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Telegraph : MonoBehaviour
    {
        public const float NoHoverAngle = 135f;
        public const float MinimumAngle = -135f;
        public const float MaximumAngle = 135f;

        private static readonly int StencilOutlineColorId =
            Shader.PropertyToID("_StencilOutlineColor");
        private static readonly int DitherAlphaEnabledId =
            Shader.PropertyToID("_DitherAlphaEnabled");
        private static readonly int DitherAlphaId =
            Shader.PropertyToID("_DitherAlpha");
        private const string DitherAlphaKeyword = "_DITHER_ALPHA_ON";
        private const string TelegraphInSfxId = "telegraphIn";
        private const string TelegraphOutSfxId = "telegraphOut";
        private const string TelegraphMoveSfxId = "telegraphMove";
        private const string TelegraphDestSfxId = "telegraphDest";
        private const string TelegraphSelectSfxId = "telegraphSelect";

        [Header("Handle")]
        [SerializeField] private Transform handle;
        [Tooltip("Handle rotation speed in degrees per second. The tween duration is calculated from the current angle distance.")]
        [SerializeField, Min(0.01f)] private float rotationSpeed = 270f;
        [Tooltip("Play the destination SFX this many seconds before a button angle is reached.")]
        [SerializeField, Min(0f)] private float destinationSfxLeadTime = 0.06f;
        [Tooltip("Optional custom DOTween ease graph. Enable it to override the default OutBack ease.")]
        [SerializeField] private bool useCustomRotationCurve;
        [SerializeField] private AnimationCurve rotationCurve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Hover outline")]
        [SerializeField] private Renderer[] handleRenderers;
        [ColorUsage(true, true)]
        [SerializeField] private Color hoverOutlineColor =
            new Color(5.3403134f, 5.3403134f, 5.3403134f, 1f);
        [SerializeField, Min(0f)] private float hoverOutlineWidthPixels = 4f;

        [Header("Appearance")]
        [Tooltip("Play the entrance animation whenever the Telegraph becomes enabled.")]
        [SerializeField] private bool playEntranceOnEnable = true;
        [Tooltip("Local-space direction from which the Telegraph enters and exits.")]
        [SerializeField] private Vector3 appearanceMoveDirection = Vector3.forward;
        [SerializeField, Min(0f)] private float appearanceMoveDistance = 4f;
        [SerializeField, Min(0f)] private float appearanceEnterDuration = 0.8f;
        [SerializeField, Min(0f)] private float appearanceExitDuration = 0.8f;
        [SerializeField] private Ease appearanceEnterMoveEase = Ease.OutCubic;
        [SerializeField] private Ease appearanceExitMoveEase = Ease.InCubic;
        [Tooltip("Dither alpha graph. Entrance evaluates 0 to 1; exit evaluates 1 to 0.")]
        [SerializeField] private AnimationCurve appearanceDitherAlphaCurve =
            AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [Tooltip("Renderers affected by the appearance dither. Empty automatically uses all child renderers.")]
        [SerializeField] private Renderer[] appearanceRenderers;

        [Header("Click feedback")]
        [SerializeField, Min(0f)] private float clickPunchDuration = 0.24f;
        [SerializeField] private Vector3 clickPunchScale =
            new Vector3(0.06f, 0.06f, 0.06f);
        [Tooltip("Punch displacement in world units.")]
        [SerializeField] private Vector3 clickPunchPosition =
            new Vector3(0f, -0.04f, 0f);
        [Tooltip("Punch rotation in degrees.")]
        [SerializeField] private Vector3 clickPunchRotation =
            new Vector3(0f, 0f, 3f);
        [SerializeField, Min(1)] private int clickPunchVibrato = 5;
        [SerializeField, Range(0f, 1f)] private float clickPunchElasticity = 0.75f;

        [Header("Input")]
        [SerializeField] private Camera inputCamera;
        [SerializeField] private LayerMask raycastLayers = ~0;
        [SerializeField, Min(0f)] private float raycastDistance = 200f;

        private TelegraphButton _hoveredButton;
        private Tween _handleTween;
        private Tween _clickPunchTween;
        private Tween _appearanceTween;
        private float _handleAngle = NoHoverAngle;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation;
        private Vector3 _baseLocalScale;
        private MaterialPropertyBlock _ditherPropertyBlock;
        private float _ditherAlpha = 1f;
        private Material[] _ditherMaterials;
        private bool _appearanceInitialized;
        private bool _inputEnabled = true;
        private bool _isAppearancePlaying;
        private bool _appearanceVisible = true;

        public float CurrentHandleAngle => _handleAngle;
        public bool IsInputEnabled => _inputEnabled;
        public bool IsAppearancePlaying => _isAppearancePlaying;
        internal TelegraphButtonKind? HoveredButtonKind =>
            _hoveredButton?.ButtonKind;

        private void Awake()
        {
            AutoBindMissingReferences();
            EnsureAppearanceInitialized();
            ResetVisualState();
        }

        private void OnEnable()
        {
            AutoBindMissingReferences();
            EnsureAppearanceInitialized();
            ResetVisualState();
            if (playEntranceOnEnable)
            {
                PlayEntranceAnimation();
            }
        }

        private void Update()
        {
            if (!_inputEnabled || _isAppearancePlaying || !_appearanceVisible)
            {
                SetHoveredButton(null);
                return;
            }

            Mouse mouse = Mouse.current;
            Camera camera = inputCamera != null ? inputCamera : Camera.main;
            if (mouse == null || camera == null)
            {
                SetHoveredButton(null);
                return;
            }

            Ray ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            TelegraphButton pointedButton = null;
            if (Physics.Raycast(
                    ray,
                    out RaycastHit hit,
                    Mathf.Max(0f, raycastDistance),
                    raycastLayers,
                    QueryTriggerInteraction.Collide))
            {
                pointedButton = hit.collider.GetComponentInParent<TelegraphButton>();
                if (pointedButton != null && pointedButton.Telegraph != this)
                {
                    pointedButton = null;
                }
            }

            SetHoveredButton(pointedButton);

            if (pointedButton != null && mouse.leftButton.wasPressedThisFrame)
            {
                SoundManager.Current?.PlaySfx(TelegraphSelectSfxId);
                PlayClickPunch();
                pointedButton.InvokeClick();
            }
        }

        private void OnDisable()
        {
            ResetVisualState();
        }

        private void OnDestroy()
        {
            StopAppearanceTween();
            StopHandleTween();
            StopClickPunch();
            ApplyHoverOutline(false);
        }

        public void PlayEntranceAnimation(Action onComplete = null)
        {
            EnsureAppearanceInitialized();
            StopAppearanceTween();
            StopClickPunch();
            ClearHoverState();

            Vector3 sourcePosition = ResolveAppearanceSourcePosition();
            transform.localPosition = sourcePosition;
            SetDitherAlpha(0f);
            _isAppearancePlaying = true;
            _appearanceVisible = true;
            SoundManager.Current?.PlaySfx(TelegraphInSfxId);

            if (appearanceEnterDuration <= 0f)
            {
                CompleteEntranceAnimation(onComplete);
                return;
            }

            _appearanceTween = DOTween.Sequence()
                .SetTarget(transform)
                .Join(transform.DOLocalMove(
                    _baseLocalPosition,
                    appearanceEnterDuration)
                    .SetEase(appearanceEnterMoveEase))
                .Join(CreateDitherAlphaTween(0f, 1f, appearanceEnterDuration))
                .OnComplete(() => CompleteEntranceAnimation(onComplete));
        }

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
            if (!enabled)
            {
                ClearHoverState();
            }
        }

        public void PlayExitAnimation(Action onComplete = null)
        {
            EnsureAppearanceInitialized();
            StopAppearanceTween();
            StopClickPunch();
            ClearHoverState();

            Vector3 sourcePosition = ResolveAppearanceSourcePosition();
            transform.localPosition = _baseLocalPosition;
            SetDitherAlpha(1f);
            _isAppearancePlaying = true;
            _appearanceVisible = true;
            SoundManager.Current?.PlaySfx(TelegraphOutSfxId);

            if (appearanceExitDuration <= 0f)
            {
                CompleteExitAnimation(sourcePosition, onComplete);
                return;
            }

            _appearanceTween = DOTween.Sequence()
                .SetTarget(transform)
                .Join(transform.DOLocalMove(
                    sourcePosition,
                    appearanceExitDuration)
                    .SetEase(appearanceExitMoveEase))
                .Join(CreateDitherAlphaTween(1f, 0f, appearanceExitDuration))
                .OnComplete(() => CompleteExitAnimation(sourcePosition, onComplete));
        }

        public void ResetAppearanceVisualState()
        {
            EnsureAppearanceInitialized();
            StopAppearanceTween();
            StopClickPunch();
            transform.localPosition = _baseLocalPosition;
            SetDitherAlpha(1f);
            _isAppearancePlaying = false;
            _appearanceVisible = true;
        }

        internal void SetHoveredButton(TelegraphButton button)
        {
            if (_hoveredButton == button)
            {
                return;
            }

            _hoveredButton = button;
            ApplyHoverOutline(button != null);
            MoveHandleTo(button == null ? NoHoverAngle : button.TargetAngle);
        }

        private void MoveHandleTo(float targetAngle)
        {
            targetAngle = Mathf.Clamp(
                targetAngle,
                MinimumAngle,
                MaximumAngle);

            StopHandleTween();
            if (handle == null || Mathf.Approximately(_handleAngle, targetAngle))
            {
                SetHandleAngle(targetAngle);
                return;
            }

            float rotationDistance = Mathf.Abs(targetAngle - _handleAngle);
            float duration = rotationDistance / Mathf.Max(0.01f, rotationSpeed);
            SoundManager.Current?.PlaySfx(TelegraphMoveSfxId);
            float tweenDuration = Mathf.Max(0.01f, duration);
            bool shouldPlayDestinationSfx =
                !Mathf.Approximately(targetAngle, NoHoverAngle);
            Tween handleTween = DOTween.To(
                    () => _handleAngle,
                    SetHandleAngle,
                    targetAngle,
                    tweenDuration)
                .SetEase(Ease.OutBack);

            if (useCustomRotationCurve && rotationCurve != null &&
                rotationCurve.length > 1)
            {
                handleTween.SetEase(rotationCurve);
            }

            Sequence rotationSequence = DOTween.Sequence()
                .SetTarget(handle)
                .Append(handleTween);

            if (shouldPlayDestinationSfx)
            {
                float destinationSfxTime = Mathf.Max(
                    0f,
                    tweenDuration - destinationSfxLeadTime);
                rotationSequence.InsertCallback(
                    destinationSfxTime,
                    () => SoundManager.Current?.PlaySfx(TelegraphDestSfxId));
            }

            _handleTween = rotationSequence.OnComplete(() =>
            {
                _handleTween = null;
                SetHandleAngle(targetAngle);
            });
        }

        private void SetHandleAngle(float angle)
        {
            _handleAngle = Mathf.Clamp(angle, MinimumAngle, MaximumAngle);
            if (handle != null)
            {
                handle.localRotation = Quaternion.Euler(0f, 0f, _handleAngle);
            }
        }

        private void StopHandleTween()
        {
            if (_handleTween == null)
            {
                return;
            }

            _handleTween.Kill();
            _handleTween = null;
        }

        private void PlayClickPunch()
        {
            if (clickPunchDuration <= 0f ||
                (clickPunchScale.sqrMagnitude < 0.0001f &&
                 clickPunchPosition.sqrMagnitude < 0.0001f &&
                 clickPunchRotation.sqrMagnitude < 0.0001f))
            {
                return;
            }

            StopClickPunch();
            Sequence punchSequence = DOTween.Sequence().SetTarget(transform);
            if (clickPunchScale.sqrMagnitude >= 0.0001f)
            {
                punchSequence.Join(transform.DOPunchScale(
                    clickPunchScale,
                    clickPunchDuration,
                    clickPunchVibrato,
                    clickPunchElasticity));
            }

            if (clickPunchPosition.sqrMagnitude >= 0.0001f)
            {
                punchSequence.Join(transform.DOPunchPosition(
                    clickPunchPosition,
                    clickPunchDuration,
                    clickPunchVibrato,
                    clickPunchElasticity,
                    false));
            }

            if (clickPunchRotation.sqrMagnitude >= 0.0001f)
            {
                punchSequence.Join(transform.DOPunchRotation(
                    clickPunchRotation,
                    clickPunchDuration,
                    clickPunchVibrato,
                    clickPunchElasticity));
            }

            _clickPunchTween = punchSequence
                .OnComplete(() => _clickPunchTween = null);
        }

        private void StopClickPunch()
        {
            _clickPunchTween?.Kill();
            _clickPunchTween = null;
            if (_appearanceInitialized)
            {
                transform.localPosition = _baseLocalPosition;
                transform.localRotation = _baseLocalRotation;
                transform.localScale = _baseLocalScale;
            }
        }

        private void StopAppearanceTween()
        {
            _appearanceTween?.Kill();
            _appearanceTween = null;
            _isAppearancePlaying = false;
        }

        private void ClearHoverState()
        {
            _hoveredButton = null;
            ApplyHoverOutline(false);
            StopHandleTween();
            SetHandleAngle(NoHoverAngle);
        }

        private void ResetVisualState()
        {
            StopAppearanceTween();
            StopHandleTween();
            StopClickPunch();
            _hoveredButton = null;
            ApplyHoverOutline(false);
            SetHandleAngle(NoHoverAngle);

            if (_appearanceInitialized)
            {
                transform.localPosition = _baseLocalPosition;
                SetDitherAlpha(1f);
                _appearanceVisible = true;
            }
        }

        private void EnsureAppearanceInitialized()
        {
            if (_appearanceInitialized)
            {
                return;
            }

            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;
            _baseLocalScale = transform.localScale;
            AutoBindAppearanceRenderers();
            EnsureDitherMaterials();
            _appearanceInitialized = true;
            SetDitherAlpha(1f);
        }

        private void AutoBindAppearanceRenderers()
        {
            if (appearanceRenderers != null && appearanceRenderers.Length > 0)
            {
                return;
            }

            appearanceRenderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        private void EnsureDitherMaterials()
        {
            AutoBindAppearanceRenderers();
            if (_ditherMaterials != null || appearanceRenderers == null)
            {
                return;
            }

            var materials = new List<Material>();
            for (int i = 0; i < appearanceRenderers.Length; i++)
            {
                Renderer renderer = appearanceRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material material = renderer.material;
                if (material == null || !material.HasProperty(DitherAlphaId))
                {
                    continue;
                }

                if (material.HasProperty(DitherAlphaEnabledId))
                {
                    material.SetFloat(DitherAlphaEnabledId, 1f);
                }

                material.EnableKeyword(DitherAlphaKeyword);
                materials.Add(material);
            }

            _ditherMaterials = materials.ToArray();
        }

        private Vector3 ResolveAppearanceSourcePosition()
        {
            Vector3 direction = appearanceMoveDirection;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            return _baseLocalPosition +
                direction.normalized * Mathf.Max(0f, appearanceMoveDistance);
        }

        private Tween CreateDitherAlphaTween(
            float from,
            float to,
            float duration)
        {
            SetDitherAlpha(from);
            Tween tween = DOTween.To(
                () => _ditherAlpha,
                SetDitherAlpha,
                to,
                duration);

            if (appearanceDitherAlphaCurve != null &&
                appearanceDitherAlphaCurve.length > 1)
            {
                tween.SetEase(appearanceDitherAlphaCurve);
            }

            return tween.SetTarget(transform);
        }

        private void SetDitherAlpha(float alpha)
        {
            _ditherAlpha = Mathf.Clamp01(alpha);
            EnsureDitherMaterials();
            if (appearanceRenderers == null || appearanceRenderers.Length == 0)
            {
                return;
            }

            _ditherPropertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < appearanceRenderers.Length; i++)
            {
                Renderer renderer = appearanceRenderers[i];
                Material sharedMaterial = renderer == null
                    ? null
                    : renderer.sharedMaterial;
                if (sharedMaterial == null || !sharedMaterial.HasProperty(DitherAlphaId))
                {
                    continue;
                }

                renderer.GetPropertyBlock(_ditherPropertyBlock);
                _ditherPropertyBlock.SetFloat(DitherAlphaId, _ditherAlpha);
                renderer.SetPropertyBlock(_ditherPropertyBlock);
            }
        }

        private void CompleteEntranceAnimation(Action onComplete)
        {
            _appearanceTween = null;
            _isAppearancePlaying = false;
            transform.localPosition = _baseLocalPosition;
            SetDitherAlpha(1f);
            _appearanceVisible = true;
            onComplete?.Invoke();
        }

        private void CompleteExitAnimation(
            Vector3 sourcePosition,
            Action onComplete)
        {
            _appearanceTween = null;
            _isAppearancePlaying = false;
            transform.localPosition = sourcePosition;
            SetDitherAlpha(0f);
            _appearanceVisible = false;
            onComplete?.Invoke();
        }

        private void ApplyHoverOutline(bool visible)
        {
            AutoBindMissingReferences();
            if (handleRenderers == null)
            {
                return;
            }

            for (int i = 0; i < handleRenderers.Length; i++)
            {
                Renderer renderer = handleRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!visible)
                {
                    PostProcessOutlineRegistry.Unregister(renderer);
                    continue;
                }

                PostProcessOutlineRegistry.Register(
                    renderer,
                    ResolveOutlineColor(renderer.sharedMaterial),
                    hoverOutlineWidthPixels);
            }
        }

        private Color ResolveOutlineColor(Material material)
        {
            if (material != null && material.HasProperty(StencilOutlineColorId))
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

        private void AutoBindMissingReferences()
        {
            if (handle == null)
            {
                handle = transform.Find("telegraph_rigging/Body/Handle");
            }

            if (handle == null)
            {
                Transform[] childTransforms =
                    GetComponentsInChildren<Transform>(includeInactive: true);
                for (int i = 0; i < childTransforms.Length; i++)
                {
                    Transform child = childTransforms[i];
                    if (child != transform && child.name == "Handle")
                    {
                        handle = child;
                        break;
                    }
                }
            }

            if (handleRenderers == null || handleRenderers.Length == 0)
            {
                Renderer[] renderers =
                    GetComponentsInChildren<Renderer>(includeInactive: true);
                var found = new System.Collections.Generic.List<Renderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer renderer = renderers[i];
                    if (renderer != null && renderer.transform.name == "Handle")
                    {
                        found.Add(renderer);
                    }
                }

                handleRenderers = found.ToArray();
            }

            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }
        }

        private void OnValidate()
        {
            rotationSpeed = Mathf.Max(0.01f, rotationSpeed);
            destinationSfxLeadTime = Mathf.Max(0f, destinationSfxLeadTime);
            rotationCurve ??= AnimationCurve.Linear(0f, 0f, 1f, 1f);
            appearanceMoveDistance = Mathf.Max(0f, appearanceMoveDistance);
            appearanceEnterDuration = Mathf.Max(0f, appearanceEnterDuration);
            appearanceExitDuration = Mathf.Max(0f, appearanceExitDuration);
            clickPunchDuration = Mathf.Max(0f, clickPunchDuration);
            clickPunchVibrato = Mathf.Max(1, clickPunchVibrato);
            clickPunchElasticity = Mathf.Clamp01(clickPunchElasticity);
            if (appearanceMoveDirection.sqrMagnitude < 0.0001f)
            {
                appearanceMoveDirection = Vector3.forward;
            }

            appearanceDitherAlphaCurve ??=
                AnimationCurve.Linear(0f, 0f, 1f, 1f);
            hoverOutlineWidthPixels = Mathf.Max(0f, hoverOutlineWidthPixels);
            raycastDistance = Mathf.Max(0f, raycastDistance);
            AutoBindMissingReferences();
        }
    }
}
