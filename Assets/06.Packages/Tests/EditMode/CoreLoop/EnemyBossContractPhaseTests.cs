using System.Collections.Generic;
using System.Linq;
using DiaBlackJack.StageProgression;
using DiaBlackJack.CoreLoop.UI;
using NUnit.Framework;

namespace DiaBlackJack.CoreLoop.Tests
{
    public sealed class EnemyBossContractPhaseTests
    {
        [Test]
        public void EPR06_U01_FinalBossProfileDefinesFixedContractPhases()
        {
            EnemyCombatProfile profile = EnemyCombatProfileCatalog.Default.GetByKey(
                EnemyCombatProfileCatalog.FinalBossKey);

            Assert.That(profile.FixedDemonContractPhases.Count, Is.EqualTo(3));
            Assert.That(
                profile.FixedDemonContractPhases.Select(phase =>
                    phase.ActivationSoulThreshold),
                Is.EqualTo(new int?[] { null, 5, 2 }));
            Assert.That(
                profile.FixedDemonContractPhases.Select(phase =>
                    phase.ActiveDefinitionKey),
                Is.EqualTo(new[]
                {
                    DemonContractCatalog.BaphometKey,
                    DemonContractCatalog.AsmodeusKey,
                    DemonContractCatalog.AzazelKey
                }));
            Assert.That(
                profile.FixedDemonContractPhases.Select(phase =>
                    phase.DiscardedDefinitionKey),
                Is.EqualTo(new[]
                {
                    DemonContractCatalog.MammonKey,
                    DemonContractCatalog.BeelzebubKey,
                    DemonContractCatalog.SatanKey
                }));
        }

        [Test]
        public void EPR06_U02_BattleStartActivatesBaphometWithoutSoulOrBaseUseCost()
        {
            CoreLoopBattle battle = CreateBossBattle();

            Assert.That(battle.Start(), Is.True);

            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(8));
            Assert.That(battle.FixedEnemyDemonContractPhaseNumber, Is.EqualTo(1));
            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.Zero);
            Assert.That(
                battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Baphomet));
            Assert.That(
                battle.EnemyDemonDeck.ContainsDiscardedDefinitionKey(
                    DemonContractCatalog.MammonKey),
                Is.True);
        }

        [Test]
        public void EPR06_U03_SoulThresholdsReplaceContractAndDiscardCounterpart()
        {
            CoreLoopBattle battle = CreateBossBattle();
            battle.Start();

            battle.ApplySoulDamage(CombatantSide.Enemy, 3);

            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.FixedEnemyDemonContractPhaseNumber, Is.EqualTo(2));
            Assert.That(
                battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Asmodeus));
            Assert.That(
                battle.EnemyDemonDeck.ContainsDiscardedDefinitionKey(
                    DemonContractCatalog.BeelzebubKey),
                Is.True);

            battle.ApplySoulDamage(CombatantSide.Enemy, 3);

            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(battle.FixedEnemyDemonContractPhaseNumber, Is.EqualTo(3));
            Assert.That(
                battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Azazel));
            Assert.That(
                battle.EnemyDemonDeck.ContainsDiscardedDefinitionKey(
                    DemonContractCatalog.SatanKey),
                Is.True);
            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.Zero);
        }

        [Test]
        public void DCR07_I02_BossAsmodeusChoiceCompletesInsideEnemyTurn()
        {
            EnemyCombatProfile profile = EnemyCombatProfileCatalog.Default.GetByKey(
                EnemyCombatProfileCatalog.FinalBossKey);
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            DemonContractCard[] demonCards = profile.DemonContractDefinitionKeys
                .Select((key, index) =>
                    new DemonContractCard(index, catalog.GetByKey(key)))
                .ToArray();
            CoreLoopBattle battle = new CoreLoopBattle(
                CreateDeck(2, 2, 2, 2, 2, 2, 2, 2, 2, 2),
                CreateDeck(2, 2, 2, 2, 2, 2, 2, 2, 2, 2),
                enemyMaximumSoul: profile.MaximumSoul,
                enemyPolicy: new FinalBossEnemyPolicy(),
                enemyDemonDeck: new DemonContractDeck(demonCards, seed: 109),
                fixedEnemyDemonContractPhases:
                    profile.FixedDemonContractPhases);
            battle.Start();
            battle.ApplySoulDamage(CombatantSide.Enemy, 3);
            int asmodeusActionCount = battle.PublicActionHistory.Count(action =>
                action.SourceCardDefinitionKey ==
                    DemonContractCatalog.AsmodeusKey);

            Assert.That(battle.TryPlayerHit(), Is.True);

            Assert.That(battle.PendingEnemyDemonContractInteraction, Is.Null);
            Assert.That(battle.State, Is.EqualTo(CoreLoopState.PlayerTurn));
            Assert.That(
                battle.PublicActionHistory.Count(action =>
                    action.SourceCardDefinitionKey ==
                        DemonContractCatalog.AsmodeusKey),
                Is.EqualTo(asmodeusActionCount + 1));
        }

        [Test]
        public void EPR06_U04_OneDamageCrossingBothLaterThresholdsAppliesPhasesInOrder()
        {
            CoreLoopBattle battle = CreateBossBattle();
            battle.Start();

            battle.ApplySoulDamage(CombatantSide.Enemy, 6);

            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(2));
            Assert.That(battle.FixedEnemyDemonContractPhaseNumber, Is.EqualTo(3));
            Assert.That(
                battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Azazel));
            Assert.That(
                battle.PublicActionHistory
                    .Where(action =>
                        action.ActorSide == CombatantSide.Enemy &&
                        action.ActionType == PublicCombatActionType.DemonContract)
                    .Select(action => action.SourceCardDefinitionKey),
                Is.EqualTo(new[]
                {
                    DemonContractCatalog.AsmodeusKey,
                    DemonContractCatalog.AzazelKey
                }));
        }

        [Test]
        public void EPR06_U05_HealingDoesNotReverseOrRepeatFixedContractPhase()
        {
            CoreLoopBattle battle = CreateBossBattle();
            battle.Start();
            battle.ApplySoulDamage(CombatantSide.Enemy, 6);
            int actionCount = battle.PublicActionHistory.Count;

            battle.Enemy.Soul.Restore(6);
            battle.ApplySoulDamage(CombatantSide.Enemy, 1);

            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(7));
            Assert.That(battle.FixedEnemyDemonContractPhaseNumber, Is.EqualTo(3));
            Assert.That(
                battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Azazel));
            Assert.That(battle.PublicActionHistory.Count, Is.EqualTo(actionCount));
        }

        [Test]
        public void EPR06_U06_FixedSequenceDisablesNormalEnemyContractAction()
        {
            CoreLoopBattle battle = CreateBossBattle();
            battle.Start();

            Assert.That(battle.EnemyDemonContractAvailability.CanBegin, Is.False);
            Assert.That(
                battle.EnemyDemonContractAvailability.FailureReason,
                Is.EqualTo(DemonContractFailureReason.BaseUseLimitReached));
            Assert.That(battle.UsedEnemyBaseDemonContractCount, Is.Zero);
        }

        [Test]
        public void EPR06_U07_NonBossProfilesKeepNoFixedContractSequence()
        {
            foreach (EnemyCombatProfile profile in
                EnemyCombatProfileCatalog.Default.Profiles.Where(profile =>
                    profile.Key != EnemyCombatProfileCatalog.FinalBossKey))
            {
                Assert.That(
                    profile.FixedDemonContractPhases,
                    Is.Empty,
                    profile.Key);
            }
        }

        [Test]
        public void EPR06_U08_LethalDamageDoesNotActivateLaterBossPhases()
        {
            CoreLoopBattle battle = CreateBossBattle();
            battle.Start();

            battle.ApplySoulDamage(CombatantSide.Enemy, 8);

            Assert.That(battle.Enemy.Soul.IsDepleted, Is.True);
            Assert.That(battle.FixedEnemyDemonContractPhaseNumber, Is.EqualTo(1));
            Assert.That(
                battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Baphomet));
            Assert.That(
                battle.PublicActionHistory.Count(action =>
                    action.ActionType == PublicCombatActionType.DemonContract),
                Is.Zero);
        }

        [Test]
        public void EPR06_U09_PresentationShowsCurrentBossContractEffect()
        {
            CoreLoopBattle battle = CreateBossBattle();
            battle.Start();

            Assert.That(
                DemonContractPresenter.Create(battle).ActiveContracts.Single(),
                Is.EqualTo("상대 · 바포메트 · 오망성 덱 적용"));

            battle.ApplySoulDamage(CombatantSide.Enemy, 3);

            Assert.That(
                DemonContractPresenter.Create(battle).ActiveContracts.Single(),
                Is.EqualTo(
                    "상대 · 아스모데우스 · 숫자 7 이하 카드 제한 · 강제 히트 선택"));

            battle.ApplySoulDamage(CombatantSide.Enemy, 3);

            Assert.That(
                DemonContractPresenter.Create(battle).ActiveContracts.Single(),
                Is.EqualTo(
                    "상대 · 아자젤 · 중복 공개 숫자 버스트 · 수동 카드 재활성"));
        }

        [Test]
        public void EPR06_U10_RoundDamageAdvancesFixedContractPhase()
        {
            DemonContractCatalog catalog = DemonContractCatalog.Default;
            var phases = new[]
            {
                new FixedDemonContractPhaseDefinition(
                    activationSoulThreshold: null,
                    DemonContractCatalog.AsmodeusKey,
                    DemonContractCatalog.BeelzebubKey),
                new FixedDemonContractPhaseDefinition(
                    activationSoulThreshold: 5,
                    DemonContractCatalog.AzazelKey,
                    DemonContractCatalog.SatanKey)
            };
            var demonCards = new[]
            {
                new DemonContractCard(
                    0,
                    catalog.GetByKey(DemonContractCatalog.AsmodeusKey)),
                new DemonContractCard(
                    1,
                    catalog.GetByKey(DemonContractCatalog.BeelzebubKey)),
                new DemonContractCard(
                    2,
                    catalog.GetByKey(DemonContractCatalog.AzazelKey)),
                new DemonContractCard(
                    3,
                    catalog.GetByKey(DemonContractCatalog.SatanKey))
            };
            CoreLoopBattle battle = new CoreLoopBattle(
                CreateDeck(10, 9, 4, 4),
                CreateDeck(10, 6, 2, 4, 4),
                enemyMaximumSoul: 6,
                enemyDemonDeck: new DemonContractDeck(demonCards, seed: 107),
                fixedEnemyDemonContractPhases: phases);
            battle.Start();

            Assert.That(battle.TryPlayerStand(), Is.True);

            Assert.That(battle.LastResolution.Value.Outcome, Is.EqualTo(
                RoundOutcome.PlayerWin));
            Assert.That(battle.Enemy.Soul.Current, Is.EqualTo(5));
            Assert.That(battle.FixedEnemyDemonContractPhaseNumber, Is.EqualTo(2));
            Assert.That(
                battle.ActiveEnemyDemonContracts.Single().Kind,
                Is.EqualTo(DemonContractKind.Azazel));
        }

        private static CoreLoopBattle CreateBossBattle()
        {
            StageDefinition stage = StageDefinition.CreateForEnemyProfile(
                "epr06-boss",
                "EP-R06 Boss",
                StageKind.FinalBossCombat,
                EnemyCombatProfileCatalog.FinalBossKey,
                playerDeckSeed: 101,
                enemyDeckSeed: 103);
            return StageBattleFactory.Create(stage, CreateRunPlayer());
        }

        private static PlayerRunState CreateRunPlayer()
        {
            var cards = new List<RunCardDefinition>(20);
            for (int index = 0; index < 20; index++)
            {
                cards.Add(new RunCardDefinition(index, index % 10 + 1));
            }

            return new PlayerRunState(12, 12, cards);
        }

        private static BlackjackDeck CreateDeck(params int[] ranks)
        {
            var cards = new List<BlackjackCard>(ranks.Length);
            for (int index = 0; index < ranks.Length; index++)
            {
                cards.Add(new BlackjackCard(index, ranks[index]));
            }

            return BlackjackDeck.CreateInDrawOrder(cards);
        }
    }
}
