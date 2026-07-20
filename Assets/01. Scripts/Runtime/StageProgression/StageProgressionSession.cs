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
        private OpponentSelectionGenerator _opponentSelectionGenerator;
        private CoreLoopSession _battleSession;
        private CoreLoopBattle _processedBattle;

        public StageProgressionSession(
            RunProgress progress,
            Func<StageDefinition, PlayerRunState, CoreLoopBattle> battleFactory = null,
            BattleRewardGenerator rewardGenerator = null,
            Func<StageDefinition, BattleRewardTier> rewardTierSelector = null,
            OpponentSelectionGenerator opponentSelectionGenerator = null)
        {
            Progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _battleFactory = battleFactory ?? StageBattleFactory.Create;
            _rewardGenerator = rewardGenerator ?? new BattleRewardGenerator(
                BattleRewardCatalog.CreateDefault(),
                DefaultRewardSeed);
            _rewardTierSelector = rewardTierSelector ?? SelectDefaultRewardTier;
            _opponentSelectionGenerator = opponentSelectionGenerator;
            ActiveStage = opponentSelectionGenerator == null
                ? progress.CurrentStage
                : null;
        }

        public StageDefinition ActiveStage { get; private set; }

        public CoreLoopBattle Battle => _battleSession?.Battle;

        public bool IsOpponentSelectionEnabled => _opponentSelectionGenerator != null;

        public OpponentSelectionOffer PendingOpponentSelection { get; private set; }

        public RunProgress Progress { get; }

        public bool TryStartRun()
        {
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

            StageDefinition nextStage = Progress.Stages[nextStageIndex];
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
            StageDefinition stage = Progress.CurrentStage;
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

            Progress.Player.SetCurrentSoul(battle.Player.Soul.Current);

            bool resultApplied;
            switch (battle.Outcome)
            {
                case BattleOutcome.PlayerVictory:
                    resultApplied = TryBeginBattleReward();
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

            _processedBattle = battle;
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
