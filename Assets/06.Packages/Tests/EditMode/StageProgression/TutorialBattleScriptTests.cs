using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.CoreLoop;
using NUnit.Framework;

namespace DiaBlackJack.StageProgression.Tests
{
    /// <summary>
    /// Plays the exact scripted tutorial battle end to end through <see cref="CoreLoopBattle"/>'s
    /// public API — the same way <c>TutorialDirector</c> will drive it — to lock in that
    /// <see cref="TutorialBattleFactory"/>'s deck and <see cref="TutorialEnemyPolicy"/> actually
    /// reproduce the user's 0-6 section script, round by round, including the two subtleties
    /// found only by direct simulation: <c>StartRound</c> deals face-up before hidden per side,
    /// and Crystal Orb's un-taken peek card is pushed back onto the top of the draw pile (so it
    /// becomes the very next draw, not whatever was queued after it).
    /// </summary>
    public sealed class TutorialBattleScriptTests
    {
        private const string TutorialStageId = TutorialBattleFactory.TutorialStageId;

        [Test]
        public void TU_U09_Round1DealsAndResolvesExactlyAsScripted()
        {
            CoreLoopBattle battle = CreateBattle();
            battle.Start();

            Assert.That(battle.Player.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 4 }));
            Assert.That(HiddenRank(battle.Player), Is.EqualTo(3));
            Assert.That(battle.Enemy.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 3 }));
            Assert.That(HiddenRank(battle.Enemy), Is.EqualTo(2));

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.Player.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 4, 3 }));
            Assert.That(battle.Enemy.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 3, 4 }));

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(
                battle.Player.Hand.GetFaceUpCards().Select(c => c.Rank),
                Is.EqualTo(new[] { 4, 3, 1 }));
            Assert.That(battle.Enemy.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 3, 4, 2 }));

            Assert.That(battle.TryPlayerStand(), Is.True);

            RoundResolution resolution = battle.LastResolution.Value;
            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.PlayerTwentyOneWin));
            Assert.That(resolution.PlayerDamage, Is.EqualTo(0));
            Assert.That(resolution.EnemyDamage, Is.EqualTo(1));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void TU_U10_Round2EnemyRevealsHiddenCardAndPlayerRevolverKillsItAt5()
        {
            CoreLoopBattle battle = CreateBattle();
            battle.Start();
            PlayRound1(battle);

            Assert.That(battle.Player.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 3 }));
            Assert.That(HiddenRank(battle.Player), Is.EqualTo(2));
            Assert.That(battle.Enemy.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 1 }));
            Assert.That(HiddenRank(battle.Enemy), Is.EqualTo(5));

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(
                battle.Player.Hand.GetFaceUpCards().Select(c => c.Rank),
                Is.EqualTo(new[] { 3, 7 }));
            // The enemy's own turn (synchronous with the player's Hit above) uses its hidden
            // Crystal Orb card, revealing it — it stays the enemy's sole "hidden" card even
            // face-up (BlackjackHand only drops the hidden-role flag when a card leaves the
            // hand entirely), so the guess below can still legally target it.
            Assert.That(HiddenRank(battle.Enemy), Is.EqualTo(5));

            int revolverCardId = battle.Player.Hand.GetFaceUpCards()
                .Single(c => c.Rank == 7).Id;
            Assert.That(battle.TryBeginPlayerCardUse(revolverCardId), Is.True);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
            Assert.That(battle.TryResolvePlayerCardChoice(5), Is.True);

            RoundResolution resolution = battle.LastResolution.Value;
            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(resolution.Cause, Is.EqualTo(RoundEndCause.CardEffectBust));
            Assert.That(resolution.EnemyDamage, Is.EqualTo(2));
        }

        [Test]
        public void TU_U11_Round3ChangeThenAsmodeusForcedHitBustsTheEnemy()
        {
            CoreLoopBattle battle = CreateBattle();
            battle.Start();
            PlayRound1(battle);
            PlayRound2(battle);

            Assert.That(battle.Player.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 9 }));
            Assert.That(HiddenRank(battle.Player), Is.EqualTo(10));
            Assert.That(battle.Enemy.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 10 }));
            Assert.That(HiddenRank(battle.Enemy), Is.EqualTo(3));

            Assert.That(battle.TryBeginPlayerChange(), Is.True);
            Assert.That(battle.TrySelectChangedCard(0), Is.True);
            Assert.That(HiddenRank(battle.Player), Is.EqualTo(1));
            // Enemy's synchronous turn: a scripted Hit (matches "상대 히트, 공개 카드 5 들어옴").
            Assert.That(battle.Enemy.Hand.GetFaceUpCards().Select(c => c.Rank), Is.EqualTo(new[] { 10, 5 }));
            Assert.That(battle.Enemy.IsStanding, Is.False);

            Assert.That(battle.TryBeginPlayerDemonContract(), Is.True);
            PendingDemonContractInteraction pending = battle.PendingPlayerDemonContractInteraction;
            DemonContractOption asmodeusOption = pending.Options
                .Single(o => o.ContractDefinitionKey == DemonContractCatalog.AsmodeusKey);
            Assert.That(
                battle.TryResolvePlayerDemonContract(pending.InteractionId, asmodeusOption.OptionId),
                Is.True);
            // Enemy's second synchronous turn: another scripted Hit, not a Stand — Asmodeus's
            // forced-hit choice below only offers itself while the opponent is still playable.
            Assert.That(
                battle.Enemy.Hand.GetFaceUpCards().Select(c => c.Rank),
                Is.EqualTo(new[] { 10, 5, 2 }));
            Assert.That(battle.Enemy.IsStanding, Is.False);

            PendingDemonContractInteraction forcedHitChoice =
                battle.PendingPlayerDemonContractInteraction;
            Assert.That(forcedHitChoice, Is.Not.Null);
            Assert.That(
                battle.TryResolvePlayerDemonContract(forcedHitChoice.InteractionId, 1),
                Is.True);

            RoundResolution resolution = battle.LastResolution.Value;
            Assert.That(resolution.Outcome, Is.EqualTo(RoundOutcome.EnemyBust));
            Assert.That(resolution.Cause, Is.EqualTo(RoundEndCause.NumericBust));
            Assert.That(resolution.EnemyDamage, Is.EqualTo(2));
        }

        private static CoreLoopBattle CreateBattle()
        {
            StageDefinition stage = StageDefinition.CreateForEnemyProfile(
                TutorialStageId,
                "Tutorial",
                StageKind.NormalCombat,
                EnemyCombatProfileCatalog.CowardlyGamblerKey,
                20260807,
                20260808);
            return TutorialBattleFactory.Create(stage, CreatePlayer());
        }

        private static void PlayRound1(CoreLoopBattle battle)
        {
            battle.TryPlayerHit();
            battle.TryPlayerHit();
            battle.TryPlayerStand();
        }

        private static void PlayRound2(CoreLoopBattle battle)
        {
            battle.TryPlayerHit();
            int revolverCardId = battle.Player.Hand.GetFaceUpCards()
                .Single(c => c.Rank == 7).Id;
            battle.TryBeginPlayerCardUse(revolverCardId);
            battle.TryResolvePlayerCardChoice(5);
        }

        private static int HiddenRank(BattleParticipant participant)
        {
            Assert.That(participant.Hand.TryGetSingleHiddenCard(out BlackjackCard hidden), Is.True);
            return hidden.Rank;
        }

        private static PlayerRunState CreatePlayer()
        {
            var cards = new List<RunCardDefinition>(20);
            int cardId = 0;
            for (int rank = 1; rank <= 10; rank++)
            {
                cards.Add(new RunCardDefinition(cardId++, rank, CardSuit.Spade));
                cards.Add(new RunCardDefinition(cardId++, rank, CardSuit.Clover));
            }

            var demonDeck = new List<RunDemonDefinition>
            {
                new RunDemonDefinition(0, DemonContractCatalog.BeelzebubKey),
                new RunDemonDefinition(1, DemonContractCatalog.AsmodeusKey)
            };

            return new PlayerRunState(12, 12, cards, demonDeck);
        }
    }
}
