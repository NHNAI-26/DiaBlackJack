using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(fileName = "CardContentCatalog", menuName = "DiaBlackJack/Cards/Card Content Catalog")]
    public sealed class CardContentCatalogSO : ScriptableObject
    {
        [SerializeField] private List<NormalCardDefinitionSO> normalCards = new List<NormalCardDefinitionSO>();
        [SerializeField] private List<DemonCardDefinitionSO> demonCards = new List<DemonCardDefinitionSO>();

        public CardContentCatalog BuildRuntimeCatalog()
        {
            var normalDefinitions = new List<CardDefinition>(normalCards.Count);
            foreach (NormalCardDefinitionSO card in normalCards)
            {
                if (card == null)
                {
                    throw new InvalidOperationException("Card content catalog contains a null normal card.");
                }

                normalDefinitions.Add(card.CreateRuntimeDefinition());
            }

            var demonDefinitions = new List<DemonContractDefinition>(demonCards.Count);
            foreach (DemonCardDefinitionSO card in demonCards)
            {
                if (card == null)
                {
                    throw new InvalidOperationException("Card content catalog contains a null demon card.");
                }

                demonDefinitions.Add(card.CreateRuntimeDefinition());
            }

            return new CardContentCatalog(normalDefinitions, demonDefinitions);
        }

        public Sprite GetDemonFaceSprite(string definitionKey)
        {
            foreach (DemonCardDefinitionSO card in demonCards)
            {
                if (card != null && string.Equals(card.Key, definitionKey, StringComparison.Ordinal))
                {
                    return card.FaceSprite;
                }
            }

            return null;
        }

        public Sprite GetNormalFaceSprite(string definitionKey, CardSuit suit)
        {
            foreach (NormalCardDefinitionSO card in normalCards)
            {
                if (card != null && string.Equals(card.Key, definitionKey, StringComparison.Ordinal))
                {
                    return card.GetFaceSprite(suit);
                }
            }

            return null;
        }

        private void OnValidate()
        {
            try
            {
                BuildRuntimeCatalog();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }
    }
}
