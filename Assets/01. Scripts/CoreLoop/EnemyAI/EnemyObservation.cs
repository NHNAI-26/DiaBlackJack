using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DiaBlackJack.CoreLoop
{
    public readonly struct SoulObservation
    {
        public SoulObservation(int current, int maximum)
        {
            if (maximum <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            if (current < 0 || current > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(current));
            }

            Current = current;
            Maximum = maximum;
        }

        public int Current { get; }

        public int Maximum { get; }
    }

    public sealed class PublicCardObservation
    {
        public PublicCardObservation(
            string definitionKey,
            int rank,
            bool canUse = false)
        {
            if (string.IsNullOrWhiteSpace(definitionKey))
            {
                throw new ArgumentException("Public card definition key cannot be empty.", nameof(definitionKey));
            }

            if (rank < 1 || rank > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            DefinitionKey = definitionKey;
            Rank = rank;
            CanUse = canUse;
        }

        public bool CanUse { get; }

        public string DefinitionKey { get; }

        public int Rank { get; }
    }

    public sealed class EnemyOwnedCardObservation
    {
        public EnemyOwnedCardObservation(
            int cardId,
            string definitionKey,
            int rank,
            bool isFaceUp,
            CardUseState useState,
            bool canUse,
            bool isHiddenCard = false)
        {
            if (cardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cardId));
            }

            if (string.IsNullOrWhiteSpace(definitionKey))
            {
                throw new ArgumentException("Owned card definition key cannot be empty.", nameof(definitionKey));
            }

            if (rank < 1 || rank > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            if (!Enum.IsDefined(typeof(CardUseState), useState))
            {
                throw new ArgumentOutOfRangeException(nameof(useState));
            }

            CardId = cardId;
            DefinitionKey = definitionKey;
            Rank = rank;
            IsFaceUp = isFaceUp;
            IsHiddenCard = isHiddenCard;
            UseState = useState;
            CanUse = canUse;
        }

        public bool CanUse { get; }

        public int CardId { get; }

        public string DefinitionKey { get; }

        public bool IsFaceUp { get; }

        public bool IsHiddenCard { get; }

        public int Rank { get; }

        public CardUseState UseState { get; }
    }

    public sealed class PublicCombatAction
    {
        public PublicCombatAction(
            CombatantSide actorSide,
            PublicCombatActionType actionType,
            string sourceCardDefinitionKey = null)
        {
            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            if (!Enum.IsDefined(typeof(PublicCombatActionType), actionType))
            {
                throw new ArgumentOutOfRangeException(nameof(actionType));
            }

            if (actionType == PublicCombatActionType.UseCard ||
                actionType == PublicCombatActionType.DemonContract)
            {
                if (string.IsNullOrWhiteSpace(sourceCardDefinitionKey))
                {
                    throw new ArgumentException(
                    "Public card or contract action requires a definition key.",
                        nameof(sourceCardDefinitionKey));
                }
            }
            else if (sourceCardDefinitionKey != null)
            {
                throw new ArgumentException(
                    "Only public card or contract actions can contain a definition key.");
            }

            ActorSide = actorSide;
            ActionType = actionType;
            SourceCardDefinitionKey = sourceCardDefinitionKey;
        }

        public PublicCombatActionType ActionType { get; }

        public CombatantSide ActorSide { get; }

        public string SourceCardDefinitionKey { get; }
    }

    public readonly struct EnemyNumberInference
    {
        public EnemyNumberInference(int number, int probabilityPercent)
        {
            if (number < 1 || number > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(number));
            }

            if (probabilityPercent < 0 || probabilityPercent > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(probabilityPercent));
            }

            Number = number;
            ProbabilityPercent = probabilityPercent;
        }

        public int Number { get; }

        public int ProbabilityPercent { get; }
    }

    public sealed class EnemyObservation
    {
        public EnemyObservation(HandValue ownHandValue)
            : this(
                ownHandValue,
                Array.Empty<EnemyOwnedCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                playerHiddenCardCount: 0,
                new SoulObservation(1, 1),
                new SoulObservation(1, 1),
                roundNumber: 0,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 0,
                playerDeckAvailableCount: 0,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                Array.Empty<EnemyActionCandidate>(),
                Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: null,
                decisionSeed: 0,
                lieDetectorComparisonKnowledge: null,
                knownPlayerHiddenCardRank: null,
                enemyChangeSoulCost: 0,
                injectedPoisonCardCount: 0,
                ownerHasActiveSatanContract: false)
        {
        }

        internal EnemyObservation(
            HandValue ownHandValue,
            IEnumerable<EnemyOwnedCardObservation> ownCards,
            IEnumerable<PublicCardObservation> playerFaceUpCards,
            int playerHiddenCardCount,
            SoulObservation playerSoul,
            SoulObservation enemySoul,
            int roundNumber,
            bool playerIsStanding,
            bool enemyIsStanding,
            int ownDeckAvailableCount,
            int playerDeckAvailableCount,
            IEnumerable<PublicCardObservation> ownDiscardedCards,
            IEnumerable<PublicCardObservation> playerDiscardedCards,
            IEnumerable<PublicCombatAction> publicActionHistory,
            IEnumerable<EnemyActionCandidate> actionCandidates,
            IEnumerable<EnemyNumberInference> numberInferences,
            CardEffectKind? pendingCardEffectKind,
            int decisionSeed,
            HiddenCardComparisonKnowledge?
                lieDetectorComparisonKnowledge = null,
            int? knownPlayerHiddenCardRank = null,
            int enemyChangeSoulCost = 0,
            int injectedPoisonCardCount = 0,
            bool ownerHasActiveSatanContract = false)
        {
            if (playerHiddenCardCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerHiddenCardCount));
            }

            if (enemyChangeSoulCost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyChangeSoulCost));
            }

            if (injectedPoisonCardCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(injectedPoisonCardCount));
            }

            if (roundNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (ownDeckAvailableCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ownDeckAvailableCount));
            }

            if (playerDeckAvailableCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerDeckAvailableCount));
            }

            if (pendingCardEffectKind.HasValue &&
                (pendingCardEffectKind.Value == CardEffectKind.None ||
                    !Enum.IsDefined(typeof(CardEffectKind), pendingCardEffectKind.Value)))
            {
                throw new ArgumentOutOfRangeException(nameof(pendingCardEffectKind));
            }

            if (knownPlayerHiddenCardRank.HasValue &&
                (playerHiddenCardCount != 1 ||
                    knownPlayerHiddenCardRank.Value < 1 ||
                    knownPlayerHiddenCardRank.Value > 10))
            {
                throw new ArgumentException(
                    "Known hidden rank requires exactly one valid hidden-role card.",
                    nameof(knownPlayerHiddenCardRank));
            }

            if (lieDetectorComparisonKnowledge.HasValue)
            {
                HiddenCardComparisonKnowledge knowledge =
                    lieDetectorComparisonKnowledge.Value;
                if (knowledge.ObserverSide != CombatantSide.Enemy ||
                    knowledge.SubjectSide != CombatantSide.Player ||
                    knowledge.RoundNumber != roundNumber)
                {
                    throw new ArgumentException(
                        "Enemy observation received comparison knowledge outside its legal boundary.",
                        nameof(lieDetectorComparisonKnowledge));
                }
            }

            OwnHandValue = ownHandValue;
            OwnCards = Copy(ownCards, nameof(ownCards));
            PlayerFaceUpCards = Copy(playerFaceUpCards, nameof(playerFaceUpCards));
            PlayerHiddenCardCount = playerHiddenCardCount;
            PlayerSoul = playerSoul;
            EnemySoul = enemySoul;
            RoundNumber = roundNumber;
            PlayerIsStanding = playerIsStanding;
            EnemyIsStanding = enemyIsStanding;
            OwnDeckAvailableCount = ownDeckAvailableCount;
            PlayerDeckAvailableCount = playerDeckAvailableCount;
            OwnDiscardedCards = Copy(ownDiscardedCards, nameof(ownDiscardedCards));
            PlayerDiscardedCards = Copy(playerDiscardedCards, nameof(playerDiscardedCards));
            PublicActionHistory = Copy(publicActionHistory, nameof(publicActionHistory));
            ActionCandidates = Copy(actionCandidates, nameof(actionCandidates));
            NumberInferences = Copy(numberInferences, nameof(numberInferences));
            PendingCardEffectKind = pendingCardEffectKind;
            DecisionSeed = decisionSeed;
            LieDetectorComparisonKnowledge =
                lieDetectorComparisonKnowledge;
            KnownPlayerHiddenCardRank = knownPlayerHiddenCardRank;
            EnemyChangeSoulCost = enemyChangeSoulCost;
            InjectedPoisonCardCount = injectedPoisonCardCount;
            OwnerHasActiveSatanContract = ownerHasActiveSatanContract;
        }

        public IReadOnlyList<EnemyActionCandidate> ActionCandidates { get; }

        public int DecisionSeed { get; }

        public bool EnemyIsStanding { get; }

        public SoulObservation EnemySoul { get; }

        public IReadOnlyList<EnemyNumberInference> NumberInferences { get; }

        public IReadOnlyList<EnemyOwnedCardObservation> OwnCards { get; }

        public int OwnDeckAvailableCount { get; }

        public IReadOnlyList<PublicCardObservation> OwnDiscardedCards { get; }

        public HandValue OwnHandValue { get; }

        public CardEffectKind? PendingCardEffectKind { get; }

        public int PlayerDeckAvailableCount { get; }

        public IReadOnlyList<PublicCardObservation> PlayerDiscardedCards { get; }

        public IReadOnlyList<PublicCardObservation> PlayerFaceUpCards { get; }

        public int PlayerHiddenCardCount { get; }

        public HiddenCardComparisonKnowledge?
            LieDetectorComparisonKnowledge { get; }

        public int? KnownPlayerHiddenCardRank { get; }

        public int EnemyChangeSoulCost { get; }

        public int InjectedPoisonCardCount { get; }

        public bool PlayerIsStanding { get; }

        public bool OwnerHasActiveSatanContract { get; }

        public SoulObservation PlayerSoul { get; }

        public IReadOnlyList<PublicCombatAction> PublicActionHistory { get; }

        public int RoundNumber { get; }

        private static IReadOnlyList<T> Copy<T>(IEnumerable<T> values, string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var copied = new List<T>();
            foreach (T value in values)
            {
                if (ReferenceEquals(value, null))
                {
                    throw new ArgumentException("Observation collections cannot contain null.", parameterName);
                }

                copied.Add(value);
            }

            return new ReadOnlyCollection<T>(copied);
        }
    }
}
