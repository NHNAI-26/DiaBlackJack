using System;
using System.Collections.Generic;
using System.Text;

namespace DiaBlackJack.CoreLoop.UI
{
    public sealed class PlayerCardViewModel
    {
        public PlayerCardViewModel(
            int cardId,
            int rank,
            string displayName,
            bool isFaceUp,
            CardUseState useState,
            bool canUse,
            CardUseUnavailableReason unavailableReason,
            string disabledReason)
        {
            CardId = cardId;
            Rank = rank;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            IsFaceUp = isFaceUp;
            UseState = useState;
            CanUse = canUse;
            UnavailableReason = unavailableReason;
            DisabledReason = disabledReason ?? string.Empty;
        }

        public int CardId { get; }

        public int Rank { get; }

        public string DisplayName { get; }

        public bool IsFaceUp { get; }

        public CardUseState UseState { get; }

        public bool CanUse { get; }

        public CardUseUnavailableReason UnavailableReason { get; }

        public string DisabledReason { get; }
    }

    public sealed class CardEffectChoiceViewModel
    {
        public CardEffectChoiceViewModel(
            int optionId,
            string label,
            int? cardId = null)
        {
            OptionId = optionId;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            CardId = cardId;
        }

        public int OptionId { get; }

        public string Label { get; }

        public int? CardId { get; }
    }

    public sealed class AutomaticCardChoiceViewModel
    {
        public AutomaticCardChoiceViewModel(
            int optionId,
            string label,
            int? cardId = null)
        {
            OptionId = optionId;
            Label = label ?? throw new ArgumentNullException(nameof(label));
            CardId = cardId;
        }

        public int OptionId { get; }

        public string Label { get; }

        public int? CardId { get; }
    }

    public sealed class AutomaticCardInteractionViewModel
    {
        public AutomaticCardInteractionViewModel(
            int interactionId,
            int sourceCardId,
            string sourceDisplayName,
            CardEffectKind effectKind,
            AutomaticCardChoiceKind choiceKind,
            IReadOnlyList<AutomaticCardChoiceViewModel> choices)
        {
            InteractionId = interactionId;
            SourceCardId = sourceCardId;
            SourceDisplayName = sourceDisplayName ??
                throw new ArgumentNullException(nameof(sourceDisplayName));
            EffectKind = effectKind;
            ChoiceKind = choiceKind;
            Choices = choices ??
                throw new ArgumentNullException(nameof(choices));
        }

        public int InteractionId { get; }

        public int SourceCardId { get; }

        public string SourceDisplayName { get; }

        public CardEffectKind EffectKind { get; }

        public AutomaticCardChoiceKind ChoiceKind { get; }

        public IReadOnlyList<AutomaticCardChoiceViewModel> Choices { get; }
    }

    public sealed class CoreLoopViewModel
    {
        public CoreLoopViewModel(
            CoreLoopState state,
            BattleOutcome outcome,
            int roundNumber,
            int turnNumber,
            string playerSoul,
            string enemySoul,
            string playerCards,
            string enemyCards,
            int playerTotal,
            int playerVisibleTotal,
            int enemyVisibleTotal,
            string playerDeck,
            string enemyDeck,
            string enemyDisplayName,
            string enemyGrade,
            string enemySummary,
            string enemyInformationTitle,
            IReadOnlyList<string> enemyInformationLines,
            string enemyWarning,
            string lastRound,
            string changeActionText,
            IReadOnlyList<string> changeCandidates,
            bool canHit,
            bool canStand,
            bool canChange,
            bool isChoosingChangeCard,
            IReadOnlyList<PlayerCardViewModel> playerCardActions,
            CombatPromptRequest? selectionPrompt,
            CardEffectKind? pendingCardEffectKind,
            IReadOnlyList<CardEffectChoiceViewModel> cardEffectChoices,
            string lastCardEffect,
            bool isResolvingCardEffect,
            AutomaticCardInteractionViewModel automaticCardInteraction,
            AutomaticCardResultPromptRequest? automaticCardResult,
            bool isResolvingAutomaticCardEffect,
            DemonContractPanelViewModel demonContract,
            bool canRestart)
        {
            State = state;
            Outcome = outcome;
            RoundNumber = roundNumber;
            TurnNumber = turnNumber;
            PlayerSoul = playerSoul;
            EnemySoul = enemySoul;
            PlayerCards = playerCards;
            EnemyCards = enemyCards;
            PlayerTotal = playerTotal;
            PlayerVisibleTotal = playerVisibleTotal;
            EnemyVisibleTotal = enemyVisibleTotal;
            PlayerTotalsText = $"총합 : {playerTotal}\n공개 카드 합 : {playerVisibleTotal}";
            EnemyVisibleTotalText = $"공개 카드 합 : {enemyVisibleTotal}";
            PlayerDeck = playerDeck;
            EnemyDeck = enemyDeck;
            EnemyDisplayName = enemyDisplayName ?? string.Empty;
            EnemyGrade = enemyGrade ?? string.Empty;
            EnemySummary = enemySummary ?? string.Empty;
            EnemyInformationTitle = enemyInformationTitle ?? string.Empty;
            EnemyInformationLines = enemyInformationLines ??
                throw new ArgumentNullException(nameof(enemyInformationLines));
            EnemyWarning = enemyWarning ?? string.Empty;
            LastRound = lastRound;
            ChangeActionText = changeActionText;
            ChangeCandidates = changeCandidates ??
                throw new ArgumentNullException(nameof(changeCandidates));
            CanHit = canHit;
            CanStand = canStand;
            CanChange = canChange;
            IsChoosingChangeCard = isChoosingChangeCard;
            PlayerCardActions = playerCardActions ??
                throw new ArgumentNullException(nameof(playerCardActions));
            SelectionPrompt = selectionPrompt;
            PendingCardEffectKind = pendingCardEffectKind;
            CardEffectChoices = cardEffectChoices ??
                throw new ArgumentNullException(nameof(cardEffectChoices));
            LastCardEffect = lastCardEffect ?? string.Empty;
            IsResolvingCardEffect = isResolvingCardEffect;
            AutomaticCardInteraction = automaticCardInteraction;
            AutomaticCardResult = automaticCardResult;
            IsResolvingAutomaticCardEffect =
                isResolvingAutomaticCardEffect;
            DemonContract = demonContract ??
                throw new ArgumentNullException(nameof(demonContract));
            CanRestart = canRestart;
        }

        public CoreLoopState State { get; }

        public BattleOutcome Outcome { get; }

        public int RoundNumber { get; }

        public int TurnNumber { get; }

        public string PlayerSoul { get; }

        public string EnemySoul { get; }

        public string PlayerCards { get; }

        public string EnemyCards { get; }

        public int PlayerTotal { get; }

        public int PlayerVisibleTotal { get; }

        public int EnemyVisibleTotal { get; }

        public string PlayerTotalsText { get; }

        public string EnemyVisibleTotalText { get; }

        public string PlayerDeck { get; }

        public string EnemyDeck { get; }

        public string EnemyDisplayName { get; }

        public string EnemyGrade { get; }

        public string EnemyInformationTitle { get; }

        public IReadOnlyList<string> EnemyInformationLines { get; }

        public string EnemySummary { get; }

        public string EnemyWarning { get; }

        public string LastRound { get; }

        public string ChangeActionText { get; }

        public IReadOnlyList<string> ChangeCandidates { get; }

        public bool CanHit { get; }

        public bool CanStand { get; }

        public bool CanChange { get; }

        public bool IsChoosingChangeCard { get; }

        public IReadOnlyList<PlayerCardViewModel> PlayerCardActions { get; }

        public CombatPromptRequest? SelectionPrompt { get; }

        public CardEffectKind? PendingCardEffectKind { get; }

        public IReadOnlyList<CardEffectChoiceViewModel> CardEffectChoices { get; }

        public string LastCardEffect { get; }

        public bool IsResolvingCardEffect { get; }

        public AutomaticCardInteractionViewModel AutomaticCardInteraction
        {
            get;
        }

        public AutomaticCardResultPromptRequest? AutomaticCardResult { get; }

        public bool IsResolvingAutomaticCardEffect { get; }

        public DemonContractPanelViewModel DemonContract { get; }

        public bool CanRestart { get; }
    }

    public static class CoreLoopPresenter
    {
        public static CoreLoopViewModel Create(
            CoreLoopBattle battle,
            string profileKey = null)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            bool canPlayerAct = battle.CanPlayerAct;
            EnemyCombatDisplaySnapshot enemyDisplay =
                EnemyCombatDisplaySnapshotFactory.Create(battle, profileKey);
            AutomaticCardInteractionViewModel automaticInteraction =
                FormatAutomaticCardInteraction(
                    battle.PendingPlayerAutomaticInteraction);
            DemonContractPanelViewModel demonContract =
                DemonContractPresenter.Create(battle);
            return new CoreLoopViewModel(
                battle.State,
                battle.Outcome,
                battle.RoundNumber,
                battle.TurnNumber,
                FormatSoul(battle.Player.Soul),
                FormatSoul(battle.Enemy.Soul),
                FormatCards(battle.Player.Hand.Cards, revealAll: true),
                FormatCards(battle.Enemy.Hand.Cards, revealAll: false),
                battle.Player.KnownHandValue.Total,
                battle.Player.VisibleHandValue.Total,
                battle.Enemy.VisibleHandValue.Total,
                FormatDeck(battle.Player.Deck),
                FormatDeck(battle.Enemy.Deck),
                enemyDisplay.DisplayName,
                FormatEnemyGrade(enemyDisplay),
                enemyDisplay.Summary,
                FormatEnemyInformationTitle(enemyDisplay),
                FormatEnemyInformationLines(enemyDisplay),
                FormatEnemyWarning(enemyDisplay),
                FormatLastRound(
                    battle.LastResolution,
                    battle.LastRoundTransition),
                FormatChangeAction(battle),
                FormatChangeCandidates(battle.PlayerChangeCandidates),
                canPlayerAct,
                battle.CanPlayerStand,
                battle.CanBeginPlayerChange,
                battle.CanSelectChangedCard,
                FormatPlayerCardActions(battle),
                CreateSelectionPrompt(
                    battle,
                    automaticInteraction,
                    demonContract),
                battle.PendingPlayerCardEffect?.EffectKind,
                FormatCardEffectChoices(battle.PendingPlayerCardEffect),
                FormatLastCardEffect(battle.LastCardEffectResult),
                battle.State == CoreLoopState.PlayerResolvingCardEffect,
                automaticInteraction,
                battle.AutomaticCardResultPrompt,
                battle.State ==
                    CoreLoopState.ResolvingAutomaticCardEffect,
                demonContract,
                battle.State == CoreLoopState.BattleEnded);
        }

        private static string FormatEnemyGrade(
            EnemyCombatDisplaySnapshot snapshot)
        {
            return snapshot.Grade.HasValue
                ? snapshot.Grade.Value.ToString().ToUpperInvariant()
                : "UNPROFILED";
        }

        private static string FormatEnemyInformationTitle(
            EnemyCombatDisplaySnapshot snapshot)
        {
            if (!snapshot.Grade.HasValue)
            {
                return "ENEMY INFORMATION";
            }

            switch (snapshot.Grade.Value)
            {
                case EnemyGrade.Normal:
                    return "INFERENCE";
                case EnemyGrade.Elite:
                    return "ELITE INFERENCE";
                case EnemyGrade.Boss:
                    return "BOSS PATTERN";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IReadOnlyList<string> FormatEnemyInformationLines(
            EnemyCombatDisplaySnapshot snapshot)
        {
            var lines = new List<string>();
            if (!snapshot.HasProfile)
            {
                lines.Add("NO PROFILE INFORMATION");
                return lines.AsReadOnly();
            }

            switch (snapshot.Grade.Value)
            {
                case EnemyGrade.Normal:
                    foreach (EnemyInferenceDisplayEntry entry in
                        snapshot.InferenceEntries)
                    {
                        lines.Add($"{entry.Number}  {entry.ProbabilityPercent.Value}%");
                    }

                    if (lines.Count == 0)
                    {
                        lines.Add("NO PUBLIC INFERENCE");
                    }

                    break;
                case EnemyGrade.Elite:
                    lines.Add(FormatLikelyNumbers(snapshot.InferenceEntries));
                    lines.Add($"CONFIDENCE {snapshot.Confidence.Value.ToString().ToUpperInvariant()}");
                    break;
                case EnemyGrade.Boss:
                    lines.Add($"PHASE {FormatBossPhase(snapshot.BossPhase.Value)}");
                    lines.Add($"DIRECTION {FormatBossDirection(snapshot.BossInferenceDirection.Value)}");
                    lines.Add($"CONFIDENCE {snapshot.Confidence.Value.ToString().ToUpperInvariant()}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return lines.AsReadOnly();
        }

        private static string FormatLikelyNumbers(
            IReadOnlyList<EnemyInferenceDisplayEntry> entries)
        {
            if (entries.Count == 0)
            {
                return "LIKELY UNKNOWN";
            }

            var builder = new StringBuilder("LIKELY ");
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" · ");
                }

                builder.Append(entries[i].Number);
            }

            return builder.ToString();
        }

        private static string FormatEnemyWarning(
            EnemyCombatDisplaySnapshot snapshot)
        {
            if (!snapshot.BossTelegraphedAction.HasValue)
            {
                return string.Empty;
            }

            switch (snapshot.BossTelegraphedAction.Value)
            {
                case BossTelegraphedAction.None:
                    return string.Empty;
                case BossTelegraphedAction.NumberGuess:
                    return "WARNING · NUMBER GUESS PREPARED";
                case BossTelegraphedAction.ForcedDraw:
                    return "WARNING · FORCED DRAW PREPARED";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string FormatBossPhase(FinalBossPhase phase)
        {
            return phase.ToString().ToUpperInvariant();
        }

        private static string FormatBossDirection(BossInferenceDirection direction)
        {
            switch (direction)
            {
                case BossInferenceDirection.Unknown:
                    return "UNKNOWN";
                case BossInferenceDirection.LowNumbers:
                    return "LOW NUMBERS";
                case BossInferenceDirection.Balanced:
                    return "BALANCED";
                case BossInferenceDirection.HighNumbers:
                    return "HIGH NUMBERS";
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction));
            }
        }

        private static IReadOnlyList<PlayerCardViewModel> FormatPlayerCardActions(
            CoreLoopBattle battle)
        {
            IReadOnlyList<CardUseAvailability> availability =
                battle.PlayerCardUseAvailability;
            var availabilityByCardId = new Dictionary<int, CardUseAvailability>(
                availability.Count);
            foreach (CardUseAvailability item in availability)
            {
                availabilityByCardId.Add(item.CardId, item);
            }

            var cards = new List<PlayerCardViewModel>(battle.Player.Hand.Count);
            foreach (BlackjackCard card in battle.Player.Hand.Cards)
            {
                CardUseAvailability item = availabilityByCardId[card.Id];
                cards.Add(new PlayerCardViewModel(
                    card.Id,
                    card.Rank,
                    card.Definition.DisplayName,
                    card.IsFaceUp,
                    card.UseState,
                    item.CanUse,
                    item.Reason,
                    FormatCardDisabledReason(card, item)));
            }

            return cards.AsReadOnly();
        }

        private static IReadOnlyList<CardEffectChoiceViewModel> FormatCardEffectChoices(
            PendingCardEffect pendingEffect)
        {
            if (pendingEffect == null)
            {
                return Array.AsReadOnly(Array.Empty<CardEffectChoiceViewModel>());
            }

            var choices = new List<CardEffectChoiceViewModel>(pendingEffect.Options.Count);
            foreach (CardEffectChoiceOption option in pendingEffect.Options)
            {
                choices.Add(new CardEffectChoiceViewModel(
                    option.Id,
                    option.Label,
                    option.CardId));
            }

            return choices.AsReadOnly();
        }

        private static AutomaticCardInteractionViewModel
            FormatAutomaticCardInteraction(
                PendingAutomaticCardInteraction interaction)
        {
            if (interaction == null)
            {
                return null;
            }

            var choices = new List<AutomaticCardChoiceViewModel>(
                interaction.Options.Count);
            foreach (AutomaticCardChoiceOption option in interaction.Options)
            {
                choices.Add(new AutomaticCardChoiceViewModel(
                    option.OptionId,
                    option.Label,
                    option.CardId));
            }

            return new AutomaticCardInteractionViewModel(
                interaction.InteractionId,
                interaction.SourceCardId,
                FormatEffectName(interaction.EffectKind),
                interaction.EffectKind,
                interaction.ChoiceKind,
                choices.AsReadOnly());
        }

        private static CombatPromptRequest? CreateSelectionPrompt(
            CoreLoopBattle battle,
            AutomaticCardInteractionViewModel automaticInteraction,
            DemonContractPanelViewModel demonContract)
        {
            if (battle.CanSelectChangedCard)
            {
                return new CombatPromptRequest(CombatPromptId.ChangeCard);
            }

            PendingAutomaticCardInteraction automatic =
                battle.PendingPlayerAutomaticInteraction;
            if (automatic != null)
            {
                return new CombatPromptRequest(
                    automatic.PromptId,
                    automaticInteraction?.SourceDisplayName);
            }

            PendingCardEffect manual = battle.PendingPlayerCardEffect;
            if (manual != null)
            {
                return new CombatPromptRequest(
                    manual.PromptId,
                    FormatEffectName(manual.EffectKind));
            }

            PendingDemonContractInteraction demon =
                battle.PendingPlayerDemonContractInteraction;
            if (demon == null)
            {
                return null;
            }

            int currentCount = 0;
            int requiredCount = 0;
            switch (demon.Kind)
            {
                case DemonContractInteractionKind.SatanDeclareFirstNumber:
                    requiredCount = 2;
                    break;
                case DemonContractInteractionKind.SatanDeclareSecondNumber:
                    currentCount = 1;
                    requiredCount = 2;
                    break;
                case DemonContractInteractionKind.BeelzebubChooseOwnerCard:
                    currentCount = 1;
                    requiredCount = 2;
                    break;
                case DemonContractInteractionKind.BeelzebubChooseOpponentCard:
                    currentCount = 2;
                    requiredCount = 2;
                    break;
            }

            return new CombatPromptRequest(
                demon.PromptId,
                demon.ContractKind?.ToString(),
                demonContract?.OwnerPreview,
                currentCount,
                requiredCount);
        }

        private static string FormatCardDisabledReason(
            BlackjackCard card,
            CardUseAvailability availability)
        {
            switch (availability.Reason)
            {
                case CardUseUnavailableReason.None:
                    return string.Empty;
                case CardUseUnavailableReason.EffectInProgress:
                    return "EFFECT IN PROGRESS";
                case CardUseUnavailableReason.NotPlayerTurn:
                    return "WAIT FOR PLAYER TURN";
                case CardUseUnavailableReason.CardNotInHand:
                    return "CARD NOT IN HAND";
                case CardUseUnavailableReason.CardIsNotManual:
                    return "NO MANUAL EFFECT";
                case CardUseUnavailableReason.CardIsUnavailable:
                    return card.UseState.ToString().ToUpperInvariant();
                case CardUseUnavailableReason.EffectNotImplemented:
                    return "EFFECT NOT IMPLEMENTED";
                case CardUseUnavailableReason.EffectRequirementsNotMet:
                    return "REQUIREMENTS NOT MET";
                case CardUseUnavailableReason.DemonContractRestricted:
                    return "BLOCKED BY DEMON CONTRACT";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string FormatLastCardEffect(CardEffectResult? result)
        {
            if (!result.HasValue)
            {
                return "No card effect yet";
            }

            CardEffectResult value = result.Value;
            string outcome = value.Succeeded ? "SUCCESS" : "FAILED";
            string continuation = value.EndedRound ? "ROUND ENDED" : "ENEMY TURN";
            return $"{FormatEffectName(value.EffectKind)}  |  {outcome}  |  {continuation}";
        }

        internal static string FormatEffectName(CardEffectKind effectKind)
        {
            switch (effectKind)
            {
                case CardEffectKind.CrystalOrb:
                    return "CRYSTAL ORB";
                case CardEffectKind.ThreatHammer:
                    return "THREAT HAMMER";
                case CardEffectKind.AutoPistol:
                    return "REVOLVER";
                case CardEffectKind.MilitaryKnife:
                    return "BOWIE KNIFE";
                case CardEffectKind.Poison:
                    return "POISON";
                case CardEffectKind.ResurrectionHerb:
                    return "RESURRECTION HERB";
                case CardEffectKind.LieDetector:
                    return "LIE DETECTOR";
                case CardEffectKind.Flamethrower:
                    return "FLAMETHROWER";
                case CardEffectKind.PocketWatch:
                    return "POCKET WATCH";
                default:
                    throw new ArgumentOutOfRangeException(nameof(effectKind));
            }
        }

        private static string FormatChangeAction(CoreLoopBattle battle)
        {
            int cost = battle.NextPlayerChangeSoulCost;
            int remainingSoul = battle.Player.Soul.Current - cost;
            return remainingSoul > 0
                ? cost == 0
                    ? $"CHANGE (FREE | {remainingSoul} SOUL LEFT)"
                    : $"CHANGE (-{cost} SOUL | {remainingSoul} LEFT)"
                : $"CHANGE (-{cost} SOUL | NEED {cost + 1}+)";
        }

        private static IReadOnlyList<string> FormatChangeCandidates(
            IReadOnlyList<BlackjackCard> candidates)
        {
            var labels = new string[candidates.Count];
            for (int i = 0; i < candidates.Count; i++)
            {
                labels[i] = candidates[i].Rank.ToString();
            }

            return Array.AsReadOnly(labels);
        }

        private static string FormatSoul(SoulPool soul)
        {
            return $"{soul.Current} / {soul.Maximum}";
        }

        private static string FormatDeck(BlackjackDeck deck)
        {
            return $"Draw {deck.DrawCount}  |  Discard {deck.DiscardCount}";
        }

        private static string FormatCards(IReadOnlyList<BlackjackCard> cards, bool revealAll)
        {
            if (cards.Count == 0)
            {
                return "-";
            }

            var builder = new StringBuilder();
            for (int i = 0; i < cards.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  ");
                }

                BlackjackCard card = cards[i];
                builder.Append(revealAll || card.IsFaceUp ? card.Rank.ToString() : "?");
            }

            return builder.ToString();
        }

        private static string FormatLastRound(
            RoundResolution? resolution,
            RoundTransition? transition)
        {
            if (transition.HasValue)
            {
                RoundTransition value = transition.Value;
                return value.Cause == RoundTransitionCause.ResurrectionHerb
                    ? $"Round {value.PreviousRoundNumber} restarted  |  No damage"
                    : "Round restarted";
            }

            if (!resolution.HasValue)
            {
                return "No round result yet";
            }

            switch (resolution.Value.Outcome)
            {
                case RoundOutcome.PlayerBust:
                    return "Player bust  |  Player soul -2";
                case RoundOutcome.EnemyBust:
                    return "Enemy bust  |  Enemy soul -2";
                case RoundOutcome.PlayerTwentyOneWin:
                    return "Player 21  |  Enemy soul -1";
                case RoundOutcome.PlayerWin:
                    return "Player wins round  |  Enemy soul -1";
                case RoundOutcome.EnemyWin:
                    return "Enemy wins round  |  Player soul -1";
                case RoundOutcome.MutualLoss:
                    return "Mutual loss  |  Both soul -1";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
