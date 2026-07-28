using System;
using System.Collections.Generic;

namespace DiaBlackJack.CoreLoop
{
    public readonly struct AutomaticCardDecision
    {
        public AutomaticCardDecision(int optionId, string reasonCode)
        {
            if (string.IsNullOrWhiteSpace(reasonCode))
            {
                throw new ArgumentException(
                    "Automatic card decision reason cannot be empty.",
                    nameof(reasonCode));
            }

            OptionId = optionId;
            ReasonCode = reasonCode;
        }

        public int OptionId { get; }

        public string ReasonCode { get; }
    }

    internal readonly struct AutomaticCardOptionObservation
    {
        public AutomaticCardOptionObservation(
            int optionId,
            int? numericValue,
            int? cardRank)
        {
            OptionId = optionId;
            NumericValue = numericValue;
            CardRank = cardRank;
        }

        public int OptionId { get; }

        public int? NumericValue { get; }

        public int? CardRank { get; }
    }

    internal sealed class AutomaticCardDecisionObservation
    {
        private readonly IReadOnlyList<AutomaticCardOptionObservation> _options;
        private readonly IReadOnlyList<EnemyNumberInference> _numberInferences;

        public AutomaticCardDecisionObservation(
            CardEffectKind effectKind,
            AutomaticCardChoiceKind choiceKind,
            CombatantSide ownerSide,
            CombatantSide decisionSide,
            int playerPublicTotal,
            int enemyPublicTotal,
            int playerSoul,
            int enemySoul,
            IEnumerable<AutomaticCardOptionObservation> options,
            IEnumerable<EnemyNumberInference> numberInferences)
        {
            if (!Enum.IsDefined(typeof(CardEffectKind), effectKind) ||
                effectKind == CardEffectKind.None)
            {
                throw new ArgumentOutOfRangeException(nameof(effectKind));
            }

            if (!Enum.IsDefined(typeof(AutomaticCardChoiceKind), choiceKind))
            {
                throw new ArgumentOutOfRangeException(nameof(choiceKind));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), ownerSide))
            {
                throw new ArgumentOutOfRangeException(nameof(ownerSide));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), decisionSide))
            {
                throw new ArgumentOutOfRangeException(nameof(decisionSide));
            }

            if (playerPublicTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerPublicTotal));
            }

            if (enemyPublicTotal < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemyPublicTotal));
            }

            if (playerSoul < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(playerSoul));
            }

            if (enemySoul < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(enemySoul));
            }

            _options = Copy(options, nameof(options));
            if (_options.Count == 0)
            {
                throw new ArgumentException(
                    "Automatic card observation requires at least one option.",
                    nameof(options));
            }

            _numberInferences = Copy(
                numberInferences,
                nameof(numberInferences));
            EffectKind = effectKind;
            ChoiceKind = choiceKind;
            OwnerSide = ownerSide;
            DecisionSide = decisionSide;
            PlayerPublicTotal = playerPublicTotal;
            EnemyPublicTotal = enemyPublicTotal;
            PlayerSoul = playerSoul;
            EnemySoul = enemySoul;
        }

        public CardEffectKind EffectKind { get; }

        public AutomaticCardChoiceKind ChoiceKind { get; }

        public CombatantSide OwnerSide { get; }

        public CombatantSide DecisionSide { get; }

        public int PlayerPublicTotal { get; }

        public int EnemyPublicTotal { get; }

        public int PlayerSoul { get; }

        public int EnemySoul { get; }

        public int OwnerPublicTotal =>
            OwnerSide == CombatantSide.Player
                ? PlayerPublicTotal
                : EnemyPublicTotal;

        public int OpponentPublicTotal =>
            OwnerSide == CombatantSide.Player
                ? EnemyPublicTotal
                : PlayerPublicTotal;

        public int OwnerSoul =>
            OwnerSide == CombatantSide.Player
                ? PlayerSoul
                : EnemySoul;

        public int DecisionPublicTotal =>
            DecisionSide == CombatantSide.Player
                ? PlayerPublicTotal
                : EnemyPublicTotal;

        public IReadOnlyList<AutomaticCardOptionObservation> Options => _options;

        public IReadOnlyList<EnemyNumberInference> NumberInferences =>
            _numberInferences;

        private static IReadOnlyList<T> Copy<T>(
            IEnumerable<T> values,
            string parameterName)
        {
            if (values == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return new List<T>(values).AsReadOnly();
        }
    }

    internal interface IAutomaticCardDecisionPolicy
    {
        AutomaticCardDecision Decide(
            AutomaticCardDecisionObservation observation);
    }

    internal static class AutomaticCardDecisionObservationFactory
    {
        public static AutomaticCardDecisionObservation Create(
            CoreLoopBattle battle,
            PendingAutomaticCardInteraction interaction)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            if (interaction == null)
            {
                throw new ArgumentNullException(nameof(interaction));
            }

            CombatantSide optionOwnerSide = GetOptionOwnerSide(interaction);
            BattleParticipant optionOwner = optionOwnerSide ==
                CombatantSide.Player
                    ? battle.Player
                    : battle.Enemy;
            var options = new List<AutomaticCardOptionObservation>(
                interaction.Options.Count);
            foreach (AutomaticCardChoiceOption option in interaction.Options)
            {
                int? cardRank = null;
                if (option.CardId.HasValue)
                {
                    if (!optionOwner.Hand.TryGetCard(
                            option.CardId.Value,
                            out BlackjackCard card) ||
                        !card.IsFaceUp ||
                        optionOwner.Hand.IsHiddenCard(option.CardId.Value))
                    {
                        throw new InvalidOperationException(
                            "Automatic card option lost its public target card.");
                    }

                    cardRank = card.Rank;
                }

                options.Add(new AutomaticCardOptionObservation(
                    option.OptionId,
                    option.NumericValue,
                    cardRank));
            }

            return new AutomaticCardDecisionObservation(
                interaction.EffectKind,
                interaction.ChoiceKind,
                interaction.OwnerSide,
                interaction.DecisionSide,
                battle.Player.VisibleHandValue.Total,
                battle.Enemy.VisibleHandValue.Total,
                battle.Player.Soul.Current,
                battle.Enemy.Soul.Current,
                options,
                EnemyObservationFactory.CreateNumberInferences(battle));
        }

        private static CombatantSide GetOptionOwnerSide(
            PendingAutomaticCardInteraction interaction)
        {
            switch (interaction.ChoiceKind)
            {
                case AutomaticCardChoiceKind.FlamethrowerOwnerDiscard:
                case AutomaticCardChoiceKind.PocketWatchManualCard:
                    return interaction.OwnerSide;
                case AutomaticCardChoiceKind.FlamethrowerOpponentDiscard:
                    return interaction.OwnerSide == CombatantSide.Player
                        ? CombatantSide.Enemy
                        : CombatantSide.Player;
                default:
                    return interaction.DecisionSide;
            }
        }
    }

    internal sealed class DefaultAutomaticCardDecisionPolicy :
        IAutomaticCardDecisionPolicy
    {
        public static readonly DefaultAutomaticCardDecisionPolicy Instance =
            new DefaultAutomaticCardDecisionPolicy();

        private DefaultAutomaticCardDecisionPolicy()
        {
        }

        public AutomaticCardDecision Decide(
            AutomaticCardDecisionObservation observation)
        {
            if (observation == null)
            {
                throw new ArgumentNullException(nameof(observation));
            }

            switch (observation.ChoiceKind)
            {
                case AutomaticCardChoiceKind.PoisonDecision:
                    return DecidePoison(observation);
                case AutomaticCardChoiceKind.ResurrectionHerbDecision:
                    return DecideResurrectionHerb(observation);
                case AutomaticCardChoiceKind.LieDetectorNumber:
                    return DecideLieDetector(observation);
                case AutomaticCardChoiceKind.FlamethrowerOwnerDiscard:
                case AutomaticCardChoiceKind.FlamethrowerOpponentDiscard:
                    return DecideFlamethrower(observation);
                case AutomaticCardChoiceKind.PocketWatchManualCard:
                    return DecidePocketWatchTarget(observation);
                case AutomaticCardChoiceKind.PocketWatchSourceDisposition:
                    return DecidePocketWatchDisposition(observation);
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(observation.ChoiceKind));
            }
        }

        private static AutomaticCardDecision DecidePoison(
            AutomaticCardDecisionObservation observation)
        {
            if ((observation.OwnerSoul <= PoisonEffectHandler.SoulCost ||
                    observation.OwnerPublicTotal >= 17) &&
                TryFindOption(
                    observation,
                    PoisonEffectHandler.StandNowOptionId,
                    out AutomaticCardOptionObservation stand))
            {
                return new AutomaticCardDecision(
                    stand.OptionId,
                    "poison-preserve-soul-by-standing");
            }

            if (TryFindOption(
                    observation,
                    PoisonEffectHandler.PaySoulOptionId,
                    out AutomaticCardOptionObservation pay))
            {
                return new AutomaticCardDecision(
                    pay.OptionId,
                    "poison-pay-for-win-reward");
            }

            return First(observation, "poison-safe-first");
        }

        private static AutomaticCardDecision DecideResurrectionHerb(
            AutomaticCardDecisionObservation observation)
        {
            bool isBehind =
                observation.OwnerPublicTotal > 21 ||
                observation.OwnerPublicTotal + 2 <
                    observation.OpponentPublicTotal;
            int desiredOptionId = isBehind
                ? ResurrectionHerbEffectHandler.RestartRoundOptionId
                : ResurrectionHerbEffectHandler.DeclineOptionId;
            if (TryFindOption(
                    observation,
                    desiredOptionId,
                    out AutomaticCardOptionObservation selected))
            {
                return new AutomaticCardDecision(
                    selected.OptionId,
                    isBehind
                        ? "resurrection-herb-restart-while-behind"
                        : "resurrection-herb-decline-while-safe");
            }

            return First(observation, "resurrection-herb-safe-first");
        }

        private static AutomaticCardDecision DecideLieDetector(
            AutomaticCardDecisionObservation observation)
        {
            int totalWeight = 0;
            foreach (EnemyNumberInference inference in
                observation.NumberInferences)
            {
                totalWeight += inference.ProbabilityPercent;
            }

            int selectedNumber = 5;
            if (observation.NumberInferences.Count > 0)
            {
                int threshold = Math.Max(1, (totalWeight + 1) / 2);
                int cumulative = 0;
                selectedNumber =
                    observation.NumberInferences[
                        observation.NumberInferences.Count - 1].Number;
                foreach (EnemyNumberInference inference in
                    observation.NumberInferences)
                {
                    cumulative += inference.ProbabilityPercent;
                    if (cumulative >= threshold)
                    {
                        selectedNumber = inference.Number;
                        break;
                    }
                }
            }

            foreach (AutomaticCardOptionObservation option in
                observation.Options)
            {
                if (option.NumericValue == selectedNumber)
                {
                    return new AutomaticCardDecision(
                        option.OptionId,
                        "lie-detector-public-weighted-median");
                }
            }

            return First(
                observation,
                "lie-detector-missing-number-safe-first");
        }

        private static AutomaticCardDecision DecideFlamethrower(
            AutomaticCardDecisionObservation observation)
        {
            if (observation.DecisionPublicTotal < 17)
            {
                return FindOrFirst(
                    observation,
                    FlamethrowerEffectHandler.SkipOptionId,
                    "flamethrower-skip-under-seventeen");
            }

            AutomaticCardOptionObservation? highest = null;
            foreach (AutomaticCardOptionObservation option in
                observation.Options)
            {
                if (!option.CardRank.HasValue)
                {
                    continue;
                }

                if (!highest.HasValue ||
                    option.CardRank.Value > highest.Value.CardRank.Value)
                {
                    highest = option;
                }
            }

            return highest.HasValue
                ? new AutomaticCardDecision(
                    highest.Value.OptionId,
                    "flamethrower-discard-highest-at-seventeen")
                : FindOrFirst(
                    observation,
                    FlamethrowerEffectHandler.SkipOptionId,
                    "flamethrower-no-card-safe-skip");
        }

        private static AutomaticCardDecision DecidePocketWatchTarget(
            AutomaticCardDecisionObservation observation)
        {
            AutomaticCardOptionObservation? highest = null;
            foreach (AutomaticCardOptionObservation option in
                observation.Options)
            {
                if (!option.CardRank.HasValue)
                {
                    continue;
                }

                if (!highest.HasValue ||
                    option.CardRank.Value > highest.Value.CardRank.Value)
                {
                    highest = option;
                }
            }

            return highest.HasValue
                ? new AutomaticCardDecision(
                    highest.Value.OptionId,
                    "pocket-watch-reactivate-highest-manual")
                : FindOrFirst(
                    observation,
                    PocketWatchEffectHandler.SkipManualCardOptionId,
                    "pocket-watch-no-manual-safe-skip");
        }

        private static AutomaticCardDecision DecidePocketWatchDisposition(
            AutomaticCardDecisionObservation observation)
        {
            bool shouldDiscard = observation.OwnerPublicTotal > 21;
            return FindOrFirst(
                observation,
                shouldDiscard
                    ? PocketWatchEffectHandler.DiscardSourceOptionId
                    : PocketWatchEffectHandler.RetainSourceOptionId,
                shouldDiscard
                    ? "pocket-watch-discard-on-public-bust"
                    : "pocket-watch-retain-while-safe");
        }

        private static AutomaticCardDecision FindOrFirst(
            AutomaticCardDecisionObservation observation,
            int optionId,
            string reasonCode)
        {
            return TryFindOption(
                    observation,
                    optionId,
                    out AutomaticCardOptionObservation selected)
                ? new AutomaticCardDecision(selected.OptionId, reasonCode)
                : First(observation, reasonCode + "-first");
        }

        private static bool TryFindOption(
            AutomaticCardDecisionObservation observation,
            int optionId,
            out AutomaticCardOptionObservation selected)
        {
            foreach (AutomaticCardOptionObservation option in
                observation.Options)
            {
                if (option.OptionId == optionId)
                {
                    selected = option;
                    return true;
                }
            }

            selected = default;
            return false;
        }

        private static AutomaticCardDecision First(
            AutomaticCardDecisionObservation observation,
            string reasonCode)
        {
            return new AutomaticCardDecision(
                observation.Options[0].OptionId,
                reasonCode);
        }
    }
}
