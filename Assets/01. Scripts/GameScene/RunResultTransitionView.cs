using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using DiaBlackJack.CoreLoop;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace DiaBlackJack.GameScene
{
    internal enum RunResultExitTransitionKind
    {
        None,
        Defeat,
        Victory
    }

    [DisallowMultipleComponent]
    internal sealed class RunResultTransitionView : MonoBehaviour
    {
        internal const int DefeatSoulTokenCount = 8;
        internal const float DefeatTokenStaggerSeconds = 0.055f;
        internal const float DefeatTokenImpactSeconds = 0.12f;
        internal const float DefeatTokenFallSeconds = 0.58f;
        internal const float DefeatFadeSeconds = 0.8f;
        internal const float VictoryBlurSeconds = 1.5f;
        internal const float VictoryEyeCloseDelaySeconds = 0.65f;
        internal const float VictoryFirstEyeCloseSeconds = 0.85f;
        internal const float VictoryEyeReopenSeconds = 0.22f;
        internal const float VictoryFinalEyeCloseSeconds = 0.32f;
        internal const float VictoryEyeReopenScale = 0.7f;
        internal const float VictoryEyelidFeatherFraction = 0.15f;

        private const int OverlaySortingOrder = 32760;
        private const int VictoryEyelidGradientResolution = 64;
        private const float DefeatFadeHoldSeconds = 0.12f;
        private const float VictoryClosedHoldSeconds = 0.18f;
        private const float VictoryBlurRadius = 1f;

        private GameObject _overlayRoot;
        private Sequence _masterSequence;
        private SoulLossPresentation _soulLossPresentation;
        private VolumeProfile _victoryVolumeProfile;
        private Coroutine _victoryCaptureCoroutine;
        private Texture2D _victorySharpFrame;
        private Texture2D _victoryEyelidGradient;
        private readonly HashSet<int> _victoryVolumeLayers =
            new HashSet<int>();
        private readonly List<Volume> _victoryBlurVolumes =
            new List<Volume>();
        private Action _completed;
        private bool _completionInvoked;

        internal bool IsPlaying { get; private set; }

        internal bool HasVictoryBlur
        {
            get
            {
                return _victoryVolumeProfile != null &&
                    _victoryVolumeProfile.TryGet(
                        out DepthOfField depthOfField) &&
                    depthOfField != null &&
                    depthOfField.mode.value == DepthOfFieldMode.Gaussian;
            }
        }

        internal bool HasVictoryEyelidFeather =>
            _victoryEyelidGradient != null;

        internal int VictoryBlurVolumeLayerCount =>
            _victoryVolumeLayers.Count;

        internal float VictoryBlurWeight => GetVictoryBlurWeight();

        internal static RunResultExitTransitionKind ResolveKind(
            GameFlowScreen screen)
        {
            switch (screen)
            {
                case GameFlowScreen.RunDefeat:
                    return RunResultExitTransitionKind.Defeat;
                case GameFlowScreen.RunVictory:
                    return RunResultExitTransitionKind.Victory;
                default:
                    return RunResultExitTransitionKind.None;
            }
        }

        internal static SoulLossRecord CreateDefeatSoulLossRecord()
        {
            return new SoulLossRecord(
                0,
                CombatantSide.Player,
                DefeatSoulTokenCount,
                0,
                DefeatSoulTokenCount,
                SoulLossCause.RoundDamage);
        }

        internal static float EvaluateVictoryEyelidFeatherAlpha(
            float normalizedHeight)
        {
            float clampedHeight = Mathf.Clamp01(normalizedHeight);
            return clampedHeight * clampedHeight *
                (3f - (2f * clampedHeight));
        }

        internal static float EvaluateVictorySharpFrameAlpha(
            float normalizedTime)
        {
            float clampedTime = Mathf.Clamp01(normalizedTime);
            float blurBlend = clampedTime * clampedTime *
                (3f - (2f * clampedTime));
            return 1f - blurBlend;
        }

        internal static int FindFirstIncludedVolumeLayer(
            int volumeLayerMask)
        {
            for (int layer = 0; layer < 32; layer++)
            {
                if ((volumeLayerMask & (1 << layer)) != 0)
                {
                    return layer;
                }
            }

            return -1;
        }

        internal bool HasVictoryBlurCoverageForMask(int volumeLayerMask)
        {
            foreach (int layer in _victoryVolumeLayers)
            {
                if ((volumeLayerMask & (1 << layer)) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool TryPlay(
            GameFlowScreen screen,
            GameHudView hud,
            Action completed)
        {
            RunResultExitTransitionKind kind = ResolveKind(screen);
            if (kind == RunResultExitTransitionKind.None ||
                IsPlaying ||
                completed == null)
            {
                return false;
            }

            CancelAndRestore();
            IsPlaying = true;
            _completionInvoked = false;
            _completed = completed;

            Canvas canvas = CreateOverlayCanvas();
            if (kind == RunResultExitTransitionKind.Defeat)
            {
                PlayDefeat(canvas, hud);
            }
            else
            {
                PlayVictory(canvas);
            }

            return true;
        }

        internal void CancelAndRestore()
        {
            if (_victoryCaptureCoroutine != null)
            {
                StopCoroutine(_victoryCaptureCoroutine);
                _victoryCaptureCoroutine = null;
            }

            _masterSequence?.Kill();
            _masterSequence = null;
            _soulLossPresentation?.Dispose();
            _soulLossPresentation = null;
            PresentationManager.Current?.ForceRestoreTransientCameraEffects();
            if (_overlayRoot != null)
            {
                _overlayRoot.SetActive(false);
            }

            DestroyRuntimeObject(_victoryVolumeProfile);
            _victoryVolumeProfile = null;
            DestroyRuntimeObject(_overlayRoot);
            _overlayRoot = null;
            _victoryVolumeLayers.Clear();
            _victoryBlurVolumes.Clear();
            DestroyRuntimeObject(_victorySharpFrame);
            _victorySharpFrame = null;
            DestroyRuntimeObject(_victoryEyelidGradient);
            _victoryEyelidGradient = null;
            _completed = null;
            _completionInvoked = false;
            IsPlaying = false;
        }

#if UNITY_EDITOR
        internal void DebugCompleteImmediately()
        {
            CompleteOnce();
        }
#endif

        private void OnDisable()
        {
            CancelAndRestore();
        }

        private void PlayDefeat(Canvas canvas, GameHudView hud)
        {
            Image fade = CreateOverlayImage(
                canvas.transform,
                "DefeatFade",
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                alpha: 0f);

            _soulLossPresentation =
                hud?.CreateTerminalDefeatSoulLossPresentation(canvas);
            if (_soulLossPresentation != null)
            {
                SoulLossRecord record = CreateDefeatSoulLossRecord();
                _soulLossPresentation.Play(
                    new List<SoulLossRecord> { record },
                    Vector3.zero,
                    null,
                    onImpact: null);
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            for (int index = 0; index < DefeatSoulTokenCount; index++)
            {
                float impactTime = DefeatTokenImpactSeconds +
                    (index * DefeatTokenStaggerSeconds);
                sequence.InsertCallback(
                    impactTime,
                    () => PresentationManager.Current?
                        .PlayPlayerDamagePresentation());
            }

            float burstSeconds = DefeatTokenFallSeconds +
                ((DefeatSoulTokenCount - 1) * DefeatTokenStaggerSeconds);
            sequence.AppendInterval(
                Mathf.Max(0f, burstSeconds - sequence.Duration()));
            sequence.Append(DOTween.To(
                    () => fade.color.a,
                    alpha => SetImageAlpha(fade, alpha),
                    1f,
                    DefeatFadeSeconds)
                .SetEase(Ease.InQuad));
            sequence.AppendInterval(DefeatFadeHoldSeconds);
            CompleteWith(sequence);
        }

        private void PlayVictory(Canvas canvas)
        {
            if (Application.isPlaying)
            {
                _victoryCaptureCoroutine =
                    StartCoroutine(CaptureSharpFrameAndPlayVictory(canvas));
                return;
            }

            PlayVictoryTimeline(canvas, null);
        }

        private IEnumerator CaptureSharpFrameAndPlayVictory(Canvas canvas)
        {
            yield return new WaitForEndOfFrame();
            _victoryCaptureCoroutine = null;
            if (!IsPlaying || canvas == null)
            {
                yield break;
            }

            try
            {
                _victorySharpFrame =
                    ScreenCapture.CaptureScreenshotAsTexture();
                if (_victorySharpFrame != null)
                {
                    _victorySharpFrame.name = "VictorySharpFrame";
                    _victorySharpFrame.hideFlags =
                        HideFlags.HideAndDontSave;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"Victory sharp-frame capture failed: " +
                    exception.Message,
                    this);
            }

            PlayVictoryTimeline(canvas, _victorySharpFrame);
        }

        private void PlayVictoryTimeline(
            Canvas canvas,
            Texture2D sharpFrameTexture)
        {
            RawImage sharpFrame = sharpFrameTexture != null
                ? CreateVictorySharpFrame(canvas, sharpFrameTexture)
                : null;
            DepthOfField depthOfField = CreateVictoryDepthOfField();
            _victoryEyelidGradient = CreateVictoryEyelidGradient();
            Image topLid = CreateOverlayImage(
                canvas.transform,
                "VictoryTopEyelid",
                new Vector2(0f, 0.5f),
                Vector2.one,
                new Vector2(0.5f, 1f),
                alpha: 1f);
            Image bottomLid = CreateOverlayImage(
                canvas.transform,
                "VictoryBottomEyelid",
                Vector2.zero,
                new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0f),
                alpha: 1f);
            CreateVictoryEyelidFeather(
                topLid.transform,
                "VictoryTopEyelidFeather",
                isTopLid: true);
            CreateVictoryEyelidFeather(
                bottomLid.transform,
                "VictoryBottomEyelidFeather",
                isTopLid: false);
            topLid.rectTransform.localScale = new Vector3(1f, 0f, 1f);
            bottomLid.rectTransform.localScale = new Vector3(1f, 0f, 1f);

            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            if (depthOfField != null && sharpFrame != null)
            {
                SetVictoryBlurWeight(1f);
                sequence.Insert(0f, DOTween.To(
                        () => 0f,
                        progress => SetRawImageAlpha(
                            sharpFrame,
                            EvaluateVictorySharpFrameAlpha(progress)),
                        1f,
                        VictoryBlurSeconds)
                    .SetEase(Ease.Linear));
            }
            else if (depthOfField != null)
            {
                sequence.Insert(0f, DOTween.To(
                        GetVictoryBlurWeight,
                        SetVictoryBlurWeight,
                        1f,
                        VictoryBlurSeconds)
                    .SetEase(Ease.InOutSine));
            }
            else
            {
                sequence.AppendInterval(VictoryBlurSeconds);
            }

            sequence.Insert(
                VictoryEyeCloseDelaySeconds,
                topLid.rectTransform
                    .DOScaleY(1f, VictoryFirstEyeCloseSeconds)
                    .SetEase(Ease.InOutSine));
            sequence.Insert(
                VictoryEyeCloseDelaySeconds,
                bottomLid.rectTransform
                    .DOScaleY(1f, VictoryFirstEyeCloseSeconds)
                    .SetEase(Ease.InOutSine));

            float reopenStartSeconds = VictoryEyeCloseDelaySeconds +
                VictoryFirstEyeCloseSeconds;
            sequence.Insert(
                reopenStartSeconds,
                topLid.rectTransform
                    .DOScaleY(VictoryEyeReopenScale, VictoryEyeReopenSeconds)
                    .SetEase(Ease.OutSine));
            sequence.Insert(
                reopenStartSeconds,
                bottomLid.rectTransform
                    .DOScaleY(VictoryEyeReopenScale, VictoryEyeReopenSeconds)
                    .SetEase(Ease.OutSine));

            float finalCloseStartSeconds = reopenStartSeconds +
                VictoryEyeReopenSeconds;
            sequence.Insert(
                finalCloseStartSeconds,
                topLid.rectTransform
                    .DOScaleY(1f, VictoryFinalEyeCloseSeconds)
                    .SetEase(Ease.InQuad));
            sequence.Insert(
                finalCloseStartSeconds,
                bottomLid.rectTransform
                    .DOScaleY(1f, VictoryFinalEyeCloseSeconds)
                    .SetEase(Ease.InQuad));
            sequence.AppendInterval(VictoryClosedHoldSeconds);
            CompleteWith(sequence);
        }

        private static RawImage CreateVictorySharpFrame(
            Canvas canvas,
            Texture2D texture)
        {
            GameObject imageObject = new GameObject(
                "VictorySharpFrame",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(canvas.transform, worldPositionStays: false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage image = imageObject.GetComponent<RawImage>();
            image.texture = texture;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Texture2D CreateVictoryEyelidGradient()
        {
            Texture2D texture = new Texture2D(
                1,
                VictoryEyelidGradientResolution,
                TextureFormat.RGBA32,
                mipChain: false)
            {
                name = "VictoryEyelidGradient",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (int y = 0; y < VictoryEyelidGradientResolution; y++)
            {
                float normalizedHeight = y /
                    (VictoryEyelidGradientResolution - 1f);
                float alpha = EvaluateVictoryEyelidFeatherAlpha(
                    normalizedHeight);
                texture.SetPixel(0, y, new Color(0f, 0f, 0f, alpha));
            }

            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            return texture;
        }

        private void CreateVictoryEyelidFeather(
            Transform lid,
            string name,
            bool isTopLid)
        {
            GameObject featherObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(RawImage));
            RectTransform rect = featherObject.GetComponent<RectTransform>();
            rect.SetParent(lid, worldPositionStays: false);
            rect.anchorMin = isTopLid
                ? new Vector2(0f, -VictoryEyelidFeatherFraction)
                : new Vector2(0f, 1f);
            rect.anchorMax = isTopLid
                ? new Vector2(1f, 0f)
                : new Vector2(1f, 1f + VictoryEyelidFeatherFraction);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            RawImage feather = featherObject.GetComponent<RawImage>();
            feather.texture = _victoryEyelidGradient;
            feather.color = Color.white;
            feather.raycastTarget = false;
            feather.uvRect = isTopLid
                ? new Rect(0f, 0f, 1f, 1f)
                : new Rect(0f, 1f, 1f, -1f);
        }

        private Canvas CreateOverlayCanvas()
        {
            _overlayRoot = new GameObject(
                "RunResultTransitionOverlay",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            _overlayRoot.transform.SetParent(transform, worldPositionStays: false);

            Canvas canvas = _overlayRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlaySortingOrder;
            CanvasScaler scaler = _overlayRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private DepthOfField CreateVictoryDepthOfField()
        {
            _victoryVolumeProfile =
                ScriptableObject.CreateInstance<VolumeProfile>();
            DepthOfField depthOfField =
                _victoryVolumeProfile.Add<DepthOfField>(overrides: true);
            depthOfField.active = true;
            depthOfField.mode.value = DepthOfFieldMode.Gaussian;
            depthOfField.gaussianStart.value = 0.1f;
            depthOfField.gaussianEnd.value = 8f;
            depthOfField.gaussianMaxRadius.value = VictoryBlurRadius;
            depthOfField.highQualitySampling.value = true;
            CreateVictoryBlurVolumes();
            return depthOfField;
        }

        private void CreateVictoryBlurVolumes()
        {
            _victoryVolumeLayers.Clear();
            _victoryBlurVolumes.Clear();
            foreach (Camera camera in Camera.allCameras)
            {
                if (!camera.TryGetComponent(
                        out UniversalAdditionalCameraData cameraData) ||
                    !cameraData.renderPostProcessing)
                {
                    continue;
                }

                int layer = FindFirstIncludedVolumeLayer(
                    cameraData.volumeLayerMask.value);
                if (layer >= 0)
                {
                    _victoryVolumeLayers.Add(layer);
                }
            }

            if (_victoryVolumeLayers.Count == 0)
            {
                _victoryVolumeLayers.Add(0);
            }

            foreach (int layer in _victoryVolumeLayers)
            {
                GameObject volumeObject = new GameObject(
                    $"VictoryBlurVolume_Layer{layer}",
                    typeof(Volume));
                volumeObject.transform.SetParent(
                    _overlayRoot.transform,
                    worldPositionStays: false);
                volumeObject.layer = layer;

                Volume volume = volumeObject.GetComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 1000f;
                volume.weight = 0f;
                volume.sharedProfile = _victoryVolumeProfile;
                _victoryBlurVolumes.Add(volume);
            }
        }

        private float GetVictoryBlurWeight()
        {
            return _victoryBlurVolumes.Count > 0 &&
                _victoryBlurVolumes[0] != null
                ? _victoryBlurVolumes[0].weight
                : 0f;
        }

        private void SetVictoryBlurWeight(float weight)
        {
            float clampedWeight = Mathf.Clamp01(weight);
            foreach (Volume volume in _victoryBlurVolumes)
            {
                if (volume != null)
                {
                    volume.weight = clampedWeight;
                }
            }
        }

        private static Image CreateOverlayImage(
            Transform parent,
            string name,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            float alpha)
        {
            GameObject imageObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(parent, worldPositionStays: false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image image = imageObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
            image.raycastTarget = true;
            return image;
        }

        private static void SetImageAlpha(Image image, float alpha)
        {
            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private static void SetRawImageAlpha(RawImage image, float alpha)
        {
            Color color = image.color;
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private void CompleteWith(Sequence sequence)
        {
            _masterSequence = sequence;
            sequence.OnComplete(CompleteOnce);
        }

        private void CompleteOnce()
        {
            if (_completionInvoked)
            {
                return;
            }

            _completionInvoked = true;
            _masterSequence = null;
            Action completed = _completed;
            _completed = null;
            completed?.Invoke();
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
