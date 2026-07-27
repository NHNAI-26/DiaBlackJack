using System;
using System.Collections.Generic;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    internal sealed class RunRestoreResult
    {
        internal RunRestoreResult(
            StageProgressionSession session,
            OpponentSelectionGenerator opponentSelectionGenerator,
            BattleRewardGenerator battleRewardGenerator,
            RunSaveSnapshot source)
        {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            OpponentSelectionGenerator = opponentSelectionGenerator ??
                throw new ArgumentNullException(nameof(opponentSelectionGenerator));
            BattleRewardGenerator = battleRewardGenerator ??
                throw new ArgumentNullException(nameof(battleRewardGenerator));
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            RootSeed = source.RootSeed;
            NextContentKind = source.NextContentKind;
            StartingDemonDefinitionKey =
                source.Player.StartingDemonDefinitionKey;
        }

        internal BattleRewardGenerator BattleRewardGenerator { get; }

        internal string NextContentKind { get; }

        internal OpponentSelectionGenerator OpponentSelectionGenerator { get; }

        internal int RootSeed { get; }

        internal StageProgressionSession Session { get; }

        internal string StartingDemonDefinitionKey { get; }
    }

    internal sealed class RunRestoreFactory
    {
        private readonly BattleRewardCatalog _battleRewardCatalog;
        private readonly EnemyCombatProfileCatalog _enemyCatalog;
        private readonly Func<int, IReadOnlyList<StageDefinition>> _stagePathFactory;

        internal RunRestoreFactory(
            Func<int, IReadOnlyList<StageDefinition>> stagePathFactory,
            EnemyCombatProfileCatalog enemyCatalog = null,
            BattleRewardCatalog battleRewardCatalog = null)
        {
            _stagePathFactory = stagePathFactory ??
                throw new ArgumentNullException(nameof(stagePathFactory));
            _enemyCatalog = enemyCatalog ?? EnemyCombatProfileCatalog.Default;
            _battleRewardCatalog = battleRewardCatalog ??
                BattleRewardCatalog.CreateDefault();
        }

        internal bool TryRestore(
            RunSaveSnapshot snapshot,
            out RunRestoreResult result,
            out RunSaveValidationResult validation)
        {
            result = null;
            if (snapshot == null)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.SnapshotMissing);
                return false;
            }

            IReadOnlyList<StageDefinition> stages;
            try
            {
                stages = _stagePathFactory(snapshot.RootSeed);
            }
            catch (ArgumentException)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.InvalidStage);
                return false;
            }
            catch (InvalidOperationException)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.InvalidStage);
                return false;
            }

            validation = RunSaveValidator.Validate(snapshot, stages);
            if (!validation.IsValid)
            {
                return false;
            }

            if (snapshot.Player.CurrentGold != 0)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.InvalidGold);
                return false;
            }

            StageProgressionState restoredState;
            if (!TryResolveStableState(snapshot, out restoredState))
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.UnstableState);
                return false;
            }

            if (snapshot.Random.OpponentOfferOrdinal >
                    CountOpponentSelectionStages(stages) ||
                snapshot.Random.BattleRewardOrdinal > stages.Count)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.InvalidRandomState);
                return false;
            }

            try
            {
                PlayerRunState player = RestorePlayer(snapshot.Player);
                RunProgress progress = RunProgress.Restore(
                    stages,
                    player,
                    snapshot.CurrentStageIndex,
                    restoredState);
                OpponentSelectionGenerator opponentGenerator =
                    RestoreOpponentGenerator(
                        snapshot.RootSeed,
                        snapshot.Random.OpponentOfferOrdinal,
                        stages);
                BattleRewardGenerator rewardGenerator = RestoreRewardGenerator(
                    snapshot.RootSeed,
                    snapshot.Random.BattleRewardOrdinal,
                    stages);
                StageProgressionSession session = new StageProgressionSession(
                    progress,
                    rewardGenerator: rewardGenerator,
                    opponentSelectionGenerator: opponentGenerator);

                result = new RunRestoreResult(
                    session,
                    opponentGenerator,
                    rewardGenerator,
                    snapshot);
                validation = RunSaveValidationResult.Valid();
                return true;
            }
            catch (ArgumentException)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.UnstableState);
                return false;
            }
            catch (InvalidOperationException)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.UnstableState);
                return false;
            }
            catch (KeyNotFoundException)
            {
                validation = RunSaveValidationResult.Invalid(
                    RunSaveValidationError.UnstableState);
                return false;
            }
        }

        private static int CountOpponentSelectionStages(
            IReadOnlyList<StageDefinition> stages)
        {
            int count = 0;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i].Kind != StageKind.FinalBossCombat)
                {
                    count++;
                }
            }

            return count;
        }

        private static PlayerRunState RestorePlayer(PlayerRunSaveSnapshot snapshot)
        {
            List<RunCardDefinition> cards =
                new List<RunCardDefinition>(snapshot.Cards.Count);
            for (int i = 0; i < snapshot.Cards.Count; i++)
            {
                RunSaveCardSnapshot card = snapshot.Cards[i];
                cards.Add(new RunCardDefinition(
                    card.Id,
                    card.DefinitionKey,
                    card.Suit));
            }

            List<RunDemonDefinition> demonCards =
                new List<RunDemonDefinition>(snapshot.DemonCards.Count);
            for (int i = 0; i < snapshot.DemonCards.Count; i++)
            {
                RunSaveDemonSnapshot card = snapshot.DemonCards[i];
                demonCards.Add(new RunDemonDefinition(card.Id, card.DefinitionKey));
            }

            return PlayerRunState.Restore(
                snapshot.MaximumSoul,
                snapshot.CurrentSoul,
                cards,
                demonCards,
                snapshot.LastIssuedCardId,
                snapshot.LastIssuedDemonCardId,
                snapshot.StartingDemonDefinitionKey);
        }

        private OpponentSelectionGenerator RestoreOpponentGenerator(
            int rootSeed,
            int ordinal,
            IReadOnlyList<StageDefinition> stages)
        {
            OpponentSelectionGenerator generator = new OpponentSelectionGenerator(
                _enemyCatalog,
                rootSeed);
            int generated = 0;
            for (int stageIndex = 0;
                 stageIndex < stages.Count && generated < ordinal;
                 stageIndex++)
            {
                if (stages[stageIndex].Kind == StageKind.FinalBossCombat)
                {
                    continue;
                }

                generator.Generate(stageIndex);
                generated++;
            }

            return generator;
        }

        private BattleRewardGenerator RestoreRewardGenerator(
            int rootSeed,
            int ordinal,
            IReadOnlyList<StageDefinition> stages)
        {
            BattleRewardGenerator generator = new BattleRewardGenerator(
                _battleRewardCatalog,
                unchecked(rootSeed + 1));
            for (int stageIndex = 0; stageIndex < ordinal; stageIndex++)
            {
                generator.Generate(SelectRewardTier(stages[stageIndex]));
            }

            return generator;
        }

        private BattleRewardTier SelectRewardTier(StageDefinition stage)
        {
            if (stage.Kind == StageKind.FinalBossCombat)
            {
                return BattleRewardTier.HighGrade;
            }

            if (stage.BattleProfileKey != null)
            {
                return _enemyCatalog
                    .GetPreviewByKey(stage.BattleProfileKey)
                    .ExpectedRewardTier;
            }

            return BattleRewardTier.Normal;
        }

        private static bool TryResolveStableState(
            RunSaveSnapshot snapshot,
            out StageProgressionState state)
        {
            if (snapshot.CheckpointKind == RunCheckpointKind.StartingDemonSelected &&
                snapshot.Status == RunSaveStatus.InProgress)
            {
                state = StageProgressionState.NotStarted;
                return true;
            }

            if (snapshot.CheckpointKind ==
                    RunCheckpointKind.CombatSettlementCompleted &&
                snapshot.Status == RunSaveStatus.InProgress)
            {
                state = StageProgressionState.StageCleared;
                return true;
            }

            if (snapshot.CheckpointKind == RunCheckpointKind.RunEnded)
            {
                if (snapshot.Status == RunSaveStatus.Victory)
                {
                    state = StageProgressionState.RunVictory;
                    return true;
                }

                if (snapshot.Status == RunSaveStatus.Defeat)
                {
                    state = StageProgressionState.RunDefeat;
                    return true;
                }
            }

            state = StageProgressionState.NotStarted;
            return false;
        }
    }
}
