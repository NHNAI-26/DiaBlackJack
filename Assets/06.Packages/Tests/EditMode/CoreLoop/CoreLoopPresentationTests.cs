using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InvalidOperationException = System.InvalidOperationException;
using DiaBlackJack.CoreLoop.UI;
using DiaBlackJack.GameScene;
using NUnit.Framework;
using UnityEngine;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class CoreLoopPresentationTests
    {
        [Test]
        public void CL_F04_EnemySoulZeroShowsVictoryAndRestart()
        {
            var session = new CoreLoopSession(CreatePlayerVictoryBattle);
            session.TryPlayerStand();

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.Outcome, Is.EqualTo(BattleOutcome.PlayerVictory));
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanRestart, Is.True);
            Assert.That(model.EnemySoul, Is.EqualTo("0 / 1"));
        }

        [Test]
        public void CL_F05_PlayerSoulZeroShowsDefeatAndRestart()
        {
            var session = new CoreLoopSession(CreatePlayerDefeatBattle);
            session.TryPlayerStand();

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.Outcome, Is.EqualTo(BattleOutcome.PlayerDefeat));
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanRestart, Is.True);
            Assert.That(model.PlayerSoul, Is.EqualTo("0 / 1"));
        }

        [Test]
        public void CL_F06_TenRestartsAlwaysCreateCleanInitialState()
        {
            int createdBattleCount = 0;
            var session = new CoreLoopSession(() =>
            {
                createdBattleCount++;
                return CreatePlayerVictoryBattle();
            });

            for (int i = 0; i < 10; i++)
            {
                Assert.That(session.TryPlayerStand(), Is.True, $"End battle {i}");
                Assert.That(session.Battle.State, Is.EqualTo(CoreLoopState.BattleEnded), $"Ended {i}");
                Assert.That(session.TryRestart(), Is.True, $"Restart {i}");
                AssertInitialState(session.Battle, i);
            }

            Assert.That(createdBattleCount, Is.EqualTo(11));
        }

        [Test]
        public void PresentationHidesEnemyPrivateCardWithoutHidingPlayerCard()
        {
            var session = new CoreLoopSession(CreatePlayerVictoryBattle);

            CoreLoopViewModel model = CoreLoopPresenter.Create(session.Battle);

            Assert.That(model.PlayerCards, Is.EqualTo("10  1"));
            Assert.That(model.EnemyCards, Is.EqualTo("10  ?"));
            Assert.That(model.PlayerTotal, Is.EqualTo(21));
            Assert.That(model.PlayerVisibleTotal, Is.EqualTo(10));
            Assert.That(model.EnemyVisibleTotal, Is.EqualTo(10));
            Assert.That(
                model.PlayerTotalsText,
                Is.EqualTo("총합 : 21\n공개 카드 합 : 10"));
            Assert.That(
                model.EnemyVisibleTotalText,
                Is.EqualTo("공개 카드 합 : 10"));
        }

        [Test]
        public void CLM01_U01_RevealedHiddenRoleStaysExcludedFromPlayerPublicTotal()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2 },
                enemyRanks: new[] { 9, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard playerHiddenCard = battle.Player.Hand.Cards[1];
            playerHiddenCard.Reveal();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.PlayerTotal, Is.EqualTo(12));
            Assert.That(model.PlayerVisibleTotal, Is.EqualTo(10));
            Assert.That(
                model.PlayerTotalsText,
                Is.EqualTo("총합 : 12\n공개 카드 합 : 10"));
        }

        [Test]
        public void CUM18_U01_GameSceneProjectsBothSidesNewestDrawAtScreenRight()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4 },
                enemyRanks: new[] { 10, 7, 5, 6 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            int playerFaceUpCardId = battle.Player.Hand.Cards[0].Id;
            int playerHiddenCardId = battle.Player.Hand.Cards[1].Id;
            int playerDrawnFaceUpCardId = battle.Player.Draw(faceUp: true).Id;
            int enemyFaceUpCardId = battle.Enemy.Hand.Cards[0].Id;
            int enemyHiddenCardId = battle.Enemy.Hand.Cards[1].Id;
            int enemyDrawnFaceUpCardId = battle.Enemy.Draw(faceUp: true).Id;
            int enemySecondDrawnFaceUpCardId = battle.Enemy.Draw(faceUp: true).Id;

            GameSceneViewModel model = GameScenePresenter.Create(battle);

            Assert.That(
                battle.Player.Hand.Cards.Select(card => card.Id),
                Is.EqualTo(new[]
                {
                    playerFaceUpCardId,
                    playerHiddenCardId,
                    playerDrawnFaceUpCardId,
                }));
            Assert.That(
                battle.Enemy.Hand.Cards.Select(card => card.Id),
                Is.EqualTo(new[]
                {
                    enemyFaceUpCardId,
                    enemyHiddenCardId,
                    enemyDrawnFaceUpCardId,
                    enemySecondDrawnFaceUpCardId,
                }));
            Assert.That(
                model.PlayerCards.Select(card => card.CardId),
                Is.EqualTo(new[]
                {
                    playerDrawnFaceUpCardId,
                    playerFaceUpCardId,
                    playerHiddenCardId,
                }));
            Assert.That(
                model.EnemyCards.Select(card => card.CardId),
                Is.EqualTo(new[]
                {
                    enemySecondDrawnFaceUpCardId,
                    enemyDrawnFaceUpCardId,
                    enemyFaceUpCardId,
                    enemyHiddenCardId,
                }));
            Assert.That(
                model.PlayerCards.Take(model.PlayerCards.Count - 1).All(card => card.IsFaceUp),
                Is.True);
            Assert.That(model.PlayerCards.Last().IsFaceUp, Is.False);
            Assert.That(
                model.EnemyCards.Take(model.EnemyCards.Count - 1).All(card => card.IsFaceUp),
                Is.True);
            Assert.That(model.EnemyCards.Last().IsFaceUp, Is.False);
            Assert.That(model.EnemyCards.Last().RevealRank, Is.False);
        }

        [Test]
        public void CUM04_U05_RevealedHiddenCardsStayAtScreenLeftEdges()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4 },
                enemyRanks: new[] { 9, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard playerHiddenCard = battle.Player.Hand.Cards[1];
            BlackjackCard enemyHiddenCard = battle.Enemy.Hand.Cards[1];
            playerHiddenCard.Reveal();
            enemyHiddenCard.Reveal();

            GameSceneViewModel model = GameScenePresenter.Create(battle);

            Assert.That(battle.Player.Hand.HiddenCardCount, Is.EqualTo(1));
            Assert.That(battle.Enemy.Hand.HiddenCardCount, Is.EqualTo(1));
            Assert.That(model.PlayerCards.Last().CardId, Is.EqualTo(playerHiddenCard.Id));
            Assert.That(model.EnemyCards.Last().CardId, Is.EqualTo(enemyHiddenCard.Id));
            Assert.That(model.PlayerCards.Last().IsFaceUp, Is.True);
            Assert.That(model.EnemyCards.Last().IsFaceUp, Is.True);
            Assert.That(model.EnemyCards.Last().RevealRank, Is.True);
            Assert.That(model.EnemyCards.Last().Rank, Is.EqualTo(7));
            Assert.That(battle.Player.VisibleHandValue.Total, Is.EqualTo(10));
            Assert.That(battle.Enemy.VisibleHandValue.Total, Is.EqualTo(9));
        }

        [Test]
        public void GSV06_U01_RoundResolutionRevealsBothHiddenCardsAndFinalTotals()
        {
            var battle = new CoreLoopBattle(
                CreateDeck(new[] { 10, 8, 2, 3 }),
                CreateDeck(new[] { 10, 7, 4, 5 }),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            int playerHiddenCardId = battle.Player.Hand.Cards[1].Id;
            int enemyHiddenCardId = battle.Enemy.Hand.Cards[1].Id;
            GameSceneViewModel resolvingModel = null;
            battle.Stepped += () =>
            {
                if (battle.State == CoreLoopState.ResolvingRound)
                {
                    resolvingModel = GameScenePresenter.Create(battle);
                }
            };

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(resolvingModel, Is.Not.Null);
            Assert.That(
                resolvingModel.PlayerCards.Single(card =>
                    card.CardId == playerHiddenCardId).IsFaceUp,
                Is.True);
            GameSceneCardViewModel enemyHidden = resolvingModel.EnemyCards.Single(
                card => card.CardId == enemyHiddenCardId);
            Assert.That(enemyHidden.IsFaceUp, Is.True);
            Assert.That(enemyHidden.RevealRank, Is.True);
            Assert.That(enemyHidden.Rank, Is.EqualTo(7));
            Assert.That(
                resolvingModel.PlayerTotalsText,
                Is.EqualTo("총합 : 18\n공개 카드 합 : 10"));
            Assert.That(
                resolvingModel.EnemyTotalsText,
                Is.EqualTo("총합 : 17\n공개 카드 합 : 10"));
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(battle.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void GSV06_U02_RoundResultHoldKeepsAtLeastTwoAndHalfSeconds()
        {
            Assert.That(
                GameManager.MinimumRoundResultHoldSeconds,
                Is.GreaterThanOrEqualTo(2.5f));
        }

        [Test]
        [Category("GSV17")]
        public void GSV17_U01_ComparisonCountsBothSidesFromScreenRight()
        {
            var battle = new CoreLoopBattle(
                CreateDeck(new[] { 2, 2, 5, 10, 1, 3, 4, 6 }),
                CreateDeck(new[] { 10, 7, 2, 3, 4, 5 }),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);
            GameSceneViewModel resolvingModel = null;
            battle.Stepped += () =>
            {
                if (battle.State == CoreLoopState.ResolvingRound)
                {
                    resolvingModel = GameScenePresenter.Create(battle);
                }
            };

            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);
            Assert.That(battle.TryPlayerStand(), Is.True);

            RoundComparisonPlan plan = resolvingModel?.RoundComparisonPlan;
            Assert.That(plan, Is.Not.Null);
            Assert.That(
                plan.Player.PublicSteps.Select(step => step.CardId),
                Is.EqualTo(resolvingModel.PlayerCards
                    .Where(card => plan.Player.HiddenStep == null ||
                        card.CardId != plan.Player.HiddenStep.CardId)
                    .Select(card => card.CardId)));
            Assert.That(
                plan.Player.PublicSteps.Select(step => step.Total),
                Is.EqualTo(new[] { 11, 21, 16, 18 }));
            Assert.That(plan.Player.HiddenStep.Total, Is.EqualTo(20));
            Assert.That(
                plan.Enemy.PublicSteps.Select(step => step.CardId),
                Is.EqualTo(resolvingModel.EnemyCards
                    .Where(card => card.CardId != plan.Enemy.HiddenStep.CardId)
                    .Select(card => card.CardId)));
            Assert.That(plan.Enemy.PublicSteps.Single().Total, Is.EqualTo(10));
            Assert.That(plan.Enemy.HiddenStep.Total, Is.EqualTo(17));
            Assert.That(plan.PlayerDamage, Is.Zero);
            Assert.That(plan.EnemyDamage, Is.EqualTo(1));
        }

        [Test]
        [Category("GSV17")]
        public void GSV17_U02_ComparisonColorMovesToGoldThenTurnsRed()
        {
            Assert.That(
                TableTotalsView.ResolveComparisonColor(15),
                Is.EqualTo(Color.white));
            Assert.That(
                TableTotalsView.ResolveComparisonBloomStrength(15),
                Is.Zero);
            Assert.That(
                TableTotalsView.ResolveComparisonBloomStrength(16),
                Is.GreaterThan(0f));
            Assert.That(
                TableTotalsView.ResolveComparisonBloomStrength(21),
                Is.EqualTo(1f));
            Assert.That(
                TableTotalsView.ResolveComparisonColor(21).r,
                Is.GreaterThan(1f));
            Assert.That(
                TableTotalsView.ResolveComparisonColor(22),
                Is.EqualTo(new Color(1f, 0.055f, 0.035f, 1f)));
            Assert.That(
                TableTotalsView.ResolveComparisonBloomStrength(22),
                Is.Zero);
        }

        [Test]
        [Category("GSV17")]
        public void GSV17_U03_ComparisonHighlightPreservesEffectHighlight()
        {
            var gameObject = new GameObject("comparison-card-test");
            try
            {
                CardView view = gameObject.AddComponent<CardView>();
                view.Bind(new GameSceneCardViewModel(
                    10,
                    5,
                    isFaceUp: true,
                    revealRank: true,
                    canUse: false,
                    "Effect source",
                    isEffectSource: true,
                    isEffectSourcePersistent: true));

                view.SetComparisonHighlighted(true);
                Assert.That(view.IsComparisonHighlighted, Is.True);
                view.SetComparisonHighlighted(false);

                Assert.That(view.IsComparisonHighlighted, Is.False);
                Assert.That(view.HasEffectiveHighlight, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        [Category("GSV17")]
        public void GSV17_U04_CancelAndResolutionIdPreventTemporaryReplayState()
        {
            var gameObject = new GameObject("comparison-total-test");
            try
            {
                TableTotalsView view = gameObject.AddComponent<TableTotalsView>();
                view.BeginComparison();
                Assert.That(view.IsComparisonActive, Is.True);
                view.CancelComparison();
                Assert.That(view.IsComparisonActive, Is.False);

                var side = new RoundComparisonSidePlan(
                    System.Array.Empty<RoundComparisonStep>(),
                    hiddenStep: null,
                    cardTotal: 0,
                    bonus: 0);
                var plan = new RoundComparisonPlan(
                    7,
                    RoundOutcome.PlayerWin,
                    RoundEndCause.TotalComparison,
                    0,
                    1,
                    side,
                    side);
                Assert.That(
                    GameManager.ShouldPlayRoundComparison(-1, plan),
                    Is.True);
                Assert.That(
                    GameManager.ShouldPlayRoundComparison(7, plan),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [TestCase(CombatantSide.Player)]
        [TestCase(CombatantSide.Enemy)]
        [Category("GSV17")]
        public void GSV17_U06_DecisiveRevolverGuessSkipsComparisonForEitherSide(
            CombatantSide actorSide)
        {
            RoundResolution resolution = CreateHiddenGuessResolution(
                actorSide,
                RoundEndCause.CardEffectBust);
            var cue = new GameSceneRevolverAnimationCue(
                1,
                10,
                actorSide,
                GameSceneRevolverAnimationPhase.Resolved,
                succeeded: true);

            RoundComparisonPlaybackMode mode =
                RoundComparisonPresenter.ResolvePlaybackMode(
                    resolution,
                    cue,
                    satanNumberGuessCue: null);
            RoundComparisonPlan plan = CreateComparisonPlan(resolution, mode);

            Assert.That(
                mode,
                Is.EqualTo(
                    RoundComparisonPlaybackMode.SkipForDecisiveHiddenGuess));
            Assert.That(
                GameManager.ShouldSkipRoundComparisonForDecisiveHiddenGuess(
                    -1,
                    plan),
                Is.True);
            Assert.That(
                GameManager.ShouldPlayRoundComparison(-1, plan),
                Is.False);
            Assert.That(plan.PlayerDamage, Is.EqualTo(resolution.PlayerDamage));
            Assert.That(plan.EnemyDamage, Is.EqualTo(resolution.EnemyDamage));
            Assert.That(
                GameManager.ShouldSkipRoundComparisonForDecisiveHiddenGuess(
                    resolution.Id,
                    plan),
                Is.False);
        }

        [TestCase(CombatantSide.Player)]
        [TestCase(CombatantSide.Enemy)]
        [Category("GSV17")]
        public void GSV17_U07_DecisiveSatanGuessSkipsComparisonForEitherSide(
            CombatantSide actorSide)
        {
            RoundResolution resolution = CreateHiddenGuessResolution(
                actorSide,
                RoundEndCause.ContractEffectBust);
            var cue = new GameSceneSatanNumberGuessAnimationCue(
                1,
                20,
                actorSide,
                30,
                succeeded: true,
                actionOrdinal: 1);

            RoundComparisonPlaybackMode mode =
                RoundComparisonPresenter.ResolvePlaybackMode(
                    resolution,
                    revolverCue: null,
                    satanNumberGuessCue: cue);

            Assert.That(
                mode,
                Is.EqualTo(
                    RoundComparisonPlaybackMode.SkipForDecisiveHiddenGuess));
        }

        [TestCase(CombatantSide.Player, RoundEndCause.CardEffectBust)]
        [TestCase(CombatantSide.Enemy, RoundEndCause.CardEffectBust)]
        [TestCase(CombatantSide.Player, RoundEndCause.NumericBust)]
        [TestCase(CombatantSide.Enemy, RoundEndCause.NumericBust)]
        [Category("GSV23")]
        public void GSV23_U01_KnifeDirectBustSkipsTotalComparison(
            CombatantSide actorSide,
            RoundEndCause cause)
        {
            RoundResolution resolution = CreateHiddenGuessResolution(
                actorSide,
                cause);
            var cue = new GameSceneKnifeAnimationCue(
                1,
                11,
                actorSide,
                GameSceneKnifeAnimationPhase.Resolved,
                succeeded: true);

            RoundComparisonPlaybackMode mode =
                RoundComparisonPresenter.ResolvePlaybackMode(
                    resolution,
                    revolverCue: null,
                    satanNumberGuessCue: null,
                    knifeCue: cue);
            RoundComparisonPlan plan = CreateComparisonPlan(resolution, mode);

            Assert.That(
                mode,
                Is.EqualTo(RoundComparisonPlaybackMode.SkipForDirectBust));
            Assert.That(
                GameManager.ShouldPlayRoundComparison(-1, plan),
                Is.False);
            Assert.That(
                GameManager.ShouldSkipRoundComparisonForDecisiveHiddenGuess(
                    -1,
                    plan),
                Is.True);
        }

        [Test]
        [Category("GSV23")]
        public void GSV23_U02_RevolverCuePreservesActionOrdinal()
        {
            var cue = new GameSceneRevolverAnimationCue(
                2,
                12,
                CombatantSide.Enemy,
                GameSceneRevolverAnimationPhase.Resolved,
                succeeded: true,
                actionOrdinal: 7);

            Assert.That(cue.ActionOrdinal, Is.EqualTo(7));
        }

        [Test]
        [Category("GSV17")]
        public void GSV17_U08_NonDecisiveGuessKeepsComparison()
        {
            RoundResolution revolverResolution = CreateHiddenGuessResolution(
                CombatantSide.Player,
                RoundEndCause.CardEffectBust);
            var failedRevolver = new GameSceneRevolverAnimationCue(
                1,
                10,
                CombatantSide.Player,
                GameSceneRevolverAnimationPhase.Resolved,
                succeeded: false);
            var successfulSatan = new GameSceneSatanNumberGuessAnimationCue(
                1,
                20,
                CombatantSide.Player,
                30,
                succeeded: true,
                actionOrdinal: 1);

            Assert.That(
                RoundComparisonPresenter.ResolvePlaybackMode(
                    revolverResolution,
                    failedRevolver,
                    satanNumberGuessCue: null),
                Is.EqualTo(RoundComparisonPlaybackMode.CountTotals));
            Assert.That(
                RoundComparisonPresenter.ResolvePlaybackMode(
                    revolverResolution,
                    revolverCue: null,
                    successfulSatan),
                Is.EqualTo(RoundComparisonPlaybackMode.CountTotals));

            var ordinaryResolution = new RoundResolution(
                2,
                RoundOutcome.PlayerWin,
                playerDamage: 0,
                enemyDamage: 1,
                cause: RoundEndCause.TotalComparison);
            Assert.That(
                RoundComparisonPresenter.ResolvePlaybackMode(
                    ordinaryResolution,
                    revolverCue: null,
                    satanNumberGuessCue: null),
                Is.EqualTo(RoundComparisonPlaybackMode.CountTotals));
        }

        [Test]
        [Category("GSV17")]
        public void GSV17_U09_MammonChoiceHudAppearsOnlyAfterPrefixUnlocks()
        {
            Assert.That(
                GameManager.ShouldHideCombatHudForPresentation(
                    inputLocked: true,
                    roundComparisonActive: true,
                    deferRoundResultPresentation: false,
                    hasBlockingAnimationCue: false),
                Is.True);
            Assert.That(
                GameManager.ShouldHideCombatHudForPresentation(
                    inputLocked: false,
                    roundComparisonActive: true,
                    deferRoundResultPresentation: false,
                    hasBlockingAnimationCue: false),
                Is.False);
        }

        private static RoundResolution CreateHiddenGuessResolution(
            CombatantSide actorSide,
            RoundEndCause cause)
        {
            return actorSide == CombatantSide.Player
                ? new RoundResolution(
                    7,
                    RoundOutcome.EnemyBust,
                    playerDamage: 0,
                    enemyDamage: 2,
                    cause: cause,
                    sourceCardKey: cause == RoundEndCause.CardEffectBust
                        ? "auto-pistol-7"
                        : null)
                : new RoundResolution(
                    7,
                    RoundOutcome.PlayerBust,
                    playerDamage: 2,
                    enemyDamage: 0,
                    cause: cause,
                    sourceCardKey: cause == RoundEndCause.CardEffectBust
                        ? "auto-pistol-7"
                        : null);
        }

        private static RoundComparisonPlan CreateComparisonPlan(
            RoundResolution resolution,
            RoundComparisonPlaybackMode playbackMode)
        {
            var side = new RoundComparisonSidePlan(
                System.Array.Empty<RoundComparisonStep>(),
                hiddenStep: null,
                cardTotal: 0,
                bonus: 0);
            return new RoundComparisonPlan(
                resolution.Id,
                resolution.Outcome,
                resolution.Cause,
                resolution.PlayerDamage,
                resolution.EnemyDamage,
                side,
                side,
                playbackMode);
        }

        [Test]
        public void CUM05_U01_EnemyHoverInfoUsesSafePlaceholderUntilRevealed()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2 },
                enemyRanks: new[] { 7, 6 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard faceUpCard = battle.Enemy.Hand.Cards[0];
            BlackjackCard hiddenCard = battle.Enemy.Hand.Cards[1];

            GameSceneViewModel initialModel = GameScenePresenter.Create(battle);
            GameSceneCardViewModel visibleCardModel = initialModel.EnemyCards.Single(
                card => card.CardId == faceUpCard.Id);
            GameSceneCardViewModel hiddenCardModel = initialModel.EnemyCards.Single(
                card => card.CardId == hiddenCard.Id);

            Assert.That(visibleCardModel.RevealRank, Is.True);
            Assert.That(visibleCardModel.DisplayName, Is.EqualTo("리볼버"));
            Assert.That(
                visibleCardModel.AbilityDescription,
                Is.EqualTo("숫자 하나를 선언합니다. 상대 비공개 카드와 일치하면 상대를 <b><color=#B71C1C>버스트</color></b>시킵니다."));
            Assert.That(visibleCardModel.ShowHoverBadgeWhenUnavailable, Is.True);
            Assert.That(visibleCardModel.ShowHoverBadgeBelow, Is.True);
            Assert.That(hiddenCardModel.RevealRank, Is.False);
            Assert.That(hiddenCardModel.Rank, Is.Zero);
            Assert.That(hiddenCardModel.DefinitionKey, Is.Empty);
            Assert.That(hiddenCardModel.DisplayName, Is.EqualTo("비공개 카드"));
            Assert.That(
                hiddenCardModel.AbilityDescription,
                Is.EqualTo("공개되기 전에는 정보를 확인할 수 없습니다."));
            Assert.That(hiddenCardModel.ShowHoverBadgeWhenUnavailable, Is.True);
            Assert.That(hiddenCardModel.ShowHoverBadgeBelow, Is.True);

            hiddenCard.Reveal();
            GameSceneCardViewModel revealedHiddenCardModel =
                GameScenePresenter.Create(battle).EnemyCards.Single(
                    card => card.CardId == hiddenCard.Id);

            Assert.That(revealedHiddenCardModel.RevealRank, Is.True);
            Assert.That(revealedHiddenCardModel.DisplayName, Is.EqualTo("위협용 해머"));
            Assert.That(
                revealedHiddenCardModel.AbilityDescription,
                Is.EqualTo("상대 공개 카드 1장을 버립니다. 상대가 <b><color=#FF9800>스탠드</color></b>했다면 <b><color=#FF9800>스탠드</color></b>를 취소하고 비공개 카드도 교체합니다."));
            Assert.That(revealedHiddenCardModel.ShowHoverBadgeWhenUnavailable, Is.True);
            Assert.That(revealedHiddenCardModel.ShowHoverBadgeBelow, Is.True);
        }

        [Test]
        public void CUM05_U02_CardViewShowsSafeHiddenEnemyInfoWithoutLeak()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2 },
                enemyRanks: new[] { 7, 6 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            GameSceneViewModel model = GameScenePresenter.Create(battle);
            GameSceneCardViewModel visibleCardModel = model.EnemyCards.Single(
                card => card.RevealRank);
            GameSceneCardViewModel hiddenCardModel = model.EnemyCards.Single(
                card => !card.RevealRank);
            GameObject gameObject = new GameObject("Enemy Hover Card Test");
            CardView cardView = gameObject.AddComponent<CardView>();

            try
            {
                cardView.Bind(visibleCardModel);
                cardView.SetHovered(true);

                Assert.That(cardView.ShouldShowHoverBadge, Is.True);
                Assert.That(
                    cardView.HoverBadgeTitle,
                    Is.EqualTo($"{visibleCardModel.Rank}. {visibleCardModel.DisplayName}"));
                Assert.That(
                    cardView.HoverBadgeDescription,
                    Is.EqualTo(visibleCardModel.AbilityDescription));
                Assert.That(cardView.ShowHoverBadgeBelow, Is.True);
                Assert.That(
                    cardView.HoverBadgeText,
                    Is.EqualTo(
                        "7 리볼버\n숫자 하나를 선언합니다. " +
                        "상대 비공개 카드와 일치하면 상대를 <b><color=#B71C1C>버스트</color></b>시킵니다."));

                cardView.Bind(hiddenCardModel);
                cardView.SetHovered(true);

                Assert.That(cardView.ShouldShowHoverBadge, Is.True);
                Assert.That(cardView.HoverBadgeTitle, Is.EqualTo("비공개 카드"));
                Assert.That(
                    cardView.HoverBadgeDescription,
                    Is.EqualTo("공개되기 전에는 정보를 확인할 수 없습니다."));
                Assert.That(
                    cardView.HoverBadgeText,
                    Is.EqualTo(
                        "비공개 카드\n공개되기 전에는 정보를 확인할 수 없습니다."));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CUM05_U03_PublicEnemyAutomaticCardIncludesEffectDescription()
        {
            var enemyCards = new List<BlackjackCard>
            {
                new BlackjackCard(0, 7),
                new BlackjackCard(1, 6),
                new BlackjackCard(
                    2,
                    CardDefinitionCatalog.GetByKey(CardDefinitionCatalog.PoisonKey)),
            };
            CoreLoopBattle battle = new CoreLoopBattle(
                CreateDeck(new[] { 10, 2 }),
                BlackjackDeck.CreateInDrawOrder(enemyCards),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard poison = battle.Enemy.Draw(faceUp: true);

            GameSceneCardViewModel poisonModel = GameScenePresenter.Create(battle)
                .EnemyCards.Single(card => card.CardId == poison.Id);

            Assert.That(poisonModel.DisplayName, Is.EqualTo("독극물"));
            Assert.That(
                poisonModel.DefinitionKey,
                Is.EqualTo(CardDefinitionCatalog.PoisonKey));
            Assert.That(
                poisonModel.AbilityDescription,
                Is.EqualTo(
                    "즉시 <b><color=#FF9800>스탠드</color></b>하거나 영혼 3을 겁니다. " +
                    "영혼이 3 미만이면 남은 영혼을 모두 겁니다. " +
                    "영혼을 걸고 승리하면 영혼 5를 회복합니다."));
            Assert.That(poisonModel.ShowHoverBadgeWhenUnavailable, Is.True);
        }

        [Test]
        public void GSV03_U02_DeckPreviewProjectsOnlyAvailablePlayerDrawCards()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 6, 7, 10 },
                enemyRanks: new[] { 10, 8 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            GameSceneDeckViewModel preview = GameScenePresenter.CreateDeckPreview(
                battle,
                DeckKind.Draw);

            Assert.That(preview.Title, Is.EqualTo("뽑을 카드"));
            Assert.That(preview.CardCount, Is.EqualTo(battle.Player.Deck.DrawCount));
            Assert.That(
                preview.CardGroups.Select(group => group.Card.CardId),
                Is.Not.Contains(battle.Player.Hand.Cards[0].Id));
            Assert.That(
                preview.CardGroups.Select(group => group.Card.CardId),
                Is.Not.Contains(battle.Player.Hand.Cards[1].Id));
            Assert.That(preview.CardGroups.All(group => group.Card.IsFaceUp), Is.True);
            Assert.That(preview.CardGroups.All(group => group.Card.RevealRank), Is.True);
            Assert.That(preview.CardGroups.All(group => !group.Card.CanUse), Is.True);
            Assert.That(
                preview.CardGroups.All(group =>
                    group.Card.ShowHoverBadgeWhenUnavailable),
                Is.True);
        }

        [Test]
        public void GSV03_U07_DeckPreviewGroupsByDefinitionAndSuitOnly()
        {
            CardDefinition standardSeven =
                CardDefinitionCatalog.GetDefaultForRank(7);
            var alternateSeven = new CardDefinition(
                "alternate-seven",
                "Alternate Seven",
                7,
                CardActivationKind.None,
                CardEffectKind.None,
                "Alternate description");
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(
                new[]
                {
                    new BlackjackCard(8, standardSeven, suit: CardSuit.Spade),
                    new BlackjackCard(2, standardSeven, suit: CardSuit.Spade),
                    new BlackjackCard(3, standardSeven, suit: CardSuit.Clover),
                    new BlackjackCard(4, alternateSeven, suit: CardSuit.Spade),
                });
            CoreLoopBattle battle = new CoreLoopBattle(
                playerDeck,
                CreateDeck(new[] { 10, 7 }),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);

            GameSceneDeckViewModel preview = GameScenePresenter.CreateDeckPreview(
                battle,
                DeckKind.Draw);

            Assert.That(preview.CardCount, Is.EqualTo(4));
            Assert.That(preview.GroupCount, Is.EqualTo(3));
            GameSceneDeckCardGroupViewModel spadeGroup =
                preview.CardGroups.Single(group =>
                    group.Card.DefinitionKey == standardSeven.Key &&
                    group.Card.Suit == CardSuit.Spade);
            Assert.That(spadeGroup.Count, Is.EqualTo(2));
            Assert.That(spadeGroup.Card.CardId, Is.EqualTo(2));
            Assert.That(
                preview.CardGroups.Single(group =>
                    group.Card.DefinitionKey == standardSeven.Key &&
                    group.Card.Suit == CardSuit.Clover).Count,
                Is.EqualTo(1));
            Assert.That(
                preview.CardGroups.Single(group =>
                    group.Card.DefinitionKey == alternateSeven.Key).Count,
                Is.EqualTo(1));
        }

        [Test]
        public void GF06_U01_DeckPreviewSupportsRuntimeCardDefinitions()
        {
            var runtimeDefinition = new CardDefinition(
                "runtime-card-1",
                "Runtime Card",
                1,
                CardActivationKind.None,
                CardEffectKind.None,
                "Runtime description");
            BlackjackDeck playerDeck = BlackjackDeck.CreateInDrawOrder(
                new[]
                {
                    new BlackjackCard(0, 10),
                    new BlackjackCard(1, 8),
                    new BlackjackCard(2, runtimeDefinition),
                });
            CoreLoopBattle battle = new CoreLoopBattle(
                playerDeck,
                CreateDeck(new[] { 10, 7 }),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);

            GameSceneDeckViewModel preview = GameScenePresenter.CreateDeckPreview(
                battle,
                DeckKind.Draw);

            GameSceneCardViewModel runtimeCard = preview.CardGroups.Single(
                group => group.Card.DefinitionKey == runtimeDefinition.Key).Card;
            Assert.That(runtimeCard.DisplayName, Is.EqualTo(runtimeDefinition.DisplayName));
            Assert.That(runtimeCard.AbilityDescription, Is.EqualTo(runtimeDefinition.Description));
        }

        [Test]
        public void BA04_PlayerTurnShowsFreeChangeAction()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanHit, Is.True);
            Assert.That(model.CanStand, Is.True);
            Assert.That(model.CanChange, Is.True);
            Assert.That(model.IsChoosingChangeCard, Is.False);
            Assert.That(model.ChangeCandidates, Is.Empty);
            Assert.That(model.ChangeActionText, Is.EqualTo("CHANGE (FREE | 12 SOUL LEFT)"));
        }

        [Test]
        public void BA04_ChoosingChangeShowsCandidatesAndDisablesGeneralActions()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerChange();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanChange, Is.False);
            Assert.That(model.IsChoosingChangeCard, Is.True);
            Assert.That(model.ChangeCandidates, Is.EqualTo(new[] { "4", "9" }));
            Assert.That(model.PlayerCards, Is.EqualTo("10"));
            Assert.That(model.EnemyCards, Is.EqualTo("10  ?"));
        }

        [Test]
        public void BA04_CompletedChangeShowsUsedStateAndClearsCandidates()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerChange();
            battle.TrySelectChangedCard(0);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanHit, Is.True);
            Assert.That(model.CanStand, Is.True);
            Assert.That(model.CanChange, Is.True);
            Assert.That(model.IsChoosingChangeCard, Is.False);
            Assert.That(model.ChangeCandidates, Is.Empty);
            Assert.That(model.ChangeActionText, Is.EqualTo("CHANGE (-1 SOUL | 11 LEFT)"));
        }

        [Test]
        public void BA04_LastSoulRuleDisablesPaidChangeAndShowsRequiredSoul()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 10, 2, 4, 9 },
                enemyRanks: new[] { 10, 7 },
                playerMaximumSoul: 1,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerChange();
            battle.TrySelectChangedCard(0);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.CanChange, Is.False);
            Assert.That(model.ChangeActionText, Is.EqualTo("CHANGE (-1 SOUL | NEED 2+)"));
        }

        [Test]
        public void BA04_ControllerForwardsBothChangeSteps()
        {
            GameObject gameObject = CreateControllerObject(out CoreLoopController controller);
            try
            {
                controller.RequestBeginChange();

                Assert.That(
                    controller.Battle.State,
                    Is.EqualTo(CoreLoopState.PlayerChoosingChangeCard));
                Assert.That(controller.CurrentViewModel.IsChoosingChangeCard, Is.True);
                Assert.That(controller.CurrentViewModel.ChangeCandidates.Count, Is.EqualTo(2));

                controller.RequestSelectChangedCard(0);

                Assert.That(controller.Battle.CompletedPlayerChangeCount, Is.EqualTo(1));
                Assert.That(controller.CurrentViewModel.IsChoosingChangeCard, Is.False);
                Assert.That(
                    controller.CurrentViewModel.ChangeActionText,
                    Is.EqualTo("CHANGE (-1 SOUL | 11 LEFT)"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CU05_PresenterShowsCardUseStateAndDisabledReason()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 5, 7, 8 },
                enemyRanks: new[] { 10, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.PlayerCardActions.Count, Is.EqualTo(2));
            PlayerCardViewModel plainCard = model.PlayerCardActions[0];
            Assert.That(plainCard.Rank, Is.EqualTo(2));
            Assert.That(plainCard.CanUse, Is.False);
            Assert.That(
                plainCard.UnavailableReason,
                Is.EqualTo(CardUseUnavailableReason.CardIsNotManual));
            Assert.That(plainCard.DisabledReason, Is.EqualTo("NO MANUAL EFFECT"));

            PlayerCardViewModel crystalOrb = model.PlayerCardActions[1];
            Assert.That(crystalOrb.Rank, Is.EqualTo(5));
            Assert.That(crystalOrb.DisplayName, Is.EqualTo("수정 구슬"));
            Assert.That(crystalOrb.IsFaceUp, Is.False);
            Assert.That(crystalOrb.UseState, Is.EqualTo(CardUseState.Available));
            Assert.That(crystalOrb.CanUse, Is.True);
            Assert.That(crystalOrb.UnavailableReason, Is.EqualTo(CardUseUnavailableReason.None));
            Assert.That(crystalOrb.DisabledReason, Is.Empty);
        }

        [Test]
        public void CU05_PresenterShowsOnlyEffectChoicesWhileSelectionIsPending()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 2 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            battle.TryBeginPlayerCardUse(battle.Player.Hand.Cards[0].Id);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);

            Assert.That(model.IsResolvingCardEffect, Is.True);
            Assert.That(model.IsChoosingChangeCard, Is.False);
            Assert.That(model.CanHit, Is.False);
            Assert.That(model.CanStand, Is.False);
            Assert.That(model.CanChange, Is.False);
            Assert.That(
                model.SelectionPrompt?.Id,
                Is.EqualTo(CombatPromptId.ManualAutoPistolDeclareNumber));
            Assert.That(
                model.CardEffectChoices.Select(choice => choice.OptionId),
                Is.EqualTo(Enumerable.Range(1, 10)));
            Assert.That(model.PlayerCardActions.All(card => !card.CanUse), Is.True);
            Assert.That(
                model.PlayerCardActions.All(
                    card => card.UnavailableReason ==
                        CardUseUnavailableReason.EffectInProgress),
                Is.True);
        }

        [Test]
        public void CU05_GameSceneShowsHammerAsOpponentTargetingEffect()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 6, 3 },
                enemyRanks: new[] { 10, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            GameSceneViewModel model = GameScenePresenter.Create(battle);
            GameSceneCardViewModel sourceModel = model.PlayerCards.Single(
                card => card.CardId == sourceCard.Id);
            Assert.That(
                model.EnemyVisual,
                Is.EqualTo(CharacterVisualState.UseCard));
            Assert.That(model.EnemyActionLabel, Is.Empty);
            Assert.That(
                sourceModel.AbilityDescription,
                Is.EqualTo("상대 공개 카드 1장을 버립니다. 상대가 <b><color=#FF9800>스탠드</color></b>했다면 <b><color=#FF9800>스탠드</color></b>를 취소하고 비공개 카드도 교체합니다."));
        }

        [Test]
        public void CU05_GameSceneCreatesPlayerHammerReadyCueWhileChoosingTarget()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 6, 3 },
                enemyRanks: new[] { 10, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            GameSceneHammerAnimationCue cue =
                GameScenePresenter.Create(battle).HammerAnimationCue;
            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.RoundNumber, Is.EqualTo(1));
            Assert.That(cue.SourceCardId, Is.EqualTo(sourceCard.Id));
            Assert.That(cue.ActorSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(cue.Phase, Is.EqualTo(GameSceneHammerAnimationPhase.Ready));
            Assert.That(
                cue.ActionOrdinal,
                Is.GreaterThan(0));
            Assert.That(cue.TargetCardId, Is.Null);
        }

        [Test]
        public void CU05_GameSceneCreatesPlayerHammerAnimationCueWhenHammerResolves()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 6, 3 },
                enemyRanks: new[] { 10, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[1];
            BlackjackCard targetCard = battle.Enemy.Hand.Cards[0];
            battle.TryBeginPlayerCardUse(sourceCard.Id);

            GameSceneHammerAnimationCue cue = null;
            CharacterVisualState? targetVisual = null;
            battle.Stepped += () =>
            {
                GameSceneViewModel currentModel =
                    GameScenePresenter.Create(battle);
                GameSceneHammerAnimationCue currentCue =
                    currentModel.HammerAnimationCue;
                if (currentCue != null &&
                    currentCue.Phase == GameSceneHammerAnimationPhase.Smash)
                {
                    cue = currentCue;
                    targetVisual = currentModel.EnemyVisual;
                }
            };

            Assert.That(battle.TryResolvePlayerCardChoice(targetCard.Id), Is.True);

            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.RoundNumber, Is.EqualTo(1));
            Assert.That(cue.SourceCardId, Is.EqualTo(sourceCard.Id));
            Assert.That(cue.ActorSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(cue.Phase, Is.EqualTo(GameSceneHammerAnimationPhase.Smash));
            Assert.That(
                cue.ActionOrdinal,
                Is.GreaterThan(0));
            Assert.That(cue.TargetCardId, Is.EqualTo(targetCard.Id));
            Assert.That(
                targetVisual,
                Is.EqualTo(CharacterVisualState.UseCard));
        }

        [Test]
        public void CU05_GameSceneCreatesEnemyHammerAnimationCueWhenHammerResolves()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(10, 7, 3, 2),
                CreateDefinitionDeck(
                    "threat-hammer-6",
                    "standard-plain-4",
                    "standard-plain-3"),
                enemyMaximumSoul: 5,
                enemyPolicy: new EnforcerEnemyPolicy());
            battle.Start();
            BlackjackCard sourceCard = battle.Enemy.Hand.Cards[0];
            BlackjackCard targetCard = battle.Player.Hand.Cards[0];

            GameSceneHammerAnimationCue cue = null;
            battle.Stepped += () =>
            {
                GameSceneHammerAnimationCue currentCue =
                    GameScenePresenter.Create(battle).HammerAnimationCue;
                if (currentCue != null &&
                    currentCue.Phase == GameSceneHammerAnimationPhase.Smash)
                {
                    cue = currentCue;
                }
            };

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.RoundNumber, Is.EqualTo(1));
            Assert.That(cue.SourceCardId, Is.EqualTo(sourceCard.Id));
            Assert.That(cue.ActorSide, Is.EqualTo(CombatantSide.Enemy));
            Assert.That(cue.Phase, Is.EqualTo(GameSceneHammerAnimationPhase.Smash));
            Assert.That(
                cue.ActionOrdinal,
                Is.GreaterThan(0));
            Assert.That(cue.TargetCardId, Is.EqualTo(targetCard.Id));
        }

        [Test]
        public void CU05_PresenterShowsUsedCardAndSafeRecentEffectResult()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 2 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];
            battle.TryBeginPlayerCardUse(sourceCard.Id);
            battle.TryResolvePlayerCardChoice(6);

            CoreLoopViewModel model = CoreLoopPresenter.Create(battle);
            PlayerCardViewModel sourceModel = model.PlayerCardActions.Single(
                card => card.CardId == sourceCard.Id);

            Assert.That(sourceModel.UseState, Is.EqualTo(CardUseState.Used));
            Assert.That(sourceModel.CanUse, Is.False);
            Assert.That(sourceModel.DisabledReason, Is.EqualTo("USED"));
            Assert.That(model.LastCardEffect, Is.EqualTo(
                "REVOLVER  |  FAILED  |  ENEMY TURN"));
            Assert.That(model.LastCardEffect, Does.Not.Contain("7"));
            Assert.That(model.EnemyCards, Does.Contain("?"));
        }

        [Test]
        public void CUM10_U01_PlayerUsedCardProjectsUsedMarkState()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 2 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(6), Is.True);

            GameSceneViewModel model = GameScenePresenter.Create(battle);
            Assert.That(
                model.PlayerCards.Single(card => card.CardId == sourceCard.Id).IsUsed,
                Is.True);
            Assert.That(
                model.PlayerCards.Where(card => card.CardId != sourceCard.Id)
                    .All(card => !card.IsUsed),
                Is.True);
        }

        [Test]
        public void CUM10_U02_EnemyUsedMarkRequiresPublicFaceUpCard()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 2, 2 },
                enemyRanks: new[] { 7, 7 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard faceUpCard = battle.Enemy.Hand.Cards[0];
            BlackjackCard hiddenCard = battle.Enemy.Hand.Cards[1];
            Assert.That(faceUpCard.TryBeginUse(), Is.True);
            Assert.That(faceUpCard.TryCompleteUse(), Is.True);
            Assert.That(hiddenCard.TryBeginUse(), Is.True);
            Assert.That(hiddenCard.TryCompleteUse(), Is.True);

            GameSceneViewModel model = GameScenePresenter.Create(battle);
            Assert.That(
                model.EnemyCards.Single(card => card.CardId == faceUpCard.Id).IsUsed,
                Is.True);
            Assert.That(
                model.EnemyCards.Single(card => card.CardId == hiddenCard.Id).IsUsed,
                Is.False);
        }

        [Test]
        public void CUM10_U03_NonUsedCardStatesDoNotProjectUsedMark()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 2 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];

            GameSceneViewModel availableModel = GameScenePresenter.Create(battle);
            Assert.That(availableModel.PlayerCards.All(card => !card.IsUsed), Is.True);

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);
            GameSceneViewModel resolvingModel = GameScenePresenter.Create(battle);
            Assert.That(resolvingModel.PlayerCards.All(card => !card.IsUsed), Is.True);
        }

        [Test]
        public void CUM06_U01_GameSceneCreatesPlayerRevolverReadyCueWhileChoosingNumber()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 2 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            GameSceneRevolverAnimationCue cue =
                GameScenePresenter.Create(battle).RevolverAnimationCue;
            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.RoundNumber, Is.EqualTo(1));
            Assert.That(cue.SourceCardId, Is.EqualTo(sourceCard.Id));
            Assert.That(cue.ActorSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(cue.Phase,
                Is.EqualTo(GameSceneRevolverAnimationPhase.Ready));
            Assert.That(
                GameScenePresenter.Create(battle).EnemyVisual,
                Is.EqualTo(CharacterVisualState.AttackThreatened));
        }

        [Test]
        [Category("CUM06")]
        public void CUM06_U02_PlayerRevolverReadyHoldsInputUntilSelectionReady()
        {
            Assert.That(
                GameManager.ShouldHoldInputForRevolverReady(
                    readyActive: true,
                    selectionReady: false,
                    CombatantSide.Player),
                Is.True);
            Assert.That(
                GameManager.ShouldHoldInputForRevolverReady(
                    readyActive: true,
                    selectionReady: true,
                    CombatantSide.Player),
                Is.False);
            Assert.That(
                GameManager.ShouldHoldInputForRevolverReady(
                    readyActive: true,
                    selectionReady: false,
                    CombatantSide.Enemy),
                Is.False);
            Assert.That(
                GameManager.ShouldHoldInputForRevolverReady(
                    readyActive: false,
                    selectionReady: false,
                    CombatantSide.Player),
                Is.False);
        }

        [Test]
        [Category("CUM06")]
        public void CUM06_U04_PlayerReadyWithoutStepsQueuesPresentationSnapshot()
        {
            var playerReady = new GameSceneRevolverAnimationCue(
                roundNumber: 1,
                sourceCardId: 7,
                CombatantSide.Player,
                GameSceneRevolverAnimationPhase.Ready);
            var enemyReady = new GameSceneRevolverAnimationCue(
                roundNumber: 1,
                sourceCardId: 8,
                CombatantSide.Enemy,
                GameSceneRevolverAnimationPhase.Ready);
            var resolved = new GameSceneRevolverAnimationCue(
                roundNumber: 1,
                sourceCardId: 7,
                CombatantSide.Player,
                GameSceneRevolverAnimationPhase.Resolved,
                succeeded: true);

            Assert.That(
                GameManager.ShouldQueuePlayerRevolverReadySnapshot(
                    timelineCount: 0,
                    playerReady),
                Is.True);
            Assert.That(
                GameManager.ShouldQueuePlayerRevolverReadySnapshot(
                    timelineCount: 1,
                    playerReady),
                Is.False);
            Assert.That(
                GameManager.ShouldQueuePlayerRevolverReadySnapshot(
                    timelineCount: 0,
                    enemyReady),
                Is.False);
            Assert.That(
                GameManager.ShouldQueuePlayerRevolverReadySnapshot(
                    timelineCount: 0,
                    resolved),
                Is.False);
        }

        [Test]
        public void CUM14_U01_KnifeShowsThreatenedBeforeSafeDrawResolvesAsMiss()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 9, 2 },
                enemyRanks: new[] { 5, 1, 2 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];
            GameSceneViewModel readyModel = null;
            GameSceneViewModel resolvedModel = null;
            battle.Stepped += () =>
            {
                GameSceneViewModel model = GameScenePresenter.Create(battle);
                if (model.KnifeAnimationCue?.Phase == GameSceneKnifeAnimationPhase.Ready)
                {
                    readyModel = model;
                }
                else if (model.KnifeAnimationCue?.Phase == GameSceneKnifeAnimationPhase.Resolved)
                {
                    resolvedModel = model;
                }
            };

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            Assert.That(readyModel, Is.Not.Null);
            Assert.That(
                readyModel.EnemyVisual,
                Is.EqualTo(CharacterVisualState.AttackThreatened));
            Assert.That(resolvedModel, Is.Not.Null);
            Assert.That(resolvedModel.KnifeAnimationCue.Succeeded, Is.False);
            Assert.That(resolvedModel.EnemyVisual, Is.Not.EqualTo(CharacterVisualState.Attacked));
            Assert.That(resolvedModel.EnemyActionLabel, Is.Empty);
        }

        [Test]
        public void CUM14_U02_KnifeBustResolvesAsHit()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 9, 2 },
                enemyRanks: new[] { 6, 1, 10, 10 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.Enemy.Draw(faceUp: true), Is.Not.Null);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];
            GameSceneViewModel resolvedModel = null;
            GameSceneViewModel comparisonModel = null;
            battle.Stepped += () =>
            {
                GameSceneViewModel model = GameScenePresenter.Create(battle);
                if (model.KnifeAnimationCue?.Phase == GameSceneKnifeAnimationPhase.Resolved)
                {
                    resolvedModel = model;
                }

                if (model.RoundComparisonPlan != null)
                {
                    comparisonModel = model;
                }
            };

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            Assert.That(resolvedModel, Is.Not.Null);
            Assert.That(resolvedModel.KnifeAnimationCue.Succeeded, Is.True);
            Assert.That(
                resolvedModel.EnemyVisual,
                Is.EqualTo(CharacterVisualState.Attacked));
            Assert.That(resolvedModel.EnemyActionLabel, Is.Empty);
            Assert.That(comparisonModel, Is.Not.Null);
            Assert.That(
                comparisonModel.RoundComparisonPlan.PlaybackMode,
                Is.EqualTo(RoundComparisonPlaybackMode.SkipForDirectBust));
        }

        [Test]
        public void CUM18_U01_SecondKnifeDoesNotReuseFirstResolvedCue()
        {
            var battle = new CoreLoopBattle(
                CreateDefinitionDeck(
                    "military-knife-9",
                    "military-knife-9",
                    "standard-plain-2",
                    "standard-plain-3"),
                CreateRankDeck(5, 1, 2, 3, 4, 5),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 5,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);

            BlackjackCard firstKnife = battle.Player.Hand.Cards
                .Single(card => card.IsFaceUp);
            BlackjackCard secondKnife = battle.Player.Hand.Cards
                .Single(card => !card.IsFaceUp);
            Assert.That(battle.TryBeginPlayerCardUse(firstKnife.Id), Is.True);

            var secondTimeline = new List<GameSceneViewModel>();
            battle.Stepped += () =>
                secondTimeline.Add(GameScenePresenter.Create(battle));
            Assert.That(battle.TryBeginPlayerCardUse(secondKnife.Id), Is.True);

            GameSceneKnifeAnimationCue[] cues = secondTimeline
                .Select(model => model.KnifeAnimationCue)
                .Where(cue => cue != null)
                .ToArray();
            Assert.That(cues, Is.Not.Empty);
            Assert.That(
                cues[0].Phase,
                Is.EqualTo(GameSceneKnifeAnimationPhase.Ready));
            Assert.That(
                cues.Select(cue => cue.ActionOrdinal).Distinct().Count(),
                Is.EqualTo(1));
        }

        [Test]
        public void CUM18_U02_SecondHiddenRevolverDoesNotReuseFirstResolvedCue()
        {
            var battle = new CoreLoopBattle(
                CreateDefinitionDeck(
                    "auto-pistol-7",
                    "auto-pistol-7",
                    "standard-plain-2",
                    "standard-plain-3"),
                CreateRankDeck(5, 1, 2, 3, 4, 5),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 5,
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);

            BlackjackCard firstRevolver = battle.Player.Hand.Cards
                .Single(card => card.IsFaceUp);
            BlackjackCard secondRevolver = battle.Player.Hand.Cards
                .Single(card => !card.IsFaceUp);
            Assert.That(
                battle.TryBeginPlayerCardUse(firstRevolver.Id),
                Is.True);
            Assert.That(battle.TryResolvePlayerCardChoice(6), Is.True);

            var secondTimeline = new List<GameSceneViewModel>();
            battle.Stepped += () =>
                secondTimeline.Add(GameScenePresenter.Create(battle));
            Assert.That(
                battle.TryBeginPlayerCardUse(secondRevolver.Id),
                Is.True);

            GameSceneRevolverAnimationCue[] cues = secondTimeline
                .Select(model => model.RevolverAnimationCue)
                .Where(cue => cue != null)
                .ToArray();
            Assert.That(
                cues.All(cue =>
                    cue.Phase == GameSceneRevolverAnimationPhase.Ready),
                Is.True);

            GameSceneRevolverAnimationCue readyCue =
                GameScenePresenter.Create(battle).RevolverAnimationCue;
            Assert.That(readyCue, Is.Not.Null);
            Assert.That(
                readyCue.Phase,
                Is.EqualTo(GameSceneRevolverAnimationPhase.Ready));
            Assert.That(
                readyCue.ActionOrdinal,
                Is.GreaterThan(0));
        }

        [Test]
        public void GSV24_U01_FirstRoundPoisonInjectionCreatesPresentationCue()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(2, 3, 4, 5, 6, 7),
                CreateRankDeck(2, 3, 4, 5, 6, 7),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 5,
                enemyPolicy: new StandPolicy(),
                injectsPoisonIntoPlayerDeckEachRound: true);

            Assert.That(battle.Start(), Is.True);
            GameSceneViewModel model = GameScenePresenter.Create(battle);

            Assert.That(model.PoisonInjectionAnimationCue, Is.Not.Null);
            Assert.That(
                model.PoisonInjectionAnimationCue.RoundNumber,
                Is.EqualTo(1));
        }

        [Test]
        public void CUM17_U02_KnifeTimelinePairsRevealWithResolvedThrow()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 9, 2 },
                enemyRanks: new[] { 5, 1, 2 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            Assert.That(battle.Start(), Is.True);
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];
            GameSceneViewModel baseline = GameScenePresenter.Create(battle);
            var timeline = new List<GameSceneViewModel>();
            battle.Stepped += () => timeline.Add(GameScenePresenter.Create(battle));

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            int concealedIndex = -1;
            int revealIndex = -1;
            for (int i = 0; i < timeline.Count; i++)
            {
                GameSceneViewModel previous = i == 0 ? baseline : timeline[i - 1];
                if (GameManager.IsKnifeConcealedCardBeat(previous, timeline[i]))
                {
                    concealedIndex = i;
                }

                if (GameManager.IsKnifeRevealBeat(previous, timeline[i]))
                {
                    revealIndex = i;
                }
            }

            Assert.That(concealedIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(revealIndex, Is.EqualTo(concealedIndex + 1));
            Assert.That(revealIndex + 1, Is.LessThan(timeline.Count));
            Assert.That(
                GameManager.IsMatchingKnifeResolvedBeat(
                    timeline[revealIndex],
                    timeline[revealIndex + 1]),
                Is.True);
            Assert.That(
                timeline[concealedIndex].EnemyTotalsText,
                Is.EqualTo(baseline.EnemyTotalsText));
            Assert.That(
                timeline[revealIndex].EnemyTotalsText,
                Is.Not.EqualTo(baseline.EnemyTotalsText));
        }

        [Test]
        public void CUM17_U03_HiddenEnemyKnifePublishesReadyAndResolvedCues()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(5, 2, 3, 4, 6, 7),
                CreateDefinitionDeck(
                    "standard-plain-2",
                    "military-knife-9",
                    "standard-plain-3",
                    "standard-plain-4"),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3,
                enemyPolicy: new UseKnifeThenStandPolicy());
            Assert.That(battle.Start(), Is.True);
            GameSceneViewModel baseline = GameScenePresenter.Create(battle);
            Assert.That(
                baseline.EnemyCards.Single(card => card.CardId == 1).IsFaceUp,
                Is.False);
            var timeline = new List<GameSceneViewModel>();
            battle.Stepped += () => timeline.Add(GameScenePresenter.Create(battle));

            Assert.That(battle.TryPlayerHit(), Is.True);

            GameSceneViewModel ready = timeline.FirstOrDefault(model =>
                model.KnifeAnimationCue?.ActorSide == CombatantSide.Enemy &&
                model.KnifeAnimationCue.Phase ==
                    GameSceneKnifeAnimationPhase.Ready);
            GameSceneViewModel resolved = timeline.FirstOrDefault(model =>
                model.KnifeAnimationCue?.ActorSide == CombatantSide.Enemy &&
                model.KnifeAnimationCue.Phase ==
                    GameSceneKnifeAnimationPhase.Resolved);
            Assert.That(ready, Is.Not.Null);
            Assert.That(resolved, Is.Not.Null);
            Assert.That(
                GameManager.IsMatchingKnifeResolvedBeat(ready, resolved),
                Is.True);
        }

        [Test]
        [Category("GSDECK")]
        public void GSDECK_U01_DeckCountsStayBoundToTimelineSnapshot()
        {
            var battle = new CoreLoopBattle(
                CreateRankDeck(10, 10, 2, 3, 4, 5),
                CreateRankDeck(10, 10, 2, 3, 4, 5),
                enemyPolicy: new StandPolicy());
            Assert.That(battle.Start(), Is.True);

            GameSceneViewModel baseline = GameScenePresenter.Create(battle);
            var timeline = new List<GameSceneViewModel>();
            battle.Stepped += () => timeline.Add(GameScenePresenter.Create(battle));

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(timeline.Count, Is.GreaterThanOrEqualTo(2));
            Assert.That(baseline.PlayerDiscardPileCount, Is.Zero);
            Assert.That(baseline.EnemyDiscardPileCount, Is.Zero);
            Assert.That(timeline[0].PlayerDiscardPileCount, Is.Zero);
            Assert.That(timeline[0].EnemyDiscardPileCount, Is.Zero);
            Assert.That(
                timeline.Any(model => model.PlayerDiscardPileCount > 0),
                Is.True);
            Assert.That(
                timeline.Any(model => model.EnemyDiscardPileCount > 0),
                Is.True);
        }

        [Test]
        public void CU05_GameSceneCreatesRevolverAnimationCueWhenRevolverResolves()
        {
            CoreLoopBattle battle = CreateBattle(
                playerRanks: new[] { 7, 2 },
                enemyRanks: new[] { 5, 7, 5 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 3);
            battle.Start();
            BlackjackCard sourceCard = battle.Player.Hand.Cards[0];
            battle.TryBeginPlayerCardUse(sourceCard.Id);

            GameSceneRevolverAnimationCue cue = null;
            CharacterVisualState? targetVisual = null;
            battle.Stepped += () =>
            {
                GameSceneViewModel currentModel =
                    GameScenePresenter.Create(battle);
                GameSceneRevolverAnimationCue currentCue =
                    currentModel.RevolverAnimationCue;
                if (currentCue != null)
                {
                    cue = currentCue;
                    targetVisual = currentModel.EnemyVisual;
                }
            };

            Assert.That(battle.TryResolvePlayerCardChoice(6), Is.True);

            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.RoundNumber, Is.EqualTo(1));
            Assert.That(cue.SourceCardId, Is.EqualTo(sourceCard.Id));
            Assert.That(cue.ActorSide, Is.EqualTo(CombatantSide.Player));
            Assert.That(cue.Phase,
                Is.EqualTo(GameSceneRevolverAnimationPhase.Resolved));
            Assert.That(cue.Succeeded, Is.False);
            // The revolver keeps the target's threatened expression through both outcomes —
            // it never flips back to neutral just because the shot missed.
            Assert.That(
                targetVisual,
                Is.EqualTo(CharacterVisualState.AttackThreatened));
        }

        [Test]
        public void CU05_ControllerForwardsStandaloneCardUseAndChoice()
        {
            GameObject gameObject = CreateControllerObject(out CoreLoopController controller);
            try
            {
                var session = new CoreLoopSession(() => CreateBattle(
                    playerRanks: new[] { 2, 5, 7, 8 },
                    enemyRanks: new[] { 10, 7, 5 },
                    playerMaximumSoul: 12,
                    enemyMaximumSoul: 3));
                ReplaceControllerSession(controller, session);
                BlackjackCard sourceCard = controller.Battle.Player.Hand.Cards[1];

                controller.RequestBeginCardUse(sourceCard.Id);

                Assert.That(
                    controller.Battle.State,
                    Is.EqualTo(CoreLoopState.PlayerResolvingCardEffect));
                Assert.That(controller.CurrentViewModel.IsResolvingCardEffect, Is.True);
                Assert.That(controller.CurrentViewModel.CardEffectChoices.Count, Is.EqualTo(3));

                controller.RequestResolveCardChoice(0);

                Assert.That(sourceCard.UseState, Is.EqualTo(CardUseState.Used));
                Assert.That(controller.CurrentViewModel.IsResolvingCardEffect, Is.False);
                Assert.That(controller.CurrentViewModel.LastCardEffect, Does.Contain("SUCCESS"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static GameObject CreateControllerObject(out CoreLoopController controller)
        {
            var gameObject = new GameObject("BA04 Controller Test");
            gameObject.AddComponent<CoreLoopView>();
            controller = gameObject.AddComponent<CoreLoopController>();
            if (controller.Battle == null)
            {
                MethodInfo awake = typeof(CoreLoopController).GetMethod(
                    "Awake",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                awake.Invoke(controller, null);
            }

            return gameObject;
        }

        private static void ReplaceControllerSession(
            CoreLoopController controller,
            CoreLoopSession session)
        {
            SetPrivateField(controller, "_stageSession", null);
            SetPrivateField(controller, "_session", session);
            MethodInfo refreshView = typeof(CoreLoopController).GetMethod(
                "RefreshView",
                BindingFlags.Instance | BindingFlags.NonPublic);
            refreshView.Invoke(controller, null);
        }

        private static void SetPrivateField(
            CoreLoopController controller,
            string fieldName,
            object value)
        {
            FieldInfo field = typeof(CoreLoopController).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(controller, value);
        }

        private static void AssertInitialState(CoreLoopBattle battle, int iteration)
        {
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn), $"State {iteration}");
            Assert.That(battle.RoundNumber, Is.EqualTo(1), $"Round {iteration}");
            Assert.That(battle.Player.Soul.Current, Is.EqualTo(12), $"Player soul {iteration}");
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(1), $"Enemy soul {iteration}");
            Assert.That(battle.Player.Hand.Count, Is.EqualTo(2), $"Player hand {iteration}");
            Assert.That(battle.Enemy.Hand.Count, Is.EqualTo(2), $"Enemy hand {iteration}");
            Assert.That(battle.LastResolution.HasValue, Is.False, $"Last result {iteration}");
        }

        private static CoreLoopBattle CreatePlayerVictoryBattle()
        {
            return CreateBattle(
                playerRanks: new[] { 10, 1 },
                enemyRanks: new[] { 10, 10 },
                playerMaximumSoul: 12,
                enemyMaximumSoul: 1);
        }

        private static CoreLoopBattle CreatePlayerDefeatBattle()
        {
            return CreateBattle(
                playerRanks: new[] { 10, 8 },
                enemyRanks: new[] { 10, 10 },
                playerMaximumSoul: 1,
                enemyMaximumSoul: 3);
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
                playerMaximumSoul,
                enemyMaximumSoul);
        }

        private static BlackjackDeck CreateRankDeck(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static BlackjackDeck CreateDefinitionDeck(params string[] definitionKeys)
        {
            var cards = new List<BlackjackCard>(definitionKeys.Length);
            for (int i = 0; i < definitionKeys.Length; i++)
            {
                cards.Add(new BlackjackCard(
                    i,
                    CardDefinitionCatalog.GetByKey(definitionKeys[i])));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static BlackjackDeck CreateDeck(IReadOnlyList<int> ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Count);
            for (int i = 0; i < ranks.Count; i++)
            {
                cards.Add(new BlackjackCard(i, ranks[i]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private sealed class StandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                return new EnemyDecision(EnemyActionType.Stand, "test-stand");
            }
        }

        private sealed class UseKnifeThenStandPolicy : IEnemyBehaviorPolicy
        {
            public EnemyDecision Decide(EnemyObservation observation)
            {
                foreach (EnemyActionCandidate candidate in
                    observation.ActionCandidates)
                {
                    if (candidate.ActionType == EnemyActionType.UseCard &&
                        !candidate.CardEffectOptionId.HasValue &&
                        CardDefinitionCatalog
                            .GetByKey(candidate.CardDefinitionKey)
                            .Effect == CardEffectKind.MilitaryKnife)
                    {
                        return EnemyDecision.FromCandidate(
                            candidate,
                            "test-hidden-enemy-knife");
                    }
                }

                foreach (EnemyActionCandidate candidate in
                    observation.ActionCandidates)
                {
                    if (candidate.ActionType == EnemyActionType.Stand)
                    {
                        return EnemyDecision.FromCandidate(
                            candidate,
                            "test-stand-after-knife");
                    }
                }

                throw new InvalidOperationException(
                    "Enemy has no knife or stand candidate.");
            }
        }
    }
}
