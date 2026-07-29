using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public enum EnemyChangeCostMode
    {
        Accumulating,
        FixedOne
    }

    public sealed class EnemyCombatProfile
    {
        public EnemyCombatProfile(
            string key,
            string displayName,
            EnemyGrade grade,
            int maximumSoul,
            string behaviorPolicyKey,
            IEnumerable<string> deckDefinitionKeys,
            string summary,
            EnemyInformationMode playerInformationMode,
            EnemyChangeCostMode changeCostMode = EnemyChangeCostMode.Accumulating,
            IEnumerable<string> demonContractDefinitionKeys = null,
            int demonContractCandidateCount =
                DemonContractDeck.MaximumCandidateCount,
            bool injectsPoisonIntoPlayerDeckEachRound = false)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Enemy profile key cannot be empty.", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Enemy display name cannot be empty.", nameof(displayName));
            }

            if (!Enum.IsDefined(typeof(EnemyGrade), grade))
            {
                throw new ArgumentOutOfRangeException(nameof(grade));
            }

            if (maximumSoul <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumSoul),
                    "Enemy maximum soul must be positive.");
            }

            if (string.IsNullOrWhiteSpace(behaviorPolicyKey))
            {
                throw new ArgumentException(
                    "Enemy behavior policy key cannot be empty.",
                    nameof(behaviorPolicyKey));
            }

            if (!EnemyBehaviorPolicyCatalog.Contains(behaviorPolicyKey))
            {
                throw new KeyNotFoundException(
                    $"Enemy behavior policy '{behaviorPolicyKey}' does not exist.");
            }

            if (deckDefinitionKeys == null)
            {
                throw new ArgumentNullException(nameof(deckDefinitionKeys));
            }

            var validatedDeckKeys = new List<string>();
            foreach (string deckDefinitionKey in deckDefinitionKeys)
            {
                if (string.IsNullOrWhiteSpace(deckDefinitionKey))
                {
                    throw new ArgumentException(
                        "Enemy deck definition key cannot be empty.",
                        nameof(deckDefinitionKeys));
                }

                CardDefinitionCatalog.GetByKey(deckDefinitionKey);
                validatedDeckKeys.Add(deckDefinitionKey);
            }

            if (validatedDeckKeys.Count == 0)
            {
                throw new ArgumentException(
                    "Enemy deck must contain at least one card definition.",
                    nameof(deckDefinitionKeys));
            }

            if (string.IsNullOrWhiteSpace(summary))
            {
                throw new ArgumentException("Enemy summary cannot be empty.", nameof(summary));
            }

            if (!Enum.IsDefined(typeof(EnemyInformationMode), playerInformationMode))
            {
                throw new ArgumentOutOfRangeException(nameof(playerInformationMode));
            }

            if (!Enum.IsDefined(typeof(EnemyChangeCostMode), changeCostMode))
            {
                throw new ArgumentOutOfRangeException(nameof(changeCostMode));
            }

            List<string> validatedDemonContractKeys = new List<string>();
            if (demonContractDefinitionKeys != null)
            {
                foreach (string definitionKey in demonContractDefinitionKeys)
                {
                    if (string.IsNullOrWhiteSpace(definitionKey))
                    {
                        throw new ArgumentException(
                            "Enemy demon contract key cannot be empty.",
                            nameof(demonContractDefinitionKeys));
                    }

                    DemonContractCatalog.Default.GetByKey(definitionKey);
                    validatedDemonContractKeys.Add(definitionKey);
                }
            }

            if (demonContractCandidateCount <= 0 ||
                demonContractCandidateCount > DemonContractDeck.LuciferCandidateCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(demonContractCandidateCount));
            }

            Key = key;
            DisplayName = displayName;
            Grade = grade;
            MaximumSoul = maximumSoul;
            BehaviorPolicyKey = behaviorPolicyKey;
            DeckDefinitionKeys = new ReadOnlyCollection<string>(validatedDeckKeys);
            Summary = summary;
            PlayerInformationMode = playerInformationMode;
            ChangeCostMode = changeCostMode;
            DemonContractDefinitionKeys = new ReadOnlyCollection<string>(
                validatedDemonContractKeys);
            DemonContractCandidateCount = demonContractCandidateCount;
            InjectsPoisonIntoPlayerDeckEachRound =
                injectsPoisonIntoPlayerDeckEachRound;
        }

        public string BehaviorPolicyKey { get; }

        public IReadOnlyList<string> DeckDefinitionKeys { get; }

        public EnemyChangeCostMode ChangeCostMode { get; }

        public int DemonContractCandidateCount { get; }

        public IReadOnlyList<string> DemonContractDefinitionKeys { get; }

        public string DisplayName { get; }

        public EnemyGrade Grade { get; }

        public string Key { get; }

        public bool InjectsPoisonIntoPlayerDeckEachRound { get; }

        public int MaximumSoul { get; }

        public EnemyInformationMode PlayerInformationMode { get; }

        public string Summary { get; }

        public EnemyProfilePreview CreatePreview()
        {
            return EnemyProfilePreview.FromProfile(this);
        }
    }
}
