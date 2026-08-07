using System.Collections.Generic;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    [Category("GSV18")]
    public sealed class SoulLossPresentationTests
    {
        [Test]
        public void GSV18_U01_ActualLossIsClampedAndZeroLossIsNotRecorded()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 9, 4, 5 },
                new[] { 10, 7, 4, 5 },
                playerMaximumSoul: 3,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            battle.ApplySoulDamage(
                CombatantSide.Player,
                9,
                SoulLossCause.AutomaticCardCost);
            battle.ApplySoulDamage(
                CombatantSide.Player,
                1,
                SoulLossCause.AutomaticCardCost);

            Assert.That(battle.SoulLossHistory.Count, Is.EqualTo(1));
            SoulLossRecord record = battle.SoulLossHistory[0];
            Assert.That(record.Id, Is.Zero);
            Assert.That(record.TargetSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(record.SoulBefore, Is.EqualTo(3));
            Assert.That(record.SoulAfter, Is.Zero);
            Assert.That(record.MaximumSoul, Is.EqualTo(3));
            Assert.That(record.LossAmount, Is.EqualTo(3));
            Assert.That(record.ResolutionId, Is.Null);
        }

        [Test]
        public void GSV18_U02_FreeThenPaidChangeRecordsOnlyActualCost()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 2, 4, 9, 6, 7, 8 },
                new[] { 10, 7, 5, 6 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryBeginPlayerChange(), Is.True);
            Assert.That(battle.TrySelectChangedCard(0), Is.True);
            Assert.That(battle.SoulLossHistory, Is.Empty);

            Assert.That(battle.TryBeginPlayerChange(), Is.True);

            Assert.That(battle.SoulLossHistory.Count, Is.EqualTo(1));
            SoulLossRecord record = battle.SoulLossHistory[0];
            Assert.That(record.Cause, Is.EqualTo(SoulLossCause.ChangeCost));
            Assert.That(record.LossAmount, Is.EqualTo(1));
            Assert.That(record.SoulBefore, Is.EqualTo(12));
            Assert.That(record.SoulAfter, Is.EqualTo(11));
        }

        [Test]
        public void GSV18_U03_CommonCostPathUsesMonotonicIdsAndCauses()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 9, 4, 5 },
                new[] { 10, 7, 4, 5 },
                playerMaximumSoul: 5,
                enemyMaximumSoul: 5);
            Assert.That(battle.Start(), Is.True);

            battle.ApplySoulDamage(
                CombatantSide.Player,
                1,
                SoulLossCause.DemonContractCost);
            battle.ApplySoulDamage(
                CombatantSide.Enemy,
                2,
                SoulLossCause.AutomaticCardCost);

            Assert.That(
                new[]
                {
                    battle.SoulLossHistory[0].Id,
                    battle.SoulLossHistory[1].Id
                },
                Is.EqualTo(new long[] { 0, 1 }));
            Assert.That(
                battle.SoulLossHistory[0].Cause,
                Is.EqualTo(SoulLossCause.DemonContractCost));
            Assert.That(
                battle.SoulLossHistory[1].Cause,
                Is.EqualTo(SoulLossCause.AutomaticCardCost));
        }

        [Test]
        public void GSV18_U04_RoundDamageLinksResolutionAndReachesViewModel()
        {
            CoreLoopBattle battle = CreateBattle(
                new[] { 10, 9, 4, 5 },
                new[] { 10, 7, 4, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.HasValue, Is.True);
            Assert.That(battle.SoulLossHistory.Count, Is.EqualTo(1));
            SoulLossRecord record = battle.SoulLossHistory[0];
            Assert.That(record.Cause, Is.EqualTo(SoulLossCause.RoundDamage));
            Assert.That(
                record.ResolutionId,
                Is.EqualTo(battle.LastResolution.Value.Id));
            Assert.That(record.TargetSide, Is.EqualTo(CombatantSide.Enemy));
            GameSceneViewModel model = GameScenePresenter.Create(battle);
            Assert.That(model.SoulLossHistory.Count, Is.EqualTo(1));
            Assert.That(model.SoulLossHistory[0].Id, Is.EqualTo(record.Id));
        }

        [Test]
        public void GSV18_U05_TokenCountAndReplayCursorUseActualRecords()
        {
            IReadOnlyList<SoulLossRecord> records = new[]
            {
                new SoulLossRecord(
                    4,
                    CombatantSide.Player,
                    5,
                    3,
                    5,
                    SoulLossCause.RoundDamage,
                    10),
                new SoulLossRecord(
                    5,
                    CombatantSide.Enemy,
                    3,
                    2,
                    3,
                    SoulLossCause.RoundDamage,
                    10)
            };

            Assert.That(SoulLossPresentation.CountTokenUnits(records), Is.EqualTo(3));
            Assert.That(GameManager.HasUnqueuedSoulLoss(3, records), Is.True);
            Assert.That(GameManager.HasUnqueuedSoulLoss(5, records), Is.False);
        }

        [Test]
        public void GSV18_U06_TokenInspectorSettingsAreNormalizedSafely()
        {
            var settings = new SoulLossTokenSettings(
                new Color(2f, 0.5f, 0.25f, 1f),
                fontScale: 0f,
                minimumFontSize: 0f,
                tokenSize: new Vector2(-20f, 0f),
                fallSeconds: 0f,
                staggerSeconds: -1f,
                impactSeconds: 5f,
                fadeSeconds: 5f,
                startRandomX: -3f,
                startYRange: new Vector2(12f, -8f),
                driftX: -4f,
                fallDistanceRange: new Vector2(185f, 145f),
                rotation: -9f,
                playerAnchor: new Vector2(-1f, 2f),
                enemyFallbackAnchor: new Vector2(2f, -1f));

            Assert.That(settings.FontScale, Is.EqualTo(0.1f));
            Assert.That(settings.MinimumFontSize, Is.EqualTo(1f));
            Assert.That(settings.TokenSize, Is.EqualTo(Vector2.one));
            Assert.That(settings.FallSeconds, Is.EqualTo(0.01f));
            Assert.That(settings.StaggerSeconds, Is.Zero);
            Assert.That(settings.ImpactSeconds, Is.EqualTo(0.01f));
            Assert.That(settings.FadeSeconds, Is.EqualTo(0.01f));
            Assert.That(settings.StartRandomX, Is.Zero);
            Assert.That(settings.StartYRange, Is.EqualTo(new Vector2(-8f, 12f)));
            Assert.That(settings.DriftX, Is.Zero);
            Assert.That(
                settings.FallDistanceRange,
                Is.EqualTo(new Vector2(145f, 185f)));
            Assert.That(settings.Rotation, Is.Zero);
            Assert.That(settings.PlayerAnchor, Is.EqualTo(new Vector2(0f, 1f)));
            Assert.That(
                settings.EnemyFallbackAnchor,
                Is.EqualTo(new Vector2(1f, 0f)));
        }

        private static CoreLoopBattle CreateBattle(
            IReadOnlyList<int> playerRanks,
            IReadOnlyList<int> enemyRanks,
            int playerMaximumSoul,
            int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                CreateDeck(playerRanks),
                CreateDeck(enemyRanks),
                playerMaximumSoul: playerMaximumSoul,
                enemyMaximumSoul: enemyMaximumSoul);
        }

        private static BlackjackDeck CreateDeck(IReadOnlyList<int> ranks)
        {
            List<BlackjackCard> cards = new List<BlackjackCard>(ranks.Count);
            for (int index = 0; index < ranks.Count; index++)
            {
                cards.Add(new BlackjackCard(index, ranks[index]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }
    }
}
