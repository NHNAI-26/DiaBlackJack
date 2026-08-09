using System;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(fileName = "DemonCard", menuName = "DiaBlackJack/Cards/Demon Card")]
    public sealed class DemonCardDefinitionSO : ScriptableObject
    {
        [SerializeField] private string key;
        [SerializeField] private string displayName;
        [TextArea(1, 3)]
        [SerializeField] private string activeEffectDescription;
        [TextArea(1, 3)]
        [SerializeField] private string costDescription;
        [Header("Codex text overrides")]
        [TextArea(1, 5)]
        [SerializeField] private string codexActiveEffectDescription;
        [TextArea(1, 5)]
        [SerializeField] private string codexCostDescription;
        [TextArea(2, 5)]
        [SerializeField] private string codexLoreDescription;
        [Min(0)]
        [SerializeField] private int baseSoulCost = 1;
        [Min(0)]
        [SerializeField] private int basePurchasePrice = 3;
        [Min(1)]
        [SerializeField] private int shopWeight = 1;
        [SerializeField] private DemonContractKind kind;
        [SerializeField] private Sprite faceSprite;

        public string Key => key;

        internal string CodexActiveEffectDescription =>
            ResolveCodexText(codexActiveEffectDescription, activeEffectDescription);

        internal string CodexCostDescription =>
            ResolveCodexText(codexCostDescription, costDescription);

        public string CodexLoreDescription => codexLoreDescription;

        internal DemonContractDefinition CreateRuntimeDefinition()
        {
            ValidateOrThrow();
            return new DemonContractDefinition(
                key,
                displayName,
                kind,
                baseSoulCost,
                activeEffectDescription,
                costDescription,
                basePurchasePrice,
                shopWeight);
        }

        internal Sprite FaceSprite => faceSprite;

        private static string ResolveCodexText(string codexText, string fallback)
        {
            return string.IsNullOrWhiteSpace(codexText)
                ? fallback
                : codexText;
        }

        internal void ValidateOrThrow()
        {
            if (string.IsNullOrWhiteSpace(key) ||
                string.IsNullOrWhiteSpace(displayName) ||
                string.IsNullOrWhiteSpace(activeEffectDescription) ||
                string.IsNullOrWhiteSpace(costDescription) ||
                string.IsNullOrWhiteSpace(codexLoreDescription) ||
                baseSoulCost < 0 ||
                basePurchasePrice < 0 ||
                shopWeight <= 0 ||
                !Enum.IsDefined(typeof(DemonContractKind), kind) ||
                faceSprite == null)
            {
                throw new InvalidOperationException(
                    $"Demon card asset '{name}' has invalid content.");
            }
        }
    }
}
