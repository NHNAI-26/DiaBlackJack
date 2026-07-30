using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;
using DiaBlackJack.CoreLoop.UI;

namespace DiaBlackJack.GameScene
{
    /// <summary>
    /// A coarse per-side visual state for the world-space character sprite. MVP stand-in for the
    /// eventual per-action animations: the view maps each value to a small tint/scale change so a
    /// hit/stand/bust/win/loss is visible at a glance. Derived from public battle state only.
    /// </summary>
    public enum CharacterVisualState
    {
        Idle,
        Active,
        Stand,
        Bust,
        Win,
        Lose,
        UseCard,
        AttackThreatened,
        Attacked,
    }

    /// <summary>
    /// A single card projected for world-space rendering. <see cref="IsFaceUp"/> is the *physical*
    /// orientation (drives the card back visual). <see cref="RevealRank"/> is whether the rank may be
    /// shown to the viewer: true for all of the player's own cards (a player sees their own hidden
    /// card), but for the enemy only when the card is face-up. When <see cref="RevealRank"/> is false
    /// the <see cref="Rank"/> is forced to 0 — the hidden enemy rank never crosses into the view.
    /// </summary>
    public sealed class GameSceneCardViewModel
    {
        public GameSceneCardViewModel(
            int cardId,
            int rank,
            bool isFaceUp,
            bool revealRank,
            bool canUse,
            string displayName,
            string abilityDescription = "",
            CardSuit suit = CardSuit.Spade,
            bool showHoverBadgeWhenUnavailable = false,
            string definitionKey = "",
            bool showHoverBadgeBelow = false)
        {
            CardId = cardId;
            Rank = rank;
            Suit = suit;
            IsFaceUp = isFaceUp;
            RevealRank = revealRank;
            CanUse = canUse;
            DisplayName = displayName ?? string.Empty;
            AbilityDescription = abilityDescription ?? string.Empty;
            ShowHoverBadgeWhenUnavailable = showHoverBadgeWhenUnavailable;
            DefinitionKey = definitionKey ?? string.Empty;
            ShowHoverBadgeBelow = showHoverBadgeBelow;
        }

        public int CardId { get; }

        public int Rank { get; }

        public CardSuit Suit { get; }

        public bool IsFaceUp { get; }

        public bool RevealRank { get; }

        /// <summary>
        /// Whether this card's manual effect can be activated right now — drives the diegetic click
        /// on the player's hand. Always false for enemy cards (the player never uses those).
        /// </summary>
        public bool CanUse { get; }

        public string DisplayName { get; }

        /// <summary>
        /// One-line Korean description of the card's effect, for the hover badge. Empty for cards
        /// without an effect and for enemy cards whose rank is not public.
        /// </summary>
        public string AbilityDescription { get; }

        public bool ShowHoverBadgeWhenUnavailable { get; }

        /// <summary>
        /// Whether the hover tooltip extends below this card. Enemy cards use this so their public
        /// information stays inside the screen instead of being clipped above the table.
        /// </summary>
        public bool ShowHoverBadgeBelow { get; }

        /// <summary>
        /// Stable card archetype key used only to select authored visuals. It remains empty for an
        /// unrevealed enemy card so the presentation boundary does not leak hidden information.
        /// </summary>
        public string DefinitionKey { get; }
    }

    /// <summary>
    /// Immutable projection for inspecting one player deck pile in the GameScene. Cards are already
    /// in display order and never reveal the next physical draw order.
    /// </summary>
    public sealed class GameSceneDeckViewModel
    {
        public GameSceneDeckViewModel(
            DeckKind kind,
            string title,
            IReadOnlyList<GameSceneCardViewModel> cards)
        {
            Kind = kind;
            Title = title ?? string.Empty;
            Cards = cards ?? throw new ArgumentNullException(nameof(cards));
        }

        public DeckKind Kind { get; }

        public string Title { get; }

        public IReadOnlyList<GameSceneCardViewModel> Cards { get; }

        public int CardCount => Cards.Count;
    }

    public enum GameSceneRevolverAnimationPhase
    {
        Ready,
        ResolvedWithRetry,
        Resolved,
    }

    public sealed class GameSceneRevolverAnimationCue
    {
        public GameSceneRevolverAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            bool succeeded)
            : this(
                roundNumber,
                sourceCardId,
                actorSide,
                GameSceneRevolverAnimationPhase.Resolved,
                succeeded)
        {
        }

        public GameSceneRevolverAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            GameSceneRevolverAnimationPhase phase,
            bool succeeded = false)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            if (!Enum.IsDefined(typeof(GameSceneRevolverAnimationPhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            Succeeded = succeeded;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public GameSceneRevolverAnimationPhase Phase { get; }

        public bool Succeeded { get; }
    }

    public enum GameSceneHammerAnimationPhase
    {
        Ready,
        Smash,
    }

    public sealed class GameSceneHammerAnimationCue
    {
        public GameSceneHammerAnimationCue(
            int roundNumber,
            int sourceCardId,
            CombatantSide actorSide,
            GameSceneHammerAnimationPhase phase,
            int? targetCardId = null)
        {
            if (roundNumber < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundNumber));
            }

            if (sourceCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceCardId));
            }

            if (!Enum.IsDefined(typeof(CombatantSide), actorSide))
            {
                throw new ArgumentOutOfRangeException(nameof(actorSide));
            }

            if (!Enum.IsDefined(typeof(GameSceneHammerAnimationPhase), phase))
            {
                throw new ArgumentOutOfRangeException(nameof(phase));
            }

            if (targetCardId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetCardId));
            }

            RoundNumber = roundNumber;
            SourceCardId = sourceCardId;
            ActorSide = actorSide;
            Phase = phase;
            TargetCardId = targetCardId;
        }

        public int RoundNumber { get; }

        public int SourceCardId { get; }

        public CombatantSide ActorSide { get; }

        public GameSceneHammerAnimationPhase Phase { get; }

        public int? TargetCardId { get; }
    }

    /// <summary>
    /// Read-only projection consumed by <c>GameSceneView</c>. Wraps the shared
    /// <see cref="CoreLoopViewModel"/> (souls, totals, state, player card actions) and adds
    /// world-render-friendly card lists for both sides.
    /// </summary>
    public sealed class GameSceneViewModel
    {
        public GameSceneViewModel(
            CoreLoopViewModel core,
            IReadOnlyList<GameSceneCardViewModel> playerCards,
            IReadOnlyList<GameSceneCardViewModel> enemyCards,
            CharacterVisualState enemyVisual,
            string enemyActionLabel,
            GameSceneRevolverAnimationCue revolverAnimationCue = null,
            GameSceneHammerAnimationCue hammerAnimationCue = null)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            PlayerCards = playerCards ?? throw new ArgumentNullException(nameof(playerCards));
            EnemyCards = enemyCards ?? throw new ArgumentNullException(nameof(enemyCards));
            EnemyVisual = enemyVisual;
            EnemyActionLabel = enemyActionLabel ?? string.Empty;
            RevolverAnimationCue = revolverAnimationCue;
            HammerAnimationCue = hammerAnimationCue;
        }

        public CoreLoopViewModel Core { get; }

        public IReadOnlyList<GameSceneCardViewModel> PlayerCards { get; }

        public IReadOnlyList<GameSceneCardViewModel> EnemyCards { get; }

        public CharacterVisualState EnemyVisual { get; }

        /// <summary>Short action token shown above the enemy character. Empty = no label.</summary>
        public string EnemyActionLabel { get; }

        public GameSceneRevolverAnimationCue RevolverAnimationCue { get; }

        public GameSceneHammerAnimationCue HammerAnimationCue { get; }
    }

    public static class GameScenePresenter
    {
        public static GameSceneViewModel Create(CoreLoopBattle battle, string profileKey = null)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            CoreLoopViewModel core = CoreLoopPresenter.Create(battle, profileKey);
            (CharacterVisualState enemyVisual, string enemyLabel) =
                ResolveSide(battle, CombatantSide.Enemy);
            return new GameSceneViewModel(
                core,
                CreatePlayerCards(core, battle),
                CreateEnemyCards(battle),
                enemyVisual,
                enemyLabel,
                CreateRevolverAnimationCue(battle),
                CreateHammerAnimationCue(battle));
        }

        /// <summary>
        /// Projects one of the player's available card piles for inspection. The source deck returns
        /// immutable, non-draw-ordered snapshots, so this view cannot disclose the next card.
        /// </summary>
        public static GameSceneDeckViewModel CreateDeckPreview(
            CoreLoopBattle battle,
            DeckKind kind)
        {
            if (battle == null)
            {
                throw new ArgumentNullException(nameof(battle));
            }

            IReadOnlyList<DeckCardDisplaySnapshot> snapshots = kind == DeckKind.Draw
                ? battle.Player.Deck.GetDrawPileDisplayCards()
                : battle.Player.Deck.GetDiscardPileDisplayCards();
            var cards = new List<GameSceneCardViewModel>(snapshots.Count);
            foreach (DeckCardDisplaySnapshot card in snapshots)
            {
                cards.Add(new GameSceneCardViewModel(
                    card.Id,
                    card.Rank,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    card.DisplayName,
                    abilityDescription: CardDefinitionCatalog.GetByKey(card.DefinitionKey).Description,
                    suit: card.Suit,
                    showHoverBadgeWhenUnavailable: true,
                    definitionKey: card.DefinitionKey));
            }

            string title = kind == DeckKind.Draw ? "뽑을 카드" : "버린 카드";
            return new GameSceneDeckViewModel(kind, title, cards.AsReadOnly());
        }

        private static GameSceneHammerAnimationCue CreateHammerAnimationCue(
            CoreLoopBattle battle)
        {
            PendingCardEffect pendingPlayerEffect = battle.PendingPlayerCardEffect;
            if (pendingPlayerEffect != null &&
                pendingPlayerEffect.EffectKind == CardEffectKind.ThreatHammer)
            {
                return new GameSceneHammerAnimationCue(
                    battle.RoundNumber,
                    pendingPlayerEffect.SourceCardId,
                    CombatantSide.Player,
                    GameSceneHammerAnimationPhase.Ready);
            }

            if (battle.PendingEnemyCardEffect != null ||
                !battle.LastCardEffectResult.HasValue ||
                !battle.LastCardEffectActorSide.HasValue)
            {
                return null;
            }

            CardEffectResult result = battle.LastCardEffectResult.Value;
            if (result.EffectKind != CardEffectKind.ThreatHammer ||
                !result.TargetCardId.HasValue ||
                !IsLastUseCardEffect(battle, result.EffectKind))
            {
                return null;
            }

            return new GameSceneHammerAnimationCue(
                battle.RoundNumber,
                result.SourceCardId,
                battle.LastCardEffectActorSide.Value,
                GameSceneHammerAnimationPhase.Smash,
                result.TargetCardId);
        }

        private static GameSceneRevolverAnimationCue CreateRevolverAnimationCue(
            CoreLoopBattle battle)
        {
            if (battle.HasPendingLeviathanAutoPistolRetry &&
                battle.LastCardEffectResult.HasValue &&
                battle.LastCardEffectActorSide.HasValue)
            {
                CardEffectResult retryResult = battle.LastCardEffectResult.Value;
                return new GameSceneRevolverAnimationCue(
                    battle.RoundNumber,
                    retryResult.SourceCardId,
                    battle.LastCardEffectActorSide.Value,
                    GameSceneRevolverAnimationPhase.ResolvedWithRetry,
                    retryResult.Succeeded);
            }

            PendingCardEffect pendingPlayerEffect = battle.PendingPlayerCardEffect;
            if (pendingPlayerEffect != null &&
                pendingPlayerEffect.EffectKind == CardEffectKind.AutoPistol)
            {
                return new GameSceneRevolverAnimationCue(
                    battle.RoundNumber,
                    pendingPlayerEffect.SourceCardId,
                    CombatantSide.Player,
                    GameSceneRevolverAnimationPhase.Ready);
            }

            if (battle.PendingEnemyCardEffect != null ||
                !battle.LastCardEffectResult.HasValue ||
                !battle.LastCardEffectActorSide.HasValue)
            {
                return null;
            }

            CardEffectResult result = battle.LastCardEffectResult.Value;
            if (result.EffectKind != CardEffectKind.AutoPistol ||
                !IsLastUseCardEffect(battle, result.EffectKind))
            {
                return null;
            }

            return new GameSceneRevolverAnimationCue(
                battle.RoundNumber,
                result.SourceCardId,
                battle.LastCardEffectActorSide.Value,
                result.Succeeded);
        }

        private static bool IsLastUseCardEffect(
            CoreLoopBattle battle,
            CardEffectKind effectKind)
        {
            PublicCombatAction last = battle.LastPublicAction;
            if (last == null ||
                last.ActionType != PublicCombatActionType.UseCard ||
                string.IsNullOrWhiteSpace(last.SourceCardDefinitionKey))
            {
                return false;
            }

            return CardDefinitionCatalog
                .GetByKey(last.SourceCardDefinitionKey)
                .Effect == effectKind;
        }

        // MVP presentation: derive one coarse visual + short action label per side from public
        // battle state only. Priority: incoming attack reaction > battle end > round resolution >
        // other card effects > this side's last action > resting. Bust is transient (the hand
        // clears the instant a round resolves), so round results are read from the surviving
        // LastResolution rather than a live hand value.
        private static (CharacterVisualState Visual, string Label) ResolveSide(
            CoreLoopBattle battle,
            CombatantSide side)
        {
            bool hasCardEffect = TryResolveCardEffect(
                battle,
                side,
                out (CharacterVisualState Visual, string Label) effect);
            if (hasCardEffect && IsAttackReaction(effect.Visual))
            {
                return effect;
            }

            if (battle.Outcome != BattleOutcome.InProgress)
            {
                bool won =
                    (side == CombatantSide.Player && battle.Outcome == BattleOutcome.PlayerVictory) ||
                    (side == CombatantSide.Enemy && battle.Outcome == BattleOutcome.PlayerDefeat);
                return won
                    ? (CharacterVisualState.Win, "WIN")
                    : (CharacterVisualState.Lose, "LOSE");
            }

            if (battle.State == CoreLoopState.ResolvingRound && battle.LastResolution.HasValue)
            {
                return ResolveRoundResult(battle.LastResolution.Value.Outcome, side);
            }

            // A card effect surfaces on the actor ("USE: <name>") and on the character it lands on
            // ("GUESS" / "DRAW" / "DISCARD") — during the choosing (pending) phase and the use beat.
            if (hasCardEffect)
            {
                return effect;
            }

            PublicCombatAction last = battle.LastPublicAction;
            if (last != null && last.ActorSide == side)
            {
                switch (last.ActionType)
                {
                    case PublicCombatActionType.Hit:
                        return (CharacterVisualState.Active, "HIT");
                    case PublicCombatActionType.Stand:
                        return (CharacterVisualState.Stand, "STAND");
                    case PublicCombatActionType.Change:
                        return (CharacterVisualState.Active, "CHANGE");
                    case PublicCombatActionType.DemonContract:
                        return (CharacterVisualState.Active, "CONTRACT");
                }
            }

            BattleParticipant self = side == CombatantSide.Player ? battle.Player : battle.Enemy;
            return self.IsStanding
                ? (CharacterVisualState.Stand, "STAND")
                : (CharacterVisualState.Idle, string.Empty);
        }

        // Per-side reaction to a round result: winner "WIN"/"21!", loser "BUST" (busted) or "LOSE".
        private static (CharacterVisualState Visual, string Label) ResolveRoundResult(
            RoundOutcome outcome,
            CombatantSide side)
        {
            switch (outcome)
            {
                case RoundOutcome.PlayerBust:
                    return side == CombatantSide.Player
                        ? (CharacterVisualState.Bust, "BUST")
                        : (CharacterVisualState.Win, "WIN");
                case RoundOutcome.EnemyBust:
                    return side == CombatantSide.Enemy
                        ? (CharacterVisualState.Bust, "BUST")
                        : (CharacterVisualState.Win, "WIN");
                case RoundOutcome.PlayerTwentyOneWin:
                    return side == CombatantSide.Player
                        ? (CharacterVisualState.Win, "21!")
                        : (CharacterVisualState.Lose, "LOSE");
                case RoundOutcome.PlayerWin:
                    return side == CombatantSide.Player
                        ? (CharacterVisualState.Win, "WIN")
                        : (CharacterVisualState.Lose, "LOSE");
                case RoundOutcome.EnemyWin:
                    return side == CombatantSide.Enemy
                        ? (CharacterVisualState.Win, "WIN")
                        : (CharacterVisualState.Lose, "LOSE");
                default:
                    return (CharacterVisualState.Idle, string.Empty);
            }
        }

        // A card effect surfaces on two characters: the ACTOR who played it ("USE: <name>") and the
        // TARGET it lands on. Shown while the player is still choosing (PendingPlayerCardEffect) and on
        // the use beat (LastPublicAction == UseCard). REVOLVER (7,8) guesses the OPPONENT's hidden card;
        // BOWIE KNIFE (9,10) forces the OPPONENT to draw; CRYSTAL ORB (5) draws for SELF; THREAT HAMMER
        // (6) discards an OPPONENT face-up card.
        private static bool TryResolveCardEffect(
            CoreLoopBattle battle,
            CombatantSide side,
            out (CharacterVisualState Visual, string Label) result)
        {
            result = default;

            CardEffectKind kind;
            CombatantSide actor;
            CardEffectResult? completedResult = null;

            PendingCardEffect pending = battle.PendingPlayerCardEffect;
            if (pending != null)
            {
                kind = pending.EffectKind;
                actor = CombatantSide.Player;
            }
            else if (battle.LastPublicAction != null &&
                     battle.LastPublicAction.ActionType == PublicCombatActionType.UseCard &&
                     battle.LastCardEffectResult.HasValue &&
                     battle.LastCardEffectActorSide.HasValue)
            {
                completedResult = battle.LastCardEffectResult.Value;
                kind = completedResult.Value.EffectKind;
                actor = battle.LastCardEffectActorSide.Value;
            }
            else
            {
                return false;
            }

            if (kind == CardEffectKind.None)
            {
                return false;
            }

            CombatantSide target = EffectTargetSide(kind, actor);
            if (side == target)
            {
                // While choosing, show what the effect will do; once resolved, show its outcome —
                // in particular the revolver's hit vs miss, which otherwise has no visible feedback.
                string label = completedResult.HasValue
                    ? EffectResultLabel(kind, completedResult.Value)
                    : EffectActionLabel(kind);
                CharacterVisualState visual = CharacterVisualState.UseCard;
                if (IsAttackEffect(kind))
                {
                    if (!completedResult.HasValue)
                    {
                        visual = CharacterVisualState.AttackThreatened;
                    }
                    else if (completedResult.Value.Succeeded)
                    {
                        visual = CharacterVisualState.Attacked;
                    }
                }

                result = (visual, label);
                return true;
            }

            if (side == actor)
            {
                result = (CharacterVisualState.UseCard, "USE: " + CoreLoopPresenter.FormatEffectName(kind));
                return true;
            }

            return false;
        }

        private static bool IsAttackEffect(CardEffectKind kind)
        {
            switch (kind)
            {
                case CardEffectKind.AutoPistol:
                case CardEffectKind.MilitaryKnife:
                case CardEffectKind.ThreatHammer:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsAttackReaction(CharacterVisualState state)
        {
            return state == CharacterVisualState.AttackThreatened ||
                state == CharacterVisualState.Attacked;
        }

        // The character an effect's visible action lands on. REVOLVER / BOWIE KNIFE / THREAT HAMMER
        // hit the opponent; CRYSTAL ORB acts on the actor's own hand.
        private static CombatantSide EffectTargetSide(CardEffectKind kind, CombatantSide actor)
        {
            switch (kind)
            {
                case CardEffectKind.AutoPistol:
                case CardEffectKind.MilitaryKnife:
                case CardEffectKind.ThreatHammer:
                    return actor == CombatantSide.Player ? CombatantSide.Enemy : CombatantSide.Player;
                default:
                    return actor;
            }
        }

        // Short token for what the effect does to its target character.
        private static string EffectActionLabel(CardEffectKind kind)
        {
            switch (kind)
            {
                case CardEffectKind.AutoPistol:
                    return "GUESS";
                case CardEffectKind.MilitaryKnife:
                    return "DRAW";
                case CardEffectKind.CrystalOrb:
                    return "DRAW";
                case CardEffectKind.ThreatHammer:
                    return "DISCARD";
                default:
                    return string.Empty;
            }
        }

        // Target label once the effect has resolved. The revolver's guess distinguishes hit vs miss;
        // every other effect reads the same as its action label.
        private static string EffectResultLabel(CardEffectKind kind, CardEffectResult result)
        {
            if (kind == CardEffectKind.AutoPistol)
            {
                return result.Succeeded ? "HIT!" : "MISS";
            }

            return EffectActionLabel(kind);
        }

        private static IReadOnlyList<GameSceneCardViewModel> CreatePlayerCards(
            CoreLoopViewModel core,
            CoreLoopBattle battle)
        {
            var cards = new List<GameSceneCardViewModel>(core.PlayerCardActions.Count);
            int hiddenCardCount = 0;
            foreach (PlayerCardViewModel card in core.PlayerCardActions)
            {
                BlackjackCard sourceCard = FindCardById(battle.Player.Hand.Cards, card.CardId);
                bool isHiddenCard = battle.Player.Hand.IsHiddenCard(card.CardId);

                // The player sees every one of their own cards, including the face-down one.
                var projectedCard = new GameSceneCardViewModel(
                    card.CardId,
                    card.Rank,
                    card.IsFaceUp,
                    revealRank: true,
                    canUse: card.CanUse,
                    card.DisplayName,
                    abilityDescription: ResolveAbilityDescription(sourceCard),
                    suit: sourceCard == null ? CardSuit.Spade : sourceCard.Suit,
                    definitionKey: sourceCard?.DefinitionKey);

                // PlayerHand's world orientation makes the highest index land at screen-left.
                // Keep hidden cards last and prepend face-up cards so new draws appear at
                // screen-right from the player's perspective.
                if (!isHiddenCard)
                {
                    cards.Insert(0, projectedCard);
                }
                else
                {
                    cards.Add(projectedCard);
                    hiddenCardCount++;
                }
            }

            return cards.AsReadOnly();
        }

        private static string ResolveAbilityDescription(BlackjackCard card)
        {
            return card?.Definition.Description ?? string.Empty;
        }

        private static BlackjackCard FindCardById(IReadOnlyList<BlackjackCard> cards, int cardId)
        {
            foreach (BlackjackCard card in cards)
            {
                if (card.Id == cardId)
                {
                    return card;
                }
            }

            return null;
        }

        /// <summary>
        /// Composition of the player's <b>draw pile</b> (cards still to draw) for the draw-deck hover
        /// panel — rank×count + total, order not shown. Discarded cards are NOT here (they show in the
        /// discard-deck panel). Rendered in IMGUI, so Korean needs no special TMP font.
        /// </summary>
        public static string FormatDrawDeck(CoreLoopBattle battle)
        {
            if (battle == null)
            {
                return string.Empty;
            }

            return FormatRankCounts(battle.Player.Deck.GetDrawPileRankCounts(), "뽑을 카드");
        }

        /// <summary>
        /// Composition of the player's <b>discard pile</b> (cards discarded this run) for the
        /// discard-deck hover panel. Reshuffled back into the draw pile when it empties.
        /// </summary>
        public static string FormatDiscardDeck(CoreLoopBattle battle)
        {
            if (battle == null)
            {
                return string.Empty;
            }

            return FormatRankCounts(battle.Player.Deck.GetDiscardPileRankCounts(), "버린 카드");
        }

        // rank×count composition, 5 per row, with a "<header>  N장" heading. Shared by both deck panels.
        private static string FormatRankCounts(IReadOnlyList<int> counts, string header)
        {
            var parts = new List<string>();
            int total = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                int count = counts[rank];
                if (count <= 0)
                {
                    continue;
                }

                parts.Add(rank + " x" + count);
                total += count;
            }

            string body;
            if (parts.Count == 0)
            {
                body = "-";
            }
            else
            {
                var lines = new List<string>();
                for (int i = 0; i < parts.Count; i += 5)
                {
                    lines.Add(string.Join("    ", parts.GetRange(i, Math.Min(5, parts.Count - i))));
                }

                body = string.Join("\n", lines);
            }

            return header + "  " + total + "장\n\n" + body;
        }

        private static IReadOnlyList<GameSceneCardViewModel> CreateEnemyCards(CoreLoopBattle battle)
        {
            IReadOnlyList<BlackjackCard> hand = battle.Enemy.Hand.Cards;
            var cards = new List<GameSceneCardViewModel>(hand.Count);
            int hiddenCardCount = 0;
            foreach (BlackjackCard card in hand)
            {
                // Face-down enemy card: emit no rank. This is the information-hiding boundary.
                bool faceUp = card.IsFaceUp;
                bool isHiddenCard = battle.Enemy.Hand.IsHiddenCard(card.Id);
                var projectedCard = new GameSceneCardViewModel(
                    card.Id,
                    faceUp ? card.Rank : 0,
                    faceUp,
                    revealRank: faceUp,
                    canUse: false,
                    faceUp ? card.Definition.DisplayName : string.Empty,
                    abilityDescription: faceUp
                        ? ResolveAbilityDescription(card)
                        : string.Empty,
                    suit: card.Suit,
                    showHoverBadgeWhenUnavailable: faceUp,
                    definitionKey: faceUp ? card.DefinitionKey : string.Empty,
                    showHoverBadgeBelow: faceUp);

                // Both sides' hidden cards sit on the screen LEFT (each player's own right, mirrored
                // across the table). The camera mirrors local X, so screen-left = highest index →
                // append the enemy's hidden card last too (face-ups first).
                if (!isHiddenCard)
                {
                    cards.Insert(cards.Count - hiddenCardCount, projectedCard);
                }
                else
                {
                    cards.Add(projectedCard);
                    hiddenCardCount++;
                }
            }

            return cards.AsReadOnly();
        }
    }
}
