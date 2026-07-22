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
            string abilityDescription = "")
        {
            CardId = cardId;
            Rank = rank;
            IsFaceUp = isFaceUp;
            RevealRank = revealRank;
            CanUse = canUse;
            DisplayName = displayName ?? string.Empty;
            AbilityDescription = abilityDescription ?? string.Empty;
        }

        public int CardId { get; }

        public int Rank { get; }

        public bool IsFaceUp { get; }

        public bool RevealRank { get; }

        /// <summary>
        /// Whether this card's manual effect can be activated right now — drives the diegetic click
        /// on the player's hand. Always false for enemy cards (the player never uses those).
        /// </summary>
        public bool CanUse { get; }

        public string DisplayName { get; }

        /// <summary>
        /// One-line Korean description of the card's manual ability, for the hover badge. Empty for
        /// cards without a usable effect (ranks 1–4) and for enemy cards.
        /// </summary>
        public string AbilityDescription { get; }
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
            CharacterVisualState playerVisual,
            CharacterVisualState enemyVisual,
            string playerActionLabel,
            string enemyActionLabel)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            PlayerCards = playerCards ?? throw new ArgumentNullException(nameof(playerCards));
            EnemyCards = enemyCards ?? throw new ArgumentNullException(nameof(enemyCards));
            PlayerVisual = playerVisual;
            EnemyVisual = enemyVisual;
            PlayerActionLabel = playerActionLabel ?? string.Empty;
            EnemyActionLabel = enemyActionLabel ?? string.Empty;
        }

        public CoreLoopViewModel Core { get; }

        public IReadOnlyList<GameSceneCardViewModel> PlayerCards { get; }

        public IReadOnlyList<GameSceneCardViewModel> EnemyCards { get; }

        public CharacterVisualState PlayerVisual { get; }

        public CharacterVisualState EnemyVisual { get; }

        /// <summary>Short action token shown above the player character this step ("HIT", "USE: REVOLVER", "BUST"…). Empty = no label.</summary>
        public string PlayerActionLabel { get; }

        public string EnemyActionLabel { get; }
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
            (CharacterVisualState playerVisual, string playerLabel) =
                ResolveSide(battle, CombatantSide.Player);
            (CharacterVisualState enemyVisual, string enemyLabel) =
                ResolveSide(battle, CombatantSide.Enemy);
            return new GameSceneViewModel(
                core,
                CreatePlayerCards(core, battle),
                CreateEnemyCards(battle),
                playerVisual,
                enemyVisual,
                playerLabel,
                enemyLabel);
        }

        // MVP presentation: derive one coarse visual + short action label per side from public
        // battle state only. Priority: battle end > round resolution > this side's last action >
        // resting. Bust is transient (the hand clears the instant a round resolves), so round
        // results are read from the surviving LastResolution rather than a live hand value.
        private static (CharacterVisualState Visual, string Label) ResolveSide(
            CoreLoopBattle battle,
            CombatantSide side)
        {
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
            if (TryResolveCardEffect(battle, side, out (CharacterVisualState Visual, string Label) effect))
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
                result = (CharacterVisualState.UseCard, label);
                return true;
            }

            if (side == actor)
            {
                result = (CharacterVisualState.UseCard, "USE: " + CoreLoopPresenter.FormatEffectName(kind));
                return true;
            }

            return false;
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

        // One-line Korean ability text per manual effect, for the hover badge. There is no such text
        // anywhere in the model, so it is authored here in the view layer.
        private static readonly Dictionary<CardEffectKind, string> AbilityDescriptions =
            new Dictionary<CardEffectKind, string>
            {
                { CardEffectKind.CrystalOrb, "덱 맨 위 2장 훔쳐보고 1장 가져오기" },
                { CardEffectKind.ThreatHammer, "적 공개 카드 1장 제거; 스탠드면 비공개 교체" },
                { CardEffectKind.AutoPistol, "적 비공개 숫자 맞히면 적 즉사" },
                { CardEffectKind.MilitaryKnife, "적에게 공개카드 1장 강제로 뽑게 함" },
            };

        private static IReadOnlyList<GameSceneCardViewModel> CreatePlayerCards(
            CoreLoopViewModel core,
            CoreLoopBattle battle)
        {
            var cards = new List<GameSceneCardViewModel>(core.PlayerCardActions.Count);
            int hiddenCardCount = 0;
            foreach (PlayerCardViewModel card in core.PlayerCardActions)
            {
                // The player sees every one of their own cards, including the face-down one.
                var projectedCard = new GameSceneCardViewModel(
                    card.CardId,
                    card.Rank,
                    card.IsFaceUp,
                    revealRank: true,
                    canUse: card.CanUse,
                    card.DisplayName,
                    abilityDescription: ResolveAbilityDescription(battle, card.CardId));

                // PlayerHand renders index 0 at the player's screen-left edge. Keep hidden cards
                // first in the projection while preserving the original hand order within each group.
                if (card.IsFaceUp)
                {
                    cards.Add(projectedCard);
                }
                else
                {
                    cards.Insert(hiddenCardCount, projectedCard);
                    hiddenCardCount++;
                }
            }

            return cards.AsReadOnly();
        }

        private static string ResolveAbilityDescription(CoreLoopBattle battle, int cardId)
        {
            foreach (BlackjackCard card in battle.Player.Hand.Cards)
            {
                if (card.Id == cardId &&
                    AbilityDescriptions.TryGetValue(card.Definition.Effect, out string description))
                {
                    return description;
                }
            }

            return string.Empty;
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
                var projectedCard = new GameSceneCardViewModel(
                    card.Id,
                    faceUp ? card.Rank : 0,
                    faceUp,
                    revealRank: faceUp,
                    canUse: false,
                    faceUp ? card.Definition.DisplayName : string.Empty);

                // Both sides' hidden cards sit on the screen LEFT (each player's own right, mirrored
                // across the table). The camera mirrors local X, so screen-left = highest index →
                // append the enemy's hidden card last too (face-ups first).
                if (faceUp)
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
