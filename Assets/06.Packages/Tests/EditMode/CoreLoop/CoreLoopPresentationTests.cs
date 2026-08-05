using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                Is.EqualTo("숫자 하나를 선언합니다. 상대 비공개 카드와 일치하면 상대를 버스트시킵니다."));
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
                Is.EqualTo("상대 공개 카드 1장을 버립니다. 상대가 스탠드했다면 스탠드를 취소하고 비공개 카드도 교체합니다."));
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
                        "상대 비공개 카드와 일치하면 상대를 버스트시킵니다."));

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
                    "즉시 스탠드하거나 영혼 3을 겁니다. " +
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
            Assert.That(model.CardEffectPrompt, Is.Not.Empty);
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
                Is.EqualTo("상대 공개 카드 1장을 버립니다. 상대가 스탠드했다면 스탠드를 취소하고 비공개 카드도 교체합니다."));
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
            battle.Stepped += () =>
            {
                GameSceneViewModel model = GameScenePresenter.Create(battle);
                if (model.KnifeAnimationCue?.Phase == GameSceneKnifeAnimationPhase.Resolved)
                {
                    resolvedModel = model;
                }
            };

            Assert.That(battle.TryBeginPlayerCardUse(sourceCard.Id), Is.True);

            Assert.That(resolvedModel, Is.Not.Null);
            Assert.That(resolvedModel.KnifeAnimationCue.Succeeded, Is.True);
            Assert.That(
                resolvedModel.EnemyVisual,
                Is.EqualTo(CharacterVisualState.Attacked));
            Assert.That(resolvedModel.EnemyActionLabel, Is.Empty);
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
            Assert.That(
                targetVisual,
                Is.EqualTo(CharacterVisualState.UseCard));
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
    }
}
