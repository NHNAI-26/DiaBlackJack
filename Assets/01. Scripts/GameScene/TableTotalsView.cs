using DG.Tweening;
using DiaBlackJack.CoreLoop;
using TMPro;
using UnityEngine;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// Drives the two world-space totals on the table. The player's label receives both the full
    /// total and the public-card total; the enemy label receives only the public-card total so its
    /// hidden rank never crosses the information boundary. Formatting is supplied by the pure
    /// presentation model; this component only writes the prepared strings to the scene TMP labels.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TableTotalsView : MonoBehaviour
    {
        private const string GlowKeyword = "GLOW_ON";
        private static readonly int FaceColorId = Shader.PropertyToID("_FaceColor");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int GlowPowerId = Shader.PropertyToID("_GlowPower");

        [SerializeField] private TMP_Text playerTotalText;
        [SerializeField] private TMP_Text enemyTotalText;

        private Tween _playerValueTween;
        private Tween _enemyValueTween;
        private Tween _playerPunchTween;
        private Tween _enemyPunchTween;
        private Material _playerOriginalMaterial;
        private Material _enemyOriginalMaterial;
        private Material _playerComparisonMaterial;
        private Material _enemyComparisonMaterial;
        private Color _playerOriginalColor = Color.white;
        private Color _enemyOriginalColor = Color.white;
        private Vector3 _playerOriginalScale = Vector3.one;
        private Vector3 _enemyOriginalScale = Vector3.one;
        private string _lastPlayerText = string.Empty;
        private string _lastEnemyText = string.Empty;
        private int _playerComparisonTotal;
        private int _enemyComparisonTotal;
        private bool _appearanceCaptured;

        internal bool IsComparisonActive { get; private set; }

        public void Render(string playerTotals, string enemyVisibleTotal)
        {
            _lastPlayerText = playerTotals ?? string.Empty;
            _lastEnemyText = enemyVisibleTotal ?? string.Empty;
            if (IsComparisonActive)
            {
                return;
            }

            if (playerTotalText != null)
            {
                playerTotalText.text = _lastPlayerText;
            }

            if (enemyTotalText != null)
            {
                enemyTotalText.text = _lastEnemyText;
            }
        }

        internal void BeginComparison()
        {
            CaptureAppearance();
            KillComparisonTweens();
            IsComparisonActive = true;
            _playerComparisonTotal = 0;
            _enemyComparisonTotal = 0;
            ApplyComparisonMaterial(
                playerTotalText,
                _playerComparisonMaterial);
            ApplyComparisonMaterial(
                enemyTotalText,
                _enemyComparisonMaterial);
            ApplyComparisonValue(playerTotalText, _playerComparisonMaterial, 0);
            ApplyComparisonValue(enemyTotalText, _enemyComparisonMaterial, 0);
            RestoreComparisonScales();
        }

        internal void AnimateComparisonTotal(
            CombatantSide side,
            int total,
            float durationSeconds)
        {
            if (!IsComparisonActive)
            {
                return;
            }

            TMP_Text label = side == CombatantSide.Player
                ? playerTotalText
                : enemyTotalText;
            Material material = side == CombatantSide.Player
                ? _playerComparisonMaterial
                : _enemyComparisonMaterial;
            int start = side == CombatantSide.Player
                ? _playerComparisonTotal
                : _enemyComparisonTotal;
            float duration = Mathf.Max(0.01f, durationSeconds);
            KillSideTweens(side);
            if (label == null)
            {
                SetComparisonTotal(side, total);
                return;
            }

            Tween valueTween = DOVirtual.Int(
                    start,
                    total,
                    duration,
                    value =>
                    {
                        SetComparisonTotal(side, value);
                        ApplyComparisonValue(label, material, value);
                    })
                .SetEase(Ease.OutCubic)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            label.transform.localScale = OriginalScale(side);
            Tween punchTween = label.transform
                .DOPunchScale(
                    Vector3.one * 0.14f,
                    duration,
                    vibrato: 5,
                    elasticity: 0.55f)
                .SetTarget(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            if (side == CombatantSide.Player)
            {
                _playerValueTween = valueTween;
                _playerPunchTween = punchTween;
            }
            else
            {
                _enemyValueTween = valueTween;
                _enemyPunchTween = punchTween;
            }
        }

        internal void CompleteComparison(
            string playerTotals,
            string enemyVisibleTotal)
        {
            _lastPlayerText = playerTotals ?? string.Empty;
            _lastEnemyText = enemyVisibleTotal ?? string.Empty;
            EndComparison(_lastPlayerText, _lastEnemyText);
        }

        internal void CancelComparison()
        {
            if (!IsComparisonActive)
            {
                return;
            }

            EndComparison(_lastPlayerText, _lastEnemyText);
        }

        internal static Color ResolveComparisonColor(int total)
        {
            if (total > 21)
            {
                return new Color(1f, 0.055f, 0.035f, 1f);
            }

            if (total <= 15)
            {
                return Color.white;
            }

            float proximity = Mathf.Clamp01((total - 15f) / 6f);
            Color gold = new Color(1f, 0.68f, 0.12f, 1f);
            Color baseColor = Color.Lerp(Color.white, gold, proximity);
            float hdrMultiplier = Mathf.Lerp(1f, 3.5f, proximity * proximity);
            return new Color(
                baseColor.r * hdrMultiplier,
                baseColor.g * hdrMultiplier,
                baseColor.b * hdrMultiplier,
                1f);
        }

        internal static float ResolveComparisonBloomStrength(int total)
        {
            if (total <= 15 || total > 21)
            {
                return 0f;
            }

            return Mathf.Clamp01((total - 15f) / 6f);
        }

        private void OnDisable()
        {
            CancelComparison();
        }

        private void OnDestroy()
        {
            KillComparisonTweens();
            DestroyMaterial(_playerComparisonMaterial);
            DestroyMaterial(_enemyComparisonMaterial);
        }

        private void CaptureAppearance()
        {
            if (_appearanceCaptured)
            {
                return;
            }

            _appearanceCaptured = true;
            CaptureLabel(
                playerTotalText,
                ref _playerOriginalMaterial,
                ref _playerComparisonMaterial,
                ref _playerOriginalColor,
                ref _playerOriginalScale,
                "Player Comparison");
            CaptureLabel(
                enemyTotalText,
                ref _enemyOriginalMaterial,
                ref _enemyComparisonMaterial,
                ref _enemyOriginalColor,
                ref _enemyOriginalScale,
                "Enemy Comparison");
        }

        private static void CaptureLabel(
            TMP_Text label,
            ref Material originalMaterial,
            ref Material comparisonMaterial,
            ref Color originalColor,
            ref Vector3 originalScale,
            string materialSuffix)
        {
            if (label == null)
            {
                return;
            }

            originalMaterial = label.fontSharedMaterial;
            originalColor = label.color;
            originalScale = label.transform.localScale;
            if (originalMaterial != null)
            {
                comparisonMaterial = new Material(originalMaterial)
                {
                    name = originalMaterial.name + " (" + materialSuffix + ")"
                };
            }
        }

        private static void ApplyComparisonMaterial(
            TMP_Text label,
            Material material)
        {
            if (label == null)
            {
                return;
            }

            if (material != null)
            {
                label.fontSharedMaterial = material;
            }

            label.color = Color.white;
        }

        private static void ApplyComparisonValue(
            TMP_Text label,
            Material material,
            int total)
        {
            if (label == null)
            {
                return;
            }

            label.text = "총합 : " + total;
            Color color = ResolveComparisonColor(total);
            float bloom = ResolveComparisonBloomStrength(total);
            if (material == null || !material.HasProperty(FaceColorId))
            {
                label.color = color;
                return;
            }

            label.color = Color.white;
            material.SetColor(FaceColorId, color);
            if (material.HasProperty(GlowColorId))
            {
                material.SetColor(GlowColorId, color);
            }

            if (material.HasProperty(GlowPowerId))
            {
                material.SetFloat(GlowPowerId, Mathf.Lerp(0f, 0.75f, bloom));
            }

            if (bloom > 0f)
            {
                material.EnableKeyword(GlowKeyword);
            }
            else
            {
                material.DisableKeyword(GlowKeyword);
            }

            label.UpdateMeshPadding();
        }

        private void EndComparison(string playerText, string enemyText)
        {
            KillComparisonTweens();
            IsComparisonActive = false;
            RestoreLabel(
                playerTotalText,
                _playerOriginalMaterial,
                _playerOriginalColor,
                _playerOriginalScale,
                playerText);
            RestoreLabel(
                enemyTotalText,
                _enemyOriginalMaterial,
                _enemyOriginalColor,
                _enemyOriginalScale,
                enemyText);
        }

        private static void RestoreLabel(
            TMP_Text label,
            Material material,
            Color color,
            Vector3 scale,
            string text)
        {
            if (label == null)
            {
                return;
            }

            if (material != null)
            {
                label.fontSharedMaterial = material;
            }

            label.color = color;
            label.transform.localScale = scale;
            label.text = text ?? string.Empty;
        }

        private void KillSideTweens(CombatantSide side)
        {
            if (side == CombatantSide.Player)
            {
                KillTween(ref _playerValueTween);
                KillTween(ref _playerPunchTween);
                if (playerTotalText != null)
                {
                    playerTotalText.transform.localScale = _playerOriginalScale;
                }

                return;
            }

            KillTween(ref _enemyValueTween);
            KillTween(ref _enemyPunchTween);
            if (enemyTotalText != null)
            {
                enemyTotalText.transform.localScale = _enemyOriginalScale;
            }
        }

        private void KillComparisonTweens()
        {
            KillTween(ref _playerValueTween);
            KillTween(ref _enemyValueTween);
            KillTween(ref _playerPunchTween);
            KillTween(ref _enemyPunchTween);
            RestoreComparisonScales();
        }

        private void RestoreComparisonScales()
        {
            if (playerTotalText != null)
            {
                playerTotalText.transform.localScale = _playerOriginalScale;
            }

            if (enemyTotalText != null)
            {
                enemyTotalText.transform.localScale = _enemyOriginalScale;
            }
        }

        private static void KillTween(ref Tween tween)
        {
            if (tween != null && tween.IsActive())
            {
                tween.Kill();
            }

            tween = null;
        }

        private void SetComparisonTotal(CombatantSide side, int total)
        {
            if (side == CombatantSide.Player)
            {
                _playerComparisonTotal = total;
            }
            else
            {
                _enemyComparisonTotal = total;
            }
        }

        private Vector3 OriginalScale(CombatantSide side)
        {
            return side == CombatantSide.Player
                ? _playerOriginalScale
                : _enemyOriginalScale;
        }

        private static void DestroyMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

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
}
