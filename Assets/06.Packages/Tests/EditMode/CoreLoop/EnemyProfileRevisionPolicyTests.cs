using System;
using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.StageProgression;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyProfileRevisionPolicyTests
    {
        [Test]
        public void EPR05_U01_ProfilesDeclareChangeContractAndPoisonRules()
        {
            EnemyCombatProfile cultist = GetProfile(EnemyCombatProfileCatalog.CultistKey);
            EnemyCombatProfile trickster = GetProfile(EnemyCombatProfileCatalog.TricksterKey);
            EnemyCombatProfile enforcer = GetProfile(EnemyCombatProfileCatalog.EnforcerKey);

            Assert.That(
                cultist.DemonContractDefinitionKeys,
                Is.EqualTo(new[]
                {
                    DemonContractCatalog.BelphegorKey,
                    DemonContractCatalog.BeelzebubKey
                }));
            Assert.That(cultist.DemonContractCandidateCount, Is.EqualTo(2));
            Assert.That(trickster.ChangeCostMode, Is.EqualTo(EnemyChangeCostMode.FixedOne));
            Assert.That(enforcer.DemonContractDefinitionKeys, Is.Empty);
            Assert.That(enforcer.InjectsPoisonIntoPlayerDeckEachRound, Is.True);
        }

        [TestCase(14, EnemyActionType.UseCard)]
        [TestCase(15, EnemyActionType.Stand)]
        public void EPR05_U02_CowardlyGamblerUsesManualCardOnDeterministicFifteenPercent(
            int decisionSeed,
            EnemyActionType expectedAction)
        {
            EnemyObservation observation = CreateObservation(
                enemySoul: 3,
                decisionSeed,
                Array.Empty<PublicCardObservation>(),
                new[]
                {
                    new EnemyActionCandidate(EnemyActionType.Hit),
                    new EnemyActionCandidate(EnemyActionType.Stand),
                    new EnemyActionCandidate(
                        EnemyActionType.UseCard,
                        cardId: 7,
                        cardDefinitionKey: "crystal-orb-5")
                });

            EnemyDecision decision = new CowardlyGamblerEnemyPolicy().Decide(observation);

            Assert.That(decision.ActionType, Is.EqualTo(expectedAction));
        }

        [TestCase(1, 0, 1, DemonContractKind.Belphegor)]
        [TestCase(4, 0, 2, DemonContractKind.Beelzebub)]
        [TestCase(1, 2, 2, DemonContractKind.Beelzebub)]
        public void EPR07_U01_CultistChoosesContractOnlyByPublicCardCount(
            int enemySoul,
            int usablePlayerCardCount,
            int playerFaceUpCardCount,
            DemonContractKind expectedKind)
        {
            PublicCardObservation[] publicCards = Enumerable.Range(0, playerFaceUpCardCount)
                .Select(index => new PublicCardObservation(
                    "threat-hammer-6",
                    rank: 6,
                    canUse: index < usablePlayerCardCount))
                .ToArray();
            EnemyObservation observation = CreateObservation(
                enemySoul,
                decisionSeed: 31,
                publicCards,
                CreateContractChoiceCandidates());

            bool selected = new CultistEnemyPolicy().TryDecideForcedAction(
                observation,
                out EnemyDecision decision);

            Assert.That(selected, Is.True);
            EnemyActionCandidate selectedCandidate = observation.ActionCandidates
                .Single(candidate => candidate.DemonContractOptionId ==
                    decision.DemonContractOptionId);
            Assert.That(selectedCandidate.DemonContractKind, Is.EqualTo(expectedKind));
        }

        [Test]
        public void EPR05_U04_RevealedHiddenCardForcesChangeAndChoosesHighestSafeCandidate()
        {
            CoreLoopBattle battle = CreateChangeBattle(EnemyChangeCostMode.Accumulating);
            battle.Start();
            Assert.That(
                battle.Enemy.Hand.TryGetSingleHiddenCard(out BlackjackCard hiddenCard),
                Is.True);
            hiddenCard.Reveal();

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.CompletedEnemyChangeCount, Is.EqualTo(1));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(
                battle.Enemy.Hand.TryGetSingleHiddenCard(out BlackjackCard replacement),
                Is.True);
            Assert.That(replacement.Rank, Is.EqualTo(1));
            Assert.That(replacement.IsFaceUp, Is.False);
        }

        [Test]
        public void EPR05_U05_FullTotalAboveTwentyOneForcesChangeWithoutRevealingHiddenCard()
        {
            CoreLoopBattle battle = CreateChangeBattle(EnemyChangeCostMode.Accumulating);
            battle.Start();
            battle.Enemy.AddTemporaryFaceUpCard(
                1000,
                CardDefinitionCatalog.GetDefaultForRank(5));

            Assert.That(battle.Enemy.VisibleHandValue.Total, Is.EqualTo(15));
            Assert.That(battle.Enemy.HandValue.IsBust, Is.True);
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.CompletedEnemyChangeCount, Is.EqualTo(1));
            Assert.That(
                battle.Enemy.Hand.TryGetSingleHiddenCard(out BlackjackCard replacement),
                Is.True);
            Assert.That(replacement.Rank, Is.EqualTo(1));
            Assert.That(battle.Enemy.HandValue.Total, Is.EqualTo(16));
        }

        [Test]
        public void EPR05_U06_TricksterPaysFixedOneWhileCommonFirstChangeIsFree()
        {
            CoreLoopBattle common = CreateChangeBattle(EnemyChangeCostMode.Accumulating);
            CoreLoopBattle trickster = CreateChangeBattle(EnemyChangeCostMode.FixedOne);

            ExecuteRevealedHiddenChange(common);
            ExecuteRevealedHiddenChange(trickster);

            Assert.That(common.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(common.NextEnemyChangeSoulCost, Is.EqualTo(1));
            Assert.That(trickster.Enemy.Soul.Current, Is.EqualTo(4));
            Assert.That(trickster.NextEnemyChangeSoulCost, Is.EqualTo(1));
        }

        [Test]
        public void DCR07_U02_EnforcerHasNoDemonAndStillInjectsPoisonBeforeDeal()
        {
            CoreLoopBattle battle = CreateProfileBattle(EnemyCombatProfileCatalog.EnforcerKey);

            Assert.That(battle.Start(), Is.True);
            Assert.That(battle.InjectedPoisonCardCount, Is.EqualTo(1));
            Assert.That(battle.Player.Deck.TotalCardCount, Is.EqualTo(21));
            Assert.That(
                battle.Player.Deck.TryGetKnownCard(
                    1000000000,
                    out BlackjackCard injectedPoison),
                Is.True);
            Assert.That(injectedPoison.Suit, Is.EqualTo(CardSuit.Spade));
            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.Zero);
            Assert.That(battle.ActiveEnemyDemonContracts, Is.Empty);
        }

        [Test]
        public void EPR05_U08_PoisonAccumulatesEachRoundAndIsRemovedAtBattleEnd()
        {
            CoreLoopBattle continuingBattle = CreatePoisonBattle(enemyMaximumSoul: 5);
            continuingBattle.Start();

            Assert.That(continuingBattle.TryPlayerStand(), Is.True);
            Assert.That(continuingBattle.RoundNumber, Is.EqualTo(2));
            Assert.That(continuingBattle.InjectedPoisonCardCount, Is.EqualTo(2));
            Assert.That(continuingBattle.Player.Deck.TotalCardCount, Is.EqualTo(22));

            CoreLoopBattle endingBattle = CreatePoisonBattle(enemyMaximumSoul: 1);
            endingBattle.Start();
            Assert.That(endingBattle.TryPlayerStand(), Is.True);

            Assert.That(endingBattle.State, Is.EqualTo(CoreLoopState.BattleEnded));
            Assert.That(endingBattle.InjectedPoisonCardCount, Is.Zero);
            Assert.That(endingBattle.Player.Deck.TotalCardCount, Is.EqualTo(20));
        }

        [Test]
        public void EPR05_U09_ObservationCopiesPublicUsabilityWithoutHiddenIdentity()
        {
            CoreLoopBattle battle = CreateChangeBattle(EnemyChangeCostMode.Accumulating);
            battle.Start();

            EnemyObservation observation = EnemyObservationFactory.Create(battle, 71);

            Assert.That(observation.PlayerFaceUpCards.Count, Is.EqualTo(1));
            Assert.That(observation.PlayerFaceUpCards[0].CanUse, Is.False);
            Assert.That(observation.KnownPlayerHiddenCardRank, Is.Null);
            Assert.That(observation.PlayerHiddenCardCount, Is.EqualTo(1));
        }

        private static EnemyCombatProfile GetProfile(string key)
        {
            return EnemyCombatProfileCatalog.Default.GetByKey(key);
        }

        private static void ExecuteRevealedHiddenChange(CoreLoopBattle battle)
        {
            battle.Start();
            battle.Enemy.Hand.TryGetSingleHiddenCard(out BlackjackCard hiddenCard);
            hiddenCard.Reveal();
            Assert.That(battle.TryPlayerHit(), Is.True);
        }

        private static CoreLoopBattle CreateChangeBattle(EnemyChangeCostMode changeCostMode)
        {
            return new CoreLoopBattle(
                CreateDeck(2, 2, 2, 2, 2, 2, 2, 2),
                CreateDeck(10, 10, 9, 1, 2, 2, 2, 2),
                playerMaximumSoul: 12,
                enemyMaximumSoul: 5,
                enemyPolicy: new SimpleEnemyPolicy(),
                enemyChangeCostMode: changeCostMode,
                enablesEnemyChange: true);
        }

        private static CoreLoopBattle CreateProfileBattle(string profileKey)
        {
            StageDefinition stage = StageDefinition.CreateForEnemyProfile(
                "epr05-stage",
                "EP-R05",
                StageKind.NormalCombat,
                profileKey,
                playerDeckSeed: 41,
                enemyDeckSeed: 43);
            return StageBattleFactory.Create(stage, CreateRunPlayer());
        }

        private static PlayerRunState CreateRunPlayer()
        {
            var cards = new List<RunCardDefinition>(20);
            for (int index = 0; index < 20; index++)
            {
                cards.Add(new RunCardDefinition(index, rank: 2));
            }

            return new PlayerRunState(12, 12, cards);
        }

        private static CoreLoopBattle CreatePoisonBattle(int enemyMaximumSoul)
        {
            return new CoreLoopBattle(
                CreateUniformDeck(rank: 10),
                CreateUniformDeck(rank: 1),
                playerMaximumSoul: 12,
                enemyMaximumSoul,
                enemyPolicy: new SimpleEnemyPolicy(),
                injectsPoisonIntoPlayerDeckEachRound: true);
        }

        private static BlackjackDeck CreateUniformDeck(int rank)
        {
            var cards = new List<BlackjackCard>(20);
            for (int id = 0; id < 20; id++)
            {
                cards.Add(new BlackjackCard(id, rank));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static BlackjackDeck CreateDeck(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int id = 0; id < ranks.Length; id++)
            {
                cards.Add(new BlackjackCard(id, ranks[id]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }

        private static IReadOnlyList<EnemyActionCandidate>
            CreateContractChoiceCandidates()
        {
            return new[]
            {
                CreateContractCandidate(0, DemonContractKind.Belphegor),
                CreateContractCandidate(1, DemonContractKind.Beelzebub)
            };
        }

        private static EnemyActionCandidate CreateContractCandidate(
            int optionId,
            DemonContractKind kind)
        {
            return new EnemyActionCandidate(
                EnemyActionType.DemonContract,
                demonContractOptionId: optionId,
                demonContractInteractionKind:
                    DemonContractInteractionKind.ChooseContract,
                demonContractKind: kind,
                demonContractDefinitionKey:
                    DemonContractCatalog.Default.Definitions
                        .Single(definition => definition.Kind == kind)
                        .Key);
        }

        private static EnemyObservation CreateObservation(
            int enemySoul,
            int decisionSeed,
            IReadOnlyList<PublicCardObservation> playerFaceUpCards,
            IReadOnlyList<EnemyActionCandidate> candidates)
        {
            return new EnemyObservation(
                new HandValue(15),
                new[]
                {
                    new EnemyOwnedCardObservation(
                        0,
                        "crystal-orb-5",
                        5,
                        isFaceUp: true,
                        CardUseState.Available,
                        canUse: true),
                    new EnemyOwnedCardObservation(
                        1,
                        "military-knife-10",
                        10,
                        isFaceUp: true,
                        CardUseState.Available,
                        canUse: true)
                },
                playerFaceUpCards,
                playerHiddenCardCount: 1,
                new SoulObservation(12, 12),
                new SoulObservation(enemySoul, 5),
                roundNumber: 1,
                playerIsStanding: false,
                enemyIsStanding: false,
                ownDeckAvailableCount: 10,
                playerDeckAvailableCount: 10,
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCardObservation>(),
                Array.Empty<PublicCombatAction>(),
                candidates,
                Array.Empty<EnemyNumberInference>(),
                pendingCardEffectKind: null,
                decisionSeed);
        }
    }
}
