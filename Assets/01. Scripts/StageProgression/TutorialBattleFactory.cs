using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    /// <summary>
    /// Battle factory for the scripted first-play tutorial. Only the tutorial's own
    /// first stage (<see cref="TutorialStageId"/>) gets a fully scripted deck order and
    /// a scripted enemy policy — every other stage in the same tutorial-flavored run
    /// (stage 2, the final boss, ...) falls back to the normal <see cref="StageBattleFactory"/>,
    /// exactly like any other run from that point on.
    /// </summary>
    public static class TutorialBattleFactory
    {
        /// <summary>
        /// Matches the id <see cref="Bootstrap.StageProgressionRuntime"/>'s prototype stage
        /// path already gives its first stage — shared, not duplicated, because the
        /// tutorial reuses that same stage list template (see
        /// <see cref="StageProgressionSession"/>'s forced-first-opponent path).
        /// </summary>
        public const string TutorialStageId = "normal-1";

        public static CoreLoopBattle Create(StageDefinition stage, PlayerRunState player)
        {
            if (stage == null || stage.Id != TutorialStageId)
            {
                return StageBattleFactory.Create(stage, player);
            }

            // Exact scripted order for the user's 0-6 section tutorial dialogue.
            // `StartRound()` draws face-up before hidden for each side (Player, Enemy,
            // Player, Enemy), so within a round the face-up rank is listed before the
            // hidden rank below even though the dialogue's own prose lists them the other
            // way around.
            //
            // Round 1 (sections 2-3): face-up 4 / hidden 3 dealt, Hit -> 3, Hit -> Ace(1),
            // Stand. Enemy: face-up 3 / hidden 2 dealt, Hit -> 4, Hit -> 2, Stand.
            // Round 2 (section 4): face-up 3 / hidden 2 dealt, Hit -> 7 (revolver). Enemy:
            // face-up 1 / hidden 5 (crystal orb) dealt; using it peeks 2 cards and takes the
            // first (rank 4, irrelevant to the script — the round ends on the player's
            // revolver guess against the enemy's still-revealed hidden "5"; BlackjackHand
            // keeps a used card's hidden-role flag until it actually leaves the hand). The
            // *second*, un-taken peeked card (rank 10) is pushed back onto the top of the
            // enemy's draw pile by `CrystalOrbEffectHandler.ReturnActorCardsToTop` — it
            // becomes the enemy's very next draw, i.e. round 3's face-up deal card, which is
            // why round 3's explicit face-up entry below is folded into this peek pair rather
            // than listed separately (confirmed by direct simulation, not just reading code).
            // Round 3 (sections 5-6): face-up 9 / hidden 10 dealt, Change draws 1 and 10
            // (either pick is fine per the script). Enemy: face-up 10 dealt via the returned
            // peek card above, hidden 3 dealt, Hit -> 5, then a second silent Hit -> 2 (see
            // TutorialEnemyPolicy for why this replaces the script's own "enemy Change" stage
            // direction, and why it must not Stand here). Asmodeus's forced hit then draws a
            // plain 9 straight off the enemy's deck, busting it (matches "상대 공개 카드에 9 추가,
            // 버스트").
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(9001, 4),
                new BlackjackCard(9002, 3),
                new BlackjackCard(9003, 3),
                new BlackjackCard(9004, 1),
                new BlackjackCard(9005, 3),
                new BlackjackCard(9006, 2),
                new BlackjackCard(9007, 7),
                new BlackjackCard(9008, 9),
                new BlackjackCard(9009, 10),
                new BlackjackCard(9010, 1),
                new BlackjackCard(9011, 10),
            });
            BlackjackDeck enemyDeck = BlackjackDeck.CreateInDrawOrder(new[]
            {
                new BlackjackCard(9101, 3),
                new BlackjackCard(9102, 2),
                new BlackjackCard(9103, 4),
                new BlackjackCard(9104, 2),
                new BlackjackCard(9105, 1),
                new BlackjackCard(9106, 5),
                new BlackjackCard(9107, 4),
                new BlackjackCard(9108, 10),
                new BlackjackCard(9109, 3),
                new BlackjackCard(9110, 5),
                new BlackjackCard(9111, 2),
                new BlackjackCard(9112, 9),
            });

            DemonContractDeck playerDemonDeck = StageBattleFactory.CreateDemonDeck(
                player.DemonDeck,
                StageBattleFactory.DeriveDemonDeckSeed(stage.PlayerDeckSeed));

            // CowardlyGambler's real profile soul (4): round 1 costs it 1 (4->3), round 2's
            // revolver bust costs 2 (3->1), round 3's Asmodeus-forced bust costs the final 2
            // (1->-1) — the enemy survives exactly until the scripted finale, no separate
            // tutorial-only override needed.
            int enemyMaximumSoul = EnemyCombatProfileCatalog.Default
                .GetByKey(EnemyCombatProfileCatalog.CowardlyGamblerKey)
                .MaximumSoul;

            return new CoreLoopBattle(
                playerDeck,
                enemyDeck,
                player.MaximumSoul,
                player.CurrentSoul,
                enemyMaximumSoul,
                enemyPolicy: new TutorialEnemyPolicy(),
                playerDemonDeck: playerDemonDeck,
                demonContractSeed: StageBattleFactory.DeriveDemonContractSeed(
                    stage.PlayerDeckSeed,
                    stage.EnemyDeckSeed));
        }

        internal static IReadOnlyList<int> PlayerDeckRanksForTest =>
            new[] { 4, 3, 3, 1, 3, 2, 7, 9, 10, 1, 10 };

        internal static IReadOnlyList<int> EnemyDeckRanksForTest =>
            new[] { 3, 2, 4, 2, 1, 5, 4, 10, 3, 5, 2, 9 };
    }
}
