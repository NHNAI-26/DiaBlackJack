using System;
using DiaBlackJack.CoreLoop;

namespace DiaBlackJack.StageProgression
{
    public sealed class StageProgressionSession
    {
        private const int DefaultRewardSeed = 20260720;

        private readonly Func<StageDefinition, PlayerRunState, CoreLoopBattle> _battleFactory;
        private readonly BattleRewardGenerator _rewardGenerator;
        private readonly Func<StageDefinition, BattleRewardTier> _rewardTierSelector;
        private readonly GoldRewardCatalog _goldRewardCatalog;
        private readonly bool _usesBattleRewards;
        private readonly StartingDemonGrantGenerator _startingDemonGrantGenerator;
        private OpponentSelectionGenerator _opponentSelectionGenerator;
        private CoreLoopSession _battleSession;
        private CoreLoopBattle _processedBattle;

        public StageProgressionSession(
            RunProgress progress,
            Func<StageDefinition, PlayerRunState, CoreLoopBattle> battleFactory = null,
            BattleRewardGenerator rewardGenerator = null,
            Func<StageDefinition, BattleRewardTier> rewardTierSelector = null,
            OpponentSelectionGenerator opponentSelectionGenerator = null,
            StartingDemonGrantGenerator startingDemonGrantGenerator = null,
            GoldRewardCatalog goldRewardCatalog = null,
            bool usesBattleRewards = true)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _battleFactory = battleFactory ?? StageBattleFactory.Create;
            _rewardGenerator = rewardGenerator ?? new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                DefaultRewardSeed);
            _rewardTierSelector = rewardTierSelector ?? SelectDefaultRewardTier;
            _goldRewardCatalog = goldRewardCatalog ?? GoldRewardCatalog.CreatePrototype();
            _usesBattleRewards = usesBattleRewards;
            _opponentSelectionGenerator = opponentSelectionGenerator;
            _startingDemonGrantGenerator = startingDemonGrantGenerator;
            ActiveStage = opponentSelectionGenerator == null
                ? progress.CurrentStage
                : null;
        }

        public StageDefinition ActiveStage { get; private set; }

        public CoreLoopBattle Battle => _battleSession?.Battle;

        public bool IsOpponentSelectionEnabled => _opponentSelectionGenerator != null;

        public bool UsesBattleRewards => _usesBattleRewards;

        public OpponentSelectionOffer PendingOpponentSelection { get; private set; }

        public StartingDemonGrant PendingStartingDemonGrant { get; private set; }

        public RunProgress Progress { get; }

        internal event Action BattleResultSynchronized;

        internal int BattleRewardOrdinal => _rewardGenerator.NextOfferOrdinal;

        internal int OpponentOfferOrdinal =>
            _opponentSelectionGenerator?.NextOfferOrdinal ?? 0;

        public bool TryStartRun()
        {
            if (_startingDemonGrantGenerator != null &&
                Progress.Player.CanReceiveStartingDemonGrant)
            {
                if (Progress.State != StageProgressionState.NotStarted ||
                    PendingStartingDemonGrant != null)
                {
                    return false;
                }

                StartingDemonGrant grant =
                    _startingDemonGrantGenerator.Generate();
                string[] definitionKeys =
                {
                    grant.Cards[0].DefinitionKey,
                    grant.Cards[1].DefinitionKey
                };
                if (!Progress.Player.TryGrantStartingDemons(definitionKeys))
                {
                    throw new InvalidOperationException(
                        "Player state rejected a validated starting demon grant.");
                }

                PendingStartingDemonGrant = grant;
                return true;
            }

            if (PendingStartingDemonGrant != null)
            {
                return false;
            }

            if (!Progress.StartRun())
            {
                return false;
            }

            PrepareCurrentStage();
            return true;
        }

        public bool TryPlayerHit()
        {
            if (!CanForwardBattleAction() || !_battleSession.TryPlayerHit())
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryPlayerStand()
        {
            if (!CanForwardBattleAction() || !_battleSession.TryPlayerStand())
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryBeginPlayerChange()
        {
            if (!CanForwardBattleAction() || !_battleSession.TryBeginPlayerChange())
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TrySelectChangedCard(int candidateIndex)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TrySelectChangedCard(candidateIndex))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryBeginPlayerCardUse(int cardId)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryBeginPlayerCardUse(cardId))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryResolvePlayerCardChoice(int optionId)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryResolvePlayerCardChoice(optionId))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryCompleteStartingDemonReveal()
        {
            if (Progress.State != StageProgressionState.NotStarted ||
                PendingStartingDemonGrant == null ||
                !Progress.Player.StartingDemonGrantCompleted)
            {
                return false;
            }

            PendingStartingDemonGrant = null;
            return true;
        }

        public bool TryResolvePlayerAutomaticCardChoice(
            int interactionId,
            int optionId)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryResolvePlayerAutomaticCardChoice(
                    interactionId,
                    optionId))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryBeginPlayerDemonContract()
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryBeginPlayerDemonContract())
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryResolvePlayerDemonContract(int interactionId, int optionId)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryResolvePlayerDemonContract(interactionId, optionId))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryResolvePlayerSatanNumbers(
            int interactionId,
            int firstNumber,
            int secondNumber)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryResolvePlayerSatanNumbers(
                    interactionId,
                    firstNumber,
                    secondNumber))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryBeginPlayerMammonReroll(int sourceContractCardId)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryBeginPlayerMammonReroll(sourceContractCardId))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryBeginPlayerMammonReroll(
            int sourceContractCardId,
            int physicalDieValue)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryBeginPlayerMammonReroll(
                    sourceContractCardId,
                    physicalDieValue))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryBeginPlayerSatanContractAction(int sourceContractCardId)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryBeginPlayerSatanContractAction(sourceContractCardId))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryBeginPlayerActiveDemonContractAction(
            int sourceContractCardId)
        {
            if (!CanForwardBattleAction() ||
                !_battleSession.TryBeginPlayerActiveDemonContractAction(
                    sourceContractCardId))
            {
                return false;
            }

            SynchronizeFinishedBattle();
            return true;
        }

        public bool TryAdvanceToNextStage()
        {
            if (Progress.State != StageProgressionState.StageCleared)
            {
                return false;
            }

            int nextStageIndex = Progress.CurrentStageIndex + 1;
            if (nextStageIndex >= Progress.Stages.Count)
            {
                throw new InvalidOperationException("A cleared stage must have a following stage.");
            }

            StageDefinition nextStage = RefreshStageDefinition(
                Progress.Stages[nextStageIndex]);
            OpponentSelectionOffer nextOffer = ShouldOfferOpponentSelection(nextStage)
                ? _opponentSelectionGenerator.Generate(nextStageIndex)
                : null;
            CoreLoopSession nextBattleSession = nextOffer == null
                ? CreateBattleSession(nextStage)
                : null;
            if (!Progress.TryAdvanceToNextStage())
            {
                throw new InvalidOperationException("Run progress rejected a validated stage advance.");
            }

            ApplyPreparedStage(nextStage, nextOffer, nextBattleSession);
            return true;
        }

        public bool TrySelectBattleReward(int optionId)
        {
            return Progress.TrySelectBattleReward(optionId);
        }

        public bool TrySkipBattleReward()
        {
            return Progress.TrySkipBattleReward();
        }

        public bool TrySelectOpponent(int offerId, string profileKey)
        {
            OpponentSelectionOffer offer = PendingOpponentSelection;
            if (Progress.State != StageProgressionState.OpponentSelection ||
                offer == null ||
                offer.OfferId != offerId ||
                offer.StageIndex != Progress.CurrentStageIndex)
            {
                return false;
            }

            OpponentSelectionCandidate selectedCandidate = FindCandidate(
                offer,
                profileKey);
            if (selectedCandidate == null)
            {
                return false;
            }

            StageDefinition template = Progress.CurrentStage;
            StageDefinition selectedStage = StageDefinition.CreateForEnemyProfile(
                template.Id,
                selectedCandidate.Preview.DisplayName,
                template.Kind,
                selectedCandidate.ProfileKey,
                template.PlayerDeckSeed,
                template.EnemyDeckSeed);
            CoreLoopSession selectedBattleSession = CreateBattleSession(selectedStage);

            if (!Progress.TryBeginBattleFromOpponentSelection())
            {
                throw new InvalidOperationException(
                    "Run progress rejected a validated opponent selection.");
            }

            ActiveStage = selectedStage;
            PendingOpponentSelection = null;
            _battleSession = selectedBattleSession;
            _processedBattle = null;
            return true;
        }

        public bool TryRestartRun()
        {
            if (!Progress.TryRestartRun())
            {
                return false;
            }

            if (_opponentSelectionGenerator != null)
            {
                _opponentSelectionGenerator = _opponentSelectionGenerator.CreateFresh();
            }

            PrepareCurrentStage();
            return true;
        }

        private bool CanForwardBattleAction()
        {
            return Progress.State == StageProgressionState.InBattle && _battleSession != null;
        }

        private CoreLoopSession CreateBattleSession(StageDefinition stage)
        {
            return new CoreLoopSession(() => _battleFactory(stage, Progress.Player));
        }

        private void PrepareCurrentStage()
        {
            StageDefinition stage = RefreshStageDefinition(Progress.CurrentStage);
            OpponentSelectionOffer offer = ShouldOfferOpponentSelection(stage)
                ? _opponentSelectionGenerator.Generate(Progress.CurrentStageIndex)
                : null;
            CoreLoopSession battleSession = offer == null
                ? CreateBattleSession(stage)
                : null;
            ApplyPreparedStage(stage, offer, battleSession);
        }

        private void ApplyPreparedStage(
            StageDefinition stage,
            OpponentSelectionOffer offer,
            CoreLoopSession battleSession)
        {
            if (offer != null && !Progress.TryBeginOpponentSelection())
            {
                throw new InvalidOperationException(
                    "Run progress rejected a validated opponent selection.");
            }

            ActiveStage = offer == null ? stage : null;
            PendingOpponentSelection = offer;
            _battleSession = battleSession;
            _processedBattle = null;
        }

        private bool ShouldOfferOpponentSelection(StageDefinition stage)
        {
            return IsOpponentSelectionEnabled &&
                stage.Kind != StageKind.FinalBossCombat;
        }

        /// <summary>
        /// Re-derives a fixed (non-selectable) stage's <see cref="StageDefinition"/>
        /// from the *current* <see cref="EnemyCombatProfileCatalog"/> instead of
        /// trusting whatever the run's stage list captured when the path was first
        /// built. <see cref="TrySelectOpponent"/> already does this at selection time
        /// via <see cref="StageDefinition.CreateForEnemyProfile"/>; a stage with a
        /// fixed enemy (no opponent selection — e.g. the final boss) skips that call
        /// entirely and previously reused the stale, pre-built definition straight
        /// from <see cref="Progress"/>.Stages, so if the live catalog's soul values
        /// changed after the stage path was generated, <see cref="StageBattleFactory"/>
        /// 's own consistency check would throw and leave the run stuck.
        /// </summary>
        private static StageDefinition RefreshStageDefinition(StageDefinition stage)
        {
            if (stage.BattleProfileKey == null)
            {
                return stage;
            }

            return StageDefinition.CreateForEnemyProfile(
                stage.Id,
                stage.DisplayName,
                stage.Kind,
                stage.BattleProfileKey,
                stage.PlayerDeckSeed,
                stage.EnemyDeckSeed);
        }

        private static OpponentSelectionCandidate FindCandidate(
            OpponentSelectionOffer offer,
            string profileKey)
        {
            if (string.IsNullOrEmpty(profileKey))
            {
                return null;
            }

            foreach (OpponentSelectionCandidate candidate in offer.Candidates)
            {
                if (StringComparer.Ordinal.Equals(candidate.ProfileKey, profileKey))
                {
                    return candidate;
                }
            }

            return null;
        }

        private void SynchronizeFinishedBattle()
        {
            CoreLoopBattle battle = Battle;
            if (battle == null ||
                battle.State != CoreLoopState.BattleEnded ||
                ReferenceEquals(battle, _processedBattle))
            {
                return;
            }

            int goldReward = ResolveGoldReward(battle);
            if (Progress.Player.CurrentGold > int.MaxValue - goldReward)
            {
                throw new InvalidOperationException(
                    "The finished battle gold reward would overflow the run gold balance.");
            }

            Progress.Player.SetCurrentSoul(battle.Player.Soul.Current);

            bool resultApplied;
            switch (battle.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    resultApplied = _usesBattleRewards
                        ? TryBeginBattleReward()
                        : Progress.TryCompleteBattleWithoutReward();
                    break;
                case BattleOutcome.PlayerDefeat:
                    resultApplied = Progress.TryDefeatRun();
                    break;
                default:
                    throw new InvalidOperationException("An ended battle must have a final outcome.");
            }

            if (!resultApplied)
            {
                throw new InvalidOperationException("Run progress rejected a finished battle result.");
            }

            if (goldReward > 0)
            {
                Progress.Player.AddGold(goldReward);
            }

            _processedBattle = battle;
            BattleResultSynchronized?.Invoke();
        }

        private int ResolveGoldReward(CoreLoopBattle battle)
        {
            if (battle.Outcome != BattleOutcome.PlayerVictory)
            {
                return 0;
            }

            StageDefinition stage = ActiveStage ?? throw new InvalidOperationException(
                "A finished battle must have an active stage.");
            return stage.BattleProfileKey == null
                ? 0
                : _goldRewardCatalog.GetAmount(stage.BattleProfileKey);
        }

        private bool TryBeginBattleReward()
        {
            StageDefinition stage = ActiveStage ?? throw new InvalidOperationException(
                "A finished battle must have an active stage.");
            BattleRewardTier tier = stage.Kind == StageKind.FinalBossCombat
                ? BattleRewardTier.HighGrade
                : _rewardTierSelector(stage);
            if (tier != BattleRewardTier.Normal && tier != BattleRewardTier.HighGrade)
            {
                throw new InvalidOperationException("Reward tier selector returned an unknown tier.");
            }

            BattleRewardCompletionTarget completionTarget =
                stage.Kind == StageKind.FinalBossCombat
                    ? BattleRewardCompletionTarget.RunVictory
                    : BattleRewardCompletionTarget.StageCleared;
            BattleRewardOffer offer = _rewardGenerator.Generate(tier);
            return Progress.TryBeginBattleReward(offer, completionTarget);
        }

        private static BattleRewardTier SelectDefaultRewardTier(StageDefinition stage)
        {
            if (stage.BattleProfileKey != null)
            {
                return EnemyCombatProfileCatalog.Default
                    .GetPreviewByKey(stage.BattleProfileKey)
                    .ExpectedRewardTier;
            }

            return BattleRewardTier.Normal;
        }
    }
}
