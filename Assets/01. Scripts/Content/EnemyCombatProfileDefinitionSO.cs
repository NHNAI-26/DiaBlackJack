using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using UnityEngine;

namespace DiaBlackJack.Content
{
    [CreateAssetMenu(
        fileName = "EnemyProfile",
        menuName = "DiaBlackJack/Enemies/Enemy Profile")]
    public sealed class EnemyCombatProfileDefinitionSO : ScriptableObject
    {
        [Serializable]
        private sealed class FixedDemonContractPhaseEntry
        {
            [SerializeField] private bool startsAtBattleStart;
            [Min(1)]
            [SerializeField] private int activationSoulThreshold = 1;
            [SerializeField] private DemonCardDefinitionSO activeCard;
            [SerializeField] private DemonCardDefinitionSO discardedCard;

            public FixedDemonContractPhaseDefinition CreateRuntimeDefinition()
            {
                if ((!startsAtBattleStart && activationSoulThreshold <= 0) ||
                    activeCard == null ||
                    discardedCard == null)
                {
                    throw new InvalidOperationException(
                        "Enemy fixed demon phase contains invalid content.");
                }

                return new FixedDemonContractPhaseDefinition(
                    startsAtBattleStart ? null : activationSoulThreshold,
                    activeCard.Key,
                    discardedCard.Key);
            }
        }

        [SerializeField] private string key;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite portrait;
        [SerializeField] private SpeechProfileSO speechProfile;
        [Header("GameScene presentation")]
        [ColorUsage(true, true)]
        [SerializeField] private Color deckTopTint =
            new Color(1.35f, 1.35f, 1.35f, 1f);
        [ColorUsage(true, true)]
        [SerializeField] private Color skullTint =
            new Color(1.35f, 1.35f, 1.35f, 1f);
        [SerializeField] private EnemyGrade grade;
        [Min(1)]
        [SerializeField] private int maximumSoul = 1;
        [Min(1)]
        [SerializeField] private int defeatGold = 1;
        [SerializeField] private string behaviorPolicyKey;
        [SerializeField] private List<NormalCardDefinitionSO> startingDeck =
            new List<NormalCardDefinitionSO>();
        [TextArea(2, 5)]
        [SerializeField] private string description;
        [SerializeField] private EnemyInformationMode playerInformationMode;
        [SerializeField] private EnemyChangeCostMode changeCostMode;
        [SerializeField] private List<DemonCardDefinitionSO> contractableDemons =
            new List<DemonCardDefinitionSO>();
        [Min(1)]
        [SerializeField] private int demonContractCandidateCount =
            DemonContractDeck.MaximumCandidateCount;
        [SerializeField] private bool injectsPoisonIntoPlayerDeckEachRound;
        [SerializeField] private List<FixedDemonContractPhaseEntry>
            fixedDemonContractPhases =
                new List<FixedDemonContractPhaseEntry>();

        public int DefeatGold => defeatGold;

        public Color DeckTopTint => deckTopTint;

        public Color SkullTint => skullTint;

        public string Key => key;

        public Sprite Portrait => portrait;

        public SpeechProfileSO SpeechProfile => speechProfile;

        internal EnemyCombatProfile CreateRuntimeProfile()
        {
            ValidateBasicContent();

            var deckKeys = new List<string>(startingDeck.Count);
            foreach (NormalCardDefinitionSO card in startingDeck)
            {
                if (card == null)
                {
                    throw new InvalidOperationException(
                        $"Enemy asset '{name}' contains a null starting card.");
                }

                deckKeys.Add(card.Key);
            }

            var demonKeys = new List<string>(contractableDemons.Count);
            foreach (DemonCardDefinitionSO demon in contractableDemons)
            {
                if (demon == null)
                {
                    throw new InvalidOperationException(
                        $"Enemy asset '{name}' contains a null demon card.");
                }

                demonKeys.Add(demon.Key);
            }

            var phases = new List<FixedDemonContractPhaseDefinition>(
                fixedDemonContractPhases.Count);
            foreach (FixedDemonContractPhaseEntry phase in
                fixedDemonContractPhases)
            {
                if (phase == null)
                {
                    throw new InvalidOperationException(
                        $"Enemy asset '{name}' contains a null fixed phase.");
                }

                phases.Add(phase.CreateRuntimeDefinition());
            }

            return new EnemyCombatProfile(
                key,
                displayName,
                grade,
                maximumSoul,
                behaviorPolicyKey,
                deckKeys,
                description,
                playerInformationMode,
                changeCostMode,
                demonKeys,
                demonContractCandidateCount,
                injectsPoisonIntoPlayerDeckEachRound,
                phases);
        }

        internal KeyValuePair<string, int> CreateGoldReward()
        {
            ValidateBasicContent();
            return new KeyValuePair<string, int>(key, defeatGold);
        }

        private void OnValidate()
        {
            if ((hideFlags & HideFlags.DontSave) != 0)
            {
                return;
            }

            try
            {
                CreateRuntimeProfile();
                CreateGoldReward();
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message, this);
            }
        }

        private void ValidateBasicContent()
        {
            if (string.IsNullOrWhiteSpace(key) ||
                string.IsNullOrWhiteSpace(displayName) ||
                portrait == null ||
                speechProfile == null ||
                !Enum.IsDefined(typeof(EnemyGrade), grade) ||
                maximumSoul <= 0 ||
                defeatGold < 0 ||
                string.IsNullOrWhiteSpace(behaviorPolicyKey) ||
                startingDeck == null ||
                startingDeck.Count == 0 ||
                string.IsNullOrWhiteSpace(description) ||
                !Enum.IsDefined(
                    typeof(EnemyInformationMode),
                    playerInformationMode) ||
                !Enum.IsDefined(typeof(EnemyChangeCostMode), changeCostMode) ||
                contractableDemons == null ||
                demonContractCandidateCount <= 0 ||
                fixedDemonContractPhases == null)
            {
                throw new InvalidOperationException(
                    $"Enemy asset '{name}' contains invalid content.");
            }

            speechProfile.ValidateOrThrow();
            if (!string.Equals(
                speechProfile.SpeakerKey,
                key,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Enemy asset '{name}' speech profile key must match '{key}'.");
            }
        }
    }
}
