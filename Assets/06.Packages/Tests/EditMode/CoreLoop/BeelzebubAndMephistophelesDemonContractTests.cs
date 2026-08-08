using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.GameScene;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class BeelzebubAndMephistophelesDemonContractTests
    {
        [Test]
        public void DCR04_U01_BeelzebubRestrictsStandThroughThreeFaceUpCards()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 2, 2, 2, 2, 2, 2, 2, 2 }),
                PlainDeck(new[] { 10, 7, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Beelzebub,
                playerCurrentSoul: 12);
            ActivateFirstContract(battle);

            Assert.That(battle.Player.Hand.GetFaceUpCards(), Has.Count.EqualTo(1));
            Assert.That(battle.CanPlayerStand, Is.False);

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.Player.Hand.GetFaceUpCards(), Has.Count.EqualTo(3));
            Assert.That(battle.CanPlayerStand, Is.False);

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.Player.Hand.GetFaceUpCards(), Has.Count.EqualTo(4));
            Assert.That(battle.CanPlayerStand, Is.True);
        }

        [Test]
        public void DCR04_U02_BeelzebubOwnerChoosesBothFaceUpDiscardCards()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 10, 2, 10, 10, 2, 2, 2, 2 }),
                PlainDeck(new[] { 8, 7, 2, 2, 2, 2 }, 100),
                new SequencePolicy(EnemyActionType.Hit, EnemyActionType.Stand),
                DemonContractKind.Beelzebub,
                playerCurrentSoul: 12);
            ActivateFirstContract(battle);

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.State,
                Is.EqualTo(CoreLoopState.PlayerResolvingDemonContract));
            ResolveBeelzebubDiscardChoices(
                battle,
                ownerCardId: 0,
                opponentCardId: 100);

            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.Player.Hand.Contains(0), Is.False);
            Assert.That(battle.Player.Hand.Contains(3), Is.True);
            Assert.That(battle.Enemy.Hand.Contains(100), Is.False);
            Assert.That(battle.Enemy.Hand.Contains(102), Is.True);
            Assert.That(
                battle.Player.Deck.GetDiscardedCards().Select(card => card.Id),
                Does.Contain(0));
            Assert.That(
                battle.Enemy.Deck.GetDiscardedCards().Select(card => card.Id),
                Does.Contain(100));
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost,
                Is.EqualTo(1));
        }

        [Test]
        public void DCR04_U03_BeelzebubSoulCostAtZeroEndsBattleWithoutCardMovement()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 10, 2, 10, 10, 2, 2, 2, 2 }),
                PlainDeck(new[] { 8, 7, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Beelzebub,
                playerCurrentSoul: 2);
            ActivateFirstContract(battle);
            Assert.That(battle.TryPlayerHit(), Is.True);
            int playerHandCount = battle.Player.Hand.Count;
            int enemyHandCount = battle.Enemy.Hand.Count;

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.Player.Soul.Current, Is.Zero);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(battle.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(playerHandCount + 1));
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(enemyHandCount));
        }

        [Test]
        public void DCR04_U04_BeelzebubAlsoReplacesCardEffectBust()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 10, 7, 2, 2, 2, 2 }),
                AutoPistolDeck(startId: 100),
                new ExactAutoPistolPolicy(guess: 7),
                DemonContractKind.Beelzebub,
                playerCurrentSoul: 12);
            var effectKinds = new List<CardEffectKind?>();
            var playerSouls = new List<int>();
            var pendingKinds = new List<DemonContractInteractionKind?>();
            var revolverPhases =
                new List<GameSceneRevolverAnimationPhase?>();
            var lastActionTypes = new List<PublicCombatActionType?>();
            battle.Stepped += () =>
            {
                effectKinds.Add(
                    battle.LastCardEffectResult?.EffectKind);
                playerSouls.Add(battle.Player.Soul.Current);
                pendingKinds.Add(
                    battle.PendingPlayerDemonContractInteraction?.Kind);
                revolverPhases.Add(
                    GameScenePresenter.Create(battle)
                        .RevolverAnimationCue?.Phase);
                lastActionTypes.Add(battle.LastPublicAction?.ActionType);
            };

            ActivateFirstContract(battle);

            int weaponResultIndex = effectKinds.FindIndex(
                kind => kind == CardEffectKind.AutoPistol);
            Assert.That(weaponResultIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(playerSouls[weaponResultIndex], Is.EqualTo(11));
            Assert.That(pendingKinds[weaponResultIndex], Is.Null);
            Assert.That(revolverPhases[weaponResultIndex],
                Is.EqualTo(GameSceneRevolverAnimationPhase.Resolved),
                $"phases={string.Join(",", revolverPhases)}; " +
                $"actions={string.Join(",", lastActionTypes)}");
            Assert.That(battle.HasPendingPostEffectBustReplacement, Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.EnemyTurn));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(11));
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(pendingKinds, Has.None.EqualTo(
                DemonContractInteractionKind.BeelzebubChooseOwnerCard));

            Assert.That(
                battle.TryContinuePostEffectBustReplacement(),
                Is.True);

            Assert.That(battle.HasPendingPostEffectBustReplacement, Is.False);
            int beelzebubIndex = pendingKinds.FindIndex(
                weaponResultIndex + 1,
                kind => kind ==
                    DemonContractInteractionKind.BeelzebubChooseOwnerCard);
            Assert.That(beelzebubIndex, Is.GreaterThan(weaponResultIndex));
            Assert.That(playerSouls[beelzebubIndex], Is.EqualTo(10));

            ResolveBeelzebubDiscardChoices(
                battle,
                battle.Player.Hand.GetFaceUpCards().Single().Id,
                battle.Enemy.Hand.GetFaceUpCards().Single().Id);

            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.Player.Hand.GetFaceUpCards(), Is.Empty);
            Assert.That(battle.Enemy.Hand.GetFaceUpCards(), Is.Empty);
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost,
                Is.EqualTo(1));
        }

        [Test]
        public void DCR04_U05_MephistophelesRevealsOwnerHiddenCardAfterSafeKnife()
        {
            CoreLoopBattle battle = CreateBattle(
                KnifeDeck(startId: 0),
                PlainDeck(new[] { 6, 7, 5, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Mephistopheles,
                playerCurrentSoul: 12);
            ActivateFirstContract(battle);
            BlackjackCard hiddenCard = battle.Player.Hand.Cards.Single(card =>
                !card.IsFaceUp);
            BlackjackCard knife = battle.Player.Hand.Cards.Single(card =>
                card.Definition.Effect == CardEffectKind.MilitaryKnife);

            Assert.That(battle.TryBeginPlayerCardUse(knife.Id), Is.True);

            Assert.That(hiddenCard.IsFaceUp, Is.True);
            Assert.That(battle.LastResolution, Is.Null);
            Assert.That(battle.LastDemonContractEffectResult.Triggered, Is.True);
            Assert.That(battle.LastDemonContractEffectResult.PaidSoulCost, Is.Zero);
        }

        [Test]
        public void DCR04_U06_MephistophelesDoesNotRevealHiddenCardWhenKnifeBusts()
        {
            CoreLoopBattle battle = CreateBattle(
                KnifeDeck(startId: 0),
                PlainDeck(new[] { 10, 7, 5, 10, 2, 2, 2, 2 }, 100),
                new SequencePolicy(EnemyActionType.Hit, EnemyActionType.Stand),
                DemonContractKind.Mephistopheles,
                playerCurrentSoul: 12);
            ActivateFirstContract(battle);
            BlackjackCard hiddenCard = battle.Player.Hand.Cards.Single(card =>
                !card.IsFaceUp);
            BlackjackCard knife = battle.Player.Hand.Cards.Single(card =>
                card.Definition.Effect == CardEffectKind.MilitaryKnife);

            Assert.That(battle.TryBeginPlayerCardUse(knife.Id), Is.True);

            Assert.That(hiddenCard.IsFaceUp, Is.False);
            Assert.That(battle.LastResolution.Value.Cause,
                Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyBust));
        }

        [Test]
        public void DCR04_U07_MephistophelesContextIsActorRelativeForEnemyOwner()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                PlainDeck(new[] { 10, 7, 2, 2 }, 0),
                KnifeDeck(startId: 100),
                new StandPolicy(),
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0),
                new DemonContractResolver(new MephistophelesDemonContractHandler()),
                playerCurrentSoul: 12);
            BlackjackCard enemyHidden = battle.Enemy.Hand.Cards.Single(card =>
                !card.IsFaceUp);
            DemonContractDefinition definition = DemonContractCatalog.Default.GetByKey(
                DemonContractCatalog.MephistophelesKey);
            var activeContract = new ActiveDemonContract(
                new DemonContractCard(500, definition),
                CombatantSide.Enemy,
                new MephistophelesRuntimeState());
            var handler = new MephistophelesDemonContractHandler();

            DemonContractAfterCardEffectStep step = handler.ResolveAfterOwnerCardEffect(
                new DemonContractContext(battle, activeContract),
                new CardEffectResult(
                    sourceCardId: 100,
                    CardEffectKind.MilitaryKnife,
                    succeeded: true,
                    endedRound: false));

            Assert.That(enemyHidden.IsFaceUp, Is.True);
            Assert.That(step.Result.Triggered, Is.True);
            Assert.That(battle.Player.Hand.Cards.Single(card => !card.IsFaceUp),
                Is.Not.Null);
        }

        [Test]
        public void DCR04_U08_HiddenTotalBustAtShowdownIsAnOrdinaryLossNotBeelzebubBust()
        {
            // A total that only exceeds 21 once the hidden card is folded into the
            // final showdown (rule.md 7.4) is never a "bust" for either side — one
            // side over 21 alone is just an ordinary total-comparison loss for that
            // side (mutual loss only when BOTH sides end up over 21). Because it's
            // never classified as a bust, no bust-reactive contract — Beelzebub's
            // replacement included — ever gets a pending interaction to resolve here.
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 6, 10, 2, 2, 2, 2, 2, 2 }),
                PlainDeck(new[] { 10, 7, 2, 2, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Beelzebub,
                playerCurrentSoul: 12);
            ActivateFirstContract(battle);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.Player.VisibleHandValue.IsBust, Is.False);
            Assert.That(battle.Player.HandValue.IsBust, Is.True);
            Assert.That(battle.CanPlayerStand, Is.True);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.LastResolution.Value.Outcome,
                Is.EqualTo(RoundOutcome.EnemyWin));
            // 12 start - 1 Beelzebub activation cost - 1 ordinary loss damage = 10.
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
        }

        [Test]
        public void DCR04_U09_BeelzebubResolvesWithOnlyOwnerPublicCards()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 10, 2, 10, 10, 2, 2, 2, 2 }),
                PlainDeck(new[] { 8, 7, 2, 2, 2, 2 }, 100),
                new StandPolicy(),
                DemonContractKind.Beelzebub,
                playerCurrentSoul: 12);
            ActivateFirstContract(battle);
            Assert.That(battle.TryPlayerHit(), Is.True);
            foreach (BlackjackCard card in
                battle.Enemy.Hand.GetPublicCards().ToArray())
            {
                Assert.That(battle.Enemy.TryDiscardCard(card.Id), Is.True);
            }

            Assert.That(battle.TryPlayerHit(), Is.True);
            PendingDemonContractInteraction ownerChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(ownerChoice.Kind, Is.EqualTo(
                DemonContractInteractionKind.BeelzebubChooseOwnerCard));
            DemonContractOption option = ownerChoice.Options.Single(candidate =>
                candidate.ContractCardId == 2);

            Assert.That(battle.TryResolvePlayerDemonContract(
                ownerChoice.InteractionId,
                option.OptionId), Is.True);

            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);
            Assert.That(battle.Player.Hand.Contains(2), Is.False);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
        }

        [Test]
        public void DCR04_U10_BeelzebubContextAcceptsOnlyOpponentPublicCard()
        {
            CoreLoopBattle battle = CreateStartedBattle(
                PlainDeck(new[] { 10, 7, 2, 2 }),
                PlainDeck(new[] { 8, 6, 2, 2 }, 100),
                new StandPolicy(),
                new DemonContractDeck(Array.Empty<DemonContractCard>(), seed: 0),
                DemonContractResolver.CreateDefault(),
                playerCurrentSoul: 12);
            foreach (BlackjackCard card in
                battle.Player.Hand.GetPublicCards().ToArray())
            {
                Assert.That(battle.Player.TryDiscardCard(card.Id), Is.True);
            }

            DemonContractDefinition definition = DemonContractCatalog.Default
                .GetByKey(DemonContractCatalog.BeelzebubKey);
            var activeContract = new ActiveDemonContract(
                new DemonContractCard(500, definition),
                CombatantSide.Player,
                new BeelzebubRuntimeState());
            var context = new DemonContractContext(battle, activeContract);
            int opponentCardId = battle.Enemy.Hand.GetPublicCards().Single().Id;

            Assert.That(context.CanChooseBeelzebubDiscardCards, Is.True);
            Assert.That(context.TryDiscardChosenFaceUpCards(
                ownerCardId: null,
                opponentCardId), Is.True);
            Assert.That(battle.Enemy.Hand.Contains(opponentCardId), Is.False);
        }

        [Test]
        public void DCR04_U11_LethalKnifeWaitsBeforeBeelzebubReplacement()
        {
            CoreLoopBattle battle = CreateBattle(
                PlainDeck(new[] { 10, 7, 5, 10, 2, 2, 2, 2 }),
                KnifeDeck(startId: 100),
                new SequencePolicy(
                    EnemyActionType.Hit,
                    EnemyActionType.UseCard),
                DemonContractKind.Beelzebub,
                playerCurrentSoul: 12);

            ActivateFirstContract(battle);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.LastCardEffectResult, Is.Not.Null);
            Assert.That(battle.LastCardEffectResult.Value.EffectKind,
                Is.EqualTo(CardEffectKind.MilitaryKnife));
            Assert.That(battle.LastCardEffectResult.Value.EndedRound, Is.True);
            Assert.That(battle.HasPendingPostEffectBustReplacement, Is.True);
            Assert.That(
                GameScenePresenter.Create(battle).KnifeAnimationCue?.Phase,
                Is.EqualTo(GameSceneKnifeAnimationPhase.Resolved));
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(11));
            Assert.That(battle.PendingPlayerDemonContractInteraction, Is.Null);

            Assert.That(
                battle.TryContinuePostEffectBustReplacement(),
                Is.True);

            Assert.That(battle.HasPendingPostEffectBustReplacement, Is.False);
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(10));
            Assert.That(battle.PendingPlayerDemonContractInteraction.Kind,
                Is.EqualTo(
                    DemonContractInteractionKind.BeelzebubChooseOwnerCard));
        }

        private static CoreLoopBattle CreateBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractKind contractKind,
            int playerCurrentSoul)
        {
            return CreateStartedBattle(
                playerDeck,
                enemyDeck,
                enemyPolicy,
                CreateDemonDeck(contractKind),
                DemonContractResolver.CreateDefault(),
                playerCurrentSoul);
        }

        private static CoreLoopBattle CreateStartedBattle(
            BlackjackDeck playerDeck,
            BlackjackDeck enemyDeck,
            IEnemyBehaviorPolicy enemyPolicy,
            DemonContractDeck demonDeck,
            DemonContractResolver resolver,
            int playerCurrentSoul)
        {
            var battle = new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                playerMaximumSoul: 12,
                playerCurrentSoul,
                enemyMaximumSoul: 5,
                enemyPolicy,
                CardEffectResolver.CreateDefault(),
                demonDeck,
                resolver);
            Assert.That(battle.Start(), Is.True);
            return battle;
        }

        private static void ActivateFirstContract(CoreLoopBattle battle)
        {
            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(battle.TryResolvePlayerDemonContract(
                pending.InteractionId,
                pending.Options[0].OptionId), Is.True);
        }

        private static void ResolveBeelzebubDiscardChoices(
            CoreLoopBattle battle,
            int ownerCardId,
            int opponentCardId)
        {
            PendingDemonContractInteraction ownerChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(ownerChoice, Is.Not.Null);
            Assert.That(ownerChoice.Kind, Is.EqualTo(
                DemonContractInteractionKind.BeelzebubChooseOwnerCard));
            DemonContractOption ownerOption = ownerChoice.Options.Single(option =>
                option.ContractCardId == ownerCardId);
            Assert.That(battle.TryResolvePlayerDemonContract(
                ownerChoice.InteractionId,
                ownerOption.OptionId), Is.True);

            PendingDemonContractInteraction opponentChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(opponentChoice.Kind, Is.EqualTo(
                DemonContractInteractionKind.BeelzebubChooseOpponentCard));
            DemonContractOption opponentOption =
                opponentChoice.Options.Single(option =>
                    option.ContractCardId == opponentCardId);
            Assert.That(battle.TryResolvePlayerDemonContract(
                opponentChoice.InteractionId,
                opponentOption.OptionId), Is.True);
        }

        private static DemonContractDeck CreateDemonDeck(DemonContractKind kind)
        {
            string key;
            switch (kind)
            {
                case DemonContractKind.Beelzebub:
                    key = DemonContractCatalog.BeelzebubKey;
                    break;
                case DemonContractKind.Mephistopheles:
                    key = DemonContractCatalog.MephistophelesKey;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind));
            }

            DemonContractDefinition definition =
                DemonContractCatalog.Default.GetByKey(key);
            return new DemonContractDeck(
                new[] { new DemonContractCard(0, definition) },
                seed: 73);
        }

        private static BlackjackDeck PlainDeck(
            IReadOnlyList<int> ranks,
            int startId = 0)
        {
            return BlackjackDeck.CreateInDrawOrder(ranks.Select(
                (rank, index) => new BlackjackCard(startId + index, rank)));
        }

        private static BlackjackDeck AutoPistolDeck(int startId)
        {
            CardDefinition autoPistol =
                CardDefinitionCatalog.GetByKey("auto-pistol-7");
            return BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(startId, autoPistol),
                new BlackjackCard(startId + 1, rank: 2),
                new BlackjackCard(startId + 2, rank: 2),
                new BlackjackCard(startId + 3, rank: 2),
                new BlackjackCard(startId + 4, rank: 2),
                new BlackjackCard(startId + 5, rank: 2)
            });
        }

        private static BlackjackDeck KnifeDeck(int startId)
        {
            CardDefinition knife =
                CardDefinitionCatalog.GetByKey("military-knife-9");
            return BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(startId, knife),
                new BlackjackCard(startId + 1, rank: 2),
                new BlackjackCard(startId + 2, rank: 2),
                new BlackjackCard(startId + 3, rank: 2),
                new BlackjackCard(startId + 4, rank: 2),
                new BlackjackCard(startId + 5, rank: 2)
            });
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return EnemyDecision.FromCandidate(
                    observation.ActionCandidates.First(candidate =>
                        candidate.ActionType == EnemyActionType.Stand),
                    "dcr04-stand");
            }
        }

        private sealed class ExactAutoPistolPolicy : IEnemyBehaviorPolicy
        {
            private readonly int _guess;

            public ExactAutoPistolPolicy(int guess)
            {
                _guess = guess;
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionCandidate candidate = observation.ActionCandidates
                    .FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.UseCard &&
                        option.CardEffectOptionNumericValue == _guess)
                    ?? observation.ActionCandidates.FirstOrDefault(option =>
                        option.ActionType == EnemyActionType.UseCard)
                    ?? observation.ActionCandidates.First(option =>
                        option.ActionType == EnemyActionType.Stand);
                return EnemyDecision.FromCandidate(candidate, "dcr04-exact-pistol");
            }
        }

        private sealed class SequencePolicy : IEnemyBehaviorPolicy
        {
            private readonly Queue<EnemyActionType> _actions;

            public SequencePolicy(params EnemyActionType[] actions)
            {
                _actions = new Queue<EnemyActionType>(actions);
            }

            public EnemyDecision Decide(EnemyObservation observation)
            {
                EnemyActionType action = _actions.Count > 0
                    ? _actions.Dequeue()
                    : EnemyActionType.Stand;
                return EnemyDecision.FromCandidate(
                    observation.ActionCandidates.First(candidate =>
                        candidate.ActionType == action),
                    "dcr04-sequence");
            }
        }
    }
}
