using System;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(fileName = "NormalCard", menuName = "DiaBlackJack/Cards/Normal Card")]
    public sealed class NormalCardDefinitionSO : ScriptableObject
    {
        [SerializeField] private string key;
        [SerializeField] private string displayName;
        [TextArea(1, 3)]
        [SerializeField] private string description;
        [Min(0)]
        [SerializeField] private int basePurchasePrice = 3;
        [Min(1)]
        [SerializeField] private int shopWeight = 1;
        [Range(1, 10)]
        [SerializeField] private int rank = 1;
        [SerializeField] private CardActivationKind activation;
        [SerializeField] private CardEffectKind effect;
        [SerializeField] private bool isStandardDeckDefault;
        [SerializeField] private Sprite spadeFaceSprite;
        [SerializeField] private Sprite cloverFaceSprite;

        public string Key => key;

        internal CardDefinition CreateRuntimeDefinition()
        {
            ValidateOrThrow();
            return new CardDefinition(
                key,
                displayName,
                rank,
                activation,
                effect,
                description,
                basePurchasePrice,
                shopWeight,
                isStandardDeckDefault);
        }

        internal Sprite GetFaceSprite(CardSuit suit)
        {
            return suit == CardSuit.Clover ? cloverFaceSprite : spadeFaceSprite;
        }

        internal void ValidateOrThrow()
        {
            bool requiresDescription = effect != CardEffectKind.None;
            if (string.IsNullOrWhiteSpace(key) ||
                string.IsNullOrWhiteSpace(displayName) ||
                (requiresDescription && string.IsNullOrWhiteSpace(description)) ||
                rank < 1 || rank > 10 ||
                basePurchasePrice < 0 ||
                shopWeight <= 0 ||
                !Enum.IsDefined(typeof(CardActivationKind), activation) ||
                !Enum.IsDefined(typeof(CardEffectKind), effect) ||
                spadeFaceSprite == null ||
                cloverFaceSprite == null)
            {
                throw new InvalidOperationException(
                    $"Normal card asset '{name}' has invalid content.");
            }
        }
    }
}
