using System;
using System.Collections.Generic;
using DG.Tweening;
using DiaBlackJack.CoreLoop;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    internal readonly struct SoulLossTokenSettings
    {
        public SoulLossTokenSettings(
            Color color,
            float fontScale,
            float minimumFontSize,
            Vector2 tokenSize,
            float fallSeconds,
            float staggerSeconds,
            float impactSeconds,
            float fadeSeconds,
            float startRandomX,
            Vector2 startYRange,
            float driftX,
            Vector2 fallDistanceRange,
            float rotation,
            Vector2 playerAnchor,
            Vector2 enemyFallbackAnchor)
        {
            Color = color;
            FontScale = Mathf.Max(0.1f, fontScale);
            MinimumFontSize = Mathf.Max(1f, minimumFontSize);
            TokenSize = new Vector2(
                Mathf.Max(1f, tokenSize.x),
                Mathf.Max(1f, tokenSize.y));
            FallSeconds = Mathf.Max(0.01f, fallSeconds);
            StaggerSeconds = Mathf.Max(0f, staggerSeconds);
            ImpactSeconds = Mathf.Clamp(impactSeconds, 0f, FallSeconds);
            FadeSeconds = Mathf.Clamp(fadeSeconds, 0.01f, FallSeconds);
            StartRandomX = Mathf.Max(0f, startRandomX);
            StartYRange = OrderedRange(startYRange);
            DriftX = Mathf.Max(0f, driftX);
            FallDistanceRange = OrderedPositiveRange(fallDistanceRange);
            Rotation = Mathf.Max(0f, rotation);
            PlayerAnchor = ClampAnchor(playerAnchor);
            EnemyFallbackAnchor = ClampAnchor(enemyFallbackAnchor);
        }

        public Color Color { get; }

        public float DriftX { get; }

        public Vector2 EnemyFallbackAnchor { get; }

        public float FadeSeconds { get; }

        public Vector2 FallDistanceRange { get; }

        public float FallSeconds { get; }

        public float FontScale { get; }

        public float ImpactSeconds { get; }

        public float MinimumFontSize { get; }

        public Vector2 PlayerAnchor { get; }

        public float Rotation { get; }

        public float StaggerSeconds { get; }

        public float StartRandomX { get; }

        public Vector2 StartYRange { get; }

        public Vector2 TokenSize { get; }

        private static Vector2 ClampAnchor(Vector2 value)
        {
            return new Vector2(
                Mathf.Clamp01(value.x),
                Mathf.Clamp01(value.y));
        }

        private static Vector2 OrderedPositiveRange(Vector2 value)
        {
            float first = Mathf.Max(0f, value.x);
            float second = Mathf.Max(0f, value.y);
            return new Vector2(
                Mathf.Min(first, second),
                Mathf.Max(first, second));
        }

        private static Vector2 OrderedRange(Vector2 value)
        {
            return new Vector2(
                Mathf.Min(value.x, value.y),
                Mathf.Max(value.x, value.y));
        }
    }

    internal sealed class SoulLossPresentation
    {
        private readonly Canvas _canvas;
        private readonly TMP_Text _template;
        private readonly RectTransform _root;
        private readonly List<TMP_Text> _tokens = new List<TMP_Text>();
        private SoulLossTokenSettings _settings;
        private Sequence _sequence;

        public SoulLossPresentation(
            Canvas canvas,
            TMP_Text template,
            SoulLossTokenSettings settings)
        {
            _canvas = canvas;
            _template = template;
            _settings = settings;
            if (_canvas == null || _template == null)
            {
                return;
            }

            var rootObject = new GameObject(
                "SoulLossPresentation",
                typeof(RectTransform));
            _root = rootObject.GetComponent<RectTransform>();
            _root.SetParent(_canvas.transform, worldPositionStays: false);
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = Vector2.zero;
            _root.offsetMax = Vector2.zero;
            _root.SetAsLastSibling();
        }

        public bool IsPlaying => _sequence != null && _sequence.IsActive();

        public void SetSettings(SoulLossTokenSettings settings)
        {
            _settings = settings;
        }

        internal static int CountTokenUnits(
            IReadOnlyList<SoulLossRecord> records)
        {
            int count = 0;
            if (records == null)
            {
                return count;
            }

            for (int index = 0; index < records.Count; index++)
            {
                count += records[index].LossAmount;
            }

            return count;
        }

        internal static IReadOnlyList<float> CreateTokenStartDelays(
            IReadOnlyList<SoulLossRecord> records,
            float staggerSeconds)
        {
            var delays = new List<float>();
            if (records == null)
            {
                return delays.AsReadOnly();
            }

            float clampedStagger = Mathf.Max(0f, staggerSeconds);
            int playerTokenOrdinal = 0;
            int enemyTokenOrdinal = 0;
            for (int recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                SoulLossRecord record = records[recordIndex];
                for (int unitIndex = 0; unitIndex < record.LossAmount; unitIndex++)
                {
                    int sideOrdinal = record.TargetSide == CombatantSide.Player
                        ? playerTokenOrdinal++
                        : enemyTokenOrdinal++;
                    delays.Add(sideOrdinal * clampedStagger);
                }
            }

            return delays.AsReadOnly();
        }

        public Sequence Play(
            IReadOnlyList<SoulLossRecord> records,
            Vector3 enemyWorldAnchor,
            Camera worldCamera,
            Action<SoulLossRecord> onImpact)
        {
            Cancel();
            if (_root == null || records == null || records.Count == 0)
            {
                return null;
            }

            Sequence sequence = DOTween.Sequence();
            int tokenOrdinal = 0;
            IReadOnlyList<float> startDelays = CreateTokenStartDelays(
                records,
                _settings.StaggerSeconds);
            for (int recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                SoulLossRecord record = records[recordIndex];
                for (int unitIndex = 0; unitIndex < record.LossAmount; unitIndex++)
                {
                    TMP_Text token = AcquireToken(tokenOrdinal);
                    float startDelay = startDelays[tokenOrdinal++];
                    Vector2 anchor = ResolveAnchor(
                        record.TargetSide,
                        enemyWorldAnchor,
                        worldCamera);
                    Vector2 start = anchor + new Vector2(
                        UnityEngine.Random.Range(
                            -_settings.StartRandomX,
                            _settings.StartRandomX),
                        UnityEngine.Random.Range(
                            _settings.StartYRange.x,
                            _settings.StartYRange.y));
                    Vector2 end = start + new Vector2(
                        UnityEngine.Random.Range(
                            -_settings.DriftX,
                            _settings.DriftX),
                        -UnityEngine.Random.Range(
                            _settings.FallDistanceRange.x,
                            _settings.FallDistanceRange.y));
                    float endRotation = UnityEngine.Random.Range(
                        -_settings.Rotation,
                        _settings.Rotation);
                    SoulLossRecord capturedRecord = record;

                    sequence.InsertCallback(startDelay, () =>
                    {
                        RectTransform rect = token.rectTransform;
                        rect.anchoredPosition = start;
                        rect.localRotation = Quaternion.identity;
                        token.alpha = 1f;
                        token.gameObject.SetActive(true);
                    });
                    sequence.Insert(
                        startDelay,
                        DOTween.To(
                                () => token.rectTransform.anchoredPosition,
                                position => token.rectTransform.anchoredPosition =
                                    position,
                                end,
                                _settings.FallSeconds)
                            .SetEase(Ease.InQuad));
                    sequence.Insert(
                        startDelay,
                        token.rectTransform
                            .DOLocalRotate(
                                new Vector3(0f, 0f, endRotation),
                                _settings.FallSeconds,
                                RotateMode.Fast)
                            .SetEase(Ease.InOutSine));
                    sequence.Insert(
                        startDelay +
                            (_settings.FallSeconds - _settings.FadeSeconds),
                        DOTween.To(
                                () => token.alpha,
                                alpha => token.alpha = alpha,
                                0f,
                                _settings.FadeSeconds)
                            .SetEase(Ease.InQuad));
                    sequence.InsertCallback(
                        startDelay + _settings.ImpactSeconds,
                        () => onImpact?.Invoke(capturedRecord));
                    sequence.InsertCallback(
                        startDelay + _settings.FallSeconds,
                        () => token.gameObject.SetActive(false));
                }
            }

            sequence.OnComplete(() =>
            {
                HideAllTokens();
                if (_sequence == sequence)
                {
                    _sequence = null;
                }
            });
            sequence.OnKill(() =>
            {
                HideAllTokens();
                if (_sequence == sequence)
                {
                    _sequence = null;
                }
            });
            _sequence = sequence;
            return sequence;
        }

        public void Cancel()
        {
            Sequence sequence = _sequence;
            _sequence = null;
            sequence?.Kill();
            HideAllTokens();
        }

        public void Dispose()
        {
            Cancel();
            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root.gameObject);
            }
        }

        private TMP_Text AcquireToken(int index)
        {
            while (_tokens.Count <= index)
            {
                var tokenObject = new GameObject(
                    $"SoulLossToken_{_tokens.Count + 1}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                RectTransform rect = tokenObject.GetComponent<RectTransform>();
                rect.SetParent(_root, worldPositionStays: false);
                TMP_Text token = tokenObject.GetComponent<TMP_Text>();
                token.font = _template.font;
                token.fontSharedMaterial = _template.fontSharedMaterial;
                token.spriteAsset = _template.spriteAsset;
                token.alignment = TextAlignmentOptions.Center;
                token.raycastTarget = false;
                CurrencyIconText.Set(
                    token,
                    $"{CurrencyIconMarkup.SoulTag} -1");
                tokenObject.SetActive(false);
                _tokens.Add(token);
            }

            TMP_Text result = _tokens[index];
            result.rectTransform.sizeDelta = _settings.TokenSize;
            result.fontSize = Mathf.Max(
                _template.fontSize * _settings.FontScale,
                _settings.MinimumFontSize);
            result.color = _settings.Color;
            return result;
        }

        private Vector2 ResolveAnchor(
            CombatantSide side,
            Vector3 enemyWorldAnchor,
            Camera worldCamera)
        {
            Vector2 screenPoint = side == CombatantSide.Player
                ? new Vector2(
                    Screen.width * _settings.PlayerAnchor.x,
                    Screen.height * _settings.PlayerAnchor.y)
                : worldCamera != null
                    ? (Vector2)worldCamera.WorldToScreenPoint(enemyWorldAnchor)
                    : new Vector2(
                        Screen.width * _settings.EnemyFallbackAnchor.x,
                        Screen.height * _settings.EnemyFallbackAnchor.y);
            Camera uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : _canvas.worldCamera != null
                    ? _canvas.worldCamera
                    : worldCamera;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _root,
                screenPoint,
                uiCamera,
                out Vector2 localPoint)
                    ? localPoint
                    : Vector2.zero;
        }

        private void HideAllTokens()
        {
            for (int index = 0; index < _tokens.Count; index++)
            {
                TMP_Text token = _tokens[index];
                if (token != null)
                {
                    token.gameObject.SetActive(false);
                }
            }
        }
    }
}
