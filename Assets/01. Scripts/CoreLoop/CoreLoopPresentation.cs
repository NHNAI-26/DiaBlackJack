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
        public CardEffectChoiceViewModel(int optionId, string label)
        {
            OptionId = optionId;
            Label = label ?? throw new ArgumentNullException(nameof(label));
        }

        public int OptionId { get; }

        public string Label { get; }
    }

    public sealed class CoreLoopViewModel
    {
        public CoreLoopViewModel(
            CoreLoopState state,
            BattleOutcome outcome,
            int roundNumber,
            string playerSoul,
            string enemySoul,
            string playerCards,
            string enemyCards,
            int playerTotal,
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
            string cardEffectPrompt,
            IReadOnlyList<CardEffectChoiceViewModel> cardEffectChoices,
            string lastCardEffect,
            bool isResolvingCardEffect,
            DemonContractPanelViewModel demonContract,
            bool canRestart)
        {
            State = state;
            Outcome = outcome;
            RoundNumber = roundNumber;
            PlayerSoul = playerSoul;
            EnemySoul = enemySoul;
            PlayerCards = playerCards;
            EnemyCards = enemyCards;
            PlayerTotal = playerTotal;
            EnemyVisibleTotal = enemyVisibleTotal;
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
            CardEffectPrompt = cardEffectPrompt ?? string.Empty;
            CardEffectChoices = cardEffectChoices ??
                throw new ArgumentNullException(nameof(cardEffectChoices));
            LastCardEffect = lastCardEffect ?? string.Empty;
            IsResolvingCardEffect = isResolvingCardEffect;
            DemonContract = demonContract ??
                throw new ArgumentNullException(nameof(demonContract));
            CanRestart = canRestart;
        }

        public CoreLoopState State { get; }

        public BattleOutcome Outcome { get; }

        public int RoundNumber { get; }

        public string PlayerSoul { get; }

        public string EnemySoul { get; }

        public string PlayerCards { get; }

        public string EnemyCards { get; }

        public int PlayerTotal { get; }

        public int EnemyVisibleTotal { get; }

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

        public string CardEffectPrompt { get; }

        public IReadOnlyList<CardEffectChoiceViewModel> CardEffectChoices { get; }

        public string LastCardEffect { get; }

        public bool IsResolvingCardEffect { get; }

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
            return new CoreLoopViewModel(
                battle.State,
                battle.Outcome,
                battle.RoundNumber,
                FormatSoul(battle.Player.Soul),
                FormatSoul(battle.Enemy.Soul),
                FormatCards(battle.Player.Hand.Cards, revealAll: true),
                FormatCards(battle.Enemy.Hand.Cards, revealAll: false),
                battle.Player.HandValue.Total,
                battle.Enemy.VisibleHandValue.Total,
                FormatDeck(battle.Player.Deck),
                FormatDeck(battle.Enemy.Deck),
                enemyDisplay.DisplayName,
                FormatEnemyGrade(enemyDisplay),
                enemyDisplay.Summary,
                FormatEnemyInformationTitle(enemyDisplay),
                FormatEnemyInformationLines(enemyDisplay),
                FormatEnemyWarning(enemyDisplay),
                FormatLastRound(battle.LastResolution),
                FormatChangeAction(battle),
                FormatChangeCandidates(battle.PlayerChangeCandidates),
                canPlayerAct,
                canPlayerAct,
                battle.CanBeginPlayerChange,
                battle.CanSelectChangedCard,
                FormatPlayerCardActions(battle),
                battle.PendingPlayerCardEffect?.Prompt,
                FormatCardEffectChoices(battle.PendingPlayerCardEffect),
                FormatLastCardEffect(battle.LastCardEffectResult),
                battle.State == CoreLoopState.PlayerResolvingCardEffect,
                DemonContractPresenter.Create(battle),
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
                choices.Add(new CardEffectChoiceViewModel(option.Id, option.Label));
            }

            return choices.AsReadOnly();
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

        private static string FormatLastRound(RoundResolution? resolution)
        {
            if (!resolution.HasValue)
            {
                return "No round result yet";
            }

            switch (resolution.Value.Outcome)
            {
                case RoundOutcome.PlayerBust:
                    return "Player bust  |  Player soul -2";
                case RoundOutcome.EnemyBust:
                    return "Enemy bust  |  Enemy soul -1";
                case RoundOutcome.PlayerTwentyOneWin:
                    return "Player 21  |  Enemy soul -2";
                case RoundOutcome.PlayerWin:
                    return "Player wins round  |  Enemy soul -1";
                case RoundOutcome.EnemyWin:
                    return "Enemy wins round  |  Player soul -1";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
